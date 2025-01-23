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

	PackedScene ENEMY_SCENE;
	RandomNumberGenerator rng;
	RichTextLabel RoundLabel;
	Timer spawnTimer, safeTimer;
	NodeArray enabledSpawnPoints = new();

	Player player;

	// Upon clearing the barricade at `key`, unlocks the enemy spawn points at 
	// `value`.
	// NOTE: Maybe defer to JSON data?
	private readonly Dictionary<string, string[]> unlocks = new() {
		{"Spawn Room", new string[] {"Outer Hallway I", "Outer Hallway II"}},
	};


	public override void _Ready () {
		// Load resources
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		spawnTimer = GetNode<Timer>("./Spawn Timer");
		safeTimer = GetNode<Timer>("./Safe Timer");
		RoundLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Round Label");
		player = GetNode<Player>("/root/main_scene/Player");

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

		newEnemy.EnemyInjured += player.AddPoints;
		newEnemy.AttackedPlayer += player.Hurt;
		GetTree().Root.CallDeferred("add_child", newEnemy);
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



