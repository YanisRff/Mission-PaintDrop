using Godot;

/// <summary>
/// Menu pause affichée quand le jeu est mis en pause.
/// Affiche le temps écoulé depuis le début du niveau.
/// </summary>
[GlobalClass]
public partial class PauseMenu : Control
{
    [Signal]
    public delegate void ResumeRequestedEventHandler();

    [Signal]
    public delegate void ResetRequestedEventHandler();

    [Signal]
    public delegate void MainMenuRequestedEventHandler();

    private Label _timeLabel;
    private Button _resumeButton;
    private Button _resetButton;
    private Button _mainMenuButton;

    // Temps écoulé depuis le début du niveau (en secondes)
    [Export]
    public float ElapsedTime { get; set; } = 0.0f;

    public override void _Ready()
    {
        _timeLabel = GetNodeOrNull<Label>("TimeLabel");
        _resumeButton = GetNodeOrNull<Button>("ResumeButton");
        _resetButton = GetNodeOrNull<Button>("ResetButton");
        _mainMenuButton = GetNodeOrNull<Button>("MainMenuButton");

        UpdateTimeDisplay();

        if (_resumeButton != null)
        {
            _resumeButton.Pressed += () => EmitSignal(SignalName.ResumeRequested);
        }

        if (_resetButton != null)
        {
            _resetButton.Pressed += () => EmitSignal(SignalName.ResetRequested);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.Pressed += () => EmitSignal(SignalName.MainMenuRequested);
        }
    }

    /// <summary>
    /// Met à jour l'affichage du temps.
    /// </summary>
    public void UpdateTimeDisplay()
    {
        if (_timeLabel != null)
        {
            _timeLabel.Text = FormatTime(ElapsedTime);
        }
    }

    /// <summary>
    /// Formate un temps en secondes en MM:SS.mmm
    /// </summary>
    private static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60);
        int wholeSeconds = (int)(seconds % 60);
        int milliseconds = (int)((seconds * 1000) % 1000);
        return $"{minutes:D2}:{wholeSeconds:D2}.{milliseconds:D3}";
    }
}