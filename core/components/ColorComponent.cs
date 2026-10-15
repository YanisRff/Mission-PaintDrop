// core/components/ColorComponent.cs
using Godot;

[GlobalClass]
public partial class ColorComponent : Node
{
    public enum ComponentRole
    {
        Surface, // surface colorée : occupe le layer de sa couleur, ne détecte rien
        Body     // corps mobile (joueur) : détecte le monde et certaines couleurs
    }

    public enum BlockRule
    {
        BlockedByOtherColors, // traverse sa propre couleur, bloqué par les autres
        BlockedByOwnColor     // bloqué par sa propre couleur, traverse les autres
    }

    [Signal]
    public delegate void ColorChangedEventHandler(PaintColor newColor);

    [Export] public ComponentRole Role { get; set; } = ComponentRole.Surface;

    // Ne sert que pour le rôle Body
    [Export] public BlockRule Rule { get; set; } = BlockRule.BlockedByOtherColors;

    // Teinte le parent avec DisplayColor (pratique pour les placeholders)
    [Export] public bool TintParent { get; set; } = true;

    private PaintColor _color;

    [Export]
    public PaintColor Color
    {
        get => _color;
        set
        {
            _color = value;
            if (!IsNodeReady()) return; // pendant le chargement de la scène, _Ready() s'en charge
            Apply();
            EmitSignal(SignalName.ColorChanged, _color);
        }
    }

    // Tous les layers à partir de FirstColorLayer
    private static uint AllColorLayersMask => uint.MaxValue << (PhysicsLayers.FirstLayerColor - 1);

    public override void _Ready() => Apply();

    private void Apply()
    {
        if (GetParent() is not CollisionObject2D body)
        {
            GD.PushError($"{nameof(ColorComponent)} doit être enfant d'un CollisionObject2D ({GetPath()})");
            return;
        }

        if (_color == null)
        {
            GD.PushWarning($"{nameof(ColorComponent)} sans PaintColor ({GetPath()})");
            return;
        }

        switch (Role)
        {
            case ComponentRole.Surface:
                body.CollisionLayer = _color.LayerMask;
                body.CollisionMask = 0;
                break;

            case ComponentRole.Body:
                body.CollisionLayer = PhysicsLayers.PlayerMask;
                body.CollisionMask = PhysicsLayers.WorldMask | ComputeBlockedMask();
                break;
        }

        if (TintParent)
            body.Modulate = _color.DisplayColor;
    }

    // La règle « quelle couleur bloque quelle couleur » vit uniquement ici
    private uint ComputeBlockedMask() => Rule switch
    {
        BlockRule.BlockedByOwnColor => _color.LayerMask,
        BlockRule.BlockedByOtherColors => AllColorLayersMask & ~_color.LayerMask,
        _ => 0u
    };
}