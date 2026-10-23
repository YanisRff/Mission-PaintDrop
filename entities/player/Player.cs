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
    [Export] public float JumpVelocity { get; set; } = -420f;
    [Export] public float Gravity { get; set; } = 1200f;
    [Export] public float MaxFallSpeed { get; set; } = 700f;

    [ExportGroup("Saut")]
    // Temps (secondes) pendant lequel on peut encore sauter après avoir quitté le sol
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float CoyoteTime { get; set; } = 0.1f;

    [ExportGroup("Pentes")]
    // Distance max (px) à laquelle le joueur est recollé au sol : évite les "petits sauts"
    // en descente. Doit être >= Speed * dt * tan(angle de la pente) (≈ 4 px à 45°).
    [Export(PropertyHint.Range, "1,32,0.5")] public float FloorSnapDistance { get; set; } = 12f;
    // Angle max (degrés) d'une surface considérée comme du sol (45° par défaut dans Godot)
    [Export(PropertyHint.Range, "0,89,1")] public float MaxSlopeAngle { get; set; } = 45f;

    private StringName _left, _right, _jump;

    // Shader de la goutte (copie propre à chaque joueur)
    private ShaderMaterial _paintMaterial;
    private Vector2 _visualVelocity;
    private float _squash;

    private float _coyoteTimer;

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

        // Pentes : le joueur reste collé au sol en descente
        FloorSnapLength = FloorSnapDistance;
        FloorMaxAngle = Mathf.DegToRad(MaxSlopeAngle);

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

        // Coyote time : le chrono est rechargé tant qu'on touche le sol,
        // puis s'écoule une fois en l'air.
        if (wasOnFloor)
            _coyoteTimer = CoyoteTime;
        else
            _coyoteTimer = Mathf.Max(_coyoteTimer - dt, 0f);

        if (wasOnFloor)
        {
            // Au sol (y compris sur une pente) : on repart d'une vitesse verticale nulle,
            // le snap au sol se charge de coller le joueur à la pente en descente.
            velocity.Y = 0f;
        }
        else
        {
            // Gravité
            velocity.Y = Mathf.Min(velocity.Y + Gravity * dt, MaxFallSpeed);
        }

        // Saut (autorisé au sol, ou pendant le coyote time)
        if (Input.IsActionJustPressed(_jump) && _coyoteTimer > 0f)
        {
            velocity.Y = JumpVelocity;
            _coyoteTimer = 0f; // empêche un second saut
        }

        // Déplacement horizontal avec accélération et friction
        float direction = Input.GetAxis(_left, _right);

        velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * dt);

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
