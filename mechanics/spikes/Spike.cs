using Godot;

[GlobalClass]
public partial class Spike : Node2D
{
    public override void _Ready()
    {
        GD.Print("Spike _Ready !");
        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
        {
            sprite.Visible = true;
            GD.Print($"Sprite visible: {sprite.Visible}, modulate: {sprite.Modulate}");
        }
    }
}