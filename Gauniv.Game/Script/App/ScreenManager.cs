using Godot;

public partial class ScreenManager : Control
{
	private Control _current;

	public override void _Ready()
	{
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

		if (_current != null)
			_current.QueueFree();

		_current = screen;
		AddChild(_current);
	}
}
