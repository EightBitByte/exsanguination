// Cure.cs
//
// Implements the cure object, which delays the infection, for now.

using Godot;
using System;

public partial class Cure : Area2D
{
	[Signal]
	public delegate void CureConsumeEventHandler();

	private bool playerInCureArea = false;
	private AudioManager AManager;
	private GUIManager GManager;

	public override void _Ready()
	{
		AManager = GetNode<AudioManager>("/root/main_scene/Audio Manager");
		GManager = GetNode<GUIManager>("/root/main_scene/GUI Manager");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInCureArea) {
			EmitSignal(SignalName.CureConsume);
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


