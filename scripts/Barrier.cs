using Godot;
using System;

public partial class Barrier : StaticBody2D
{
	[Export]
	public int Cost = 100;

	[Export]
	public string BarrierName = "default";

	Character player;
	Enemy_Manager manager;

	bool playerInBuyArea = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		manager = GetNode<Enemy_Manager>("/root/main_scene/Enemy Manager");
		player = GetNode<Character>("/root/main_scene/Character");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && player.HasEnoughMoney(Cost) && playerInBuyArea) {
			player.AddPoints(-Cost);
			player.HidePurchaseLabel();
			manager.Call("OpenedArea", BarrierName);
			player.PlaySound("buy");

			QueueFree();
		}
	}

	private void OnBuyAreaEntered(Node2D body)
	{
		if (body.Name == "Character") {
			player.ShowPurchaseLabel(Cost);
			playerInBuyArea = true;
		}
	}


	private void OnBuyAreaExited(Node2D body)
	{
		if (body.Name == "Character") {
			player.HidePurchaseLabel();
			playerInBuyArea = false;
		}
	}
}

