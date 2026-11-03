using Godot;

/// <summary>
/// Données et métadonnées d'un niveau (nom, scène, niveau suivant, par-time).
/// Utilisé par le LevelManager pour charger et enchaîner les niveaux.
/// </summary>
[GlobalClass]
public partial class LevelData : Resource
{
    [Export]
    public string LevelName { get; set; } = string.Empty;

    [Export]
    public LevelData NextLevel { get; set; }

    [Export]
    public float ParTime { get; set; } = 0.0f;

	[Export]
	public PaintColor[] StartPaints = new PaintColor[2];

	/// <summary>
	/// Chemin vers la scène du niveau (ex: "res://levels/world_01.tscn").
	/// Utilisé pour éviter les références circulaires entre ressources.
	/// </summary>
	[Export]
	public string LevelScenePath { get; set; } = string.Empty;

	/// <summary>
	/// Charge et retourne la PackedScene associée à ce niveau.
	/// </summary>
	public PackedScene GetLevelScene()
	{
		if (string.IsNullOrEmpty(LevelScenePath))
		{
			GD.PrintErr($"[LevelData] LevelScenePath is empty for {LevelName}");
			return null;
		}
		return ResourceLoader.Load<PackedScene>(LevelScenePath);
	}
}
