using Godot;

public partial class MatchScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	[Export] public NodePath BoardControllerPath;

	private BoardController _board;

	public override void _Ready()
	{
		_screenManager = GetParent<ScreenManager>();
		_net = _screenManager.NetClient;

		_board = GetNodeOrNull<BoardController>(BoardControllerPath);
		if (_board == null)
			GD.PrintErr("[MatchScreen] BoardController introuvable, renseigne BoardControllerPath.");

		if (_net != null)
		{
			_net.GameStateReceived += OnGameStateReceived;

			// Si on connaît déjà la game courante, on demande son état
			if (!string.IsNullOrEmpty(_net.CurrentGameId))
			{
				_ = _net.SendGetGameState(_net.CurrentGameId);
			}
		}
	}

	public void OnBackPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/lobby_screen.tscn");
	}

	private void OnGameStateReceived(GetGameStateResponse state)
	{
		if (_board == null) return;

		// Conversion GetGameStateResponse -> BoardController.GameState (ton type interne)
		var localState = new BoardController.GameState
		{
			BoardSize = state.BoardSize,
			CurrentPlayer = state.currentPlayer == "Black" ? 1 : 2
		};

		for (int x = 0; x < state.BoardSize; x++)
		for (int y = 0; y < state.BoardSize; y++)
		{
			var cell = state.Board[x, y];
			if (cell == null) continue;

			int player = cell == StoneColor.Black ? 1 : 2;
			localState.Stones.Add(new BoardController.StoneState(x, y, player));
		}

		_board.ApplyGameState(localState);
	}
}
