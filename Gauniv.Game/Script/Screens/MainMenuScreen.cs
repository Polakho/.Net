using Godot;

public partial class MainMenuScreen : Control
{
	private ScreenManager _screenManager;

	public override void _Ready()
	{

		_screenManager = GetParent<ScreenManager>();
	}

	public void OnPlayPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/lobby_screen.tscn");
	}

	public void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
