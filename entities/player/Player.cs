// entities/player/Player.cs
using Godot;

public partial class Player : CharacterBody2D
{
    [Export(PropertyHint.Range, "0,1,1")] public int PlayerIndex { get; set; } = 0;

    // Couleur de la goutte, transmise au ColorComponent
    [Export] public PaintColor Paint { get; set; }

    [ExportGroup("Apparence")]
    [Export] public float Radius { get; set; } = 16f;

    [ExportGroup("Mouvement")]
    [Export] public float Speed { get; set; } = 220f;
    [Export] public float Acceleration { get; set; } = 1600f;
    [Export] public float Friction { get; set; } = 2000f;
    [Export] public float JumpVelocity { get; set; } = -420f;
    [Export] public float Gravity { get; set; } = 1200f;
    [Export] public float MaxFallSpeed { get; set; } = 700f;

    private StringName _left, _right, _jump;

    // _EnterTree s'exécute avant le _Ready des enfants : le ColorComponent
    // reçoit sa couleur avant de configurer les layers, sans avertissement.
    public override void _EnterTree()
    {
        GetNode<ColorComponent>("ColorComponent").Color = Paint;
    }

    public override void _Ready()
    {
        // Noms d'actions préfixés par joueur : p1_left, p2_jump...
        var prefix = $"p{PlayerIndex + 1}_";
        _left = prefix + "left";
        _right = prefix + "right";
        _jump = prefix + "jump";

        // La forme de collision suit toujours le rayon dessiné
        GetNode<CollisionShape2D>("CollisionShape2D").Shape = new CircleShape2D { Radius = Radius };

		GD.Print($"Layer: {CollisionLayer}  Mask: {CollisionMask}");

        QueueRedraw();
    }

    // Cercle blanc : le ColorComponent le teinte via Modulate (TintParent)
    public override void _Draw() => DrawCircle(Vector2.Zero, Radius, Colors.White);

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 velocity = Velocity;

        // Gravité
        if (!IsOnFloor())
            velocity.Y = Mathf.Min(velocity.Y + Gravity * dt, MaxFallSpeed);

        // Saut
        if (Input.IsActionJustPressed(_jump) && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Déplacement horizontal avec accélération et friction
        float direction = Input.GetAxis(_left, _right);
        float rate = direction != 0f ? Acceleration : Friction;
        velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, rate * dt);

        Velocity = velocity;
        MoveAndSlide();
    }
}