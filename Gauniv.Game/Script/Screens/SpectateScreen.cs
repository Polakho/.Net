using Godot;

public partial class SpectateScreen : Control
{
	private ScreenManager _screenManager;

	public override void _Ready()
	{
		_screenManager = GetParent<ScreenManager>();
	}

	public void OnBackPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/lobby_screen.tscn");
	}
}
