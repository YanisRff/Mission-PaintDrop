using Godot;

[Tool]
public partial class BaseBlock : StaticBody2D
{
	private const float SizeStep = 10f;


	// Marge autour du mur pour le quad du shader (doit être égale à MARGIN dans paint_wall.gdshader)
	public const float Margin = 16f;

	private PaintColor _paint;
	private Vector2 _size = new(100, 100); // à adapter à la taille actuelle de tes murs

	// Couleur utilisée pour les collisions (surchargée par BlockTrigger)
	protected virtual PaintColor ActivePaint => _paint;

	// Couleur utilisée pour le dessin (surchargée par BlockTrigger pour l'animer)
	protected virtual Color ActiveDisplayColor => _paint?.DisplayColor ?? Colors.White;

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

	// Taille du mur en pixels, par pas de 10
	[Export(PropertyHint.Range, "10,2000,10,suffix:px")]
	public Vector2 Size
	{
		get => _size;
		set
		{
			var snapped = value.Snapped(new Vector2(SizeStep, SizeStep));
			_size = new Vector2(Mathf.Max(SizeStep, snapped.X), Mathf.Max(SizeStep, snapped.Y));
			Refresh();
		}
	}

	public override void _EnterTree()
	{
		if (_paint == null) return;
		_paint.Changed -= Refresh;
		_paint.Changed += Refresh;
	}

	public override void _ExitTree()
	{
		if (_paint != null) _paint.Changed -= Refresh;
	}

	public override void _Ready() => Refresh();

	public override void _Draw()
	{
		if (_paint == null) return;

		var color = ActiveDisplayColor;

		// Avec le shader de peinture : un quad centré, un peu plus grand que le mur.
		// Le shader en découpe la capsule (il faut de la place pour les bords effilochés).
		if (Material is ShaderMaterial)
		{
			var pad = new Vector2(Margin, Margin);
			DrawRect(new Rect2(-_size / 2f - pad, _size + pad * 2f), color);
			return;
		}

		// Sans shader : rendu d'origine (capsule unie)
		// Rayon = moitié du petit côté : les bouts sont complètement arrondis
		var style = new StyleBoxFlat
		{
			BgColor = color,
			AntiAliasing = true,
			CornerDetail = 16
		};
		style.SetCornerRadiusAll(Mathf.RoundToInt(Mathf.Min(_size.X, _size.Y) / 2f));

		// Centré sur l'origine du nœud
		DrawStyleBox(style, new Rect2(-_size / 2f, _size));
	}

	protected virtual void Refresh()
	{
		// Pendant le chargement de la scène, _Ready() s'en charge
		if (!IsNodeReady()) return;

		QueueRedraw();

		// Collision : capsule orientée selon le grand côté
		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision?.Shape is CapsuleShape2D capsule)
		{
			float shortSide = Mathf.Min(_size.X, _size.Y);
			float longSide = Mathf.Max(_size.X, _size.Y);

			capsule.Radius = shortSide / 2f;
			capsule.Height = longSide; // la hauteur d'une capsule inclut les deux bouts

			// La capsule est verticale par défaut : on la couche si le mur est plus large que haut
			collision.Rotation = _size.X > _size.Y ? Mathf.Pi / 2f : 0f;
		}

		// Dans l'éditeur : pas de configuration des layers/masks
		if (Engine.IsEditorHint()) return;

		var component = GetNodeOrNull<ColorComponent>("ColorComponent");
		if (component != null)
			component.Color = ActivePaint;
	}
}
