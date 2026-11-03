using Godot;

[Tool]
[GlobalClass]
public partial class PaintColor : Resource
{
    [Export] public StringName Id { get; set; }

    private Color _displayColor = Colors.White;

    [Export]
    public Color DisplayColor
    {
        get => _displayColor;
        set
        {
            _displayColor = value;
            EmitChanged(); // prévient les blocs qui utilisent cette couleur
        }
    }

    // Numéro du layer de collision (1 à 32), comme dans l'éditeur
    [Export(PropertyHint.Range, "1,32,1")]
    public int PhysicsLayer { get; set; } = PhysicsLayers.FirstLayerColor;

    // Bitmask prêt à assigner à CollisionLayer / CollisionMask
    public uint LayerMask => PhysicsLayers.ToMask(PhysicsLayer);
}