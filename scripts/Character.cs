// Character.cs
//
// Implements the player character node. 

using Godot;
using System;
using System.Collections.Generic;

public partial class Character : CharacterBody2D
{
	// TODO: Refactor variable names for more clarity
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
	ColorRect vignetteBox, gameOverScreen;
	Texture2D pistolStance, rifleStance, unarmedStance, enemyTexture;
	AudioStreamPlayer2D[] audioManagers = new AudioStreamPlayer2D[10];
	AudioStreamWav pistolShot, rifleShot, dryFire, pillSound;
	AudioStreamMP3 pistolReload, rifleReload, buySound;
	PackedScene BULLET_SCENE, ENEMY_SCENE;
	TextureButton gameOverButton;

	private double firingCooldown = 0;
	private double healCooldown = 3000;
	private double timeSinceLastDamage = 0;
	public int money = 0;
	private int health = 100;
	private float infectionPercent = 0f;
	private Weapon[] heldWeapons = new Weapon[2];
	// Second dimension is ammo in mag, ammo in reserve
	// TODO: Refactor this 2D array into a struct (little bit more dev-friendly)
	private int[,] ammoCounts = new int[2, 2];
	private List<Weapon> allWeapons = new List<Weapon>(); 
	private int activeWeapon = 0;
	public bool isReloading = false, movementEnabled = true, shootingEnabled = true;
	private double timeSpentReloading = 0;

	private int currentAudioPlayer = 0;

	private static Vector2 pistolPos = new(100, -372), riflePos = new(108, -260);
	private static Vector2 pistolScale = new(0.25f, -0.25f), rifleScale = new(0.75f, -0.75f);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Load resources
		// TODO: Refactor into a resource/node manager.
		characterSprite = GetChild<Sprite2D>(0);
		weaponSprite = GetNode<Sprite2D>("/root/main_scene/Character/Character Sprite/Weapon Sprite");
		underarmSprite = GetNode<Sprite2D>("/root/main_scene/Character/Character Sprite/Underarm");
		pistolStance = GD.Load<Texture2D>("res://assets/Character (Pistol).svg");
		rifleStance = GD.Load<Texture2D>("res://assets/Character (Rifle).svg");
		unarmedStance = GD.Load<Texture2D>("res://assets/Character (Unarmed).svg");
		enemyTexture = GD.Load<Texture2D>("res://assets/Enemy Sprite.svg");

		pointLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Point Label");
		gunLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Gun Label");
		ammoLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Ammo Label");
		purchaseLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Purchase Label");

		reloadBar = GetNode<TextureProgressBar>("/root/main_scene/Character/Reload Bar");
		infectionBar = GetNode<TextureProgressBar>("/root/main_scene/GUI/Infection Bar");
		vignetteBox = GetNode<ColorRect>("/root/main_scene/GUI/Vignette");
		hurtVignette = (ShaderMaterial)vignetteBox.Material;
		gameOverScreen = GetNode<ColorRect>("/root/main_scene/GUI/Game Over");
		gameOverButton = GetNode<TextureButton>("/root/main_scene/GUI/Game Over/TextureButton");

		pistolShot = GD.Load<AudioStreamWav>("res://assets/pistolshot.wav");
		rifleShot = GD.Load<AudioStreamWav>("res://assets/rifleshot.wav");
		pistolReload = GD.Load<AudioStreamMP3>("res://assets/pistolreload.mp3");
		rifleReload = GD.Load<AudioStreamMP3>("res://assets/riflereload.mp3");
		dryFire = GD.Load<AudioStreamWav>("res://assets/dryfire.wav");
		buySound = GD.Load<AudioStreamMP3>("res://assets/buy.mp3");
		pillSound = GD.Load<AudioStreamWav>("res://assets/pill.wav");

		BULLET_SCENE = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");

		// Set up GUI
		// TODO: Refactor into a GUI manager, the player should only be concerned 
		// with things related to the object.
		hurtVignette.SetShaderParameter("inner_radius", 1.0);
		vignetteBox.Visible = false;
		reloadBar.Visible = false;
		purchaseLabel.Visible = false;

		// Set up weapons
		LoadWeaponsJson();
		GiveWeapon(0, 0);
		GiveWeapon(3, 1);
		SetWeapon(0);

		// Set up audio
		// TODO: Refactor into audio manager, the player should only be concerned 
		// with things related to the object.
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
		// TODO: Refactor into separate method
		bool weaponCooldownDone = firingCooldown > RateOfFireMs;
		bool weaponHasAmmoInMag = ammoCounts[activeWeapon, MAGAZINE] > 0;
		bool semiAutoFire = !heldWeapons[activeWeapon].Automatic && Input.IsActionJustPressed("fire");
		bool autoFire = heldWeapons[activeWeapon].Automatic && Input.IsActionPressed("fire");
		bool magazineFull = ammoCounts[activeWeapon, MAGAZINE] == heldWeapons[activeWeapon].MagazineSize;
		bool reserveEmpty = ammoCounts[activeWeapon, RESERVE] == 0;


		if ((semiAutoFire || autoFire) && weaponCooldownDone && weaponHasAmmoInMag && !isReloading && shootingEnabled) {
			ShootBullet();
		} else if ((semiAutoFire && !weaponHasAmmoInMag && shootingEnabled) || (heldWeapons[activeWeapon].Automatic && Input.IsActionJustPressed("fire") && shootingEnabled)) {
			PlaySound("dry_fire");
		}

		// Initiate reload
		// TODO: Refactor into separate method
		if (Input.IsActionJustPressed("reload") && !magazineFull && !reserveEmpty && !isReloading && shootingEnabled) {
			reloadBar.Value = 0;
			reloadBar.Visible = true;
			isReloading = true;
			PlaySound(heldWeapons[activeWeapon].Stance == WeaponStance.Pistol ? "pistol_reload" : "rifle_reload");
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
		// TODO: Refactor into separate method
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

		if (movementEnabled)
			MoveAndSlide();
	}

	/// <summary>
	/// Add (or subtract) <c>points</c> to the player's bank.
	/// </summary>
	/// <param name="points">The number of points to add or subtract.</param>
	public void AddPoints (int points) {
		money += points;
		string cents = money % 100 == 0 ? "00" : (money % 100).ToString();
		pointLabel.Text = $"${money/100}.{cents}";
	}


	/// <summary>
	/// Damage the player by <c>attackDamage</c>.
	/// </summary>
	/// <param name="attackDamage">The amount of damage to deal to the player.</param>
	public void Hurt (int attackDamage) {
		if (attackDamage > 0)
			infectionPercent += 0.01f;

		health = health < attackDamage ? 0 : health - attackDamage;
		AdjustVignette();

		if (health <= 0)
			GameOver();
		
		timeSinceLastDamage = 0;
	}


	// 1 is not visible, 0 is full strength
	// TODO: Move to GUI manager
	/// <summary>
	/// Adjust the hurt vignette on the GUI.
	/// </summary>
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


	// TODO: Move into separate manager
	/// <summary>
	/// Load the JSON file associated with the weapon.
	/// </summary>
	private void LoadWeaponsJson() {
		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> jsonDict = (Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>)Json.ParseString(System.IO.File.ReadAllText("data/weapons.json"));

		foreach (System.Collections.Generic.KeyValuePair<String, Godot.Collections.Dictionary<String, String>> pair in jsonDict) {
			Godot.Collections.Dictionary<string, string> weaponDict = pair.Value;

			allWeapons.Add(new Weapon(weaponDict));
			GD.Print(new Weapon(weaponDict));
		}
	}


	// TODO: Move to GUI manager
	/// <summary>
	/// Update the Ammo label on the GUI.
	/// </summary>
	private void UpdateAmmo() {
		ammoLabel.Text = $"{ammoCounts[activeWeapon, MAGAZINE]} / {ammoCounts[activeWeapon, RESERVE]}";
	}


	// TODO: Move to GUI manager
	/// <summary>
	/// Show the purchase label for the given item at <c>purchaseAmount</c> cost.
	/// </summary>
	/// <param name="purchaseAmount"></param>
	public void ShowPurchaseLabel(int purchaseAmount) {
		string cents = purchaseAmount % 100 == 0 ? "00" : (purchaseAmount % 100).ToString();

		purchaseLabel.Text = $"[F] Clear for ${purchaseAmount/100}.{cents}";
		purchaseLabel.Visible = true;
	}

	// TODO: Move to GUI Manager	
	/// <summary>
	/// Hide the purchase label from the GUI.
	/// </summary>
	public void HidePurchaseLabel() {
		purchaseLabel.Visible = false;
	}


	/// <summary>
	/// Returns whether the player has enough money to purchase the item with <c>cost</c> points.
	/// </summary>
	/// <param name="cost">The amount of points the item costs.</param>
	public bool HasEnoughMoney(int cost) {
		return money >= cost;
	}


	/// <summary>
	/// Shoots a bullet from the player's weapon.
	/// </summary>
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

		if (heldWeapons[activeWeapon].Stance == WeaponStance.Pistol)
			PlaySound("pistol");
		else
			PlaySound("rifle");
	}


	/// <summary>
	/// Reloads the player's weapon's magazine from reserve ammo, if available.
	/// </summary>
	public void ReloadWeapon() {
		// TODO: Refactor how we track ammo -- this is so confusing 😵
		// If reserve is less than mag size, reload up to reserve, otherwise fill mag
		int amtToReload =  Math.Min(ammoCounts[activeWeapon, RESERVE], 
									heldWeapons[activeWeapon].MagazineSize - ammoCounts[activeWeapon, MAGAZINE]);
		ammoCounts[activeWeapon, MAGAZINE] += amtToReload;

		// Subtract reloaded ammo from the reserve
		ammoCounts[activeWeapon, RESERVE] -= amtToReload;

		// Update HUD
		UpdateAmmo();
		reloadBar.Visible = false;

		isReloading = false;
		timeSpentReloading = 0;
	}

	/// <summary>
	/// Gives the player weapon with ID <c>weaponID</c> in slot <c>slot</c>.
	/// </summary>
	/// <param name="weaponID">The ID of the weapon to give the player.</param>
	/// <param name="slot">The slot in which to place the weapon.</param>
	public void GiveWeapon (int weaponID, int slot = -1) {
		if (slot == -1) {
			slot = heldWeapons[1].Name == "None" ? 1 : activeWeapon;
		}

		heldWeapons[slot] = allWeapons[weaponID];
		ammoCounts[slot, MAGAZINE] = heldWeapons[slot].MagazineSize;
		ammoCounts[slot, RESERVE] = heldWeapons[slot].ReserveSize;

		SetWeapon(slot);
	}


	/// <summary>
	/// Set the current active weapon.
	/// </summary>
	/// <param name="slotNum">The index/slot of the weapon to set as active.</param>
	private void SetWeapon (int slotNum) {
		activeWeapon = slotNum;
		RateOfFireMs = 1 / heldWeapons[activeWeapon].RateOfFire;
		gunLabel.Text = heldWeapons[activeWeapon].Name;

		if (heldWeapons[activeWeapon].Stance == WeaponStance.Pistol) {
			characterSprite.Texture = pistolStance;
			weaponSprite.Scale = pistolScale;
			weaponSprite.Position = pistolPos;
			weaponSprite.Visible = true;
			underarmSprite.Visible = false;
		} else if (heldWeapons[activeWeapon].Stance == WeaponStance.Rifle) {
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

		// TODO: Don't load weapon sprites upon change! These should be loaded upon entering game!
		if (heldWeapons[activeWeapon].Name != "None")
			weaponSprite.Texture = GD.Load<Texture2D>($"res://assets/{heldWeapons[activeWeapon].Name}.svg");

		UpdateAmmo();
	}


	/// <summary>
	/// Resets the infection's progress to zero.
	/// </summary>
	public void ResetInfection() {
		infectionPercent = 0f;
		infectionBar.Value = infectionPercent * 100;
	}

	
	/// <summary>
	/// Increases the infection percentage, bringing the player closer to game over.
	/// </summary>
	private void OnInfectionTick() {
		if (infectionPercent == 1)
			GameOver();
		else
			infectionPercent += 0.01f;

		infectionBar.Value = infectionPercent * 100;
	}


	// TODO: Move to GUI Manager and create custom signal for this.
	/// <summary>
	/// Triggers the game over for the player.
	/// </summary>
	private void GameOver() {
		gameOverScreen.Visible = true;
		shootingEnabled = false;
		movementEnabled = false;
		
		characterSprite.Texture = enemyTexture;
		weaponSprite.Visible = false;
	}


	// TODO: Move to GUI Manager	
	/// <summary>
	/// Shows the bottom label with text <c>text</c>.
	/// </summary>
	/// <param name="text"></param>
	public void ShowLabel(string text) {
		purchaseLabel.Visible = true;
		purchaseLabel.Text = text;
	}


	// TODO: Move to GUI Manager	
	/// <summary>
	/// Hides the bottom label from the GUI.
	/// </summary>
	public void HideLabel() {
		purchaseLabel.Visible = false;
	}


	// TODO: Move to Audio Manager	
	/// <summary>
	/// Plays sound <c>type</c>.
	/// </summary>
	/// <param name="type">The type of sound to play.</param>
	public void PlaySound(string type) {
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
		else if (type == "buy")
			currentManager.Stream = buySound;
		else if (type == "pill")
			currentManager.Stream = pillSound;

		currentManager.Play();
	}
}