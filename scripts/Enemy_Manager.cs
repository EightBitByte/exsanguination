using Godot;
using System;

public partial class Enemy_Manager : Node2D
{
	[Export]
	float bloodSpread = 50;

	PackedScene ENEMY_SCENE, BLOOD_POOL_SCENE;
	RandomNumberGenerator rng;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		BLOOD_POOL_SCENE = GD.Load<PackedScene>("res://scenes/blood_pool.tscn");
		rng = new RandomNumberGenerator();

		SpawnEnemy(new Vector2(200, 200));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SpawnEnemy(Vector2 position) {
		CharacterBody2D newEnemy = ENEMY_SCENE.Instantiate<CharacterBody2D>();
		newEnemy.GlobalPosition = position;
		GetTree().Root.CallDeferred("add_child", newEnemy);
	}

	public void SpawnBloodPool(Vector2 position) {
		Sprite2D bloodpool = BLOOD_POOL_SCENE.Instantiate<Sprite2D>();
		bloodpool.Rotation = rng.RandfRange(0, (float)(2 * Math.PI));
		float scaleFactor = rng.RandfRange(0.05f, 0.15f);
		bloodpool.Scale = new Vector2(scaleFactor, scaleFactor);

		position.X += rng.RandfRange(-bloodSpread/2, bloodSpread/2);
		position.Y += rng.RandfRange(-bloodSpread/2, bloodSpread/2);

		bloodpool.GlobalPosition = position;

		GetTree().Root.AddChild(bloodpool);
	}
}
