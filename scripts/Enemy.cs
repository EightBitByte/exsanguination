// Enemy.cs
//
// Implements the enemy character node. Enemies follow and attack the player.

using Godot;
using System;


public partial class Enemy : CharacterBody2D
{
	// Milliseconds in a second.
	const float MILLIS = 1000;

	[Signal]
	public delegate void EnemyInjuredEventHandler(int hitBounty);

	[Signal]
	public delegate void AttackedPlayerEventHandler(int damage);

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
	float AttackRange = 150;

	[Export]
	float AttackDamage = 34;

	[Export]
	private float BloodSpread = 50;

	private CharacterBody2D Player;
	private NavigationAgent2D Pathfinding;
	private Sprite2D Sprite;
	private Area2D AttackBox;
	private EnemyManager EManager;
	private double pathRecalculationTimeElapsedMs = 0;
	private double attackCooldownTimeElapsedMs = 0;
	private double timeElapsedWhilePlayerInProximityMs = 0;
	private float health;
	private bool playerInAttackBox = false;
	private PackedScene bloodPoolScene;
	private RandomNumberGenerator rng;
	private float minBloodPoolScale = 0.10f;
	private float maxBloodPoolScale = 0.15f;

	private static string rootPath = "/root/main_scene/";

	public override void _Ready()
	{
		Player = GetNode<CharacterBody2D>(rootPath + "Player");
		EManager = GetNode<EnemyManager>(rootPath + "Enemy Manager");
		Pathfinding = GetChild<NavigationAgent2D>(2);
		Sprite = GetChild<Sprite2D>(0);
		AttackBox = GetChild<Area2D>(4);

		bloodPoolScene = GD.Load<PackedScene>("res://scenes/blood_pool.tscn");
		rng = new();
		rng.Randomize();

		health = MaxHP;
	}


	public override void _PhysicsProcess(double delta)
	{
		pathRecalculationTimeElapsedMs += delta;
		attackCooldownTimeElapsedMs += delta;
		
		// Recalculate pathfinding every so often
		if (pathRecalculationTimeElapsedMs > MsToRecalculatePath / MILLIS) {
			Pathfinding.TargetPosition = Player.Position;
			pathRecalculationTimeElapsedMs = 0;
		}

		// If we're close enough to the player for long enough, and it's been long 
		// enough since our last attack
		if (Position.DistanceTo(Player.Position) < AttackRange) {
			timeElapsedWhilePlayerInProximityMs += delta;

			if (timeElapsedWhilePlayerInProximityMs > TimeInProximityBeforeAttack / MILLIS 
			&& attackCooldownTimeElapsedMs > AttackCooldown / MILLIS && playerInAttackBox) {

				EmitSignal(SignalName.AttackedPlayer, AttackDamage);
				attackCooldownTimeElapsedMs = 0;
			}

		} else {
			timeElapsedWhilePlayerInProximityMs = 0;
		}


		Vector2 NextPos = Pathfinding.GetNextPathPosition();
		Vector2 Direction = GlobalPosition.DirectionTo(NextPos);
		Rotation = (float)(Math.Atan2(Direction.Y, Direction.X) + Math.PI/2);

		Velocity = Direction * Speed;
		MoveAndSlide();
	}

	public void Hurt(float damage) {
		health -= damage;
		EmitSignal(SignalName.EnemyInjured, HitBounty);
		SpawnBloodPool();

		if (health <= 0) {
			EmitSignal(SignalName.EnemyInjured, KillBounty);
			++EManager.KilledInfected;
			--EManager.infectedAlive;
			QueueFree();
		}
	}

	public void SpawnBloodPool () {
		Sprite2D bloodpool = bloodPoolScene.Instantiate<Sprite2D>();
		bloodpool.Rotation = rng.RandfRange(0, (float)(2 * Math.PI));
		float scaleFactor = rng.RandfRange(0.10f, 0.15f);
		bloodpool.Scale = new Vector2(scaleFactor, scaleFactor);

		Vector2 poolPosition = GlobalPosition;

		poolPosition.X += rng.RandfRange(-BloodSpread/2, BloodSpread/2);
		poolPosition.Y += rng.RandfRange(-BloodSpread/2, BloodSpread/2);

		bloodpool.GlobalPosition = poolPosition;

		GetTree().Root.AddChild(bloodpool);
	}

	private void OnAttackBoxEntered(Node2D body)
	{
		// TODO: Needs a more robust way of checking. Perhaps comparing the reference to the Character itself?
		if (body.Name == "Player")
			playerInAttackBox = true;
	}

	private void OnAttackBoxExited(Node2D body)
	{
		if (body.Name == "Player")
			playerInAttackBox = false;
	}
}
