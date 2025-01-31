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

	public override void _Ready()
	{
		Node2D SceneNode = SharedData.Instance.GetRootSceneNode();
		EManager = SceneNode.GetNode<EnemyManager>("./Enemy Manager");
		GManager = SceneNode.GetNode<GUIManager>("./GUI Manager");
		AManager = SceneNode.GetNode<AudioManager>("./Audio Manager");
		player = SceneNode.GetNode<Player>("./Player");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && player.HasEnoughPoints(Cost) && playerInBuyArea) {
			player.AddPoints(-Cost);
			GManager.HidePurchaseLabel();
			AManager.PlaySound(Sound.Buy);

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
			GManager.ShowPurchaseLabel(Cost);
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
			GManager.HidePurchaseLabel();
			playerInBuyArea = false;
		}
	}
}

