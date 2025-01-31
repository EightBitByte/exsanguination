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
	private TextureRect ShopGUI;
	private Player player;

	private GUIManager GManager;
	private AudioManager AManager;

	private bool playerInShopArea = false;

	public override void _Ready()
	{
		Node2D SceneNode = SharedData.Instance.GetRootSceneNode();
		player = SceneNode.GetNode<Player>("Player");
		ShopGUI = SceneNode.GetNode<TextureRect>("GUI/Shop");

		GManager = SceneNode.GetNode<GUIManager>("GUI Manager");
		AManager = SceneNode.GetNode<AudioManager>("Audio Manager");

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
			ShopGUI.Visible = true;
			GManager.HideLabel();
		}

		if (ShopGUI.Visible) {
			SetActiveStatusOfButton(500, Buys[0]);
			SetActiveStatusOfButton(1000, Buys[1]);
			SetActiveStatusOfButton(2000, Buys[2]);

			if (Input.IsActionJustPressed("exit")) {
				ShopGUI.Visible = false;
				GManager.ShowLabel("[F] Open Shop");
				player.shootingEnabled = true;
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
		button.Disabled = !player.HasEnoughPoints(cost);
	}

	private void OnShopEnter(Node2D body) {
		if (body.Name == "Player") {
			GManager.ShowLabel("[F] Open Shop");
			player.shootingEnabled = false;
			playerInShopArea = true;
		}
	}

	private void OnShopExit(Node2D body) {
		if (body.Name == "Player") {
			playerInShopArea = false;
			GManager.HideLabel();
			player.shootingEnabled = true;
			ShopGUI.Visible = false;
		}
	}

	private void PlayerBuys(int buttonIdx) {
		switch (buttonIdx) {
			case 0:
				player.GiveWeapon(WeaponId.ColtM1911);
				player.AddPoints(-500);
				break;
			case 1:
				player.GiveWeapon(WeaponId.M4Carbine);
				player.AddPoints(-1000);
				break;
			case 2:
				// TODO: Give the player a cure. Preferably, through a signal.
				player.AddPoints(-2000);
				break;
		}

		AManager.PlaySound(Sound.Buy);
	}
}


