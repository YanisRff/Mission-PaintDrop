using Godot;

/// <summary>
/// Scène de base pour tous les niveaux du jeu.
/// Contient 2 points de spawn, la zone d'arrivée (Goal), la caméra fixe,
/// et transmet l'événement de victoire à l'EventBus lorsque les 2 joueurs atteignent le but.
/// </summary>
[GlobalClass]
public partial class BaseLevel : Node2D
{
	[Export]
	public LevelData LevelData { get; set; }

	[Export]
	public PackedScene PlayerScene { get; set; }

	[Export]
	public Goal GoalZone { get; set; }

	[Export]
	public SpawnPoint SpawnPoint1 { get; set; }

	[Export]
	public SpawnPoint SpawnPoint2 { get; set; }

	public override void _Ready()
	{
		// Recherche automatique des nœuds s'ils ne sont pas assignés dans l'inspecteur
		GoalZone ??= GetNodeOrNull<Goal>("Goal");
		SpawnPoint1 ??= GetNodeOrNull<SpawnPoint>("SpawnPoint1");
		SpawnPoint2 ??= GetNodeOrNull<SpawnPoint>("SpawnPoint2");

		if (GoalZone != null)
		{
			GoalZone.GoalCompleted += OnGoalCompleted;
		}

		SpawnPlayersIfConfigured();
	}

	/// <summary>
	/// Fait apparaître les deux joueurs aux points de spawn si la PlayerScene est configurée.
	/// </summary>
	private void SpawnPlayersIfConfigured()
	{
		if (PlayerScene == null)
		{
			return;
		}

		SpawnPlayer(0, SpawnPoint1);
		SpawnPlayer(1, SpawnPoint2);
	}

	private void SpawnPlayer(int playerIndex, SpawnPoint spawnPoint)
	{
		if (spawnPoint == null)
		{
			return;
		}

		Node playerInstance = PlayerScene.Instantiate();

		PaintColor startPaint = LevelData.StartPaints[playerIndex];

		if (playerInstance is Node2D player2D)
		{
			player2D.GlobalPosition = spawnPoint.GlobalPosition;
			player2D.Set("PlayerIndex", playerIndex);
			player2D.Set("player_index", playerIndex);

			player2D.Set("Paint", startPaint);

			AddChild(player2D);
		}
	}

	/// <summary>
	/// Déclenché lorsque les deux joueurs ont atteint le but.
	/// </summary>
	private void OnGoalCompleted()
	{
		GD.Print($"[BaseLevel] Victoire ! Niveau terminé : {LevelData?.LevelName ?? Name}");
		EventBus.Instance?.EmitSignal(EventBus.SignalName.LevelCompleted, LevelData);
	}

	/// <summary>
	/// Renvoie la position globale du spawn pour un index de joueur donné.
	/// </summary>
	public Vector2 GetSpawnPosition(int playerIndex)
	{
		if (playerIndex == 0 && SpawnPoint1 != null)
		{
			return SpawnPoint1.GlobalPosition;
		}
		if (playerIndex == 1 && SpawnPoint2 != null)
		{
			return SpawnPoint2.GlobalPosition;
		}
		return GlobalPosition;
	}
}
