using Godot;
using System;

public partial class Brush : Area2D
{

	[Export] public PaintColor Paint { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		Sprite2D myBrush = GetNode<Sprite2D>("%Sprite2D");

		// Supposons que Paint.Id est de type StringName
		string texturePath = Paint.Id.ToString() switch
		{
			var id when id.Contains("red")  => "res://assets/art/props/brush/drawing_brush_red.png",
			var id when id.Contains("blu") => "res://assets/art/props/brush/drawing_brush_blue.png",
			var id when id.Contains("yel") => "res://assets/art/props/brush/drawing_brush_yellow.png",
			var id when id.Contains("ora") => "res://assets/art/props/brush/drawing_brush_orange.png",
			var id when id.Contains("pur") => "res://assets/art/props/brush/drawing_brush_purple.png",
			var id when id.Contains("gre") => "res://assets/art/props/brush/drawing_brush_green.png",
			_ => "res://assets/art/props/brush/drawing_brush_black.png"
		};

		myBrush.Texture = GD.Load<Texture2D>(texturePath);

		CollisionMask = PhysicsLayers.WorldMask | uint.MaxValue << (PhysicsLayers.FirstLayerColor - 1) & ~Paint.LayerMask;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void onBodyEntered(Node body)
    {

        if (body.IsInGroup("Player"))
        {

			Player myPlayer = body as Player;
            // Récupération de l'Autoload et émission du signal
            EventBus.Instance.EmitSignal(EventBus.SignalName.ColorChanged, myPlayer.PlayerIndex , Paint);

			QueueFree();
        }
    }
}
