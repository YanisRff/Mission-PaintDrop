using Godot;

/// <summary>
/// Point d'apparition d'un joueur dans le niveau.
/// Chaque niveau en contient deux (un pour le joueur 0, un pour le joueur 1).
/// </summary>
[GlobalClass]
public partial class SpawnPoint : Marker2D
{
	[Export(PropertyHint.Range, "0,1,1")]
	public int PlayerIndex { get; set; } = 0;
}
