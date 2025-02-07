// Barrier.cs
//
// Defines a Barrier object that players can clear to access a new area.

using Godot;
using System;

/// <summary>
/// Defines an area within the map that can be accessed.
/// </summary>
public enum Area {
	SpawnRoom,
	OuterHallwayI,
	OuterHallwayII
}

public partial class Barrier : StaticBody2D
{
	[Signal]
	public delegate void AreaOpenedEventHandler(Area area);

	[Export]
	public int Cost = 100;

	[Export]
	public int[] OpensAreas = {-1};

	private Player player;
	private EnemyManager EManager;
	private GUIManager GManager;
	private AudioManager AManager;

	bool playerInBuyArea = false;

	SharedData Global;

	public override void _Ready()
	{
		Global = SharedData.Instance;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && player.HasEnoughPoints(Cost) && playerInBuyArea) {
			SharedData.Instance.EmitSignal(SharedData.SignalName.ModifyPoints, -Cost);
			SharedData.Instance.EmitSignal(SharedData.SignalName.HideActiveLabel);
			SharedData.Instance.EmitSignal(SharedData.SignalName.PlaySound, (int)Sound.Buy);

			QueueFree();
		}
	}

	/// <summary>
	/// Called whenever a Node2D enters the buy area. Shows the purchase label 
	/// when a player enters the area.
	/// </summary>
	/// <param name="body">The `Node2D` that entered.</param>
	private void OnBuyAreaEntered(Node2D body)
	{
		// TODO: This seems a little odd, is there anyway we can not have to check 
		// this in this manner? It seems not very flexible to change.
		if (body.Name == "Player") {
			SharedData.Instance.EmitSignal(SharedData.SignalName.ShowPurchaseLabel, Cost);
			playerInBuyArea = true;
		}
	}

	/// <summary>
	/// Called whenever a Node2D leaves the buy area for this barrier. Hides the 
	/// purchase label when a player leaves the area.
	/// </summary>
	/// <param name="body"></param>
	private void OnBuyAreaExited(Node2D body)
	{
		if (body.Name == "Player") {
			SharedData.Instance.EmitSignal(SharedData.SignalName.HideActiveLabel);
			playerInBuyArea = false;
		}
	}
}

