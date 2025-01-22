// Cure.cs
//
// Implements the cure object, which delays the infection, for now.

using Godot;
using System;

public partial class Cure : Area2D
{
	private bool playerInCureArea = false;
	private AudioManager AManager;
	private GUIManager GManager;

	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInCureArea) {
			// TODO: Make this a signal.
			player.ResetInfection();
			GManager.HideLabel();
			AManager.PlaySound(Sound.Pill);
			QueueFree();
		}
	}

	private void OnPlayerEnteredCureArea(Node2D body)
	{
		if (body.Name == "Character") {
			GManager.ShowLabel("[F] Stabilize Infection");
			playerInCureArea = true;
		}
	}


	private void OnPlayerExitedCureArea(Node2D body)
	{
		if (body.Name == "Character") {
			playerInCureArea = false;
			GManager.HideLabel();
		}
	}
}


