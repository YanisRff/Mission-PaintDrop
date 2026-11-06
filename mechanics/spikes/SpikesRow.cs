using Godot;

[Tool]
[GlobalClass]
public partial class SpikesRow : Node2D
{
    [Export] public PackedScene SpikeScene { get; set; }

    public override void _EnterTree()
    {
        GD.Print("SpikesRow _EnterTree !");
        TestInstantiate();
    }

    private void TestInstantiate()
    {
        if (SpikeScene == null)
        {
            GD.Print("ERREUR: SpikeScene est NULL !");
            return;
        }

        var spike = (Node2D)SpikeScene.Instantiate();
        if (spike == null)
        {
            GD.Print("ERREUR: Instantiation échouée !");
            return;
        }

        AddChild(spike);
        GD.Print("✅ 1 Spike créée et ajoutée !");
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        GD.Print("_ValidateProperty appelé");
        TestInstantiate();
    }
}