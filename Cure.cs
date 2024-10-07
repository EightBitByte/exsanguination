using Godot;
using System;

public partial class Cure : Area2D
{
	Character player;
	private bool playerInCureArea = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Character>("/root/main_scene/Character");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInCureArea) {
			player.ResetInfection();
			player.HideLabel();
			QueueFree();
		}
	}

	private void OnPlayerEnteredCureArea(Node2D body)
	{
		if (body.Name == "Character") {
			player.ShowLabel("[F] Stabilize Infection");
			playerInCureArea = true;
		}
	}


	private void OnPlayerExitedCureArea(Node2D body)
	{
		playerInCureArea = false;
	}
}


