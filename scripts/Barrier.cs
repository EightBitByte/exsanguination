using Godot;
using System;

public partial class Barrier : StaticBody2D
{
	[Export]
	public int cost = 100;

	Character player;

	bool playerInBuyArea = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Character>("/root/main_scene/Character");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && player.HasEnoughMoney(cost)) {
			player.AddPoints(-cost);
			player.HidePurchaseLabel();
			QueueFree();

		}
	}

	private void OnBuyAreaEntered(Node2D body)
	{
		if (body.Name == "Character") {
			player.ShowPurchaseLabel(cost);
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

