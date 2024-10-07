using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Principal;

public partial class Character : CharacterBody2D
{
	const int MILLIS = 1000;

	const int RESERVE = 1, MAGAZINE = 0;

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

	Sprite2D characterSprite, weaponSprite, underarmSprite;
	RichTextLabel pointLabel, gunLabel, ammoLabel, purchaseLabel;
	TextureProgressBar reloadBar, infectionBar;
	ShaderMaterial hurtVignette;
	ColorRect vignetteBox;
	Texture2D pistolStance, rifleStance, unarmedStance;
	AudioStreamPlayer2D[] audioManagers = new AudioStreamPlayer2D[10];
	AudioStreamWav pistolShot, rifleShot, dryFire;
	AudioStreamMP3 pistolReload, rifleReload;
	PackedScene BULLET_SCENE, ENEMY_SCENE;

	private double firingCooldown = 0;
	private double healCooldown = 3000;
	private double timeSinceLastDamage = 0;
	public int money = 0;
	private int health = 100;
	private float infectionPercent = 0f;
	private Weapon[] heldWeapons = new Weapon[2];
	// Second dimension is ammo in mag, ammo in reserve
	private int[,] ammoCounts = new int[2, 2];
	private List<Weapon> allWeapons = new List<Weapon>(); 
	private int activeWeapon = 0;
	private bool isReloading;
	private double timeSpentReloading = 0;

	private int currentAudioPlayer = 0;

	private static Vector2 pistolPos = new(100, -372), riflePos = new(108, -260);
	private static Vector2 pistolScale = new(0.25f, -0.25f), rifleScale = new(0.75f, -0.75f);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Load resources
		characterSprite = GetChild<Sprite2D>(0);
		weaponSprite = GetNode<Sprite2D>("/root/main_scene/Character/Character Sprite/Weapon Sprite");
		underarmSprite = GetNode<Sprite2D>("/root/main_scene/Character/Character Sprite/Underarm");
		pistolStance = GD.Load<Texture2D>("res://assets/Character (Pistol).svg");
		rifleStance = GD.Load<Texture2D>("res://assets/Character (Rifle).svg");
		unarmedStance = GD.Load<Texture2D>("res://assets/Character (Unarmed).svg");

		pointLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Point Label");
		gunLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Gun Label");
		ammoLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Ammo Label");
		purchaseLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Purchase Label");

		reloadBar = GetNode<TextureProgressBar>("/root/main_scene/Character/Reload Bar");
		infectionBar = GetNode<TextureProgressBar>("/root/main_scene/GUI/Infection Bar");
		vignetteBox = GetNode<ColorRect>("/root/main_scene/GUI/Vignette");
		hurtVignette = (ShaderMaterial)vignetteBox.Material;

		pistolShot = GD.Load<AudioStreamWav>("res://assets/pistolshot.wav");
		rifleShot = GD.Load<AudioStreamWav>("res://assets/rifleshot.wav");
		pistolReload = GD.Load<AudioStreamMP3>("res://assets/pistolreload.mp3");
		rifleReload = GD.Load<AudioStreamMP3>("res://assets/riflereload.mp3");
		dryFire = GD.Load<AudioStreamWav>("res://assets/dryfire.wav");

		BULLET_SCENE = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");

		// Set up GUI
		hurtVignette.SetShaderParameter("inner_radius", 1.0);
		vignetteBox.Visible = false;
		reloadBar.Visible = false;
		purchaseLabel.Visible = false;

		// Set up weapons
		LoadWeaponsJson();
		GiveWeapon(0, 0);
		GiveWeapon(1, 1);
		SetWeapon(0);

		// Set up audio
		for (int i = 0; i < 10; ++i) {
			audioManagers[i] = new AudioStreamPlayer2D();
			AddChild(audioManagers[i]);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process (double delta) {
		firingCooldown += delta;

		Vector2 viewportCenter = GetViewportRect().Size / 2;
		Vector2 mousePos = GetViewport().GetMousePosition() - viewportCenter;
		double viewAngle = Math.Atan2(mousePos.Y, mousePos.X);

		characterSprite.Rotation = (float)viewAngle + (float)viewOffset;

		// Shooting	=======================
		bool weaponCooldownDone = firingCooldown > RateOfFireMs;
		bool weaponHasAmmoInMag = ammoCounts[activeWeapon, MAGAZINE] > 0;
		bool semiAutoFire = !heldWeapons[activeWeapon].Automatic && Input.IsActionJustPressed("fire");
		bool autoFire = heldWeapons[activeWeapon].Automatic && Input.IsActionPressed("fire");
		bool magazineFull = ammoCounts[activeWeapon, MAGAZINE] == heldWeapons[activeWeapon].MagSize;
		bool reserveEmpty = ammoCounts[activeWeapon, RESERVE] == 0;


		if ((semiAutoFire || autoFire) && weaponCooldownDone && weaponHasAmmoInMag && !isReloading) {
			ShootBullet();
		} else if ((semiAutoFire && !weaponHasAmmoInMag) || (heldWeapons[activeWeapon].Automatic && Input.IsActionJustPressed("fire"))) {
			PlaySound("dry_fire");
		}

		// Initiate reload
		if (Input.IsActionJustPressed("reload") && !magazineFull && !reserveEmpty && !isReloading) {
			reloadBar.Value = 0;
			reloadBar.Visible = true;
			isReloading = true;
			PlaySound(heldWeapons[activeWeapon].Stance == "pistol" ? "pistol_reload" : "rifle_reload");
		}

		// If we're reloading, count the time and update the reload bar until we reach the end
		if (isReloading && timeSpentReloading < heldWeapons[activeWeapon].ReloadTime) {
			timeSpentReloading += delta;
			reloadBar.Value = timeSpentReloading / heldWeapons[activeWeapon].ReloadTime * 100;

		// Finish reload
		} else if (isReloading) {
			ReloadWeapon();
		}

		// Swapping Guns ==========================
		if (Input.IsActionJustPressed("swap")) {
			// Cancel reload
			if (isReloading) {
				isReloading = false;
				timeSpentReloading = 0;
				reloadBar.Visible = false;
			}

			SetWeapon(activeWeapon == 0 ? 1 : 0);
		}


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
		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> jsonDict = (Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>)Json.ParseString(System.IO.File.ReadAllText("data/weapons.json"));

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

	public void ShootBullet() {
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

		if (heldWeapons[activeWeapon].Stance == "pistol")
			PlaySound("pistol");
		else
			PlaySound("rifle");
	}

	public void ReloadWeapon() {
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

	public void GiveWeapon (int weaponID, int slot = -1) {
		if (slot == -1) {
			slot = heldWeapons[1].Name == "None" ? 1 : activeWeapon;
		}

		heldWeapons[slot] = allWeapons[weaponID];
		ammoCounts[slot, MAGAZINE] = heldWeapons[slot].MagSize;
		ammoCounts[slot, RESERVE] = heldWeapons[slot].ReserveSize;

		if (slot == activeWeapon)
			SetWeapon(activeWeapon);
	}

	private void SetWeapon (int weaponIdx) {
		activeWeapon = weaponIdx;
		RateOfFireMs = 1 / heldWeapons[activeWeapon].RateOfFire;
		gunLabel.Text = heldWeapons[activeWeapon].Name;

		if (heldWeapons[activeWeapon].Stance == "pistol") {
			characterSprite.Texture = pistolStance;
			weaponSprite.Scale = pistolScale;
			weaponSprite.Position = pistolPos;
			weaponSprite.Visible = true;
			underarmSprite.Visible = false;
		} else if (heldWeapons[activeWeapon].Stance == "rifle") {
			characterSprite.Texture = rifleStance;
			weaponSprite.Scale = rifleScale;
			weaponSprite.Position = riflePos;
			weaponSprite.Visible = true;
			underarmSprite.Visible = true;
		} else {
			characterSprite.Texture = unarmedStance;
			weaponSprite.Visible = false;
			underarmSprite.Visible = false;
		}

		if (heldWeapons[activeWeapon].Name != "None")
			weaponSprite.Texture = GD.Load<Texture2D>($"res://assets/{heldWeapons[activeWeapon].Name}.svg");

		UpdateAmmo();
	}

	public void ResetInfection() {
		infectionPercent = 0f;
		infectionBar.Value = infectionPercent * 100;
	}

	private void OnInfectionTick()
	{
		if (infectionPercent == 1)
			GameOver();
		else
			infectionPercent += 0.01f;

		infectionBar.Value = infectionPercent * 100;
	}

	private void GameOver() {
		;
	}

	public void ShowLabel(string text) {
		purchaseLabel.Visible = true;
		purchaseLabel.Text = text;
	}

	public void HideLabel() {
		purchaseLabel.Visible = false;
	}

	private void PlaySound(string type) {
		currentAudioPlayer = (currentAudioPlayer + 1) % 10;
		AudioStreamPlayer2D currentManager = audioManagers[currentAudioPlayer];

		if (type == "pistol")	
			currentManager.Stream = pistolShot;
		else if (type == "rifle")
			currentManager.Stream = rifleShot;
		else if (type == "pistol_reload")
			currentManager.Stream = pistolReload;
		else if (type == "rifle_reload")
			currentManager.Stream = rifleReload;
		else if (type == "dry_fire")
			currentManager.Stream = dryFire;

		currentManager.Play();
	}
}
