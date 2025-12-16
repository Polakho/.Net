using Godot;

public partial class DebugGrid : Node2D
{
	[Export] public Vector2 BoardOrigin = new(64, 64);
	[Export] public float CellSize = 32f;
	[Export] public int BoardSize = 13; // votre plateau 13x13

	[Export] public bool DrawLines = true;
	[Export] public bool DrawPoints = true;

	public override void _Draw()
	{
		// Points et lignes en coordonnées monde (Node2D)
		// On ne fixe pas de couleur ici, Godot prendra une couleur par défaut,
		// mais si vous voulez je peux vous donner une version avec couleurs.
		for (int y = 0; y < BoardSize; y++)
		{
			for (int x = 0; x < BoardSize; x++)
			{
				var p = BoardOrigin + new Vector2(x * CellSize, y * CellSize);

				if (DrawPoints)
					DrawCircle(p, 3f, new Color(1, 0, 0, 0.8f)); // rouge semi-transparent

				if (DrawLines)
				{
					// Lignes: dessiner uniquement la première fois (pour éviter doublons)
					// Horizontales
					if (x == 0)
					{
						var p2 = BoardOrigin + new Vector2((BoardSize - 1) * CellSize, y * CellSize);
						DrawLine(p, p2, new Color(1, 0, 0, 0.35f), 2f);
					}

					// Verticales
					if (y == 0)
					{
						var p2 = BoardOrigin + new Vector2(x * CellSize, (BoardSize - 1) * CellSize);
						DrawLine(p, p2, new Color(1, 0, 0, 0.35f), 2f);
					}
				}
			}
		}
	}

	public override void _Ready()
	{
		QueueRedraw();
	}
}
