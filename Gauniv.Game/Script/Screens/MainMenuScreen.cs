using Godot;

public partial class MainMenuScreen : Control
{
	private ScreenManager _screenManager;

	public override void _Ready()
	{
		// Comme MainMenuScreen est enfant direct de ScreenManager
		_screenManager = GetParent<ScreenManager>();
	}

	public void OnPlayPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	public void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
