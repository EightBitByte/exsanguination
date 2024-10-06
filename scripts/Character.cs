using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

public partial class Character : CharacterBody2D
{
	const int MILLIS = 1000;

	const int RESERVE = 1;
	const int MAGAZINE = 0;

	[Export]
	public int MaxHP = 100;
	
	[Export]
	private int RegenAmt = 2;
	[Export]
	private int RegenDelay = 100;
	[Export]
	public float Speed = 400;

	[Export]
	///<summary>Amount of time between each bullet fired (in milliseconds)</summary>
	public float RateOfFireMs = 200;

	private float rotation;
	private double viewOffset = Math.PI / 2;

	Sprite2D characterSprite;
	RichTextLabel pointLabel, gunLabel, ammoLabel, purchaseLabel;
	TextureProgressBar reloadBar;
	ShaderMaterial hurtVignette;
	ColorRect vignetteBox;

	PackedScene BULLET_SCENE, ENEMY_SCENE;

	private double firingCooldown = 0;
	private double healCooldown = 3000;
	private double timeSinceLastDamage = 0;
	private int money = 0;
	private int health = 100;
	private Weapon[] heldWeapons = new Weapon[2];
	// Second dimension is ammo in mag, ammo in reserve
	private int[,] ammoCounts = new int[2, 2];
	private List<Weapon> allWeapons = new List<Weapon>(); 
	private int activeWeapon = 0;
	private bool isReloading;

	private double timeSpentReloading = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Load resources
		characterSprite = GetChild<Sprite2D>(0);
		pointLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Point Label");
		gunLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Gun Label");
		ammoLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Ammo Label");
		purchaseLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Purchase Label");
		reloadBar = GetNode<TextureProgressBar>("/root/main_scene/Character/Reload Bar");
		vignetteBox = GetNode<ColorRect>("/root/main_scene/GUI/Vignette");
		hurtVignette = (ShaderMaterial)vignetteBox.Material;

		BULLET_SCENE = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");

		// Set up GUI
		hurtVignette.SetShaderParameter("inner_radius", 1.0);
		vignetteBox.Visible = false;
		reloadBar.Visible = false;
		purchaseLabel.Visible = false;

		// Set up weapons
		LoadWeaponsJson();
		heldWeapons[0] = allWeapons[0];
		activeWeapon = 0;
		RateOfFireMs = 1 / heldWeapons[0].RateOfFire;
		ammoCounts[0, 0] = heldWeapons[0].MagSize;
		ammoCounts[0, 1] = heldWeapons[0].ReserveSize;
		gunLabel.Text = heldWeapons[0].Name;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process (double delta) {
		firingCooldown += delta;

		Vector2 viewportCenter = GetViewportRect().Size / 2;
		Vector2 mousePos = GetViewport().GetMousePosition() - viewportCenter;
		double viewAngle = Math.Atan2(mousePos.Y, mousePos.X);

		characterSprite.Rotation = (float)viewAngle + (float)viewOffset;

		// TODO: Implement semi-auto firing
		if (Input.IsActionPressed("fire") && firingCooldown > RateOfFireMs 
		&& ammoCounts[activeWeapon, MAGAZINE] > 0 && !isReloading) {
			Bullet bullet = BULLET_SCENE.Instantiate<Bullet>();
			bullet.BulletDamage = heldWeapons[activeWeapon].BulletDamage;

			// Position bullet to come out of front of player
			float rotationOffset = characterSprite.Rotation - (float)viewOffset;
			Vector2 bulletOffset = new Vector2(120, 0).Rotated(rotationOffset);
			bullet.Position = Position + bulletOffset;
			bullet.Rotation = rotationOffset;

			ammoCounts[activeWeapon, MAGAZINE] -= 1;
			UpdateAmmo();

			GetTree().Root.AddChild(bullet);
			firingCooldown = 0;
		}

		// Initiate reload
		if (Input.IsActionJustPressed("reload") && ammoCounts[activeWeapon, MAGAZINE] != heldWeapons[activeWeapon].MagSize && !isReloading) {
			GD.Print("Reloading...");
			reloadBar.Value = 0;
			reloadBar.Visible = true;
			isReloading = true;
		}

		if (isReloading && timeSpentReloading < heldWeapons[activeWeapon].ReloadTime) {
			timeSpentReloading += delta;
			reloadBar.Value = timeSpentReloading / heldWeapons[activeWeapon].ReloadTime * 100;

		// Finish reload
		} else if (isReloading) {
			// If reserve is less than mag size, reload up to reserve, otherwise fill mag
			int amtToReload =  Math.Min(ammoCounts[activeWeapon, RESERVE], heldWeapons[activeWeapon].MagSize - ammoCounts[activeWeapon, MAGAZINE]);
			ammoCounts[activeWeapon, MAGAZINE] += amtToReload;

			// Subtract reloaded ammo from the reserve
			ammoCounts[activeWeapon, RESERVE] -= amtToReload;

			// Update HUD
			UpdateAmmo();
			reloadBar.Visible = false;

			isReloading = false;
			timeSpentReloading = 0;
		}

		//TODO: Keep in mind swapping weapons mid-reload, don't want to speed up reload, be sure to cancel it

	}

    public override void _PhysicsProcess (double delta) {
		Vector2 moveDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		timeSinceLastDamage += delta;

		if (timeSinceLastDamage > healCooldown / MILLIS && health < 100) {
			health = health + RegenAmt > MaxHP ? MaxHP : health + RegenAmt;
			AdjustVignette();
			timeSinceLastDamage = (healCooldown - RegenDelay) / MILLIS;
		}
		
		Velocity = moveDirection * Speed;
		MoveAndSlide();
	}

	public void AddPoints (int points) {
		money += points;
		string cents = money % 100 == 0 ? "00" : (money % 100).ToString();
		pointLabel.Text = $"${money/100}.{cents}";
	}

	public void Hurt (int attackDmg) {
		health = health < attackDmg ? 0 : health - attackDmg;
		AdjustVignette();
		
		timeSinceLastDamage = 0;
	}

	// 1 is not visible, 0 is full strength
	public void AdjustVignette () {
		double percentHP = 1.0 * health / MaxHP;
		double strength;

		if (percentHP < 0.34) {
			strength = 0.1;
		} else {
			strength = 0.01 * health - 0.274;
		}

		if (percentHP == 1.0) {
			vignetteBox.Visible = false;
		} else {
			vignetteBox.Visible = true;
		}

		hurtVignette.SetShaderParameter("inner_radius", strength);
	}

	private void LoadWeaponsJson() {
		Godot.Collections.Dictionary<String, Godot.Collections.Dictionary<String, String>> jsonDict = (Godot.Collections.Dictionary<String, Godot.Collections.Dictionary<string, string>>)Json.ParseString(System.IO.File.ReadAllText("data/weapons.json"));

		foreach (System.Collections.Generic.KeyValuePair<String, Godot.Collections.Dictionary<String, String>> pair in jsonDict) {
			Godot.Collections.Dictionary<string, string> weaponDict = pair.Value;

			allWeapons.Add(new Weapon(weaponDict));
			GD.Print(new Weapon(weaponDict));
		}
	}

	private void UpdateAmmo() {
		ammoLabel.Text = $"{ammoCounts[activeWeapon, MAGAZINE]} / {ammoCounts[activeWeapon, RESERVE]}";
	}

	public void ShowPurchaseLabel(int purchaseAmt) {
		string cents = purchaseAmt % 100 == 0 ? "00" : (purchaseAmt % 100).ToString();

		purchaseLabel.Text = $"[F] Clear for ${purchaseAmt/100}.{cents}";
		purchaseLabel.Visible = true;
	}
	
	public void HidePurchaseLabel() {
		purchaseLabel.Visible = false;
	}

	public bool HasEnoughMoney(int cost) {
		return money >= cost;
	}


}
