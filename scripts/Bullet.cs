using Godot;
using System;

public partial class Bullet : CharacterBody2D
{
	[Export]
	private float BulletSpeed = 800;

	public float BulletDamage = 10;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 CalculatedVelocity = Vector2.Zero;
		CalculatedVelocity.X = BulletSpeed;

		Velocity = CalculatedVelocity.Rotated(Rotation);

		KinematicCollision2D collision = MoveAndCollide(Velocity * (float)delta);

		if (collision != null) {
			var collider = collision.GetCollider();

			if (collider.GetClass() == "CharacterBody2D") {
				collider.Call("Hurt", BulletDamage);
			}

			QueueFree();
		}
	}
}
