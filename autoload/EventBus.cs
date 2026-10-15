using Godot;

/// <summary>
/// Bus d'événements global (Autoload) pour découpler la communication entre les différents systèmes.
/// </summary>
public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }

	[Signal]
	public delegate void LevelCompletedEventHandler(Resource levelData);

	[Signal]
	public delegate void PlayerDiedEventHandler(int playerIndex);

	[Signal]
	public delegate void ColorChangedEventHandler(int playerIndex, PaintColor newColor);

	public override void _EnterTree()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			QueueFree();
		}
	}
}
