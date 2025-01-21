// Shop.cs
//
// Implements the shop area, where the player can buy guns, ammo, and cures.

using Godot;
using System;
using System.Collections.Generic;

public partial class Shop : Area2D
{
	// TODO: Make the shop dynamically update (add JSON?)
	TextureButton[] Buys = new TextureButton[3];
	TextureRect ShopGUI;
	Character player;

	private bool playerInShopArea = false;

	public override void _Ready()
	{
		player = GetNode<Character>("/root/main_scene/Character");
		ShopGUI = GetNode<TextureRect>("/root/main_scene/GUI/Shop");

		Buys[0] = GetNode<TextureButton>("/root/main_scene/GUI/Shop/Weapon I/Button");
		Buys[1] = GetNode<TextureButton>("/root/main_scene/GUI/Shop/Weapon II/Button");
		Buys[2] = GetNode<TextureButton>("/root/main_scene/GUI/Shop/Weapon III/Button");

		Buys[0].Pressed += () => {PlayerBuys(0);};
		Buys[1].Pressed += () => {PlayerBuys(1);};
		Buys[2].Pressed += () => {PlayerBuys(2);};
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInShopArea) {
			ShopGUI.Visible = true;
			player.HideLabel();
		}

		if (ShopGUI.Visible) {
			SetActiveStatusOfButton(500, Buys[0]);
			SetActiveStatusOfButton(1000, Buys[1]);
			SetActiveStatusOfButton(2000, Buys[2]);

			if (Input.IsActionJustPressed("exit")) {
				ShopGUI.Visible = false;
				player.ShowLabel("[F] Open Shop");
				player.shootingEnabled = true;
			}
		}
	}

	/// <summary>
	/// Enables/disables buttons if the player has enough money to purchase the 
	/// item.
	/// </summary>
	/// <param name="threshold"></param>
	/// <param name="button"></param>
	private void SetActiveStatusOfButton(int cost, TextureButton button) {
		if (player.HasEnoughMoney(cost))
			button.Disabled = false;
		else
			button.Disabled = true;
	}

	private void OnShopEnter(Node2D body) {
		if (body.Name == "Character") {
			player.ShowLabel("[F] Open Shop");
			player.shootingEnabled = false;
			playerInShopArea = true;
		}
	}

	private void OnShopExit(Node2D body) {
		if (body.Name == "Character") {
			playerInShopArea = false;
			player.HideLabel();
			player.shootingEnabled = true;
			ShopGUI.Visible = false;
		}
	}

	private void PlayerBuys(int buttonIdx) {
		switch (buttonIdx) {
			case 0:
				player.GiveWeapon(buttonIdx);
				player.AddPoints(-500);
				break;
			case 1:
				player.GiveWeapon(buttonIdx);
				player.AddPoints(-1000);
				break;
			case 2:
				player.ResetInfection();
				player.AddPoints(-2000);
				break;
		}

		player.PlaySound("buy");
	}
}


