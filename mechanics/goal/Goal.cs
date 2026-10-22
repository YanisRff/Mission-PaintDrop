using System.Collections.Generic;
using Godot;

/// <summary>
/// Zone d'arrivée (but) du niveau.
/// Détecte les joueurs et émet un signal de victoire lorsque les deux joueurs sont dans la zone.
/// </summary>
[GlobalClass]
public partial class Goal : Area2D
{
	[Signal]
	public delegate void PlayerEnteredEventHandler(int playerIndex);

	[Signal]
	public delegate void PlayerExitedEventHandler(int playerIndex);

	[Signal]
	public delegate void GoalCompletedEventHandler();

	[Export]
	public int RequiredPlayers { get; set; } = 2;

	private readonly HashSet<int> _playersInside = new();
	private bool _isCompleted = false;

	public override void _Ready()
	{
		// Détecte uniquement la couche des joueurs (Player = layer 2)
		CollisionLayer = 0;
		CollisionMask = PhysicsLayers.PlayerMask;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		int? playerIndex = GetPlayerIndex(body);
		if (playerIndex == null)
		{
			return;
		}

		_playersInside.Add(playerIndex.Value);
		EmitSignal(SignalName.PlayerEntered, playerIndex.Value);

		if (!_isCompleted && _playersInside.Count >= RequiredPlayers)
		{
			_isCompleted = true;
			EmitSignal(SignalName.GoalCompleted);
		}
	}

	private void OnBodyExited(Node2D body)
	{
		int? playerIndex = GetPlayerIndex(body);
		if (playerIndex == null)
		{
			return;
		}

		_playersInside.Remove(playerIndex.Value);
		EmitSignal(SignalName.PlayerExited, playerIndex.Value);
	}

	/// <summary>
	/// Récupère l'index du joueur (0 ou 1) de façon tolérante (PascalCase ou snake_case).
	/// </summary>
	private static int? GetPlayerIndex(Node2D body)
	{
		Variant val = body.Get("PlayerIndex");
		if (val.VariantType != Variant.Type.Nil)
		{
			return (int)val;
		}

		val = body.Get("player_index");
		if (val.VariantType != Variant.Type.Nil)
		{
			return (int)val;
		}

		return null;
	}
}
