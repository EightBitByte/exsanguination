using Godot;
using System;

public partial class Bullet : CharacterBody2D
{
	[Export]
	float BulletSpeed = 800;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 CalculatedVelocity = Vector2.Zero;
		CalculatedVelocity.X = BulletSpeed;

		Velocity = CalculatedVelocity.Rotated(Rotation);

		var collision = MoveAndCollide(Velocity * (float)delta);

		if (collision != null) {
			QueueFree();
		}
	}
}
