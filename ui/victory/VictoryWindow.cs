using Godot;

/// <summary>
/// Fenêtre de victoire affichée à la fin d'un niveau.
/// Affiche le nom du niveau, le par-time, et propose un bouton pour passer au niveau suivant.
/// </summary>
[GlobalClass]
public partial class VictoryWindow : Control
{
	[Export]
	public string LevelName { get; set; } = "Level";

	[Export]
	public float ParTime { get; set; } = 0.0f;

	[Export]
	public float CompletionTime { get; set; } = 0.0f;

	private Label _levelNameLabel;
	private Label _parTimeLabel;
	private Label _completionTimeLabel;
	private Label _resultLabel;

	public override void _Ready()
	{
		// Trouve tous les nœuds enfants
		_levelNameLabel = GetNodeOrNull<Label>("LevelNameLabel");
		_parTimeLabel = GetNodeOrNull<Label>("ParTimeLabel");
		_completionTimeLabel = GetNodeOrNull<Label>("CompletionTimeLabel");
		_resultLabel = GetNodeOrNull<Label>("ResultLabel");
		
		// Met à jour l'interface avec les données
		UpdateUI();
	}

	/// <summary>
	/// Met à jour l'interface avec le nom du niveau et les temps.
	/// </summary>
	private void UpdateUI()
	{
		if (_levelNameLabel != null)
		{
			_levelNameLabel.Text = LevelName;
		}

		if (_parTimeLabel != null)
		{
			_parTimeLabel.Text = FormatTime(ParTime);
		}

		if (_completionTimeLabel != null)
		{
			_completionTimeLabel.Text = FormatTime(CompletionTime);
		}

		// Affiche le résultat (Par Time, Great, Perfect, etc.)
		if (_resultLabel != null && ParTime > 0)
		{
			float ratio = CompletionTime / ParTime;
			_resultLabel.Text = GetResultText(ratio);
			_resultLabel.AddThemeColorOverride("font_color", GetResultColor(ratio));
		}
	}

	/// <summary>
	/// Formate un temps en secondes en format MM:SS.mmm
	/// </summary>
	private static string FormatTime(float seconds)
	{
		int minutes = (int)(seconds / 60);
		int wholeSeconds = (int)(seconds % 60);
		int milliseconds = (int)((seconds * 1000) % 1000);
		
		return $"{minutes:D2}:{wholeSeconds:D2}.{milliseconds:D3}";
	}

	/// <summary>
	/// Retourne le texte du résultat en fonction du ratio temps/par-time.
	/// </summary>
	private static string GetResultText(float ratio)
	{
		if (ratio <= 0.8f) return "PERFECT!";
		if (ratio <= 1.0f) return "GREAT!";
		if (ratio <= 1.2f) return "GOOD";
		if (ratio <= 1.5f) return "OK";
		return "KEEP TRYING";
	}

	/// <summary>
	/// Retourne la couleur du résultat en fonction du ratio.
	/// </summary>
	private static Color GetResultColor(float ratio)
	{
		if (ratio <= 0.8f) return Colors.Gold;
		if (ratio <= 1.0f) return new Color(0.5f, 1.0f, 0.5f); // Light green
		if (ratio <= 1.2f) return new Color(0.7f, 0.7f, 1.0f); // Light blue
		if (ratio <= 1.5f) return Colors.Orange;
		return new Color(1.0f, 0.5f, 0.5f); // Light red
	}

	/// <summary>
	/// Définit le temps de complétion (appelé depuis LevelManager).
	/// </summary>
	public void SetCompletionTime(float time)
	{
		CompletionTime = time;
		if (_completionTimeLabel != null)
		{
			_completionTimeLabel.Text = FormatTime(time);
		}
		
		// Met à jour le résultat
		if (_resultLabel != null && ParTime > 0)
		{
			float ratio = time / ParTime;
			_resultLabel.Text = GetResultText(ratio);
			_resultLabel.AddThemeColorOverride("font_color", GetResultColor(ratio));
		}
	}
}
