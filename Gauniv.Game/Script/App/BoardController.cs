using Godot;
using System;
using System.Collections.Generic;

public partial class BoardController : Node2D
{
	// --- Références scène (renseigne via Inspecteur si possible) ---
	[Export] public NodePath StonesContainerPath;
	[Export] public NodePath InfoLabelPath;
	[Export] public NodePath CameraPath;
	[Export] public NodePath GridSpritePath;

	// --- Textures (prototype) ---
	[Export] public Texture2D BlackStoneTexture;
	[Export] public Texture2D WhiteStoneTexture;

	// --- Paramètres plateau ---
	[Export] public int BoardSize = 9;
	[Export] public float BackgroundSizePx = 640f;
	[Export] public float StoneScaleInCell = 0.90f;

	// --- Calibrage (debug) ---
	[Export] public Vector2 BoardOrigin = new Vector2(64, 64);
	[Export] public float CellSize = 64f;

	// --- Caméra ---
	[Export] public float CameraMarginPx = 40f;

	private Node2D _stones;
	private Label _infoLabel;
	private Camera2D _camera;
	private Sprite2D _gridSprite;

	// Référence au client réseau
	private GameServerClient _netClient;
	private string _currentPlayerId;
	private string _gameState = "Pending"; // "Pending", "InProgress", "Finished"

	// Etat local (prototype). Plus tard : l'état viendra uniquement du serveur.
	private int[,] _grid;        // 0 vide, 1 noir, 2 blanc
	private int _currentPlayer;  // 1 noir, 2 blanc

	// ============================
	// Types “contrat” (simple)
	// ============================

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

	// ============================
	// Setter pour le NetworkClient
	// ============================

	public void SetNetworkClient(GameServerClient netClient)
	{
		_netClient = netClient;
	}

	// ============================
	// Godot lifecycle
	// ============================

	public override void _Ready()
	{
		ResolveReferences();

		if (_stones == null || _infoLabel == null || _camera == null || _gridSprite == null || _gridSprite.Texture == null)
		{
			GD.PushError("BoardController: références manquantes. Vérifie Stones, InfoLabel, Camera2D, GridSprite (+ texture).");
			SetProcessUnhandledInput(false);
			return;
		}

		RecomputeGridFromSprite();

		// Init local
		_grid = new int[BoardSize, BoardSize];
		_currentPlayer = 1;

		// Applique un état initial “comme si serveur”
		ApplyGameState(BuildLocalGameState());

		FitCameraToBackground();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMSizeChanged)
			FitCameraToBackground();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mb)
			return;

		if (!mb.Pressed || mb.ButtonIndex != MouseButton.Left)
			return;

		Vector2 worldPos = GetGlobalMousePosition();
		if (TryWorldToIntersection(worldPos, out int ix, out int iy))
		{
			// Intention de coup (plus tard : envoi au serveur)
			SendMoveRequest(ix, iy);
		}
	}

	// ============================
	// API “prête serveur”
	// ============================

	/// <summary>
	/// Intention : le joueur veut jouer en (x,y).
	/// Envoie le coup au serveur après validation locale.
	/// </summary>
	public void SendMoveRequest(int x, int y)
	{
		// 1) Vérifications locales minimales
		if (_grid[x, y] != 0)
		{
			GD.PrintErr("[BoardController] Case occupée!");
			return;
		}

		// 2) Vérifier que la partie est en cours
		if (_gameState != "InProgress")
		{
			GD.PrintErr("[BoardController] La partie n'est pas en cours!");
			return;
		}

		// 3) Vérifier que c'est le tour du joueur
		// Le serveur nous envoie le joueur courant (1=Noir, 2=Blanc)
		// On doit identifier qui on est (Noir ou Blanc)
		// Note: pour l'instant, on assume que c'est notre tour si c'est notre couleur
		// A améliorer avec un vrai ID de joueur du serveur
		if (_netClient == null)
		{
			GD.PrintErr("[BoardController] Client réseau non défini!");
			return;
		}

		// 4) Envoyer le coup au serveur
		_ = _netClient.SendMakeMove(_netClient.CurrentGameId, x, y, false);

		GD.Print($"[BoardController] Coup envoyé au serveur: ({x}, {y})");
	}

	/// <summary>
	/// Applique un état complet (comme si reçu du serveur).
	/// C'est CETTE méthode qui deviendra centrale.
	/// </summary>
	public void ApplyGameState(GameState state)
	{
		// Vérification que le nœud est toujours valide
		if (IsQueuedForDeletion())
		{
			GD.PrintErr("ApplyGameState: BoardController node is no longer valid");
			return;
		}

		if (state == null) return;

		// Si BoardSize peut varier, tu peux gérer un resize ici.
		if (state.BoardSize != BoardSize)
		{
			BoardSize = state.BoardSize;
			_grid = new int[BoardSize, BoardSize];
			RecomputeGridFromSprite();
		}

		// Mettre à jour l'état de la partie
		_gameState = state.GameStateStatus;

		// Reconstruire état local à partir du state (source de vérité)
		Array.Clear(_grid, 0, _grid.Length);
		foreach (var s in state.Stones)
		{
			if (s.X < 0 || s.Y < 0 || s.X >= BoardSize || s.Y >= BoardSize)
				continue;

			_grid[s.X, s.Y] = s.Player;
		}

		_currentPlayer = state.CurrentPlayer;

		// Désactiver/activer les inputs selon l'état de la partie
		SetProcessUnhandledInput(_gameState == "InProgress");

		// Reconstruire le visuel (simple/robuste)
		ClearBoardVisual();
		foreach (var s in state.Stones)
		{
			SpawnStoneVisual(s.X, s.Y, s.Player);
		}

		UpdateUi();
	}

	// ============================
	// Construction “local server” (proto)
	// ============================

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

	// ============================
	// UI / Visuel
	// ============================

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

		// IMPORTANT : on place en monde pour éviter les surprises de transforms
		sprite.GlobalPosition = IntersectionToWorld(x, y);

		float targetPx = CellSize * Mathf.Clamp(StoneScaleInCell, 0.1f, 1.2f);
		float basePx = Mathf.Max(tex.GetWidth(), tex.GetHeight());
		float s = (basePx <= 0.0f) ? 1.0f : (targetPx / basePx);

		sprite.Scale = new Vector2(s, s);

		_stones.AddChild(sprite);
	}

	private Vector2 IntersectionToWorld(int x, int y)
		=> BoardOrigin + new Vector2(x * CellSize, y * CellSize);

	private bool TryWorldToIntersection(Vector2 worldPos, out int x, out int y)
	{
		Vector2 local = worldPos - BoardOrigin;

		x = (int)Mathf.Round(local.X / CellSize);
		y = (int)Mathf.Round(local.Y / CellSize);

		Vector2 snapped = new Vector2(x * CellSize, y * CellSize);
		float dist = (local - snapped).Length();
		float tolerance = CellSize * 0.35f;

		if (dist > tolerance) return false;
		if (x < 0 || y < 0 || x >= BoardSize || y >= BoardSize) return false;

		return true;
	}

	// ============================
	// Calibrage grille depuis GridSprite
	// ============================

	private void RecomputeGridFromSprite()
	{
		if (_gridSprite == null || _gridSprite.Texture == null)
			return;

		Vector2 texSize = _gridSprite.Texture.GetSize();
		Vector2 scale = _gridSprite.GlobalScale.Abs();
		Vector2 displayedSize = texSize * scale;

		// Base top-left en local
		Vector2 topLeftLocal = _gridSprite.Centered ? -(displayedSize / 2f) : Vector2.Zero;

		// Offset se rajoute au dessin (signe +) — c’est la version qui marche dans ton setup
		topLeftLocal += _gridSprite.Offset;

		BoardOrigin = _gridSprite.ToGlobal(topLeftLocal);

		CellSize = displayedSize.X / Mathf.Max(1, BoardSize - 1);
	}

	// ============================
	// Caméra
	// ============================

	private void FitCameraToBackground()
	{
		if (_camera == null) return;

		Vector2 bgSize = new Vector2(BackgroundSizePx, BackgroundSizePx);
		Vector2 bgCenter = bgSize / 2f;

		_camera.GlobalPosition = bgCenter;

		Vector2 viewport = GetViewportRect().Size;
		float availableW = Mathf.Max(1f, viewport.X - 2f * CameraMarginPx);
		float availableH = Mathf.Max(1f, viewport.Y - 2f * CameraMarginPx);

		float zoomX = availableW / bgSize.X;
		float zoomY = availableH / bgSize.Y;

		float z = Mathf.Min(zoomX, zoomY);
		z = Mathf.Clamp(z, 0.05f, 10f);

		_camera.Zoom = new Vector2(z, z);
	}

	// ============================
	// Résolution références
	// ============================

	private void ResolveReferences()
	{
		_stones = GetNodeOrNull<Node2D>(StonesContainerPath);
		_infoLabel = GetNodeOrNull<Label>(InfoLabelPath);
		_camera = GetNodeOrNull<Camera2D>(CameraPath);
		_gridSprite = GetNodeOrNull<Sprite2D>(GridSpritePath);

		// Fallbacks selon ton arbre :
		_stones ??= GetNodeOrNull<Node2D>("../Stones");
		_gridSprite ??= GetNodeOrNull<Sprite2D>("../GridSprite");
		_camera ??= GetNodeOrNull<Camera2D>("../Camera2D");
		_infoLabel ??= GetNodeOrNull<Label>("../../Ui/InfoLabel");

		if (_stones == null) GD.PushError("BoardController: Stones introuvable (../Stones).");
		if (_gridSprite == null) GD.PushError("BoardController: GridSprite introuvable (../GridSprite).");
		if (_camera == null) GD.PushError("BoardController: Camera2D introuvable (../Camera2D).");
		if (_infoLabel == null) GD.PushError("BoardController: InfoLabel introuvable (../../Ui/InfoLabel).");
	}
}
