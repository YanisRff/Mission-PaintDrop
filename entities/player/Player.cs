// entities/player/Player.cs
using Godot;

public partial class Player : CharacterBody2D
{
    [Export(PropertyHint.Range, "0,1,1")] public int PlayerIndex { get; set; } = 0;

    // Couleur de la goutte, transmise au ColorComponent
    [Export] public PaintColor Paint { get; set; }

    [ExportGroup("Apparence")]
    [Export] public float Radius { get; set; } = 16f;

    [ExportGroup("Traînée")]
    [Export] public bool LeaveTrail { get; set; } = true;
    // Durée avant que la peinture ait totalement disparu (secondes)
    [Export(PropertyHint.Range, "0.5,10,0.1")] public float TrailLifetime { get; set; } = 3f;
    // Largeur de la traînée, en proportion du rayon
    [Export(PropertyHint.Range, "0.1,1.2,0.05")] public float TrailWidth { get; set; } = 0.7f;
    // ShaderMaterial avec paint_trail.gdshader (peut être partagé entre les joueurs)
    [Export] public Material TrailMaterial { get; set; }

    [ExportGroup("Mouvement")]
    [Export] public float Speed { get; set; } = 220f;
    [Export] public float Acceleration { get; set; } = 1600f;
    [Export] public float Friction { get; set; } = 2000f;
    [Export] public float JumpVelocity { get; set; } = -420f;
    [Export] public float Gravity { get; set; } = 1200f;
    [Export] public float MaxFallSpeed { get; set; } = 700f;

    private StringName _left, _right, _jump;

    // Shader de la goutte (copie propre à chaque joueur)
    private ShaderMaterial _paintMaterial;
    private Vector2 _visualVelocity;
    private float _squash;

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

        SetupPaintShader();
        SetupTrail();

        QueueRedraw();
    }

    private void SetupPaintShader()
    {
        // Chaque joueur a sa propre copie : sinon les deux goutes partageraient
        // la même vitesse / le même écrasement dans le shader.
        if (Material is not ShaderMaterial shared)
            return;

        _paintMaterial = (ShaderMaterial)shared.Duplicate();
        Material = _paintMaterial;

        _paintMaterial.SetShaderParameter("radius", Radius);
        // Vitesse (par axe) à laquelle l'étirement est maximal : course et saut
        _paintMaterial.SetShaderParameter("speed_reference", new Vector2(Speed, Mathf.Abs(JumpVelocity)));
    }

    private void SetupTrail()
    {
        if (!LeaveTrail)
            return;

        var trail = new PaintTrail
        {
            Target = this,
            TrailColor = Paint?.DisplayColor ?? Colors.White,
            Lifetime = TrailLifetime,
            HalfWidth = Radius * TrailWidth,
            Material = TrailMaterial,
        };

        // Le parent est occupé à instancier ses enfants : on ajoute le nœud en différé.
        // La traînée est un frère du joueur (pas un enfant) : elle reste en place
        // quand le joueur bouge et ne subit pas son Modulate.
        GetParent().CallDeferred(Node.MethodName.AddChild, trail);
    }

    // Quad blanc plus grand que le cercle : le shader découpe la goutte dedans
    // (il faut de la place pour la traîne qui s'étire derrière elle).
    // Le ColorComponent le teinte via Modulate (TintParent).
    public override void _Draw()
    {
        float h = Radius * 2f;
        DrawRect(new Rect2(-h, -h, h * 2f, h * 2f), Colors.White);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 velocity = Velocity;

        bool wasOnFloor = IsOnFloor();

        // Gravité
        if (!wasOnFloor)
            velocity.Y = Mathf.Min(velocity.Y + Gravity * dt, MaxFallSpeed);

        // Saut
        if (Input.IsActionJustPressed(_jump) && wasOnFloor)
            velocity.Y = JumpVelocity;

        // Déplacement horizontal avec accélération et friction
        float direction = Input.GetAxis(_left, _right);
        float rate = direction != 0f ? Acceleration : Friction;
        velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, rate * dt);

        float impactSpeed = velocity.Y;

        Velocity = velocity;
        MoveAndSlide();

        // Atterrissage : la goutte s'écrase proportionnellement à la vitesse de chute
        if (!wasOnFloor && IsOnFloor())
            _squash = Mathf.Clamp(impactSpeed / MaxFallSpeed, 0f, 1f);

        UpdatePaintShader(dt);
    }

    private void UpdatePaintShader(float dt)
    {
        if (_paintMaterial == null)
            return;

        // Lissage pour éviter que la forme tremble quand la vitesse varie brusquement
        _visualVelocity = _visualVelocity.Lerp(Velocity, 1f - Mathf.Exp(-14f * dt));
        _squash *= Mathf.Exp(-9f * dt);

        _paintMaterial.SetShaderParameter("velocity", _visualVelocity);
        _paintMaterial.SetShaderParameter("squash", _squash);
    }
}
