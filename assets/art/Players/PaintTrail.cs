// entities/player/PaintTrail.cs
using System.Collections.Generic;
using Godot;

/// <summary>
/// Traînée de peinture laissée derrière une cible. Chaque point mémorise son
/// âge : le ruban rétrécit et sèche progressivement, puis disparaît.
/// Ce nœud est créé par le Player et placé juste avant lui dans l'arbre
/// (donc dessiné derrière lui, sans hériter de son Modulate).
///
/// Le ruban fournit au shader (paint_trail.gdshader) :
///   COLOR.rgb = couleur, COLOR.a = fraîcheur (1 -> 0),
///   UV.x = distance le long du ruban (px), UV.y = position en travers (-1 à 1).
/// </summary>
public partial class PaintTrail : Node2D
{
    public Node2D Target { get; set; }
    public Color TrailColor { get; set; } = Colors.White;

    /// <summary>Durée de vie d'un point, en secondes.</summary>
    public float Lifetime { get; set; } = 3f;

    /// <summary>Demi-largeur du ruban à sa création, en pixels.</summary>
    public float HalfWidth { get; set; } = 11f;

    /// <summary>Part de la vie pendant laquelle la peinture reste intacte (0 à 1).</summary>
    public float HoldRatio { get; set; } = 0.25f;

    private const float MinSpacing = 5f;       // distance mini entre deux points
    private const float BreakDistance = 96f;   // au-delà : téléportation, on coupe le ruban

    private struct TrailPoint
    {
        public Vector2 Pos;
        public float Born;
        public bool Break; // true : ne pas relier au point précédent
    }

    private readonly List<TrailPoint> _points = new();
    private float _time;
    private bool _targetAlive = true;
    private Vector2 _head;

    // Tampons réutilisés pour éviter les allocations à chaque quad
    private readonly Vector2[] _quad = new Vector2[4];
    private readonly Color[] _quadColors = new Color[4];
    private readonly Vector2[] _quadUvs = new Vector2[4];

    public override void _Ready()
    {
        // Se place juste avant la cible : la traînée est dessinée derrière elle
        if (IsInstanceValid(Target) && Target.GetParent() == GetParent())
            GetParent().MoveChild(this, Target.GetIndex());
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;

        // Supprime les points trop vieux
        int expired = 0;
        while (expired < _points.Count && _time - _points[expired].Born > Lifetime)
            expired++;
        if (expired > 0)
            _points.RemoveRange(0, expired);

        _targetAlive = IsInstanceValid(Target) && Target.IsInsideTree();

        if (_targetAlive)
        {
            _head = ToLocal(Target.GlobalPosition);

            if (_points.Count == 0)
            {
                _points.Add(new TrailPoint { Pos = _head, Born = _time });
            }
            else
            {
                float dist = _head.DistanceTo(_points[^1].Pos);
                if (dist >= MinSpacing)
                    _points.Add(new TrailPoint { Pos = _head, Born = _time, Break = dist > BreakDistance });
            }
        }
        else if (_points.Count == 0)
        {
            // Le joueur n'existe plus et tout est effacé
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        int stored = _points.Count;
        int n = stored + (_targetAlive ? 1 : 0); // + point "vivant" sous le joueur
        if (n < 2)
            return;

        var pos = new Vector2[n];
        var age = new float[n];
        var connected = new bool[n]; // le point i est-il relié au point i-1 ?
        var along = new float[n];    // distance cumulée le long du ruban

        for (int i = 0; i < n; i++)
        {
            if (i < stored)
            {
                pos[i] = _points[i].Pos;
                age[i] = Mathf.Clamp((_time - _points[i].Born) / Lifetime, 0f, 1f);
                connected[i] = i > 0 && !_points[i].Break;
            }
            else
            {
                pos[i] = _head;
                age[i] = 0f;
                connected[i] = true;
            }

            along[i] = i == 0 ? 0f : along[i - 1] + (connected[i] ? pos[i].DistanceTo(pos[i - 1]) : 0f);
        }

        var left = new Vector2[n];
        var right = new Vector2[n];
        var colors = new Color[n];

        for (int i = 0; i < n; i++)
        {
            Vector2 dir;
            if (i == 0) dir = pos[1] - pos[0];
            else if (i == n - 1) dir = pos[i] - pos[i - 1];
            else dir = pos[i + 1] - pos[i - 1];
            dir = dir.LengthSquared() > 0.0001f ? dir.Normalized() : Vector2.Right;

            Vector2 normal = new Vector2(-dir.Y, dir.X);

            float t = age[i];
            float width = HalfWidth * (1f - 0.7f * t); // la trace s'amincit en séchant
            left[i] = pos[i] + normal * width;
            right[i] = pos[i] - normal * width;

            // Alpha = fraîcheur de la peinture : le shader en fait un séchage progressif
            float fresh = 1f - SmoothStep(HoldRatio, 1f, t);
            colors[i] = new Color(TrailColor, fresh);
        }

        for (int i = 0; i < n - 1; i++)
        {
            if (!connected[i + 1])
                continue;

            _quad[0] = left[i];
            _quad[1] = left[i + 1];
            _quad[2] = right[i + 1];
            _quad[3] = right[i];

            _quadColors[0] = colors[i];
            _quadColors[1] = colors[i + 1];
            _quadColors[2] = colors[i + 1];
            _quadColors[3] = colors[i];

            _quadUvs[0] = new Vector2(along[i], 1f);
            _quadUvs[1] = new Vector2(along[i + 1], 1f);
            _quadUvs[2] = new Vector2(along[i + 1], -1f);
            _quadUvs[3] = new Vector2(along[i], -1f);

            DrawPrimitive(_quad, _quadColors, _quadUvs);
        }
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
