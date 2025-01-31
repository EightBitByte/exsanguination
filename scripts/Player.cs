// Character.cs
//
// Implements the player character node. 

using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;

public class Ammo
{
	public Ammo() { AmmoInMagazine = 0; AmmoInReserve = 0; }

	public int AmmoInMagazine {get; set;}
	public int AmmoInReserve {get; set;}
}

public enum WeaponId {
	None,
	ColtM1911,
	M4Carbine
}

public partial class Player : CharacterBody2D
{
	/// <summary>The number of milliseconds in a second.</summary>
	const int MILLIS = 1000;

	[Export]
	public int MaxHP = 100;
	
	/// <summary>
	/// The number of hit points the player regenerates each tick after the delay.
	/// </summary>
	[Export]
	private int HPRegenAmount = 2;

	/// <summary>
	/// The number of milliseconds to wait in between regeneration ticks.
	/// </summary>
	[Export]
	private int HPRegenDelayMs = 100;

	/// <summary>The speed at which the player moves.</summary>
	[Export]
	public float Speed = 400;

	[Export]
	/// <summary>The number of milliseconds between each bullet fired.</summary>
	public float RateOfFireMs = 200;

	/// <summary>
	/// The number of radians to offset the rotation of the sprite to follow the 
	/// cursor.
	/// </summary>
	private const double viewOffset = Math.PI / 2;

	///<summary>The number of milliseconds since the weapon was last fired.</summary>
	private double firingCooldown = 0;

	/// <summary> 
	/// The cooldown duration, in milliseconds, before healing can begin after 
	/// taking damage.
	/// </summary>
	private double damageHealCooldownMs = 3000;

	/// <summary>The number of milliseconds since damage was last received.</summary>
	private double timeSinceDamageMs = 0;

	public int Points = 0;
	private int HP = 100;
	private float infectionProgress = 0f;
	private Weapon[] heldWeapons = new Weapon[2];

	/// <summary>The ammunition counts of the two respective weapons.</summary>
	private Ammo[] ammunition = new Ammo[2];

	private List<Weapon> allWeapons = new List<Weapon>(); 
	private List<Texture2D> weaponTextures = new List<Texture2D>();

	private int activeWeaponSlot = 0;
    private int maxNumWeapons = 2;
	public bool isReloading = false, 
				movementEnabled = true, 
				shootingEnabled = true;
	private double reloadTimeElapsed = 0;

	private Vector2 pistolPos = new(100, -372), riflePos = new(108, -260);
	private Vector2 pistolScale = new(0.25f, -0.25f), rifleScale = new(0.75f, -0.75f);

	private PackedScene bulletScene;
	private Sprite2D characterSpriteNode, weaponSpriteNode, underarmSpriteNode; 
	private Texture2D pistolStance, rifleStance, unarmedStance, enemyTexture;

	private GUIManager GManager;
	private AudioManager AManager;


	public override void _Ready() {
		// Load resources
		Node2D SceneNode = SharedData.Instance.GetRootSceneNode();
		GManager = SceneNode.GetNode<GUIManager>("./GUI Manager");
		AManager = SceneNode.GetNode<AudioManager>("./Audio Manager");

		characterSpriteNode = SceneNode.GetNode<Sprite2D>("./Player/Player Sprite");
		weaponSpriteNode = SceneNode.GetNode<Sprite2D>("./Player/Player Sprite/Weapon Sprite");
		underarmSpriteNode = SceneNode.GetNode<Sprite2D>("./Player/Player Sprite/Underarm");

		pistolStance = GD.Load<Texture2D>("res://assets/Stance-Pistol.svg");
		rifleStance = GD.Load<Texture2D>("res://assets/Stance-Rifle.svg");
		unarmedStance = GD.Load<Texture2D>("res://assets/Stance-Unarmed.svg");
		enemyTexture = GD.Load<Texture2D>("res://assets/Enemy Sprite.svg");

		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");

		// Set up weapons
		ammunition[0] = new();
		ammunition[1] = new();

		LoadWeaponsJson();
		GiveWeapon(WeaponId.ColtM1911, 0);
		GiveWeapon(WeaponId.None, 1);
		SetWeapon(0);

		// SignalSingleton.Instance.MyVariable = 2;
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process (double delta) {
		firingCooldown += delta;

		Vector2 viewportCenter = GetViewportRect().Size / 2;
		Vector2 mousePos = GetViewport().GetMousePosition() - viewportCenter;
		double viewAngle = Math.Atan2(mousePos.Y, mousePos.X);

		characterSpriteNode.Rotation = (float)viewAngle + (float)viewOffset;

		CheckShootingInput();
        CheckReloadInput(delta);
        CheckSwapInput();
	}


	public override void _PhysicsProcess (double delta) {
		Vector2 moveDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		timeSinceDamageMs += delta;

		if (timeSinceDamageMs > damageHealCooldownMs / MILLIS && HP < 100) {
			HP = HP + HPRegenAmount > MaxHP ? MaxHP : HP + HPRegenAmount;
			GManager.AdjustHurtVignette(1.0 * HP / MaxHP);
			timeSinceDamageMs = (damageHealCooldownMs - HPRegenDelayMs) / MILLIS;
		}
		
		Velocity = moveDirection * Speed;

		if (movementEnabled)
			MoveAndSlide();
	}

	/// <summary>Checks for shooting input and does so if conditions are met.</summary>
	public void CheckShootingInput() {
		bool holdingAWeapon = heldWeapons[activeWeaponSlot].ID != 0;
		bool semiAutoFire = !heldWeapons[activeWeaponSlot].Automatic && Input.IsActionJustPressed("fire");
		bool autoFire = heldWeapons[activeWeaponSlot].Automatic && Input.IsActionPressed("fire");
		bool weaponCooldownDone = firingCooldown > RateOfFireMs;
		bool weaponHasAmmoInMag = ammunition[activeWeaponSlot].AmmoInMagazine > 0;

		if ((semiAutoFire || autoFire) && weaponCooldownDone && weaponHasAmmoInMag 
				&& !isReloading && shootingEnabled) {
			ShootBullet();
		} else if (holdingAWeapon && ((semiAutoFire && !weaponHasAmmoInMag && shootingEnabled) || 
				(heldWeapons[activeWeaponSlot].Automatic && Input.IsActionJustPressed("fire") 
				&& shootingEnabled))) {
			AManager.PlaySound(Sound.DryFire);
		}

	}


    /// <summary>Checks for reload input and does so if conditions are met.</summary>
    public void CheckReloadInput(double delta) {
		bool magazineFull = ammunition[activeWeaponSlot].AmmoInMagazine 
                            == heldWeapons[activeWeaponSlot].MagazineSize;
		bool reserveEmpty = ammunition[activeWeaponSlot].AmmoInReserve == 0;

		if (Input.IsActionJustPressed("reload") && !magazineFull && !reserveEmpty 
                && !isReloading && shootingEnabled) {
			GManager.UpdateReloadBar(0);
			GManager.ShowReloadBar();
			isReloading = true;
			AManager.PlaySound(heldWeapons[activeWeaponSlot].Stance == WeaponStance.Pistol ? 
								Sound.PistolReload : Sound.RifleReload);
		}

		if (isReloading && reloadTimeElapsed < 
                heldWeapons[activeWeaponSlot].ReloadTime) {
			reloadTimeElapsed += delta;
			GManager.UpdateReloadBar(reloadTimeElapsed / heldWeapons[activeWeaponSlot].ReloadTime * 100);

		} else if (isReloading) {
			ReloadWeapon();
		}
    }


    /// <summary>Checks for swapping input and does so if conditions are met.</summary>
    public void CheckSwapInput() {
		if (Input.IsActionJustPressed("swap")) {
			if (isReloading)
                CancelReload();

            activeWeaponSlot = (activeWeaponSlot + 1) % maxNumWeapons;
			SetWeapon(activeWeaponSlot);
		}
    }


    private void CancelReload() {
        isReloading = false;
        reloadTimeElapsed = 0;
        GManager.HideReloadBar();
    }


	/// <summary>
	/// Add (or subtract) <c>points</c> to the player's bank.
	/// </summary>
	/// <param name="points">The number of points to add or subtract.</param>
	public void AddPoints (int points) {
		Points += points;
		GManager.UpdatePoints(Points);
	}


	/// <summary>
	/// Damage the player by <c>attackDamage</c>.
	/// </summary>
	/// <param name="attackDamage">The amount of damage to deal to the player.</param>
	public void Hurt (int attackDamage) {
		if (attackDamage > 0)
			infectionProgress += 0.01f;

		HP = HP < attackDamage ? 0 : HP - attackDamage;
		GManager.AdjustHurtVignette(1.0 * HP / MaxHP);

		if (HP <= 0)
			GameOver();
		
		timeSinceDamageMs = 0;
	}


	/// <summary>Load the JSON file associated with the weapon.</summary>
	private void LoadWeaponsJson() {
		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> jsonDict = 
            (Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>) Json.ParseString(System.IO.File.ReadAllText("data/weapons.json"));

		foreach (System.Collections.Generic.KeyValuePair<String, Godot.Collections.Dictionary<String, String>> pair in jsonDict) {
			Godot.Collections.Dictionary<string, string> weaponDict = pair.Value;

			Weapon addedWeapon = new Weapon(int.Parse(pair.Key), weaponDict);
			allWeapons.Add(addedWeapon);
			if (addedWeapon.Name != "None")
				weaponTextures.Add(GD.Load<Texture2D>($"res://assets/{addedWeapon.Name}.svg"));
		}
	}


	/// <summary>
	/// Returns whether the player has enough points to purchase the item with <c>cost</c> points.
	/// </summary>
	/// <param name="cost">The number of points the item costs.</param>
	public bool HasEnoughPoints(int cost) {
		return Points >= cost;
	}


	/// <summary>
	/// Shoots a bullet from the player's weapon.
	/// </summary>
	public void ShootBullet() {
		Bullet bullet = bulletScene.Instantiate<Bullet>();
		bullet.BulletDamage = heldWeapons[activeWeaponSlot].BulletDamage;

		// Position bullet to come out of front of player
		float rotationOffset = characterSpriteNode.Rotation - (float)viewOffset;
		Vector2 bulletOffset = new Vector2(120, 0).Rotated(rotationOffset);
		bullet.Position = Position + bulletOffset;
		bullet.Rotation = rotationOffset;

		ammunition[activeWeaponSlot].AmmoInMagazine -= 1;
		GManager.UpdateAmmo(ammunition[activeWeaponSlot]);

		GetTree().Root.AddChild(bullet);
		firingCooldown = 0;

		if (heldWeapons[activeWeaponSlot].Stance == WeaponStance.Pistol)
			AManager.PlaySound(Sound.PistolShot);
		else
			AManager.PlaySound(Sound.RifleShot);
	}


	/// <summary>
	/// Reloads the player's weapon's magazine from reserve ammo, if available.
	/// </summary>
	public void ReloadWeapon() {
		// If reserve is less than mag size, reload up to reserve, otherwise fill mag
		int amtToReload =  Math.Min(ammunition[activeWeaponSlot].AmmoInReserve, 
									heldWeapons[activeWeaponSlot].MagazineSize - ammunition[activeWeaponSlot].AmmoInMagazine);
		ammunition[activeWeaponSlot].AmmoInMagazine += amtToReload;

		// Subtract reloaded ammo from the reserve
		ammunition[activeWeaponSlot].AmmoInReserve -= amtToReload;

		// Update HUD
		GManager.UpdateAmmo(ammunition[activeWeaponSlot]);
		GManager.HideReloadBar();

		isReloading = false;
		reloadTimeElapsed = 0;
	}


	/// <summary>
	/// Gives the player weapon with ID <c>weaponID</c> in slot <c>slot</c>.
	/// </summary>
	/// <param name="weaponID">The ID of the weapon to give the player.</param>
	/// <param name="slot">The slot in which to place the weapon.</param>
	public void GiveWeapon (WeaponId weaponID, int slot = -1) {
		if (slot == -1) {
			slot = heldWeapons[1].Name == "None" ? 1 : activeWeaponSlot;
		}

		heldWeapons[slot] = allWeapons[(int)weaponID];
		ammunition[slot].AmmoInMagazine = heldWeapons[slot].MagazineSize;
		ammunition[slot].AmmoInReserve = heldWeapons[slot].ReserveSize;
		SetWeapon(slot);
	}


	/// <summary>
	/// Set the current active weapon.
	/// </summary>
	/// <param name="slotNum">The index/slot of the weapon to set as active.</param>
	private void SetWeapon (int slotNum) {
		activeWeaponSlot = slotNum;
		RateOfFireMs = 1 / heldWeapons[activeWeaponSlot].RateOfFire;
		GManager.UpdateGunLabel(heldWeapons[activeWeaponSlot].Name);

		if (heldWeapons[activeWeaponSlot].Stance == WeaponStance.Pistol) {
			characterSpriteNode.Texture = pistolStance;
			weaponSpriteNode.Scale = pistolScale;
			weaponSpriteNode.Position = pistolPos;
			weaponSpriteNode.Visible = true;
			underarmSpriteNode.Visible = false;
		} else if (heldWeapons[activeWeaponSlot].Stance == WeaponStance.Rifle) {
			characterSpriteNode.Texture = rifleStance;
			weaponSpriteNode.Scale = rifleScale;
			weaponSpriteNode.Position = riflePos;
			weaponSpriteNode.Visible = true;
			underarmSpriteNode.Visible = true;
		} else {
			characterSpriteNode.Texture = unarmedStance;
			weaponSpriteNode.Visible = false;
			underarmSpriteNode.Visible = false;
		}

		if (heldWeapons[activeWeaponSlot].Name != "None")
			weaponSpriteNode.Texture = weaponTextures[heldWeapons[activeWeaponSlot].ID - 1];

		GManager.UpdateAmmo(ammunition[activeWeaponSlot]);
	}


	/// <summary>
	/// Increases the infection percentage, bringing the player closer to game over.
	/// </summary>
	private void OnInfectionTick() {
		if (infectionProgress == 1)
			GameOver();
		else
			infectionProgress += 0.01f;

		GManager.UpdateInfectionBar(infectionProgress * 100);
	}


	// TODO: Create custom signal for this.
	/// <summary>
	/// Triggers the game over for the player.
	/// </summary>
	private void GameOver() {
		GManager.ShowGameOver();
		shootingEnabled = false;
		movementEnabled = false;
		
		characterSpriteNode.Texture = enemyTexture;
		weaponSpriteNode.Visible = false;
	}

	/// <summary>
	/// On consumption of a cure, resets the infection progression.
	/// </summary>
	private void OnCureConsume() {
		infectionProgress = 0f;
		GManager.UpdateInfectionBar(infectionProgress * 100);
	}

}
