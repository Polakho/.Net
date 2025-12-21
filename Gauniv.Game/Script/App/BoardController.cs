using Godot;
using System;
using System.Collections.Generic;

public partial class BoardController : Node2D
{

	[Export] public NodePath StonesContainerPath;
	[Export] public NodePath InfoLabelPath;
	[Export] public NodePath GridSpritePath;

	[Export] public Texture2D BlackStoneTexture;
	[Export] public Texture2D WhiteStoneTexture;

	[Export] public int BoardSize = 9;
	[Export] public float BackgroundSizePx = 640f;
	[Export] public float StoneScaleInCell = 1.0f;

	[Export] public Vector2 BoardOrigin = new Vector2(64, 64);
	[Export] public float CellSize = 64f;

	private Node2D _stones;
	private Label _infoLabel;
	private Sprite2D _gridSprite;

	private GameServerClient _netClient;
	private string _currentPlayerId;
	private string _gameState = "Pending"; // "Pending", "InProgress", "Finished"
	private bool _isSpectator = false;

	private int[,] _grid;        // 0 vide, 1 noir, 2 blanc
	private int _currentPlayer;  // 1 noir, 2 blanc

	private Sprite2D _hoverPreview;
	private int _lastHoverX = -1;
	private int _lastHoverY = -1;

	public readonly struct StoneState
	{
		public readonly int X;
		public readonly int Y;
		public readonly int Player; // 1 noir, 2 blanc

		public StoneState(int x, int y, int player)
		{
			X = x;
			Y = y;
			Player = player;
		}
	}

	public sealed class GameState
	{
		public int BoardSize;
		public int CurrentPlayer; // 1 noir, 2 blanc
		public List<StoneState> Stones = new();
		public string GameStateStatus = "Pending"; // "Pending", "InProgress", "Finished"
	}

	public void SetNetworkClient(GameServerClient netClient)
	{
		_netClient = netClient;
	}

	public void SetSpectatorMode(bool isSpectator)
	{
		_isSpectator = isSpectator;
		GD.Print($"[BoardController] Mode spectateur défini à: {_isSpectator}");

		if (_isSpectator)
		{
			SetProcessInput(false);
		}
	}

	public override void _Ready()
	{
		GD.Print("[BoardController] _Ready appelé");
		ResolveReferences();

		if (_stones == null || _infoLabel == null || _gridSprite == null || _gridSprite.Texture == null)
		{
			GD.PushError("BoardController: références manquantes. Vérifie Stones, InfoLabel, GridSprite (+ texture).");
			GD.Print($"[BoardController] _stones null? {_stones == null}");
			GD.Print($"[BoardController] _infoLabel null? {_infoLabel == null}");
			GD.Print($"[BoardController] _gridSprite null? {_gridSprite == null}");
			GD.Print($"[BoardController] _gridSprite.Texture null? {_gridSprite?.Texture == null}");
			SetProcessInput(false);
			return;
		}

		RecomputeGridFromSprite();
		GD.Print($"[BoardController] Après RecomputeGridFromSprite: BoardOrigin={BoardOrigin}, CellSize={CellSize}");

		_grid = new int[BoardSize, BoardSize];
		_currentPlayer = 1;

		ApplyGameState(BuildLocalGameState());

		CreateHoverPreview();

		CreateDebugMarker(0, 0, new Color(1, 0, 0, 0.8f)); // Rouge pour (0,0)
		CreateDebugMarker(8, 8, new Color(0, 1, 0, 0.8f)); // Vert pour (8,8)
		CreateDebugMarker(4, 4, new Color(0, 0, 1, 0.8f)); // Bleu pour (4,4)
		
		GD.Print($"[BoardController] _Ready terminé, ProcessInput devrait être activé selon l'état de la partie");
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMSizeChanged)
			RecomputeGridFromSprite();
	}

	public override void _ExitTree()
	{

		if (_hoverPreview != null && IsInstanceValid(_hoverPreview) && !_hoverPreview.IsQueuedForDeletion())
		{
			_hoverPreview.QueueFree();
			_hoverPreview = null;
		}
	}

	public override void _Process(double delta)
	{
		if (_hoverPreview == null || !IsInstanceValid(_hoverPreview) || _hoverPreview.IsQueuedForDeletion())
			return;
		
		if (_isSpectator)
		{
			_hoverPreview.Visible = false;
			return;
		}
			
		if (_gameState != "InProgress")
		{
			_hoverPreview.Visible = false;
			return;
		}

		Vector2 mouseGlobal = GetGlobalMousePosition();
		Vector2 mouseLocal = GetParent<Node2D>().ToLocal(mouseGlobal);
		
		if (TryLocalToIntersection(mouseLocal, out int ix, out int iy))
		{
			bool canPlace = _grid[ix, iy] == 0;
			bool isPlayerTurn = _netClient?.IsLocalPlayersTurn ?? false;
			
			if (canPlace && isPlayerTurn)
			{
				_hoverPreview.Visible = true;
				_hoverPreview.Position = IntersectionToLocal(ix, iy);
				
				UpdateHoverPreviewColor();
				
				_lastHoverX = ix;
				_lastHoverY = iy;
			}
			else
			{
				_hoverPreview.Visible = false;
			}
		}
		else
		{
			_hoverPreview.Visible = false;
		}
	}

	public override void _Input(InputEvent @event)
	{

		if (_isSpectator)
			return;
			
		if (@event is not InputEventMouseButton mb)
			return;
		
		if (!mb.Pressed || mb.ButtonIndex != MouseButton.Left)
			return;

		Vector2 mouseGlobal = GetGlobalMousePosition();
		Vector2 mouseLocal = GetParent<Node2D>().ToLocal(mouseGlobal);
		
		if (TryLocalToIntersection(mouseLocal, out int ix, out int iy))
		{
			GD.Print($"[BoardController] Clic sur intersection ({ix}, {iy})");

			if (_netClient != null && !_netClient.IsLocalPlayersTurn)
			{
				GD.Print($"[BoardController] ⚠ Ce n'est pas le tour du joueur!");
				if (_infoLabel != null)
				{
					_infoLabel.Text = "⚠ Ce n'est pas votre tour!";

					var timer = GetTree().CreateTimer(2.0);
					timer.Timeout += UpdateUi;
				}
				return;
			}

			ShowClickFeedback(ix, iy);

			SendMoveRequest(ix, iy);

			GetViewport().SetInputAsHandled();
		}
	}

	private void ShowClickFeedback(int x, int y)
	{
		if (_stones == null || _stones.IsQueuedForDeletion())
			return;

		var feedback = new ColorRect
		{
			Color = new Color(1, 1, 0, 0.5f), // Jaune semi-transparent
			Size = new Vector2(CellSize * 0.3f, CellSize * 0.3f)
		};

		Vector2 localPos = IntersectionToLocal(x, y);
		feedback.Position = localPos - feedback.Size / 2;

		_stones.AddChild(feedback);

		var timer = GetTree().CreateTimer(0.5);
		timer.Timeout += () => 
		{
			if (feedback != null && IsInstanceValid(feedback) && !feedback.IsQueuedForDeletion())
				feedback.QueueFree();
		};
	}

	public void SendMoveRequest(int x, int y)
	{

		if (_netClient == null)
		{
			GD.PrintErr("[BoardController] Client réseau non défini!");
			return;
		}

		if (_gameState != "InProgress")
		{
			GD.PrintErr("[BoardController] La partie n'est pas en cours!");
			return;
		}

		if (!_netClient.IsLocalPlayersTurn)
		{
			GD.PrintErr("[BoardController] Ce n'est pas votre tour!");
			return;
		}

		if (x < 0 || y < 0 || x >= BoardSize || y >= BoardSize)
		{
			GD.PrintErr("[BoardController] Coordonnées hors limites!");
			return;
		}

		if (_grid[x, y] != 0)
		{
			GD.PrintErr("[BoardController] Case occupée!");
			return;
		}

		GD.Print($"[BoardController] Envoi du coup au serveur: ({x}, {y})");
		_ = _netClient.SendMakeMove(_netClient.CurrentGameId, x, y, false);

	}

	public void ApplyGameState(GameState state)
	{

		if (IsQueuedForDeletion())
		{
			GD.PrintErr("ApplyGameState: BoardController node is no longer valid");
			return;
		}

		if (state == null) return;

		if (state.BoardSize != BoardSize)
		{
			BoardSize = state.BoardSize;
			_grid = new int[BoardSize, BoardSize];
			RecomputeGridFromSprite();
		}

		_gameState = state.GameStateStatus;

		Array.Clear(_grid, 0, _grid.Length);
		foreach (var s in state.Stones)
		{
			if (s.X < 0 || s.Y < 0 || s.X >= BoardSize || s.Y >= BoardSize)
				continue;

			_grid[s.X, s.Y] = s.Player;
		}

		_currentPlayer = state.CurrentPlayer;

		bool shouldProcessInput = _gameState == "InProgress";
		GD.Print($"[BoardController] ApplyGameState: _gameState={_gameState}, SetProcessInput({shouldProcessInput})");
		SetProcessInput(shouldProcessInput);

		ClearBoardVisual();
		foreach (var s in state.Stones)
		{
			SpawnStoneVisual(s.X, s.Y, s.Player);
		}

		UpdateUi();
	}

	private GameState BuildLocalGameState()
	{
		var state = new GameState
		{
			BoardSize = BoardSize,
			CurrentPlayer = _currentPlayer,
			GameStateStatus = _gameState
		};

		for (int x = 0; x < BoardSize; x++)
		for (int y = 0; y < BoardSize; y++)
		{
			int p = _grid[x, y];
			if (p != 0)
				state.Stones.Add(new StoneState(x, y, p));
		}

		return state;
	}

	private void UpdateUi()
	{
		if (_infoLabel == null) return;
		_infoLabel.Text = _currentPlayer == 1 ? "Tour: Noir" : "Tour: Blanc";
	}

	private void ClearBoardVisual()
	{
		if (_stones == null || _stones.IsQueuedForDeletion())
			return;

		try
		{
			foreach (var child in _stones.GetChildren())
			{

				if (child == _hoverPreview)
					continue;
					
				if (child is Node n && !n.IsQueuedForDeletion())
					n.QueueFree();
			}
		}
		catch (ObjectDisposedException)
		{
			GD.PrintErr("ClearBoardVisual: Node has been disposed");
		}
	}

	private void SpawnStoneVisual(int x, int y, int player)
	{
		if (_stones == null || _stones.IsQueuedForDeletion())
			return;

		Texture2D tex = (player == 1) ? BlackStoneTexture : WhiteStoneTexture;
		if (tex == null) return;

		var sprite = new Sprite2D
		{
			Texture = tex,
			ZIndex = 10
		};

		Vector2 localPos = IntersectionToLocal(x, y);
		sprite.Position = localPos;
		
		GD.Print($"[SpawnStoneVisual] intersection=({x},{y}), localPos={localPos}");
		GD.Print($"[SpawnStoneVisual] _stones.Position={_stones.Position}, _stones.GlobalPos={_stones.GlobalPosition}");

		float targetPx = CellSize * Mathf.Clamp(StoneScaleInCell, 0.1f, 1.2f);
		float basePx = Mathf.Max(tex.GetWidth(), tex.GetHeight());
		float s = (basePx <= 0.0f) ? 1.0f : (targetPx / basePx);

		sprite.Scale = new Vector2(s, s);

		_stones.AddChild(sprite);
	}

	private Vector2 IntersectionToWorld(int x, int y)
		=> BoardOrigin + new Vector2(x * CellSize, y * CellSize);

	private Vector2 IntersectionToLocal(int x, int y)
	{

		Vector2 localOrigin = _gridSprite.Position + new Vector2(64, 64);
		return localOrigin + new Vector2(x * CellSize, y * CellSize);
	}

	private bool TryWorldToIntersection(Vector2 worldPos, out int x, out int y)
	{
		Vector2 local = worldPos - BoardOrigin;

		x = (int)Mathf.Round(local.X / CellSize);
		y = (int)Mathf.Round(local.Y / CellSize);

		Vector2 snapped = new Vector2(x * CellSize, y * CellSize);
		float dist = (local - snapped).Length();
		float tolerance = CellSize * 0.35f;

		bool isValid = (x >= 0 && y >= 0 && x < BoardSize && y < BoardSize);
		bool isCloseEnough = dist <= tolerance;
		
		if (isValid && isCloseEnough)
		{
			GD.Print($"[TryWorldToIntersection] worldPos={worldPos}, BoardOrigin={BoardOrigin}");
			GD.Print($"[TryWorldToIntersection] local={local}, calculated=({x},{y})");
			GD.Print($"[TryWorldToIntersection] CellSize={CellSize}, dist={dist}, tolerance={tolerance}");
		}

		if (dist > tolerance)
			return false;
		
		if (!isValid)
			return false;

		return true;
	}

	private bool TryLocalToIntersection(Vector2 localPos, out int x, out int y)
	{

		Vector2 localOrigin = _gridSprite.Position + new Vector2(64, 64);
		Vector2 relativePos = localPos - localOrigin;

		x = (int)Mathf.Round(relativePos.X / CellSize);
		y = (int)Mathf.Round(relativePos.Y / CellSize);

		Vector2 snapped = new Vector2(x * CellSize, y * CellSize);
		float dist = (relativePos - snapped).Length();
		float tolerance = CellSize * 0.35f;

		bool isValid = (x >= 0 && y >= 0 && x < BoardSize && y < BoardSize);
		bool isCloseEnough = dist <= tolerance;
		
		if (isValid && isCloseEnough)
		{
			GD.Print($"[TryLocalToIntersection] localPos={localPos}, localOrigin={localOrigin}");
			GD.Print($"[TryLocalToIntersection] relativePos={relativePos}, intersection=({x},{y})");
		}

		return isCloseEnough && isValid;
	}

	private void RecomputeGridFromSprite()
	{
		if (_gridSprite == null || _gridSprite.Texture == null)
		{
			GD.Print("[BoardController] RecomputeGridFromSprite: _gridSprite ou Texture null");
			return;
		}

		Vector2 spritePos = _gridSprite.GlobalPosition;

		Vector2 gridMargin = new Vector2(64, 64);

		BoardOrigin = spritePos + gridMargin;

		CellSize = 64f;
		
		GD.Print($"[RecomputeGridFromSprite]");
		GD.Print($"  spritePos (GlobalPosition du nœud)={spritePos}");
		GD.Print($"  gridMargin={gridMargin}");
		GD.Print($"  BoardOrigin = spritePos + gridMargin = {BoardOrigin}");
		GD.Print($"  CellSize={CellSize}");
		GD.Print($"  => Premier point (0,0) à {IntersectionToWorld(0, 0)}");
		GD.Print($"  => Dernier point (8,8) à {IntersectionToWorld(8, 8)}");
	}

	private void ResolveReferences()
	{
		_stones = GetNodeOrNull<Node2D>(StonesContainerPath);
		_infoLabel = GetNodeOrNull<Label>(InfoLabelPath);
		_gridSprite = GetNodeOrNull<Sprite2D>(GridSpritePath);

		_stones ??= GetNodeOrNull<Node2D>("../Stones");
		_gridSprite ??= GetNodeOrNull<Sprite2D>("../GridSprite");
		_infoLabel ??= GetNodeOrNull<Label>("../../Ui/InfoLabel");

		if (_stones == null) GD.PushError("BoardController: Stones introuvable (../Stones).");
		if (_gridSprite == null) GD.PushError("BoardController: GridSprite introuvable (../GridSprite).");
		if (_infoLabel == null) GD.PushError("BoardController: InfoLabel introuvable (../../Ui/InfoLabel).");
	}

	private void CreateHoverPreview()
	{

		if (_stones == null || _stones.IsQueuedForDeletion())
		{
			GD.Print("[BoardController.CreateHoverPreview] _stones n'est pas disponible");
			return;
		}

		if (_hoverPreview != null && IsInstanceValid(_hoverPreview) && !_hoverPreview.IsQueuedForDeletion())
		{
			GD.Print("[BoardController.CreateHoverPreview] Le cercle de prévisualisation existe déjà");
			return;
		}

		_hoverPreview = new Sprite2D
		{
			ZIndex = 5, // En dessous des pierres (qui sont à 10)
			Visible = false
		};

		var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
		
		Vector2 center = new Vector2(32, 32);
		float radius = 28f;
		
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				float dist = new Vector2(x, y).DistanceTo(center);
				if (dist <= radius)
				{

					float alpha = 0.4f;
					if (dist > radius - 2f)
					{

						alpha *= (radius - dist) / 2f;
					}
					image.SetPixel(x, y, new Color(1, 1, 1, alpha));
				}
			}
		}

		_hoverPreview.Texture = ImageTexture.CreateFromImage(image);

		float targetPx = CellSize * Mathf.Clamp(StoneScaleInCell, 0.1f, 1.2f);
		float scale = targetPx / 64f;
		_hoverPreview.Scale = new Vector2(scale, scale);

		_stones.AddChild(_hoverPreview);
		
		GD.Print("[BoardController.CreateHoverPreview] Cercle de prévisualisation créé avec succès");
	}

	private void UpdateHoverPreviewColor()
	{
		if (_hoverPreview == null || _hoverPreview.IsQueuedForDeletion())
			return;

		Color previewColor;
		
		if (_netClient != null)
		{

			previewColor = _netClient.LocalPlayerColor switch
			{
				StoneColor.Black => new Color(0.2f, 0.2f, 0.2f, 0.5f), // Noir semi-transparent
				StoneColor.White => new Color(0.9f, 0.9f, 0.9f, 0.5f), // Blanc semi-transparent
				_ => new Color(0.5f, 0.5f, 1.0f, 0.4f) // Bleu par défaut
			};
		}
		else
		{

			previewColor = _currentPlayer == 1 
				? new Color(0.2f, 0.2f, 0.2f, 0.5f) 
				: new Color(0.9f, 0.9f, 0.9f, 0.5f);
		}

		_hoverPreview.Modulate = previewColor;
	}

	private void CreateDebugMarker(int x, int y, Color color)
	{
		if (_stones == null || _stones.IsQueuedForDeletion())
			return;

		var marker = new ColorRect
		{
			Color = color,
			Size = new Vector2(10, 10),
			ZIndex = 20 // Au-dessus de tout
		};

		Vector2 localPos = IntersectionToLocal(x, y);
		marker.Position = localPos - new Vector2(5, 5); // Centrer le marqueur

		_stones.AddChild(marker);
	}
}
