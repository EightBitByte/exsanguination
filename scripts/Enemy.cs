using Godot;
using System;
using System.Runtime.ExceptionServices;

public partial class Enemy : CharacterBody2D
{
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

	CharacterBody2D Player;
	NavigationAgent2D Pathfinding;
	Sprite2D Sprite;
	double timeSinceLastPath = 0;
	private float health;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Player = GetNode<CharacterBody2D>("/root/main_scene/Character");
		Pathfinding = GetChild<NavigationAgent2D>(2);
		Sprite = GetChild<Sprite2D>(0);
		health = maxHP;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		timeSinceLastPath += delta;
		
		// Recalculate pathfinding every so often
		if (timeSinceLastPath > MsToRecalculatePath / 1000) {
			Pathfinding.TargetPosition = Player.Position;
			timeSinceLastPath = 0;
		}


		Vector2 NextPos = Pathfinding.GetNextPathPosition();
		Vector2 Direction = GlobalPosition.DirectionTo(NextPos);
		Sprite.Rotation = (float)(Math.Atan2(Direction.Y, Direction.X) + Math.PI/2);

		Velocity = Direction * Speed;
		MoveAndSlide();
	}

	/// <summary>
	/// Reduces the enemy's health by <c>damage</c>
	/// </summary>
	/// <param name="damage">The amount of damage to subtract from the enemy HP.</param>
	public void Hurt(float damage) {
		health -= damage;
		Player.Call("addPoints", HitBounty);

		if (health <= 0) {
			Player.Call("addPoints", KillBounty);
			QueueFree();
		}

		GD.Print("Enemy Hurt for ", damage, ", health at ", health);
	}

}
