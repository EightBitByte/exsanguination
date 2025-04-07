// SharedData.cs
//
// Implements an autoload singleton for loading all shared assets and functions.

using Godot;
using System.Collections.Generic;

/// <summary>
// Implements an autoload singleton for loading all shared assets and functions.
/// </summary>
public partial class SharedData : Node2D
{
	/// <summary>
	// The public instance of SharedData, which preloads and holds all of the assets.
	/// </summary>
	public static SharedData Instance {get; private set;}

	private static readonly string assetsBasePath = "res://assets/";

	// Begin definitions of GUI signals
	[Signal]
	public delegate void UpdatePointLabelEventHandler (int points);
	[Signal]
	public delegate void PlayerHurtEventHandler (double percentHP);
	[Signal]
	public delegate void UpdateAmmoLabelEventHandler (int ammoInMagazine, 
		int ammoInReserve);	
	[Signal]
	public delegate void ShowActiveLabelEventHandler (string labelText);
	[Signal]
	public delegate void ShowPurchaseLabelEventHandler (int cost);
	[Signal]
	public delegate void HideActiveLabelEventHandler ();
	[Signal]
	public delegate void ToggleReloadBarEventHandler (bool enabled);
	[Signal]
	public delegate void UpdateReloadBarEventHandler (double reloadProgress);
	[Signal]
	public delegate void UpdateWeaponLabelEventHandler (string weaponName);
	[Signal]
	public delegate void UpdateInfectionBarEventHandler (double infectionProgress);
	
	// Begin definitions of Audio signals
	[Signal]
	public delegate void PlaySoundEventHandler (int soundID);

	// Begin definition of Player signals
	[Signal]
	public delegate void ModifyPointsEventHandler (int points);
	[Signal]
	public delegate void CureConsumeEventHandler ();
	[Signal]
	public delegate void ToggleShootingEventHandler (bool enabled);
	[Signal]
	public delegate void GameOverEventHandler();
	[Signal]
	public delegate void GiveWeaponEventHandler(int weaponID);

	public AudioStreamWav PistolShot, RifleShot, DryFire, PillSound, 
						  PistolReload, RifleReload, BuySound;

	public RichTextLabel PointLabel, WeaponLabel, AmmoLabel, ActiveLabel;
	public TextureProgressBar ReloadBar, InfectionBar;
	public ColorRect VignetteBox, GameOverScreen;
	public ShaderMaterial HurtVignette;
	public TextureButton GameOverButton;

	public Timer SpawnTimer, SafeTimer;
	public PackedScene EnemyScene, BulletScene, BloodPoolScene;

	public Player PlayerNode;
	public GUIManager GUIManagerInstance;
	public AudioManager AudioManagerInstance;

	public Sprite2D CharacterSpriteNode, WeaponSpriteNode, UnderarmSpriteNode;
	public Texture2D PistolStance, RifleStance, UnarmedStance, EnemyTexture;
	public TextureRect ShopGUI;

	public List<Weapon> AllWeapons = new(); 
	public List<Texture2D> weaponTextures = new();

	private static readonly string scenePath = "res://scenes/";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

		Node2D SceneNode = GetRootSceneNode();

		LoadGUINodes(SceneNode);
		LoadCharacterNodes(SceneNode);
		LoadEssentialScenes();
		LoadAudioAssets();
		LoadTextureAssets();
		LoadWeaponsJson();
		ConnectGUISignals();
		ConnectActionSignals();
	}


	/// <summary>
	/// Returns the scene <c>Node2D</c> with a name that ends with 'Scene'.
	/// </summary>
	public Node2D GetRootSceneNode() {
		foreach (Node n in GetTree().Root.GetChildren())
			if (((string)n.Name).EndsWith("Scene")) 
				return (Node2D)n;

		return null;
	}

	/// <summary>
	/// Translates a stance to the sound the weapon makes.
	/// </summary>
	public Sound StanceToSound(WeaponStance stance, bool isReloadSound = false) {
        return stance switch
        {
            WeaponStance.Pistol => isReloadSound ? Sound.PistolReload : Sound.PistolShot,
            WeaponStance.Rifle => isReloadSound ? Sound.RifleReload : Sound.RifleShot,
            _ => Sound.DryFire,
        };
    }


	private void LoadGUINodes(Node2D sceneNode) {
		PointLabel = sceneNode.GetNode<RichTextLabel>("./GUI/Point Label");
		WeaponLabel = sceneNode.GetNode<RichTextLabel>("./GUI/Weapon Label");
		AmmoLabel = sceneNode.GetNode<RichTextLabel>("./GUI/Ammo Label");
		ActiveLabel = sceneNode.GetNode<RichTextLabel>("./GUI/Active Label");

		ReloadBar = sceneNode.GetNode<TextureProgressBar>("./Player/Reload Bar");
		InfectionBar = sceneNode.GetNode<TextureProgressBar>("./GUI/Infection Bar");
		VignetteBox = sceneNode.GetNode<ColorRect>("./GUI/Vignette");
		HurtVignette = (ShaderMaterial)VignetteBox.Material;
		GameOverScreen = sceneNode.GetNode<ColorRect>("./GUI/Game Over");
		GameOverButton = sceneNode.GetNode<TextureButton>("./GUI/Game Over/TextureButton");
		ShopGUI = sceneNode.GetNode<TextureRect>("./GUI/Shop");
	}


	private void LoadAudioAssets() {
		PistolShot = GD.Load<AudioStreamWav>(assetsBasePath + "audio/pistolshot.wav");
		RifleShot = GD.Load<AudioStreamWav>(assetsBasePath + "audio/rifleshot.wav");
		PistolReload = GD.Load<AudioStreamWav>(assetsBasePath + "audio/pistolreload.wav");
		RifleReload = GD.Load<AudioStreamWav>(assetsBasePath + "audio/riflereload.wav");
		DryFire = GD.Load<AudioStreamWav>(assetsBasePath + "audio/dryfire.wav");
		BuySound = GD.Load<AudioStreamWav>(assetsBasePath + "audio/buy.wav");
		PillSound = GD.Load<AudioStreamWav>(assetsBasePath + "audio/pill.wav");
	}

	
	private void LoadCharacterNodes(Node2D sceneNode) {
		SpawnTimer = sceneNode.GetNode<Timer>("./Enemy Manager/Spawn Timer");
		SafeTimer = sceneNode.GetNode<Timer>("./Enemy Manager/Safe Timer");
		PlayerNode = sceneNode.GetNode<Player>("./Player");
		AudioManagerInstance = sceneNode.GetNode<AudioManager>("./Audio Manager");
		GUIManagerInstance = sceneNode.GetNode<GUIManager>("./GUI Manager");
		CharacterSpriteNode = sceneNode.GetNode<Sprite2D>("./Player/Player Sprite");
		WeaponSpriteNode = sceneNode.GetNode<Sprite2D>("./Player/Player Sprite/Weapon Sprite");
		UnderarmSpriteNode = sceneNode.GetNode<Sprite2D>("./Player/Player Sprite/Underarm");
	}


	private void LoadTextureAssets() {
		PistolStance = GD.Load<Texture2D>(assetsBasePath + "textures/Stance-Pistol.svg");
		RifleStance = GD.Load<Texture2D>(assetsBasePath + "textures/Stance-Rifle.svg");
		UnarmedStance = GD.Load<Texture2D>(assetsBasePath + "textures/Stance-Unarmed.svg");
		EnemyTexture = GD.Load<Texture2D>(assetsBasePath + "textures/Enemy-Sprite.svg");
	}


	private void LoadEssentialScenes() {
		EnemyScene = GD.Load<PackedScene>(scenePath + "enemy.tscn");
		BulletScene = GD.Load<PackedScene>(scenePath + "bullet.tscn");
		BloodPoolScene = GD.Load<PackedScene>(scenePath + "blood_pool.tscn");
	}


	private void LoadWeaponsJson() {
		//TODO: Move from Player.cs
	}


	private void ConnectGUISignals() {
		UpdatePointLabel += GUIManagerInstance.UpdatePoints;	
		PlayerHurt += GUIManagerInstance.AdjustHurtVignette;
		UpdateAmmoLabel += GUIManagerInstance.UpdateAmmo;
		ShowActiveLabel += GUIManagerInstance.ShowActiveLabel;
		ShowPurchaseLabel += GUIManagerInstance.ShowActiveLabel;
		HideActiveLabel += GUIManagerInstance.HideActiveLabel;
		ToggleReloadBar += GUIManagerInstance.ToggleReloadBar;
		UpdateReloadBar += GUIManagerInstance.UpdateReloadBar;
		UpdateWeaponLabel += GUIManagerInstance.UpdateWeaponLabel;
		UpdateInfectionBar += GUIManagerInstance.UpdateInfectionBar;
		PlaySound += AudioManagerInstance.PlaySound;
	}


	private void ConnectActionSignals() {
		CureConsume += PlayerNode.ResetInfection;
		ModifyPoints += PlayerNode.ModifyPoints;
	}
}