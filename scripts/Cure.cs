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
		Node2D SceneNode = SharedData.Instance.GetRootSceneNode();
		AManager = SceneNode.GetNode<AudioManager>("./Audio Manager");
		GManager = SceneNode.GetNode<GUIManager>("./GUI Manager");
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
		if (body.Name == "Player") {
			GManager.ShowLabel("[F] Stabilize Infection");
			playerInCureArea = true;
		}
	}


	private void OnPlayerExitedCureArea(Node2D body)
	{
		if (body.Name == "Player") {
			playerInCureArea = false;
			GManager.HideLabel();
		}
	}
}


