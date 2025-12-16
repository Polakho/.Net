using Godot;
using System;

public partial class GoBoard : Node2D
{
	[Export] public NodePath StonesContainerPath;
	[Export] public Texture2D BlackStoneTexture;
	[Export] public Texture2D WhiteStoneTexture;
	[Export] public NodePath InfoLabelPath;
	[Export] public NodePath BackgroundPath;

	// Chemin vers la Camera2D
	[Export] public NodePath CameraPath;

	// Calibration de la grille
	[Export] public Vector2 BoardOrigin = new Vector2(64, 64);
	[Export] public float CellSize = 32f;
	[Export] public int BoardSize = 19;

	// Marge visuelle autour du board (en pixels écran)
	[Export] public float CameraMarginPx = 40f;

	private Node2D _stones;
	private Label _label;
	private Camera2D _camera;
	private Sprite2D _background;

	private int[,] _grid; // 0 vide, 1 noir, 2 blanc
	private int _currentPlayer = 1;

	public override void _Ready()
	{
		_stones = GetNode<Node2D>(StonesContainerPath);
		_label = GetNode<Label>(InfoLabelPath);
		_camera = GetNodeOrNull<Camera2D>(CameraPath);
		_background = GetNode<Sprite2D>(BackgroundPath);

		_grid = new int[BoardSize, BoardSize];
		UpdateUi();

		AjusterCameraPourAfficherBoard();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMSizeChanged)
		{
			AjusterCameraPourAfficherBoard();
		}
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
		var tex = player == 1 ? BlackStoneTexture : WhiteStoneTexture;

		var sprite = new Sprite2D
		{
			Texture = tex,
			Position = IntersectionToWorld(x, y),
			ZIndex = 10
		};

		// Redimensionnement: la pierre fait ~90% d'une cellule
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

	/// <summary>
	/// Centre la caméra sur le centre du background et ajuste le zoom pour que tout le background soit visible.
	/// </summary>
	private void AjusterCameraPourAfficherBoard()
	{
		if (_camera == null || _background == null || _background.Texture == null)
			return;

		// Taille de la texture (pixels)
		Vector2 texSize = _background.Texture.GetSize();

		// Taille réellement affichée dans le monde (prend en compte les scales héritées)
		Vector2 worldScale = _background.GlobalScale.Abs();
		Vector2 displayedSize = new Vector2(texSize.X * worldScale.X, texSize.Y * worldScale.Y);

		// Coin haut-gauche monde du sprite selon Centered
		Vector2 topLeft = _background.Centered
			? _background.GlobalPosition - displayedSize / 2f
			: _background.GlobalPosition;

		// Centre du background (en monde)
		Vector2 center = topLeft + displayedSize / 2f;

		// 1) Centrer la caméra
		_camera.GlobalPosition = center;

		// 2) Calcul du zoom pour faire rentrer toute l'image
		Vector2 viewport = GetViewportRect().Size;

		float availableWidth = Mathf.Max(1f, viewport.X - 2f * CameraMarginPx);
		float availableHeight = Mathf.Max(1f, viewport.Y - 2f * CameraMarginPx);

		// IMPORTANT (Godot): Zoom = écran / monde
		float zoomX = availableWidth / displayedSize.X;
		float zoomY = availableHeight / displayedSize.Y;

		// On prend le plus petit pour que tout rentre dans les deux dimensions
		float z = Mathf.Min(zoomX, zoomY);
		z = Mathf.Clamp(z, 0.05f, 10f);

		_camera.Zoom = new Vector2(z, z);
	}
}
