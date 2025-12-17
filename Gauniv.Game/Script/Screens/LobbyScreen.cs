using Godot;

public partial class LobbyScreen : Control
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

	public void OnCreatePressed()
	{
		// Placeholder: plus tard -> create room via serveur, puis aller en match
		_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	public void OnJoinPressed()
	{
		// Placeholder: plus tard -> join selected room as player
		_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	public void OnSpectatePressed()
	{
		// Placeholder: plus tard -> join selected room as spectator
		_screenManager.GoTo("res://Scenes/Screens/spectate_screen.tscn");
	}
}
