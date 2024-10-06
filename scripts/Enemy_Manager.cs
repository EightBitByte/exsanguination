using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Enemy_Manager : Node2D
{
	[Export]
	private float BloodSpread = 50;
	[Export]
	private int Round = 1;
	[Export]
	// Max infected on map at one time
	private int SpawnLimit = 5;
	// Active infected on map right now
	[Export]
	private int StartingInfectedHP = 30;
	public int infectedActive = 0;
	// Total infected spawned this Round
	private int infectedSpawned = 0;
	// Infected killed this Round
	public int KilledInfected = 0;
	// Infected health
	private int infectedHealth;
	// Infected health multiplies by this every round
	private double healthMultiplier;


	PackedScene ENEMY_SCENE, BLOOD_POOL_SCENE;
	RandomNumberGenerator rng;
	RichTextLabel RoundLabel;
	Timer spawnTimer, safeTimer;
	Godot.Collections.Array<Node2D> enabledSpawnPoints = new();


	private Dictionary<string, string[]> unlocks = new() {
		{"Spawn Room", new string[] {"Outer Hallway I", "Outer Hallway II"}},
	};

	// Called when the node enters the scene tree for the first time.
	public override void _Ready () {
		// Load resources
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		BLOOD_POOL_SCENE = GD.Load<PackedScene>("res://scenes/blood_pool.tscn");
		spawnTimer = GetNode<Timer>("./Spawn Timer");
		safeTimer = GetNode<Timer>("./Safe Timer");
		RoundLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Round Label");
		rng = new();
		rng.Randomize();

		// Load spawn point
		enabledSpawnPoints.Add(GetNode<Node2D>("./Spawn Room"));
		RoundLabel.Text = "Round 1";

		// Set zombie parameters
		// NOTE: Maybe add speed?
		infectedHealth = StartingInfectedHP;
	}

	public override void _Process (double delta) {
		// If killed amount to spawn this Round, go into safe mode
		if (KilledInfected >= Round * 1.5 + 5) {
			KilledInfected = 0;
			infectedSpawned = 0;
			++Round;

			RoundLabel.Text = $"Round {Round} (safe)";
			spawnTimer.Stop();
			safeTimer.Start();
		}
	}

	/// <summary>
	/// Spawns an enemy at position <c>position</c>.
	/// </summary>
	public void SpawnEnemy (Vector2 position) {
		Enemy newEnemy = ENEMY_SCENE.Instantiate<Enemy>();
		newEnemy.GlobalPosition = position;
		newEnemy.MaxHP = infectedHealth;
		GetTree().Root.CallDeferred("add_child", newEnemy);
	}

	/// <summary>
	/// Called by enemies when they are hit to spawn a pool of blood.
	/// </summary>
	public void SpawnBloodPool (Vector2 position) {
		Sprite2D bloodpool = BLOOD_POOL_SCENE.Instantiate<Sprite2D>();
		bloodpool.Rotation = rng.RandfRange(0, (float)(2 * Math.PI));
		float scaleFactor = rng.RandfRange(0.05f, 0.15f);
		bloodpool.Scale = new Vector2(scaleFactor, scaleFactor);

		position.X += rng.RandfRange(-BloodSpread/2, BloodSpread/2);
		position.Y += rng.RandfRange(-BloodSpread/2, BloodSpread/2);

		bloodpool.GlobalPosition = position;

		GetTree().Root.AddChild(bloodpool);
	}

	/// <summary>
	/// Called by the barrier upon purchase to open up new enemy spawns.
	/// </summary>
	public void OpenedArea (string barrierName) {
		if (barrierName != "default")
			foreach (string spawnpointName in unlocks[barrierName])
				enabledSpawnPoints.Add(GetNode<Node2D>($"./{spawnpointName}"));
	}

	/// <summary>
	/// When the timer times out, spawn a new enemy
	/// </summary>
	private void OnSpawnTimerTick () {
		// If the amount spawned doesn't exceed the spawn limit, spawn
		if (infectedActive < SpawnLimit && infectedSpawned < Round * 1.5 + 5) {
			++infectedActive;
			++infectedSpawned;

			int chosenSpawn = rng.RandiRange(0, enabledSpawnPoints.Count-1);
			SpawnEnemy(enabledSpawnPoints[chosenSpawn].Position);

			GD.Print($"Spawning enemy at {enabledSpawnPoints[chosenSpawn].Position}, ({enabledSpawnPoints[chosenSpawn].Name})");
		}
	}

	/// <summary>
	/// When the safe timer times out, start the next Round
	/// </summary>
	private void OnSafeTimerTick()
	{
		RoundLabel.Text = $"Round {Round}";
		infectedHealth = (int)(infectedHealth * healthMultiplier);

		safeTimer.Stop();
		spawnTimer.Start();
	}
}



