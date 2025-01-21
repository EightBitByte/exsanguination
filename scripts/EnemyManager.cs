// EnemyManager.cs
//
// Defines a Enemy Manager Node, which is in charge of managing enemy spawn 
// events, enemy spawn limits, and the amount of health each 
// enemy spawns with.

using Godot;
using System;
using System.Collections.Generic;

using NodeArray = Godot.Collections.Array<Godot.Node2D>;

public partial class EnemyManager : Node2D
{
	// TODO: Shouldn't enemies be in charge of spawning their own blood? Just a thought.
	[Export]
	private float BloodSpread = 50;

	[Export]
	private int Round = 1;

	[Export]
	private int SpawnLimit = 5;			// Max infected on map at one time

	[Export]
	private int StartingInfectedHP = 30;

	public int infectedAlive = 0; 		
	private int infectedSpawned = 0;
	public int KilledInfected = 0;		
	private int infectedHealth;
	private double infectedHealthMultiplier = 1.1;

	PackedScene ENEMY_SCENE, BLOOD_POOL_SCENE;
	RandomNumberGenerator rng;
	RichTextLabel RoundLabel;
	Timer spawnTimer, safeTimer;
	NodeArray enabledSpawnPoints = new();

	// Upon clearing the barricade at `key`, unlocks the enemy spawn points at 
	// `value`.
	// NOTE: Maybe defer to JSON data?
	private readonly Dictionary<string, string[]> unlocks = new() {
		{"Spawn Room", new string[] {"Outer Hallway I", "Outer Hallway II"}},
	};


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
		// NOTE: Possible bug here, need to make sure all infected dead before ending round
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
	/// <param name="position">The global position at which to spawn the enemy.</param>
	/// </summary>
	public void SpawnEnemy (Vector2 position) {
		Enemy newEnemy = ENEMY_SCENE.Instantiate<Enemy>();
		newEnemy.GlobalPosition = position;
		newEnemy.MaxHP = infectedHealth;
		GetTree().Root.CallDeferred("add_child", newEnemy);
	}


	// TODO: Refactor so that enemies are in charge of this.
	/// <summary>
	/// Called by enemies when they are hit to spawn a pool of blood.
	/// </summary>
	/// <param name="position">The global position at which to spawn the blood pool.</param>
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
	/// <param name="barrierName">The name of the barrier that was unlocked.</param>
	public void OpenedArea (string barrierName) {
		if (barrierName != "default")
			foreach (string spawnpointName in unlocks[barrierName])
				enabledSpawnPoints.Add(GetNode<Node2D>($"./{spawnpointName}"));
	}


	/// <summary>
	/// Called when the timer times out. Spawns a new enemy.
	/// </summary>
	private void OnSpawnTimerTick () {
		// If the amount spawned doesn't exceed the spawn limit, spawn
		if (infectedAlive < SpawnLimit && infectedSpawned < Round * 1.5 + 5) {
			++infectedAlive;
			++infectedSpawned;

			// TODO: Instead of just choosing a random spawn, perhaps choose the one closest to the player?
			int chosenSpawn = rng.RandiRange(0, enabledSpawnPoints.Count-1);
			SpawnEnemy(enabledSpawnPoints[chosenSpawn].Position);

			GD.Print($"DEBUG: Spawning enemy at {enabledSpawnPoints[chosenSpawn].Position},"
						+ $"({enabledSpawnPoints[chosenSpawn].Name})");
		}
	}

	/// <summary>
	/// Called when the safe timer times out. Starts the next round.
	/// </summary>
	private void OnSafeTimerTick()
	{
		RoundLabel.Text = $"Round {Round}";
		infectedHealth = (int)(infectedHealth * infectedHealthMultiplier);

		safeTimer.Stop();
		spawnTimer.Start();
	}
}



