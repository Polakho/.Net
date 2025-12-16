using Godot;
using System;

public partial class GoBoard : Node2D
{
	[Export] public NodePath StonesContainerPath;
	[Export] public Texture2D BlackStoneTexture;
	[Export] public Texture2D WhiteStoneTexture;
	[Export] public NodePath InfoLabelPath;

	[Export] public Vector2 BoardOrigin = new Vector2(0, 0);
	[Export] public float CellSize = 32f;
	[Export] public int BoardSize = 13;

	private Node2D _stones;
	private Label _label;

	private int[,] _grid; // 0 vide, 1 noir, 2 blanc
	private int _currentPlayer = 1;

	public override void _Ready()
	{
		_stones = GetNode<Node2D>(StonesContainerPath);
		_label = GetNode<Label>(InfoLabelPath);

		_grid = new int[BoardSize, BoardSize];
		UpdateUi();
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
		var sprite = new Sprite2D
		{
			Texture = player == 1 ? BlackStoneTexture : WhiteStoneTexture,
			Position = IntersectionToWorld(x, y),
			ZIndex = 10
		};

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
}
