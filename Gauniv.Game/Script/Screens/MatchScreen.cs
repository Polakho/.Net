using Godot;

public partial class MatchScreen : Control
{
	private ScreenManager _screenManager;

	public override void _Ready()
	{
		_screenManager = GetParent<ScreenManager>();
	}

	public void OnBackPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/main_menu_screen.tscn");
	}
}
