using Godot;
using System;

public partial class BaseBlock : StaticBody2D
{
	[Export] PaintColor Paint { get; set; }

	public override void _EnterTree()
    {
        GetNode<ColorComponent>("ColorComponent").Color = Paint;
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<ColorRect>("Visual").Color = GetNode<ColorComponent>("ColorComponent").Color.DisplayColor;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
