using Godot;

/// <summary>
/// Menu principal du jeu.
/// Permet de démarrer une nouvelle partie ou de quitter.
/// </summary>
[GlobalClass]
public partial class MainMenu : Control
{
    [Signal]
    public delegate void StartGameEventHandler();

    [Signal]
    public delegate void QuitGameEventHandler();

    private Button _startButton;
    private Button _quitButton;

    public override void _Ready()
    {
        _startButton = GetNodeOrNull<Button>("StartButton");
        _quitButton = GetNodeOrNull<Button>("QuitButton");

        if (_startButton != null)
        {
            _startButton.Pressed += OnStartButtonPressed;
        }

        if (_quitButton != null)
        {
            _quitButton.Pressed += OnQuitButtonPressed;
        }
    }

    private void OnStartButtonPressed()
    {
    	GetTree().ChangeSceneToFile("res://levels/sandbox.tscn");
        //EmitSignal(SignalName.StartGame);
    }

    private void OnQuitButtonPressed()
    {
        EmitSignal(SignalName.QuitGame);
        // Quitter le jeu directement
        GetTree().Quit();
    }
}