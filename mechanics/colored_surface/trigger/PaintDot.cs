using Godot;

// Un point de peinture : un disque limité à l'intérieur de la capsule du mur
[Tool]
public partial class PaintDot : Node2D
{
	private const int Segments = 128;
	private const float EdgeInset = 2f; // le point s'arrête un peu avant le bord du mur

	private float _radius;
	private Color _fillColor = Colors.White;
	private Vector2 _wallSize = new(100, 100);

	public float Radius
	{
		get => _radius;
		set
		{
			if (Mathf.IsEqualApprox(_radius, value)) return;
			_radius = value;
			QueueRedraw();
		}
	}

	public Color FillColor
	{
		get => _fillColor;
		set
		{
			if (_fillColor == value) return;
			_fillColor = value;
			QueueRedraw();
		}
	}

	// Taille du mur (le point est centré sur l'origine du mur)
	public Vector2 WallSize
	{
		get => _wallSize;
		set
		{
			if (_wallSize == value) return;
			_wallSize = value;
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (_radius < 0.1f) return;

		// Pour chaque direction : on s'arrête au rayon du point ou au bord du mur, le plus proche
		var points = new Vector2[Segments];
		for (int i = 0; i < Segments; i++)
		{
			float angle = Mathf.Tau * i / Segments;
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			points[i] = dir * Mathf.Min(_radius, WallDistance(dir));
		}

		DrawColoredPolygon(points, _fillColor);
	}

	// Distance entre le centre et le bord de la capsule, dans la direction donnée
	private float WallDistance(Vector2 dir)
	{
		bool horizontal = _wallSize.X >= _wallSize.Y;
		float radius = Mathf.Max(Mathf.Min(_wallSize.X, _wallSize.Y) / 2f - EdgeInset, 0.5f);
		float half = Mathf.Abs(_wallSize.X - _wallSize.Y) / 2f; // demi-longueur de la partie droite

		// On se ramène au cas horizontal (la capsule est symétrique)
		var d = horizontal ? dir : new Vector2(dir.Y, dir.X);

		// Partie droite : on touche le côté plat avant les bouts ?
		if (Mathf.Abs(d.Y) > 0.0001f)
		{
			float t = radius / Mathf.Abs(d.Y);
			if (t * Mathf.Abs(d.X) <= half) return t;
		}

		// Sinon on touche un des bouts arrondis : cercle de rayon `radius` centré en (±half, 0)
		float cx = Mathf.Sign(d.X) * half;
		float b = d.X * cx;
		float disc = b * b - cx * cx + radius * radius;
		return b + Mathf.Sqrt(Mathf.Max(disc, 0f));
	}
}