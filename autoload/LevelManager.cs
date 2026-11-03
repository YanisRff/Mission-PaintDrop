using Godot;

/// <summary>
/// Gère le chargement, le déchargement et l'enchaînement des niveaux.
/// Écoute les événements de victoire et affiche la fenêtre de victoire avec le bouton pour le niveau suivant.
/// </summary>
[GlobalClass]
public partial class LevelManager : Node
{
	public static LevelManager Instance { get; private set; }

	[Signal]
	public delegate void LevelLoadedEventHandler(LevelData levelData);

	[Signal]
	public delegate void LevelChangedEventHandler(LevelData from, LevelData to);

	[Export]
	public LevelData FirstLevel { get; set; }

	[Export]
	public PackedScene MainMenuScene { get; set; }

	[Export]
	public PackedScene VictoryWindowScene { get; set; }

	[Export]
	public PackedScene PauseMenuScene { get; set; }

	private Control _pauseMenu;
	private float _levelStartTime;

	public LevelData CurrentLevel { get; private set; }
	public Node CurrentLevelInstance { get; private set; }

	private bool _isPaused = false;
	private float _pausedTimeElapsed = 0.0f;

	private Control _victoryWindow;


	/// <summary>
	/// Indique si le jeu est actuellement en pause.
	/// </summary>
	public bool IsPaused => _isPaused;

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

	public override void _Ready()
	{
		// Écoute l'événement de niveau terminé depuis l'EventBus
		EventBus.Instance.LevelCompleted += OnLevelCompleted;
		
		// Charge automatiquement la scène VictoryWindow si non configurée
		if (VictoryWindowScene == null)
		{
			string[] possiblePaths = {
				"res://ui/victory/VictoryWindow.tscn",
				"res://ui/victory/VictoryWindow.tres"
			};
			
			foreach (string path in possiblePaths)
			{
				VictoryWindowScene = ResourceLoader.Load<PackedScene>(path);
				if (VictoryWindowScene != null)
				{
					GD.Print($"[LevelManager] Auto-loaded VictoryWindow scene from: {path}");
					break;
				}
			}
			
			if (VictoryWindowScene == null)
			{
				GD.PrintErr("[LevelManager] Could not auto-load VictoryWindow scene. Please assign it in the LevelManager inspector.");
			}
		}
		else
		{
			GD.Print("[LevelManager] VictoryWindowScene is already configured");
		}
		// Initialiser le temps de début
		_levelStartTime = 0;

		// Récupère le nœud racine pour les inputs
		var root = GetTree().Root;
		root.SetProcessInput(true);  // S'assurer que la racine traite les inputs
	}

	public override void _Input(InputEvent @event)
	{
		// Vérifie si la touche Échap est pressée
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				if (_pauseMenu == null)
				{
					PauseGame();  // Pause si pas déjà en pause
				}
				else
				{
					ResumeGame(); // Reprend si déjà en pause
				}
			}
		}
	}

	/// <summary>
	/// Charge le premier niveau au démarrage.
	/// Appelé depuis le menu principal ou au démarrage du jeu.
	/// </summary>
	public void StartGame()
	{
		GD.Print($"[LevelManager] Game Start");
		LoadLevel(FirstLevel);
	}

	/// <summary>
	/// Charge le niveau spécifié.
	/// Note: Pour charger des niveaux dynamiquement, configurez une correspondance
	/// entre LevelData et PackedScenes dans l'inspecteur.
	/// </summary>
	public void LoadLevel(LevelData levelData)
	{
		if (levelData == null)
		{
			GD.Print("[LevelManager] No level to load!");
			return;
		}

		LevelData previousLevel = CurrentLevel;
		CurrentLevel = levelData;

		GD.Print($"[LevelManager] Level set: {levelData.LevelName}");
		EmitSignal(SignalName.LevelLoaded, levelData);
		
		if (previousLevel != null)
		{
			EmitSignal(SignalName.LevelChanged, previousLevel, levelData);
		}
	}

	/// <summary>
	/// Décharge le niveau actuel.
	/// </summary>
	private void UnloadCurrentLevel()
	{
		if (CurrentLevelInstance != null)
		{
			// Sauvegarde la référence avant de la libérer
			Node nodeToFree = CurrentLevelInstance;
			CurrentLevelInstance = null;
			nodeToFree.QueueFree();
		}
	}

	/// <summary>
	/// Charge le niveau suivant dans la chaîne.
	/// Appelé quand le joueur clique sur "Next Level" dans la fenêtre de victoire.
	/// Note: Pour activer le chargement dynamique, configurez LevelData avec NextLevel
	/// et implémentez le chargement de scène dans cette méthode.
	/// </summary>
	public void LoadNextLevel()
	{
		GD.Print("[LevelManager] LoadNextLevel() called!");
		HideVictoryWindow();
		if (CurrentLevel?.NextLevel != null)
		{
			GD.Print($"[LevelManager] Next level: {CurrentLevel.NextLevel.LevelName}");
			// TODO: Implémenter le chargement de la scène pour NextLevel
			// Exemple: GetTree().ChangeSceneToFile("res://levels/world_01.tscn");
		}
		else
		{
			GD.Print("[LevelManager] No next level! This was the last level.");
			// TODO: Return to main menu or show completion screen
		}
	}

	/// <summary>
	/// Appelé quand un niveau est terminé (les 2 joueurs ont atteint le but).
	/// Affiche la fenêtre de victoire.
	/// </summary>
	private void OnLevelCompleted(Resource levelDataResource, float completionTime)
	{
		if (levelDataResource is not LevelData completedLevel)
		{
			GD.PrintErr("[LevelManager] LevelCompleted received non-LevelData resource");
			return;
		}

		GD.Print($"[LevelManager] Level completed: {completedLevel.LevelName} in {completionTime:F2}s");
		ShowVictoryWindow(completedLevel, completionTime);
	}

	/// <summary>
	/// Affiche la fenêtre de victoire avec le bouton "Next Level".
	/// </summary>
	private void ShowVictoryWindow(LevelData completedLevel, float completionTime)
	{
		// Détruit toute fenêtre de victoire existante
		if (_victoryWindow != null)
		{
			_victoryWindow.QueueFree();
			_victoryWindow = null;
		}

		if (VictoryWindowScene == null)
		{
			GD.PrintErr("[LevelManager] VictoryWindowScene is not set! Check that res://ui/victory/VictoryWindow.tscn exists and is properly configured.");
			// Fallback: load next level immediately (won't work without the scene)
			LoadNextLevel();
			return;
		}

		_victoryWindow = (Control)VictoryWindowScene.Instantiate();
		
		// Configure la fenêtre avec les données du niveau
		_victoryWindow.Set("LevelName", completedLevel.LevelName);
		_victoryWindow.Set("ParTime", completedLevel.ParTime);
		
		// Met à jour le temps de complétion
		if (_victoryWindow is VictoryWindow victoryWindow)
		{
			victoryWindow.SetCompletionTime(completionTime);
		}
		else
		{
			_victoryWindow.Set("CompletionTime", completionTime);
		}
		
		// Connecte le signal du bouton Next Level
		Button nextLevelButton = _victoryWindow.GetNodeOrNull<Button>("NextLevelButton");
		GD.Print($"[LevelManager] NextLevelButton found: {nextLevelButton != null}");
		if (nextLevelButton != null)
		{
			GD.Print("[LevelManager] Connecting NextLevelButton");
			nextLevelButton.Pressed += OnNextLevelButtonPressed;
		}

		// Connecte le bouton Restart Level
		Button restartButton = _victoryWindow.GetNodeOrNull<Button>("RestartButton");
		GD.Print($"[LevelManager] RestartButton found: {restartButton != null}");
		if (restartButton != null)
		{
			GD.Print("[LevelManager] Connecting RestartButton");
			restartButton.Pressed += OnRestartButtonPressed;
		}

		Button mainMenuButton = _victoryWindow.GetNodeOrNull<Button>("MainMenuButton");
		if (mainMenuButton != null)
		{
			GD.Print("[LevelManager] Connecting MainMenuButton");
			mainMenuButton.Pressed += () => {
				GD.Print("[LevelManager] MainMenuButton clicked!");
				ReturnToMainMenu();
			};
		}

		// Ajoute à l'arbre de scène (sera affiché au-dessus de tout)
		GetTree().Root.AddChild(_victoryWindow);
		_victoryWindow.Visible = true;
	}

	/// <summary>
	/// Gestionnaire du clic sur le bouton Next Level.
	/// </summary>
	private void OnNextLevelButtonPressed()
	{
		GD.Print("[LevelManager] OnNextLevelButtonPressed called!");
		LoadNextLevel();
	}

	/// <summary>
	/// Gestionnaire du clic sur le bouton Restart Level.
	/// </summary>
	private void OnRestartButtonPressed()
	{
		GD.Print("[LevelManager] OnRestartButtonPressed called!");
		RestartCurrentLevel();
	}

	/// <summary>
	/// Cache la fenêtre de victoire.
	/// </summary>
	private void HideVictoryWindow()
	{
		if (_victoryWindow != null)
		{
			_victoryWindow.QueueFree();
			_victoryWindow = null;
		}
	}

	/// <summary>
	/// Recommence le niveau actuel.
	/// </summary>
	public void RestartCurrentLevel()
	{
		GD.Print("[LevelManager] RestartCurrentLevel() called!");
		HideVictoryWindow();
		// Recharge la scène actuelle
		GD.Print("[LevelManager] Calling ReloadCurrentScene()");
		GetTree().ReloadCurrentScene();
		GD.Print("[LevelManager] ReloadCurrentScene() returned");
	}

	/// <summary>
	/// Retourne au menu principal.
	/// </summary>
	public void ReturnToMainMenu()
	{
		GD.Print("[LevelManager] Returning to main menu");
		HideVictoryWindow();
		UnloadCurrentLevel();

		if (MainMenuScene != null)
		{
			GetTree().ChangeSceneToPacked(MainMenuScene);
		}
		else
		{
			GetTree().ChangeSceneToFile("res://ui/main_menu/MainMenu.tscn");
			GD.PrintErr("[LevelManager] MainMenuScene is not set!");
		}
	}

	/// <summary>
	/// Définit le temps de début du niveau (appelé depuis BaseLevel).
	/// </summary>
	public void SetLevelStartTime(float startTime)
	{
		_levelStartTime = startTime;
	}

	/// <summary>
	/// Obtient le temps écoulé depuis le début du niveau.
	/// </summary>
	public float GetElapsedTime()
	{
		if (_levelStartTime > 0)
		{
			return (Time.GetTicksMsec() - _levelStartTime) / 1000.0f;
		}
		return 0.0f;
	}

	/// <summary>
	/// Met le jeu en pause et affiche le menu pause.
	/// </summary>
	public void PauseGame()
	{
		if (_pauseMenu != null || _isPaused)
		{
			return;
		}

		_isPaused = true;
		_pausedTimeElapsed = GetElapsedTime(); // ⭐ Sauvegarde le temps actuel

		GD.Print("[LevelManager] Pausing game");

		if (PauseMenuScene != null)
		{
			var canvas = new CanvasLayer();
			canvas.Name = "PauseOverlay";
			canvas.Layer = 1000;
			GetTree().Root.AddChild(canvas);

			_pauseMenu = (Control)PauseMenuScene.Instantiate();
			_pauseMenu.MouseFilter = Control.MouseFilterEnum.Stop;
			canvas.AddChild(_pauseMenu);
			_pauseMenu.GrabFocus();

			if (_pauseMenu is PauseMenu pauseMenu)
			{
				pauseMenu.ElapsedTime = _pausedTimeElapsed; // ⭐ Affiche le temps figé
				pauseMenu.UpdateTimeDisplay();
			}

			if (_pauseMenu is PauseMenu pauseMenuCtrl)
			{
				pauseMenuCtrl.ResumeRequested += OnResumeRequested;
				pauseMenuCtrl.ResetRequested += OnResetRequested;
				pauseMenuCtrl.MainMenuRequested += OnMainMenuRequested;
			}
		}
		else
		{
			GD.PrintErr("[LevelManager] PauseMenuScene is not set!");
		}
	}

	/// <summary>
	/// Reprend le jeu et cache le menu pause.
	/// </summary>
	public void ResumeGame()
	{
		if (_pauseMenu != null)
		{
			GD.Print("[LevelManager] Resuming game");
			_pauseMenu.GetParent()?.QueueFree();
			_pauseMenu = null;
			_levelStartTime = Time.GetTicksMsec() - (_pausedTimeElapsed * 1000);
			_isPaused = false;
		}
	}

	/// <summary>
	/// Gestionnaire du signal ResumeRequested.
	/// </summary>
	private void OnResumeRequested()
	{
		ResumeGame();
	}

	/// <summary>
	/// Gestionnaire du signal ResetRequested.
	/// </summary>
	private void OnResetRequested()
	{
		ResumeGame();
		RestartCurrentLevel();
	}

	/// <summary>
	/// Gestionnaire du signal MainMenuRequested.
	/// </summary>
	private void OnMainMenuRequested()
	{
		ResumeGame();
		ReturnToMainMenu();
	}
}
