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
	public PackedScene LevelScene { get; set; }

	[Export]
	public LevelData NextLevel { get; set; }

	[Export]
	public float ParTime { get; set; } = 0.0f;
}
