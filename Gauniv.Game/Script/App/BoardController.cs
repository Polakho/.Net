using Godot;
using System;

public partial class BoardController : Node2D
{
	// Références scène (à renseigner idéalement dans l’Inspecteur)
	[Export] public NodePath StonesContainerPath;
	[Export] public NodePath InfoLabelPath;
	[Export] public NodePath CameraPath;
	[Export] public NodePath GridSpritePath;

	// Textures (prototype)
	[Export] public Texture2D BlackStoneTexture;
	[Export] public Texture2D WhiteStoneTexture;

	// Paramètres plateau
	[Export] public int BoardSize = 9;                 // 9x9 pour tester
	[Export] public float BackgroundSizePx = 640f;     // fond (monde) 640x640
	[Export] public float StoneScaleInCell = 0.90f;    // 90% de la cellule

	// Grille calculée depuis GridSprite (exporté pour debug dans l’inspecteur)
	[Export] public Vector2 BoardOrigin = new Vector2(64, 64); // coin haut-gauche en monde
	[Export] public float CellSize = 64f;

	// Caméra
	[Export] public float CameraMarginPx = 40f;

	private Node2D _stones;
	private Label _infoLabel;
	private Camera2D _camera;
	private Sprite2D _gridSprite;

	private int[,] _grid; // 0 vide, 1 noir, 2 blanc
	private int _currentPlayer = 1;

	public override void _Ready()
	{
		ResolveReferences();

		// Si on ne peut pas fonctionner, on stoppe proprement.
		if (_stones == null || _infoLabel == null || _camera == null || _gridSprite == null || _gridSprite.Texture == null)
		{
			GD.PushError("BoardController: références manquantes. Vérifie Stones, InfoLabel, Camera2D, GridSprite (+ texture).");
			SetProcessUnhandledInput(false);
			return;
		}

		RecomputeGridFromSprite();

		_grid = new int[BoardSize, BoardSize];
		UpdateUi();

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
			// Prototype : on “demande” un coup, et on l’applique localement.
			// Plus tard : RequestPlayMove enverra au serveur, puis on appliquera via ApplyMove/GameState.
			RequestPlayMove(ix, iy);
		}
	}

	// ----------------------------
	// Flux "serveur-like" (proto)
	// ----------------------------

	private void RequestPlayMove(int x, int y)
	{
		// Plus tard : envoyer une requête au serveur.
		// Là : on valide minimalement local et on applique.
		if (!IsCellEmpty(x, y))
			return;

		ApplyMove(x, y, _currentPlayer);

		_currentPlayer = (_currentPlayer == 1) ? 2 : 1;
		UpdateUi();
	}

	private void ApplyMove(int x, int y, int player)
	{
		_grid[x, y] = player;
		SpawnStone(x, y, player);
	}

	private bool IsCellEmpty(int x, int y) => _grid[x, y] == 0;

	// ----------------------------
	// UI
	// ----------------------------

	private void UpdateUi()
	{
		if (_infoLabel == null) return;
		_infoLabel.Text = _currentPlayer == 1 ? "Tour: Noir" : "Tour: Blanc";
	}

	// ----------------------------
	// Placement / rendu
	// ----------------------------

	private void SpawnStone(int x, int y, int player)
	{
		if (_stones == null) return;

		Texture2D tex = (player == 1) ? BlackStoneTexture : WhiteStoneTexture;
		if (tex == null)
		{
			GD.PushError("BoardController: texture de pierre manquante (BlackStoneTexture / WhiteStoneTexture).");
			return;
		}

		var sprite = new Sprite2D
		{
			Texture = tex,
			Position = IntersectionToWorld(x, y),
			ZIndex = 10
		};

		// Mise à l’échelle : StoneScaleInCell * CellSize
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

		// On “snap” à la grille : arrondi à l’intersection la plus proche
		x = (int)Mathf.Round(local.X / CellSize);
		y = (int)Mathf.Round(local.Y / CellSize);

		// Tolérance de clic autour de l’intersection
		Vector2 snapped = new Vector2(x * CellSize, y * CellSize);
		float dist = (local - snapped).Length();
		float tolerance = CellSize * 0.35f;

		if (dist > tolerance) return false;
		if (x < 0 || y < 0 || x >= BoardSize || y >= BoardSize) return false;

		return true;
	}

	// ----------------------------
	// Grille : calcul depuis GridSprite
	// ----------------------------

	private void RecomputeGridFromSprite()
	{
		if (_gridSprite == null || _gridSprite.Texture == null)
			return;

		Vector2 texSize = _gridSprite.Texture.GetSize();
		Vector2 scale = _gridSprite.GlobalScale.Abs();
		Vector2 displayedSize = texSize * scale;

		// Top-left de la texture dans l'espace LOCAL du Sprite2D
		// Centered=false => base = (0,0)
		// Centered=true  => base = (-size/2)
		Vector2 topLeftLocal = _gridSprite.Centered ? -(displayedSize / 2f) : Vector2.Zero;

		// Offset se rajoute au dessin de la texture (signe +)
		topLeftLocal += _gridSprite.Offset;

		// Convertir en monde (prend en compte transforms parents)
		BoardOrigin = _gridSprite.ToGlobal(topLeftLocal);

		CellSize = displayedSize.X / Mathf.Max(1, BoardSize - 1);
	}

	// ----------------------------
	// Caméra : fit background
	// ----------------------------

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

	// ----------------------------
	// Résolution des références
	// ----------------------------

	private void ResolveReferences()
	{
		// 1) Via NodePath exporté
		_stones = GetNodeOrNull<Node2D>(StonesContainerPath);
		_infoLabel = GetNodeOrNull<Label>(InfoLabelPath);
		_camera = GetNodeOrNull<Camera2D>(CameraPath);
		_gridSprite = GetNodeOrNull<Sprite2D>(GridSpritePath);

		// 2) Fallbacks “intelligents” selon ton arbre actuel
		// BoardController est sous BoardRoot : on peut chercher en relatif.
		_stones ??= GetNodeOrNull<Node2D>("../Stones");
		_gridSprite ??= GetNodeOrNull<Sprite2D>("../GridSprite");
		_camera ??= GetNodeOrNull<Camera2D>("../Camera2D");

		// InfoLabel est sous MatchScreen/Ui/InfoLabel.
		// Depuis BoardController (MatchScreen/BoardRoot/BoardController), le chemin relatif est:
		// ../../Ui/InfoLabel
		_infoLabel ??= GetNodeOrNull<Label>("../../Ui/InfoLabel");

		// Logs utiles
		if (_stones == null) GD.PushError("BoardController: Stones introuvable (renseigne StonesContainerPath ou vérifie ../Stones).");
		if (_gridSprite == null) GD.PushError("BoardController: GridSprite introuvable (renseigne GridSpritePath ou vérifie ../GridSprite).");
		if (_camera == null) GD.PushError("BoardController: Camera2D introuvable (renseigne CameraPath ou vérifie ../Camera2D).");
		if (_infoLabel == null) GD.PushError("BoardController: InfoLabel introuvable (renseigne InfoLabelPath ou vérifie ../../Ui/InfoLabel).");
	}
}
