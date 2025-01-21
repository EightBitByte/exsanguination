// Bullet.cs
//
// Defines a bullet projectile object that, upon collision with an enemy, 
// deals damage.

using Godot;
using System;

public partial class Bullet : CharacterBody2D
{
	[Export]
	private float BulletSpeed = 800;

	[Export]
	public float BulletDamage = 10;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 CalculatedVelocity = Vector2.Zero;
		CalculatedVelocity.X = BulletSpeed;

		Velocity = CalculatedVelocity.Rotated(Rotation);

		KinematicCollision2D collision = MoveAndCollide(Velocity * (float)delta);

		if (collision != null) {
			var collider = collision.GetCollider();

			// TODO: Again, not very resilient to change. Look into a better way to ID an enemy.
			if (collider.GetClass() == "CharacterBody2D") {
				collider.Call("Hurt", BulletDamage);
			}

			QueueFree();
		}
	}
}
