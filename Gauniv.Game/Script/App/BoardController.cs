using Godot;
using System;
using System.Collections.Generic;

public partial class BoardController : Node2D
{
	// --- Références scène (renseigne via Inspecteur si possible) ---
	[Export] public NodePath StonesContainerPath;
	[Export] public NodePath InfoLabelPath;
	[Export] public NodePath GridSpritePath;

	// --- Textures (prototype) ---
	[Export] public Texture2D BlackStoneTexture;
	[Export] public Texture2D WhiteStoneTexture;

	// --- Paramètres plateau ---
	[Export] public int BoardSize = 9;
	[Export] public float BackgroundSizePx = 640f;
	[Export] public float StoneScaleInCell = 1.0f;

	// --- Calibrage (debug) ---
	[Export] public Vector2 BoardOrigin = new Vector2(64, 64);
	[Export] public float CellSize = 64f;

	private Node2D _stones;
	private Label _infoLabel;
	private Sprite2D _gridSprite;

	// Référence au client réseau
	private GameServerClient _netClient;
	private string _currentPlayerId;
	private string _gameState = "Pending"; // "Pending", "InProgress", "Finished"

	// Etat local (prototype). Plus tard : l'état viendra uniquement du serveur.
	private int[,] _grid;        // 0 vide, 1 noir, 2 blanc
	private int _currentPlayer;  // 1 noir, 2 blanc

	// Cercle de prévisualisation pour le survol
	private Sprite2D _hoverPreview;
	private int _lastHoverX = -1;
	private int _lastHoverY = -1;

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

		// Init local
		_grid = new int[BoardSize, BoardSize];
		_currentPlayer = 1;

		// Applique un état initial "comme si serveur"
		ApplyGameState(BuildLocalGameState());
		
		// Créer le cercle de prévisualisation
		CreateHoverPreview();
		
		// DEBUG : Ajouter un marqueur visuel au point (0,0) pour vérifier l'alignement
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
		// Nettoyer le cercle de prévisualisation
		if (_hoverPreview != null && IsInstanceValid(_hoverPreview) && !_hoverPreview.IsQueuedForDeletion())
		{
			_hoverPreview.QueueFree();
			_hoverPreview = null;
		}
	}

	public override void _Process(double delta)
	{
		// Mise à jour du cercle de prévisualisation basé sur la position de la souris
		// Vérifications robustes pour éviter les ObjectDisposedException
		if (_hoverPreview == null || !IsInstanceValid(_hoverPreview) || _hoverPreview.IsQueuedForDeletion())
			return;
			
		if (_gameState != "InProgress")
		{
			_hoverPreview.Visible = false;
			return;
		}

		// Convertir la position globale de la souris en position locale par rapport à BoardRoot
		Vector2 mouseGlobal = GetGlobalMousePosition();
		Vector2 mouseLocal = GetParent<Node2D>().ToLocal(mouseGlobal);
		
		if (TryLocalToIntersection(mouseLocal, out int ix, out int iy))
		{
			// Vérifier si l'intersection est vide et si c'est le tour du joueur
			bool canPlace = _grid[ix, iy] == 0;
			bool isPlayerTurn = _netClient?.IsLocalPlayersTurn ?? false;
			
			if (canPlace && isPlayerTurn)
			{
				// Afficher le cercle de prévisualisation
				_hoverPreview.Visible = true;
				// Utiliser la position locale au lieu de GlobalPosition
				_hoverPreview.Position = IntersectionToLocal(ix, iy);
				
				// Changer la couleur selon le joueur actuel
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
		if (@event is not InputEventMouseButton mb)
			return;
		
		if (!mb.Pressed || mb.ButtonIndex != MouseButton.Left)
			return;

		// Convertir la position globale de la souris en position locale par rapport à BoardRoot
		Vector2 mouseGlobal = GetGlobalMousePosition();
		Vector2 mouseLocal = GetParent<Node2D>().ToLocal(mouseGlobal);
		
		if (TryLocalToIntersection(mouseLocal, out int ix, out int iy))
		{
			GD.Print($"[BoardController] Clic sur intersection ({ix}, {iy})");
			
			// Vérification rapide avant d'envoyer
			if (_netClient != null && !_netClient.IsLocalPlayersTurn)
			{
				GD.Print($"[BoardController] ⚠ Ce n'est pas le tour du joueur!");
				if (_infoLabel != null)
				{
					_infoLabel.Text = "⚠ Ce n'est pas votre tour!";
					// Rétablir le texte normal après 2 secondes
					var timer = GetTree().CreateTimer(2.0);
					timer.Timeout += UpdateUi;
				}
				return;
			}

			// Feedback visuel temporaire (optionnel)
			ShowClickFeedback(ix, iy);

			// Intention de coup : envoi au serveur
			SendMoveRequest(ix, iy);
			
			// Consommer l'événement pour éviter qu'il ne se propage
			GetViewport().SetInputAsHandled();
		}
	}

	/// <summary>
	/// Affiche un feedback visuel temporaire à l'intersection cliquée
	/// </summary>
	private void ShowClickFeedback(int x, int y)
	{
		if (_stones == null || _stones.IsQueuedForDeletion())
			return;

		// Créer un cercle semi-transparent pour montrer où le joueur a cliqué
		var feedback = new ColorRect
		{
			Color = new Color(1, 1, 0, 0.5f), // Jaune semi-transparent
			Size = new Vector2(CellSize * 0.3f, CellSize * 0.3f)
		};

		Vector2 localPos = IntersectionToLocal(x, y);
		feedback.Position = localPos - feedback.Size / 2;

		_stones.AddChild(feedback);

		// Supprimer le feedback après 0.5 seconde
		var timer = GetTree().CreateTimer(0.5);
		timer.Timeout += () => 
		{
			if (feedback != null && IsInstanceValid(feedback) && !feedback.IsQueuedForDeletion())
				feedback.QueueFree();
		};
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
		// 1) Vérifier que le client réseau est disponible
		if (_netClient == null)
		{
			GD.PrintErr("[BoardController] Client réseau non défini!");
			return;
		}

		// 2) Vérifier que la partie est en cours
		if (_gameState != "InProgress")
		{
			GD.PrintErr("[BoardController] La partie n'est pas en cours!");
			return;
		}

		// 3) Vérifier que c'est le tour du joueur
		if (!_netClient.IsLocalPlayersTurn)
		{
			GD.PrintErr("[BoardController] Ce n'est pas votre tour!");
			return;
		}

		// 4) Vérifications locales minimales
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

		// 5) Envoyer le coup au serveur
		GD.Print($"[BoardController] Envoi du coup au serveur: ({x}, {y})");
		_ = _netClient.SendMakeMove(_netClient.CurrentGameId, x, y, false);
		
		// Note: Le serveur broadcast automatiquement le nouvel état à tous les joueurs après validation
		// Pas besoin d'appeler GetGameState ici
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

		// Activer les inputs uniquement si la partie est en cours
		// (La vérification du tour se fera dans _Input)
		bool shouldProcessInput = _gameState == "InProgress";
		GD.Print($"[BoardController] ApplyGameState: _gameState={_gameState}, SetProcessInput({shouldProcessInput})");
		SetProcessInput(shouldProcessInput);

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
				// Ne pas supprimer le cercle de prévisualisation
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

		// Utiliser la position locale par rapport à BoardRoot pour cohérence avec le cercle de prévisualisation
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
		// Calcul en coordonnées locales par rapport à BoardRoot
		// Le GridSprite est à position locale dans BoardRoot
		// Avec centered=false, la texture est dessinée à partir de Position (sans offset)
		// L'image board.png a une marge de 64px autour de la grille
		// Donc le premier point d'intersection (0,0) est à Position du GridSprite + marge de 64px
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

		// Logs détaillés pour diagnostiquer le décalage
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
		// Calcul en coordonnées locales par rapport à BoardRoot
		// Le premier point d'intersection est à Position du GridSprite + Marge (64px)
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

	// ============================
	// Calibrage grille depuis GridSprite
	// ============================

	private void RecomputeGridFromSprite()
	{
		if (_gridSprite == null || _gridSprite.Texture == null)
		{
			GD.Print("[BoardController] RecomputeGridFromSprite: _gridSprite ou Texture null");
			return;
		}

		// Configuration connue de la scène:
		// - GridSprite n'est pas centré (centered=false)
		// - L'image board.png fait 640x640px avec une marge de 64px autour de la grille
		// - La zone de jeu utile (la grille) est de 512x512px
		// - Pour un plateau 9x9, il y a 8 intervalles donc CellSize = 512/8 = 64px
		// - Avec centered=false, la texture est dessinée à partir de GlobalPosition
		// - Le premier point d'intersection (0,0) est à GlobalPosition + marge (64px)
		
		// Position globale du coin haut-gauche du nœud GridSprite
		Vector2 spritePos = _gridSprite.GlobalPosition;
		
		// La texture board.png a une marge de 64px autour de la grille
		Vector2 gridMargin = new Vector2(64, 64);
		
		// Le premier point d'intersection est à: position du nœud + marge de l'image
		BoardOrigin = spritePos + gridMargin;
		
		// CellSize fixe pour un plateau 9x9 (512px / 8 intervalles)
		CellSize = 64f;
		
		GD.Print($"[RecomputeGridFromSprite]");
		GD.Print($"  spritePos (GlobalPosition du nœud)={spritePos}");
		GD.Print($"  gridMargin={gridMargin}");
		GD.Print($"  BoardOrigin = spritePos + gridMargin = {BoardOrigin}");
		GD.Print($"  CellSize={CellSize}");
		GD.Print($"  => Premier point (0,0) à {IntersectionToWorld(0, 0)}");
		GD.Print($"  => Dernier point (8,8) à {IntersectionToWorld(8, 8)}");
	}

	// ============================
	// Résolution références
	// ============================

	private void ResolveReferences()
	{
		_stones = GetNodeOrNull<Node2D>(StonesContainerPath);
		_infoLabel = GetNodeOrNull<Label>(InfoLabelPath);
		_gridSprite = GetNodeOrNull<Sprite2D>(GridSpritePath);

		// Fallbacks selon ton arbre :
		_stones ??= GetNodeOrNull<Node2D>("../Stones");
		_gridSprite ??= GetNodeOrNull<Sprite2D>("../GridSprite");
		_infoLabel ??= GetNodeOrNull<Label>("../../Ui/InfoLabel");

		if (_stones == null) GD.PushError("BoardController: Stones introuvable (../Stones).");
		if (_gridSprite == null) GD.PushError("BoardController: GridSprite introuvable (../GridSprite).");
		if (_infoLabel == null) GD.PushError("BoardController: InfoLabel introuvable (../../Ui/InfoLabel).");
	}

	// ============================
	// Prévisualisation au survol
	// ============================

	private void CreateHoverPreview()
	{
		// Ne créer le cercle que si _stones est valide et qu'on n'en a pas déjà un
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

		// Créer un sprite circulaire pour la prévisualisation
		_hoverPreview = new Sprite2D
		{
			ZIndex = 5, // En dessous des pierres (qui sont à 10)
			Visible = false
		};

		// Créer une texture circulaire procédurale
		var image = Image.Create(64, 64, false, Image.Format.Rgba8);
		
		Vector2 center = new Vector2(32, 32);
		float radius = 28f;
		
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				float dist = new Vector2(x, y).DistanceTo(center);
				if (dist <= radius)
				{
					// Cercle semi-transparent
					float alpha = 0.4f;
					if (dist > radius - 2f)
					{
						// Bord progressif pour un effet plus doux
						alpha *= (radius - dist) / 2f;
					}
					image.SetPixel(x, y, new Color(1, 1, 1, alpha));
				}
			}
		}

		_hoverPreview.Texture = ImageTexture.CreateFromImage(image);
		
		// Calculer l'échelle pour que le cercle corresponde à la taille d'une cellule
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

		// Déterminer la couleur selon le joueur actuel
		Color previewColor;
		
		if (_netClient != null)
		{
			// Utiliser la couleur du joueur local
			previewColor = _netClient.LocalPlayerColor switch
			{
				StoneColor.Black => new Color(0.2f, 0.2f, 0.2f, 0.5f), // Noir semi-transparent
				StoneColor.White => new Color(0.9f, 0.9f, 0.9f, 0.5f), // Blanc semi-transparent
				_ => new Color(0.5f, 0.5f, 1.0f, 0.4f) // Bleu par défaut
			};
		}
		else
		{
			// Fallback : utiliser _currentPlayer
			previewColor = _currentPlayer == 1 
				? new Color(0.2f, 0.2f, 0.2f, 0.5f) 
				: new Color(0.9f, 0.9f, 0.9f, 0.5f);
		}

		_hoverPreview.Modulate = previewColor;
	}

	// ============================
	// Debug
	// ============================

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
		
		GD.Print($"[DEBUG] Marqueur {color} placé à intersection ({x},{y}) = localPos {localPos}");
	}
}
