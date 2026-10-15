using Godot;

[GlobalClass]
public partial class PaintColor : Resource
{
    [Export] public StringName Id { get; set; }
    [Export] public Color DisplayColor { get; set; } = Colors.White;

    // Numéro du layer de collision (1 à 32), comme dans l'éditeur
    [Export(PropertyHint.Range, "1,32,1")]
    public int PhysicsLayer { get; set; } = PhysicsLayers.Black;

    // Bitmask prêt à assigner à CollisionLayer / CollisionMask
    public uint LayerMask => PhysicsLayers.ToMask(PhysicsLayer);
}