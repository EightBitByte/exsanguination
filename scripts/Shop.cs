// Shop.cs
//
// Implements the shop area, where the player can buy guns, ammo, and cures.

using Godot;
using System;
using System.Collections.Generic;

public partial class Shop : Area2D
{
	// TODO: Make the shop dynamically update (add JSON?)
	private TextureButton[] Buys = new TextureButton[3];

	private bool playerInShopArea = false;
	private SharedData Global;

	public override void _Ready()
	{
		Node2D SceneNode = SharedData.Instance.GetRootSceneNode();
		Global = SharedData.Instance;

		Buys[0] = SceneNode.GetNode<TextureButton>("GUI/Shop/Weapon I/Button");
		Buys[1] = SceneNode.GetNode<TextureButton>("GUI/Shop/Weapon II/Button");
		Buys[2] = SceneNode.GetNode<TextureButton>("GUI/Shop/Weapon III/Button");

		Buys[0].Pressed += () => {PlayerBuys(0);};
		Buys[1].Pressed += () => {PlayerBuys(1);};
		Buys[2].Pressed += () => {PlayerBuys(2);};
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInShopArea) {
			Global.ShopGUI.Visible = true;
			EmitSignal(SharedData.SignalName.HideActiveLabel);
		}

		if (Global.ShopGUI.Visible) {
			SetActiveStatusOfButton(500, Buys[0]);
			SetActiveStatusOfButton(1000, Buys[1]);
			SetActiveStatusOfButton(2000, Buys[2]);

			if (Input.IsActionJustPressed("exit")) {
				Global.ShopGUI.Visible = false;
				EmitSignal(SharedData.SignalName.ShowActiveLabel, "[F] Open Shop");
				EmitSignal(SharedData.SignalName.ToggleShooting, true);
			}
		}
	}

	/// <summary>
	/// Enables/disables buttons if the player has enough money to purchase the 
	/// item.
	/// </summary>
	/// <param name="cost">The number of points the player must have to purchase this item.</param>
	/// <param name="button">A reference to the button to disable/enable.</param>
	private void SetActiveStatusOfButton(int cost, TextureButton button) {
		button.Disabled = !Global.PlayerNode.HasEnoughPoints(cost);
	}

	private void OnShopEnter(Node2D body) {
		if (body.Name == "Player") {
			EmitSignal(SharedData.SignalName.ShowActiveLabel, "[F] Open Shop");
			EmitSignal(SharedData.SignalName.ToggleShooting, false);
			playerInShopArea = true;
		}
	}

	private void OnShopExit(Node2D body) {
		if (body.Name == "Player") {
			playerInShopArea = false;
			EmitSignal(SharedData.SignalName.HideActiveLabel);
			EmitSignal(SharedData.SignalName.ToggleShooting, true);
			Global.ShopGUI.Visible = false;
		}
	}

	private void PlayerBuys(int buttonIdx) {
		switch (buttonIdx) {
			case 0:
				EmitSignal(SharedData.SignalName.GiveWeapon, 
							(int)WeaponId.ColtM1911);
				EmitSignal(SharedData.SignalName.ModifyPoints, -500);
				break;
			case 1:
				EmitSignal(SharedData.SignalName.GiveWeapon, 
							(int)WeaponId.M4Carbine);
				EmitSignal(SharedData.SignalName.ModifyPoints, -1000);
				break;
			case 2:
				EmitSignal(SharedData.SignalName.CureConsume);
				EmitSignal(SharedData.SignalName.ModifyPoints, -2000);
				break;
		}

		EmitSignal(SharedData.SignalName.PlaySound, (int)Sound.Buy);
	}
}


