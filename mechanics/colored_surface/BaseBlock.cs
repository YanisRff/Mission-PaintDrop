using Godot;
using System;

[Tool]
public partial class BaseBlock : StaticBody2D
{
	private PaintColor _paint;

	[Export]
	public PaintColor Paint
	{
		get => _paint;
		set
		{
			if (_paint == value) return;

			if (_paint != null) _paint.Changed -= Refresh;
			_paint = value;
			if (_paint != null) _paint.Changed += Refresh;

			Refresh();
		}
	}

	public override void _EnterTree()
	{
		// Réabonnement (idempotent) au cas où le nœud quitte puis revient dans l'arbre
		if (_paint == null) return;
		_paint.Changed -= Refresh;
		_paint.Changed += Refresh;
	}

	public override void _ExitTree()
	{
		if (_paint != null) _paint.Changed -= Refresh;
	}

	public override void _Ready() => Refresh();

	private void Refresh()
	{
		// Pendant le chargement de la scène, _Ready() s'en charge
		if (!IsNodeReady()) return;

		var visual = GetNodeOrNull<ColorRect>("Visual");
		if (visual != null && _paint != null)
			visual.Color = _paint.DisplayColor;

		// Dans l'éditeur : visuel uniquement, on ne touche pas aux collisions
		if (Engine.IsEditorHint()) return;

		var component = GetNodeOrNull<ColorComponent>("ColorComponent");
		if (component != null)
			component.Color = _paint;
	}
}