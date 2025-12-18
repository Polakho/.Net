using Godot;

public partial class ScreenManager : Control
{
	private Control _current;

	public override void _Ready()
	{
		// Force ScreenManager à remplir le viewport
		SetAnchorsPreset(LayoutPreset.FullRect);
		OffsetLeft = 0;
		OffsetTop = 0;
		OffsetRight = 0;
		OffsetBottom = 0;

		GoTo("res://Scenes/Screens/main_menu_screen.tscn");
	}

	public void GoTo(string scenePath)
	{
		var packed = GD.Load<PackedScene>(scenePath);
		if (packed == null)
		{
			GD.PrintErr($"ScreenManager: impossible de charger: {scenePath}");
			return;
		}

		var screen = packed.Instantiate<Control>();

		_current?.QueueFree();
		_current = screen;
		AddChild(_current);

		// Force l’écran à remplir ScreenManager
		_current.SetAnchorsPreset(LayoutPreset.FullRect);
		_current.OffsetLeft = 0;
		_current.OffsetTop = 0;
		_current.OffsetRight = 0;
		_current.OffsetBottom = 0;

		_current.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_current.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
	}
}
