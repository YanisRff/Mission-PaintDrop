using Godot;

[Tool]
public partial class ButtonTrigger : Area2D
{
	public enum TriggerMode
	{
		Hold,   // actif tant que quelqu'un est dessus
		Toggle  // bascule à chaque nouvel appui
	}

	private const float SocleHeight = 6f;
	private const float SocleOverhang = 4f;  // le socle dépasse du plateau de chaque côté
	private const float PlateHeight = 10f;
	private const float PressDepth = 7f;
	private const float PressDuration = 0.08f;
	private const float ReleaseDuration = 0.15f;

	private PaintColor _paint;
	private float _width = 60f;
	private int _bodyCount;
	private bool _isOn;          // état logique (mode Toggle)
	private float _press;        // 0 = relevé, 1 = enfoncé
	private float _pressTarget;

	private readonly StyleBoxFlat _plateStyle = new() { AntiAliasing = true, CornerDetail = 8 };
	private readonly StyleBoxFlat _socleStyle = new()
	{
		AntiAliasing = true,
		CornerDetail = 8,
	};

	[Export] public StringName Channel { get; set; }
	[Export] public TriggerMode Mode { get; set; } = TriggerMode.Hold;

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

	// Largeur du bouton en pixels, par pas de 10
	[Export(PropertyHint.Range, "20,400,10,suffix:px")]
	public float Width
	{
		get => _width;
		set
		{
			_width = Mathf.Max(20f, Mathf.Snapped(value, 10f));
			Refresh();
		}
	}

	public ButtonTrigger()
	{
		_plateStyle.SetCornerRadius(Corner.TopLeft, 4);
		_plateStyle.SetCornerRadius(Corner.TopRight, 4);
		_socleStyle.SetCornerRadiusAll(3);
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

	public override void _Ready()
	{
		SetProcess(false);
		Refresh();

		if (Engine.IsEditorHint()) return;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		var component = GetNodeOrNull<ColorComponent>("ColorComponent");
		if (component != null) component.ColorChanged += OnComponentColorChanged;

		ApplyDetectionMask();
	}

	public override void _Process(double delta)
	{
		float duration = _pressTarget > _press ? PressDuration : ReleaseDuration;
		_press = Mathf.MoveToward(_press, _pressTarget, (float)delta / duration);
		QueueRedraw();

		if (Mathf.IsEqualApprox(_press, _pressTarget))
		{
			_press = _pressTarget;
			SetProcess(false);
		}
	}

	public override void _Draw()
	{
		float halfWidth = _width / 2f;
		float depth = PressDepth * _press;

		// Le plateau descend derrière le socle : il "rentre" dans le sol
		float top = -(SocleHeight + PlateHeight) + depth;
		float bottom = -SocleHeight / 2f; // recouvert par le socle
		_plateStyle.BgColor = _paint?.DisplayColor ?? Colors.White;
		DrawStyleBox(_plateStyle, new Rect2(-halfWidth, top, _width, bottom - top));

		_socleStyle.BgColor = _paint?.DisplayColor ?? Colors.White;
		DrawStyleBox(_socleStyle, new Rect2(
			-halfWidth - SocleOverhang, -SocleHeight,
			_width + SocleOverhang * 2f, SocleHeight));
	}

	private void Refresh()
	{
		// Pendant le chargement de la scène, _Ready() s'en charge
		if (!IsNodeReady()) return;

		QueueRedraw();

		// Zone de détection : couvre le plateau et le socle
		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision?.Shape is RectangleShape2D rect)
		{
			rect.Size = new Vector2(_width, SocleHeight + PlateHeight);
			collision.Position = new Vector2(0f, -(SocleHeight + PlateHeight) / 2f);
		}

		// Dans l'éditeur : on ne touche pas au ColorComponent
		if (Engine.IsEditorHint()) return;

		var component = GetNodeOrNull<ColorComponent>("ColorComponent");
		if (component != null) component.Color = _paint;
	}

	// Changement de couleur venu de l'extérieur (futur changeur de couleur, etc.)
	private void OnComponentColorChanged(PaintColor newColor)
	{
		Paint = newColor;
		ApplyDetectionMask(); // ColorComponent a réécrit le mask
	}

	// Le rôle Surface de ColorComponent met le mask à 0 : on le réécrit pour détecter les joueurs
	private void ApplyDetectionMask() => CollisionMask = PhysicsLayers.WorldMask | uint.MaxValue << (PhysicsLayers.FirstLayerColor - 1) & ~ Paint.LayerMask;

	private void OnBodyEntered(Node2D body)
	{
		if(body.IsInGroup("Interactor"))
		{
			_bodyCount++;
			if (_bodyCount == 1) Press();
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body.IsInGroup("Interactor"))
		{
			_bodyCount = Mathf.Max(0, _bodyCount - 1);
			if (_bodyCount == 0) Release();
		}
	}

	private void Press()
	{
		_pressTarget = 1f;
		SetProcess(true);

		if (Mode == TriggerMode.Toggle) _isOn = !_isOn;
		EmitTrigger(Mode == TriggerMode.Toggle ? _isOn : true);
	}

	private void Release()
	{
		_pressTarget = 0f;
		SetProcess(true);

		if (Mode == TriggerMode.Hold) EmitTrigger(false);
	}

	private void EmitTrigger(bool active)
	{
		if (Channel == null || Channel.IsEmpty)
		{
			GD.PushWarning($"{nameof(ButtonTrigger)} sans Channel ({GetPath()})");
			return;
		}

		GetNode<EventBus>("/root/EventBus")
			.EmitSignal(EventBus.SignalName.TriggerChanged, Channel, active);
	}
}