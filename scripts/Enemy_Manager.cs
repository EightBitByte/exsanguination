using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Enemy_Manager : Node2D
{
	[Export]
	float bloodSpread = 50;

	PackedScene ENEMY_SCENE, BLOOD_POOL_SCENE;
	RandomNumberGenerator rng;
	Timer spawnTimer;

	Godot.Collections.Array<Node2D> enabledSpawnPoints = new();

	private int round;

	private Dictionary<string, string[]> unlocks = new() {
		{"Spawn Room", new string[] {"Outer Hallway I", "Outer Hallway II"}},
	};

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){
		// Load resources
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		BLOOD_POOL_SCENE = GD.Load<PackedScene>("res://scenes/blood_pool.tscn");
		spawnTimer = GetNode<Timer>("./Spawn Timer");
		rng = new();
		rng.Randomize();

		// Load spawn point
		enabledSpawnPoints.Add(GetNode<Node2D>("./Spawn Room"));
	}

	/// <summary>
	/// Spawns an enemy at position <c>position</c>.
	/// </summary>
	public void SpawnEnemy(Vector2 position) {
		GD.Print("Spawning enemy at ", position);
		CharacterBody2D newEnemy = ENEMY_SCENE.Instantiate<CharacterBody2D>();
		newEnemy.GlobalPosition = position;
		GetTree().Root.CallDeferred("add_child", newEnemy);
	}

	/// <summary>
	/// Called by enemies when they are hit to spawn a pool of blood.
	/// </summary>
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

	/// <summary>
	/// Called by the barrier upon purchase to open up new enemy spawns.
	/// </summary>
	public void OpenedArea(string barrierName) {
		if (barrierName != "default")
			foreach (string spawnpointName in unlocks[barrierName])
				enabledSpawnPoints.Add(GetNode<Node2D>($"./{spawnpointName}"));
	}

	/// <summary>
	/// When the timer times out, spawn a new enemy
	/// </summary>
	private void OnSpawnTimerTick() {
		GD.Print("Spawn Timer Ticked");
		int chosenSpawn = rng.RandiRange(0, enabledSpawnPoints.Count-1);
		SpawnEnemy(enabledSpawnPoints[chosenSpawn].Position);
	}
}


