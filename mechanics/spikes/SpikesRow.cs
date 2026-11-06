using Godot;

[Tool]
[GlobalClass]
public partial class SpikesRow : Node2D
{
    // Petit délai avant reconstruction : évite de reconstruire la rangée des
    // dizaines de fois par frame quand plusieurs propriétés changent d'un coup.
    private const float RebuildDelaySec = 0.05f;

    private const string SpikeScenePath = "res://mechanics/spikes/Spike.tscn";

    [Export]
    public PackedScene SpikeScene
    {
        get => _spikeScene;
        set
        {
            _spikeScene = value;
            ScheduleRebuild();
        }
    }

    [Export(PropertyHint.Range, "1,100,1")]
    public int Count
    {
        get => _count;
        set
        {
            _count = Mathf.Clamp(value, 1, 100);
            ScheduleRebuild();
        }
    }

    [Export(PropertyHint.Range, "1,500,1,suffix:px")]
    public float Spacing
    {
        get => _spacing;
        set
        {
            _spacing = Mathf.Max(1f, value);
            ScheduleRebuild();
        }
    }

    [Export]
    public PaintColor Paint
    {
        get => _paint;
        set
        {
            if (_paint == value) return;
            if (_paint != null) _paint.Changed -= ScheduleRebuild;
            _paint = value;
            if (_paint != null) _paint.Changed += ScheduleRebuild;
            ScheduleRebuild();
        }
    }

    private PackedScene _spikeScene;
    private int _count = 5;
    private float _spacing = 24f;
    private PaintColor _paint;
    private Timer _rebuildTimer;

    public override void _EnterTree()
    {
        _rebuildTimer = GetNodeOrNull<Timer>("RebuildTimer");
        if (_rebuildTimer == null)
        {
            _rebuildTimer = new Timer
            {
                Name = "RebuildTimer",
                WaitTime = RebuildDelaySec,
                OneShot = true
            };
            _rebuildTimer.Timeout += Rebuild;
            AddChild(_rebuildTimer);
            _rebuildTimer.SetMeta("_editor_ignore_save_", true);
        }

        ScheduleRebuild();
    }

    public override void _ExitTree()
    {
        if (_paint != null)
            _paint.Changed -= ScheduleRebuild;

        if (_rebuildTimer != null)
            _rebuildTimer.Timeout -= Rebuild;

        ClearSpikes();
    }

    private void ScheduleRebuild()
    {
        // Pendant le chargement de la scène, _EnterTree() s'en charge
        if (!IsInsideTree()) return;
        if (_rebuildTimer == null) return;

        // Relance le délai si un autre changement arrive entre-temps
        _rebuildTimer.Stop();
        _rebuildTimer.Start();
    }

    private void Rebuild()
    {
        ClearSpikes();

        var spikeScene = _spikeScene;
        if (spikeScene == null)
        {
            // Fallback : la scène par défaut, même si l'inspecteur a été oublié
            spikeScene = GD.Load<PackedScene>(SpikeScenePath);
            if (spikeScene != null)
                _spikeScene = spikeScene;
        }

        if (spikeScene == null)
        {
            GD.PushError($"SpikesRow: SpikeScene est NULL et {SpikeScenePath} est introuvable ({GetPath()})");
            return;
        }

        for (int i = 0; i < _count; i++)
        {
            if (spikeScene.Instantiate() is not Spike spike)
            {
                GD.PushError($"SpikesRow: SpikeScene doit instancier un Spike ({GetPath()})");
                return;
            }

            spike.Position = new Vector2(i * _spacing, 0);
            spike.Paint = _paint;
            // Les pointes générées ne doivent pas être sauvegardées dans la scène
            spike.SetMeta("_editor_ignore_save_", true);
            AddChild(spike);
        }
    }

    private void ClearSpikes()
    {
        foreach (var child in GetChildren())
        {
            if (child is not Spike spike) continue;
            spike.Cleanup();
            spike.QueueFree();
        }
    }
}
