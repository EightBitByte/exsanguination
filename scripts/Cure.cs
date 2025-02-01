// Cure.cs
//
// Implements the cure object, which delays the infection, for now.

using Godot;
using System;

public partial class Cure : Area2D
{

	private bool playerInCureArea = false;

	private SharedData Global;

	public override void _Ready()
	{
		Global = SharedData.Instance;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInCureArea) {
			EmitSignal(SharedData.SignalName.CureConsume);
			EmitSignal(SharedData.SignalName.HideActiveLabel);
			QueueFree();
		}
	}

	private void OnPlayerEnteredCureArea(Node2D body)
	{
		if (body.Name == "Player") {
			EmitSignal(SharedData.SignalName.ShowActiveLabel, "[F] Stabilize Infection");
			playerInCureArea = true;
		}
	}


	private void OnPlayerExitedCureArea(Node2D body)
	{
		if (body.Name == "Player") {
			playerInCureArea = false;
			EmitSignal(SharedData.SignalName.HideActiveLabel);
		}
	}
}


