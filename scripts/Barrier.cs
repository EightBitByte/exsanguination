// Barrier.cs
//
// Defines a Barrier object that players can clear to access a new area.

using Godot;
using System;

public partial class Barrier : StaticBody2D
{
	[Export]
	public int Cost = 100;

	[Export]
	public string BarrierName = "default";

	private Character player;
	private EnemyManager manager;

	bool playerInBuyArea = false;

	public override void _Ready()
	{
		manager = GetNode<EnemyManager>("/root/main_scene/Enemy Manager");
		player = GetNode<Character>("/root/main_scene/Character");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && player.HasEnoughPoints(Cost) && playerInBuyArea) {
			player.AddPoints(-Cost);
			player.HidePurchaseLabel();
			manager.Call("OpenedArea", BarrierName);
			player.PlaySound("buy");

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
		if (body.Name == "Character") {
			player.ShowPurchaseLabel(Cost);
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
		if (body.Name == "Character") {
			player.HidePurchaseLabel();
			playerInBuyArea = false;
		}
	}
}

