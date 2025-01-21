// Cure.cs
//
// Implements the cure object, which delays the infection, for now.

using Godot;
using System;

public partial class Cure : Area2D
{
	Character player;
	private bool playerInCureArea = false;

	public override void _Ready()
	{
		// TODO: Should be call to manager.
		player = GetNode<Character>("/root/main_scene/Character");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInCureArea) {
			player.ResetInfection();
			// TODO: These two should be GUI and Audio manager calls.
			player.HideLabel();
			player.PlaySound("pill");
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
		if (body.Name == "Character") {
			playerInCureArea = false;
			player.HideLabel();
		}
	}
}


