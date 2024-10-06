using Godot;
using System;
using System.Runtime.ExceptionServices;


public partial class Enemy : CharacterBody2D
{
	// Milliseconds in a second.
	const float MILLIS = 1000;

	[Export]
	float maxHP = 100;

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
	float attackDmg = 34;

	CharacterBody2D Player;
	NavigationAgent2D Pathfinding;
	Sprite2D Sprite;
	Area2D AttackBox;
	Node2D EnemyManager;
	double timeSinceLastPath = 0;
	double timeSinceLastAttack = 0;
	double closeTime = 0;
	private float health;
	private bool playerInAttackBox = false;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Player = GetNode<CharacterBody2D>("/root/main_scene/Character");
		EnemyManager = GetNode<Node2D>("/root/main_scene/Enemy Manager");
		Pathfinding = GetChild<NavigationAgent2D>(2);
		Sprite = GetChild<Sprite2D>(0);
		AttackBox = GetChild<Area2D>(4);

		health = maxHP;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
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

				Player.Call("Hurt", attackDmg);
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
	/// Reduces the enemy's health by <c>damage</c>
	/// </summary>
	/// <param name="damage">The amount of damage to subtract from the enemy HP.</param>
	public void Hurt(float damage) {
		health -= damage;
		Player.Call("AddPoints", HitBounty);
		EnemyManager.Call("SpawnBloodPool", GlobalPosition);

		if (health <= 0) {
			Player.Call("AddPoints", KillBounty);
			QueueFree();
		}
	}

	private void OnAttackBoxEntered(Node2D body)
	{
		if (body.Name == "Character")
			playerInAttackBox = true;
	}

	private void OnAttackBoxExited(Node2D body)
	{
		if (body.Name == "Character")
			playerInAttackBox = false;
	}
}
