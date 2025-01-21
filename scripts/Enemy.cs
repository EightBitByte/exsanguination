// Enemy.cs
//
// Implements the enemy character node. Enemies follow and attack the player.

using Godot;
using System;


public partial class Enemy : CharacterBody2D
{
	// Milliseconds in a second.
	const float MILLIS = 1000;

	[Export]
	public float MaxHP = 100;

	[Export]
	float Speed = 200;

	[Export]
	int HitBounty = 10;
	[Export]
	int KillBounty = 60;

	[Export]
	float MsToRecalculatePath = 100;
	[Export]
	float AttackCooldown = 1000;
	[Export]
	float TimeInProximityBeforeAttack = 100;
	[Export]
	float DetectAttackDistance = 150;
	[Export]
	float AttackDamage = 34;

	// TODO: These four should be manager calls (except for pathfinding)
	private CharacterBody2D Player;
	private NavigationAgent2D Pathfinding;
	private Sprite2D Sprite;
	private Area2D AttackBox;
	private EnemyManager EManager;
	private double timeSinceLastPath = 0;
	private double timeSinceLastAttack = 0;
	private double closeTime = 0;
	private float health;
	private bool playerInAttackBox = false;


	public override void _Ready()
	{
		Player = GetNode<CharacterBody2D>("/root/main_scene/Character");
		EManager = GetNode<EnemyManager>("/root/main_scene/Enemy Manager");
		Pathfinding = GetChild<NavigationAgent2D>(2);
		Sprite = GetChild<Sprite2D>(0);
		AttackBox = GetChild<Area2D>(4);

		health = MaxHP;
	}


	public override void _PhysicsProcess(double delta)
	{
		timeSinceLastPath += delta;
		timeSinceLastAttack += delta;
		
		// Recalculate pathfinding every so often
		if (timeSinceLastPath > MsToRecalculatePath / MILLIS) {
			Pathfinding.TargetPosition = Player.Position;
			timeSinceLastPath = 0;
		}

		// If we're close enough to the player for long enough, and it's been long enough since our last swing
		if (Position.DistanceTo(Player.Position) < DetectAttackDistance) {
			closeTime += delta;

			if (closeTime > TimeInProximityBeforeAttack / MILLIS 
			&& timeSinceLastAttack > AttackCooldown / MILLIS && playerInAttackBox) {

				Player.Call("Hurt", AttackDamage);
				timeSinceLastAttack = 0;
			}

		} else {
			closeTime = 0;
		}


		Vector2 NextPos = Pathfinding.GetNextPathPosition();
		Vector2 Direction = GlobalPosition.DirectionTo(NextPos);
		Rotation = (float)(Math.Atan2(Direction.Y, Direction.X) + Math.PI/2);

		Velocity = Direction * Speed;
		MoveAndSlide();
	}

	/// <summary>
	/// Reduces the enemy's health by <c>damage</c>.
	/// </summary>
	/// <param name="damage">The amount of damage to subtract from the enemy HP.</param>
	public void Hurt(float damage) {
		health -= damage;
		// TODO: Refactor into a signal upon hurt to award player points instead of 
		// holding reference (reducing coupling).
		Player.Call("AddPoints", HitBounty);
		// TODO: Refactor into own method.. enemy should spawn its own blood
		EManager.Call("SpawnBloodPool", GlobalPosition);

		if (health <= 0) {
			Player.Call("AddPoints", KillBounty);
			++EManager.KilledInfected;
			--EManager.infectedAlive;
			QueueFree();
		}
	}

	/// <summary>
	/// Called upon a <c>Node2D</c> entering its attack range.
	/// </summary>
	/// <param name="body">The Node2D entering the attack range.</param>
	private void OnAttackBoxEntered(Node2D body)
	{
		// TODO: Needs a more robust way of checking. Perhaps comparing the reference to the Character itself?
		if (body.Name == "Character")
			playerInAttackBox = true;
	}

	/// <summary>
	/// Called upon a <c>Node2D</c> exiting its attack range.
	/// </summary>
	/// <param name="body">The Node2D exiting the attack range.</param>
	private void OnAttackBoxExited(Node2D body)
	{
		if (body.Name == "Character")
			playerInAttackBox = false;
	}
}
