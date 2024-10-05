using Godot;
using System;

public partial class Character : CharacterBody2D
{
	const int MILLIS = 1000;

	[Export]
	public int MaxHP = 100;
	
	[Export]
	private int RegenAmt = 2;
	[Export]
	private int RegenDelay = 100;
	[Export]
	public float Speed = 400;

	[Export]
	public float TriggerSpeedMs = 200;

	private float rotation;
	private double viewOffset = Math.PI / 2;

	Sprite2D characterSprite;
	RichTextLabel pointLabel;
	ShaderMaterial hurtVignette;
	ColorRect vignetteBox;

	PackedScene BULLET_SCENE, ENEMY_SCENE;

	private double firingCooldown = 0;
	private double healCooldown = 3000;
	private double timeSinceLastDamage = 0;
	private int money = 0;
	private int health = 100;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		characterSprite = GetChild<Sprite2D>(0);
		pointLabel = GetNode<RichTextLabel>("/root/main_scene/GUI/Point Label");
		vignetteBox = GetNode<ColorRect>("/root/main_scene/GUI/Vignette");
		hurtVignette = (ShaderMaterial)vignetteBox.Material;
		BULLET_SCENE = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		ENEMY_SCENE = GD.Load<PackedScene>("res://scenes/enemy.tscn");

		// TODO: Remove test enemy
		CharacterBody2D testEnemy = ENEMY_SCENE.Instantiate<CharacterBody2D>();
		testEnemy.Position = new Vector2(200, -100);
		GetTree().Root.CallDeferred("add_child", testEnemy);

		hurtVignette.SetShaderParameter("inner_radius", 1.0);
		vignetteBox.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process (double delta) {
		firingCooldown += delta;

		Vector2 viewportCenter = GetViewportRect().Size / 2;
		Vector2 mousePos = GetViewport().GetMousePosition() - viewportCenter;
		double viewAngle = Math.Atan2(mousePos.Y, mousePos.X);

		characterSprite.Rotation = (float)viewAngle + (float)viewOffset;

		if (Input.IsActionPressed("fire") && firingCooldown > TriggerSpeedMs / MILLIS) {
			CharacterBody2D bullet = BULLET_SCENE.Instantiate<CharacterBody2D>();

			float rotationOffset = characterSprite.Rotation - (float)viewOffset;
			Vector2 bulletOffset = new Vector2(120, 0).Rotated(rotationOffset);

			bullet.Position = Position + bulletOffset;
			bullet.Rotation = rotationOffset;


			GetTree().Root.AddChild(bullet);
			firingCooldown = 0;
		}
	}

    public override void _PhysicsProcess (double delta) {
		Vector2 moveDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		timeSinceLastDamage += delta;

		if (timeSinceLastDamage > healCooldown / MILLIS && health < 100) {
			health = health + RegenAmt > MaxHP ? MaxHP : health + RegenAmt;
			adjustVignette();
			timeSinceLastDamage = (healCooldown - RegenDelay) / MILLIS;
		}
		
		Velocity = moveDirection * Speed;
		MoveAndSlide();
	}

	public void addPoints (int points) {
		money += points;
		pointLabel.Text = $"${money/100}.{money%100}";
	}

	public void Hurt (int attackDmg) {
		health = health < attackDmg ? 0 : health - attackDmg;
		adjustVignette();
		
		timeSinceLastDamage = 0;
	}

	// 1 is not visible, 0 is full strength
	public void adjustVignette () {
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
}
