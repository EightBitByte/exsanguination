using Godot;
using System;
using System.Collections.Generic;

public partial class Shop : Area2D
{
	TextureButton[] Buys = new TextureButton[3];
	TextureRect ShopGUI;
	Character player;

	private bool playerInShopArea = false;

	// Called when the node enters the scene tree for the first time.
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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("buy") && playerInShopArea) {
			ShopGUI.Visible = true;
			player.HideLabel();
		}

		if (ShopGUI.Visible) {
			SetButtonMeetsThreshold(500, Buys[0]);
			SetButtonMeetsThreshold(1000, Buys[1]);
			SetButtonMeetsThreshold(2000, Buys[2]);

			if (Input.IsActionJustPressed("exit")) {
				ShopGUI.Visible = false;
				player.ShowLabel("[F] Open Shop");
				player.shootingEnabled = true;
			}
		}
	}

	private void SetButtonMeetsThreshold(int threshold, TextureButton button) {
		if (player.HasEnoughMoney(threshold))
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


