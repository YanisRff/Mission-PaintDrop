using Godot;

[Tool]
[GlobalClass]
public partial class Spike : Node2D
{
    private PaintColor _paint;

    [Export]
    public PaintColor Paint
    {
        get => _paint;
        set
        {
            if (_paint == value) return;
            if (_paint != null) _paint.Changed -= Apply;
            _paint = value;
            if (_paint != null) _paint.Changed += Apply;
            Apply();
        }
    }

    public override void _Ready()
    {
        var area = GetNodeOrNull<Area2D>("Area2D");
        if (area != null && !Engine.IsEditorHint())
            area.BodyEntered += OnBodyEntered;

        Apply();
    }

    /// <summary>
    /// Débranche tous les callbacks avant destruction : évite les erreurs
    /// "managed_callable->delegate_handle.value == nullptr" en éditeur.
    /// </summary>
    public void Cleanup()
    {
        if (_paint != null)
        {
            _paint.Changed -= Apply;
            _paint = null;
        }

        var area = GetNodeOrNull<Area2D>("Area2D");
        if (area != null)
            area.BodyEntered -= OnBodyEntered;
    }

    public override void _ExitTree() => Cleanup();

    private void Apply()
    {
        // Pendant le chargement de la scène, _Ready() s'en charge
        if (!IsNodeReady()) return;

        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        var area = GetNodeOrNull<Area2D>("Area2D");

        if (sprite != null)
            sprite.Modulate = _paint?.DisplayColor ?? Colors.White;

        if (area == null) return;

        // Comme les murs : la pointe occupe le layer de sa couleur,
        // et détecte uniquement les joueurs.
        area.CollisionLayer = _paint?.LayerMask ?? 0u;
        area.CollisionMask = PhysicsLayers.PlayerMask;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player player) return;

        // Comme pour les murs : le joueur traverse la pointe de sa propre couleur
        if (_paint != null && player.Paint == _paint) return;

        GD.Print("[Spike] Joueur touché : restart du niveau");
        if (LevelManager.Instance != null)
            LevelManager.Instance.RestartCurrentLevel();
    }
}
