using Godot;

[Tool]
public partial class BlockTrigger : BaseBlock
{
	private const float DotRestRadius = 10f;    // rayon du point au repos
	private const float DotRestMaxRatio = 0.3f; // le point reste petit sur les murs fins (part du petit côté)
	private const float CoverMargin = 0f;       // rayon final = moitié du grand côté + cette marge
	private const float FadeStart = 0.8f;       // à partir de là, le fond se fond vers la nouvelle couleur
	private const float AppearDuration = 0.25f; // apparition du nouveau point après un remplissage

	private PaintDot _dot;

	private PaintColor _triggeredPaint;
	private bool _isTriggered;       // état logique : pilote la collision, change tout de suite
	private bool _visualTriggered;   // couleur de fond actuellement affichée
	private float _fill;             // 0 = point au repos, 1 = mur entièrement rempli
	private float _appear = 1f;      // 0 -> 1 : apparition du point après un remplissage
	private Tween _tween;

	// Canal écouté : doit être identique à celui du bouton
	[Export] public StringName Channel { get; set; }

	[Export(PropertyHint.Range, "0.1,3,0.05,suffix:s")]
	public float TransitionDuration { get; set; } = 0.6f;

	// Couleur du mur de base : c'est la propriété Paint héritée de BaseBlock

	[Export]
	public PaintColor TriggeredPaint
	{
		get => _triggeredPaint;
		set
		{
			if (_triggeredPaint == value) return;

			if (_triggeredPaint != null) _triggeredPaint.Changed -= Refresh;
			_triggeredPaint = value;
			if (_triggeredPaint != null) _triggeredPaint.Changed += Refresh;

			Refresh();
		}
	}

	// Couleur affichée en fond, et couleur "de l'autre côté" (celle du point)
	private PaintColor VisualPaint => _visualTriggered ? _triggeredPaint : Paint;
	private PaintColor OtherPaint => _visualTriggered ? Paint : _triggeredPaint;

	// La collision suit l'état logique, pas l'animation
	protected override PaintColor ActivePaint =>
		_isTriggered && _triggeredPaint != null ? _triggeredPaint : base.ActivePaint;

	protected override Color ActiveDisplayColor
	{
		get
		{
			var main = VisualPaint;
			var other = OtherPaint;
			if (main == null || other == null) return base.ActiveDisplayColor;

			// En fin de remplissage, le fond rejoint la couleur du point : ça masque les restes de bords
			float t = Mathf.Clamp((_fill - FadeStart) / (1f - FadeStart), 0f, 1f);
			float blend = t * t * (3f - 2f * t);
			return main.DisplayColor.Lerp(other.DisplayColor, blend);
		}
	}

	public override void _EnterTree()
	{
		base._EnterTree();

		if (_triggeredPaint != null)
		{
			_triggeredPaint.Changed -= Refresh;
			_triggeredPaint.Changed += Refresh;
		}

		if (Engine.IsEditorHint()) return;
		GetNode<EventBus>("/root/EventBus").TriggerChanged += OnTriggerChanged;
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		if (_triggeredPaint != null) _triggeredPaint.Changed -= Refresh;

		if (Engine.IsEditorHint()) return;
		_tween?.Kill();
		var bus = GetNodeOrNull<EventBus>("/root/EventBus");
		if (bus != null) bus.TriggerChanged -= OnTriggerChanged;
	}

	public override void _Ready()
	{
		// Nettoie d'éventuels points restants (ancienne version, rechargement du code dans l'éditeur)
		foreach (var child in GetChildren())
		{
			if (child is not PaintDot) continue;
			RemoveChild(child);
			child.QueueFree();
		}

		// Le point ne doit jamais dépasser du mur : il est découpé par la forme dessinée du bloc
		ClipChildren = ClipChildrenMode.Disabled;

		// Un seul point, au centre du mur
		_dot = new PaintDot { Position = Vector2.Zero };
		AddChild(_dot); // pas de Owner : n'est pas sauvegardé dans le .tscn

		base._Ready(); // appelle Refresh()
	}

	protected override void Refresh()
	{
		base.Refresh();
		if (IsNodeReady()) UpdateDot();
	}

	private void OnTriggerChanged(StringName channel, bool active)
	{
		if (Channel == null || Channel.IsEmpty || channel != Channel) return;
		SetTriggered(active);
	}

	private void SetTriggered(bool triggered)
	{
		if (_isTriggered == triggered) return;
		_isTriggered = triggered;

		// La collision change tout de suite, l'animation ne concerne que le visuel
		Refresh();

		_tween?.Kill();
		_appear = 1f; // si on interrompt l'apparition du point

		_tween = CreateTween();
		if (triggered != _visualTriggered)
		{
			// Remplissage vers l'autre couleur, puis bascule et apparition du nouveau point
			float duration = Mathf.Max(0.01f, TransitionDuration * (1f - _fill));
			_tween.TweenMethod(Callable.From<float>(SetFill), _fill, 1f, duration);
			_tween.TweenCallback(Callable.From(() => CommitVisual(triggered)));
			_tween.TweenMethod(Callable.From<float>(SetAppear), 0f, 1f, AppearDuration);
		}
		else
		{
			// Retour en arrière avant la fin du remplissage : la peinture se rétracte
			float duration = Mathf.Max(0.01f, TransitionDuration * _fill);
			_tween.TweenMethod(Callable.From<float>(SetFill), _fill, 0f, duration);
		}
	}

	private void SetFill(float value)
	{
		_fill = value;
		QueueRedraw();
		UpdateDot();
	}

	private void SetAppear(float value)
	{
		_appear = value;
		UpdateDot();
	}

	// Le mur est plein : la couleur de fond change, le point repart dans l'autre couleur
	private void CommitVisual(bool triggered)
	{
		_visualTriggered = triggered;
		_fill = 0f;
		_appear = 0f;
		QueueRedraw();
		UpdateDot();
	}

	private void UpdateDot()
	{
		if (_dot == null) return;

		bool hasBothColors = Paint != null && _triggeredPaint != null;
		_dot.Visible = hasBothColors;
		if (!hasBothColors) return;
		_dot.WallSize = Size;

		float shortSide = Mathf.Min(Size.X, Size.Y);
		float longSide = Mathf.Max(Size.X, Size.Y);

		float restRadius = Mathf.Min(DotRestRadius, shortSide * DotRestMaxRatio);
		// Le point le plus éloigné du centre est le bout de la capsule, à longSide / 2
		float fullRadius = longSide / 2f + CoverMargin;

		float p = Mathf.Clamp(_fill, 0f, 1f);
		p = p * p * (3f - 2f * p); // adoucit le départ et l'arrivée

		_dot.Radius = Mathf.Lerp(restRadius * _appear, fullRadius, p);
		_dot.FillColor = OtherPaint.DisplayColor;
	}
}