using Godot;
using System;

public partial class GoBoard : Node2D
{
	[Export] public NodePath StonesContainerPath;
	[Export] public Texture2D BlackStoneTexture;
	[Export] public Texture2D WhiteStoneTexture;
	[Export] public NodePath InfoLabelPath;
	[Export] public NodePath CameraPath;

	// Paramètres demandés
	[Export] public int BoardSize = 9;              // 9x9
	[Export] public float BackgroundSizePx = 640f;  // fond 640x640
	[Export] public float GridSizePx = 512f;        // grille 512x512

	// Calculés automatiquement (mais exportés si tu veux les voir)
	[Export] public Vector2 BoardOrigin = new Vector2(64, 64);
	[Export] public float CellSize = 64f;

	[Export] public float CameraMarginPx = 40f;

	// Option d’affichage
	[Export] public bool DrawGrid = true;
	[Export] public float GridLineWidth = 3f;

	private Node2D _stones;
	private Label _label;
	private Camera2D _camera;

	private int[,] _grid; // 0 vide, 1 noir, 2 blanc
	private int _currentPlayer = 1;

	public override void _Ready()
	{
		_stones = GetNodeOrNull<Node2D>(StonesContainerPath);
		_label = GetNodeOrNull<Label>(InfoLabelPath);
		_camera = GetNodeOrNull<Camera2D>(CameraPath);

		// Calcule BoardOrigin et CellSize pour une grille 512x512 centrée dans un fond 640x640
		// 9x9 => 8 cellules
		CellSize = GridSizePx / (BoardSize - 1);
		float offset = (BackgroundSizePx - GridSizePx) / 2f;
		BoardOrigin = new Vector2(offset, offset);

		_grid = new int[BoardSize, BoardSize];
		UpdateUi();

		AjusterCameraPourAfficherBackground();

		QueueRedraw(); // force le dessin de la grille
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMSizeChanged)
			AjusterCameraPourAfficherBackground();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			Vector2 worldPos = GetGlobalMousePosition();
			if (TryWorldToIntersection(worldPos, out int ix, out int iy))
			{
				TryPlay(ix, iy);
			}
		}
	}

	private void UpdateUi()
	{
		if (_label == null) return;
		_label.Text = _currentPlayer == 1 ? "Tour: Noir" : "Tour: Blanc";
	}

	private bool TryPlay(int x, int y)
	{
		if (_grid[x, y] != 0) return false;

		_grid[x, y] = _currentPlayer;
		SpawnStone(x, y, _currentPlayer);

		_currentPlayer = (_currentPlayer == 1) ? 2 : 1;
		UpdateUi();
		return true;
	}

	private void SpawnStone(int x, int y, int player)
	{
		if (_stones == null) return;

		var tex = player == 1 ? BlackStoneTexture : WhiteStoneTexture;
		if (tex == null) return;

		var sprite = new Sprite2D
		{
			Texture = tex,
			Position = IntersectionToWorld(x, y),
			ZIndex = 10
		};

		// pierre ≈ 90% d’une cellule
		float target = CellSize * 0.9f;
		float baseSize = Mathf.Max(tex.GetWidth(), tex.GetHeight());
		float scale = target / baseSize;
		sprite.Scale = new Vector2(scale, scale);

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

	public override void _Draw()
	{
		if (!DrawGrid) return;

		float max = (BoardSize - 1) * CellSize;

		// Verticales
		for (int x = 0; x < BoardSize; x++)
		{
			float px = BoardOrigin.X + x * CellSize;
			DrawLine(
				new Vector2(px, BoardOrigin.Y),
				new Vector2(px, BoardOrigin.Y + max),
				Colors.Black,
				GridLineWidth
			);
		}

		// Horizontales
		for (int y = 0; y < BoardSize; y++)
		{
			float py = BoardOrigin.Y + y * CellSize;
			DrawLine(
				new Vector2(BoardOrigin.X, py),
				new Vector2(BoardOrigin.X + max, py),
				Colors.Black,
				GridLineWidth
			);
		}
	}

	private void AjusterCameraPourAfficherBackground()
	{
		if (_camera == null) return;

		// On considère que le background est un carré 640x640 qui commence à (0,0)
		Vector2 bgTopLeft = Vector2.Zero;
		Vector2 bgSize = new Vector2(BackgroundSizePx, BackgroundSizePx);
		Vector2 bgCenter = bgTopLeft + bgSize / 2f;

		_camera.GlobalPosition = bgCenter;

		Vector2 viewport = GetViewportRect().Size;
		float availableWidth = Mathf.Max(1f, viewport.X - 2f * CameraMarginPx);
		float availableHeight = Mathf.Max(1f, viewport.Y - 2f * CameraMarginPx);

		// Zoom = écran / monde (Godot 4)
		float zoomX = availableWidth / bgSize.X;
		float zoomY = availableHeight / bgSize.Y;

		float z = Mathf.Min(zoomX, zoomY);
		z = Mathf.Clamp(z, 0.05f, 10f);

		_camera.Zoom = new Vector2(z, z);
	}
}
