// EnemyManager.cs
//
// Defines a Enemy Manager Node, which is in charge of managing enemy spawn 
// events, enemy spawn limits, and the amount of health each 
// enemy spawns with.

using Godot;
using System;
using System.Collections.Generic;

using NodeArray = Godot.Collections.Array<Godot.Node2D>;

public partial class EnemyManager : Node
{
	[Export]
	private int Round = 1;

	[Export]
	private int MaxInfectedActive = 5;			// Max infected on map at one time

	[Export]
	private int StartingInfectedHP = 30;

	private int infectedAlive = 0; 		
	private int infectedSpawned = 0;
	private int killedInfected = 0;		
	private int currentInfectedHealth;	// Infected health on spawn
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
	private readonly Dictionary<Area, Area[]> unlocks = new() {
		{Area.SpawnRoom, new Area[] {Area.OuterHallwayI, Area.OuterHallwayII}},
	};


	public override void _Ready () {
		// Load resources
		Node2D SceneNode = SharedData.Instance.GetRootSceneNode();
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		spawnTimer = GetNode<Timer>("./Spawn Timer");
		safeTimer = GetNode<Timer>("./Safe Timer");
		RoundLabel = SceneNode.GetNode<RichTextLabel>("./GUI/Round Label");
		player = SceneNode.GetNode<Player>("./Player");

		rng = new();
		rng.Randomize();

		// Load spawn point
		enabledSpawnPoints.Add(GetNode<Node2D>("./Spawn Room"));
		RoundLabel.Text = "Round 1";

		// Set zombie parameters
		// NOTE: Maybe add speed?
		currentInfectedHealth = StartingInfectedHP;
	}


	public override void _Process (double delta) {
		// If killed amount to spawn this Round, go into safe mode
		if (killedInfected >= Round * 1.5 + 5) {
			killedInfected = 0;
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
		newEnemy.MaxHP = currentInfectedHealth;

		newEnemy.EnemyInjured += player.AddPoints;
		newEnemy.AttackedPlayer += player.Hurt;
		newEnemy.EnemyKilled += OnEnemyKill;
		GetTree().Root.CallDeferred("add_child", newEnemy);
	}


	public void OpenedArea (Area barrier) {
		if ((int)barrier != -1)
			foreach (Area spawnpointName in unlocks[barrier])
				enabledSpawnPoints.Add(GetNode<Node2D>($"./{spawnpointName}"));
	}


	/// <summary>
	/// Called when the timer times out. Spawns a new enemy.
	/// </summary>
	private void OnSpawnTimerTick () {
		// If the amount spawned doesn't exceed the spawn limit, spawn
		if (infectedAlive < MaxInfectedActive && ReachedInfectedSpawnLimit()) {
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
	private void OnSafeTimerTick() {
		RoundLabel.Text = $"Round {Round}";
		currentInfectedHealth = (int)(currentInfectedHealth * infectedHealthMultiplier);

		safeTimer.Stop();
		spawnTimer.Start();
	}


	private void OnEnemyKill() {
		--infectedAlive;
		++killedInfected;
	}

	
	private bool ReachedInfectedSpawnLimit() {
		return infectedSpawned < Round * 1.5 + 5;
	}
}