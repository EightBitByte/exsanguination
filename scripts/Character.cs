using Godot;
using System;

public partial class Character : CharacterBody2D
{
	[Export]
	public float Speed = 400;

	[Export]
	public float TriggerSpeedMs = 200;

	private float rotation;
	private double viewOffset = Math.PI / 2;

	Sprite2D characterSprite;

	PackedScene BULLET_SCENE;

	double firingCooldown = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		characterSprite = GetChild<Sprite2D>(0);
		BULLET_SCENE = GD.Load<PackedScene>("res://scenes/bullet.tscn");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		firingCooldown += delta;

		Vector2 viewportCenter = GetViewportRect().Size / 2;
		Vector2 mousePos = GetViewport().GetMousePosition() - viewportCenter;
		double viewAngle = Math.Atan2(mousePos.Y, mousePos.X);

		GD.Print("mousePos: ", mousePos);
		GD.Print("viewAngle: ", viewAngle);
		characterSprite.Rotation = (float)viewAngle + (float)viewOffset;

		if (Input.IsActionPressed("fire") && firingCooldown > TriggerSpeedMs / 1000) {
			CharacterBody2D bullet = BULLET_SCENE.Instantiate<CharacterBody2D>();

			float rotationOffset = characterSprite.Rotation - (float)viewOffset;
			Vector2 bulletOffset = new Vector2(120, 0).Rotated(rotationOffset);

			bullet.Position = Position + bulletOffset;
			bullet.Rotation = rotationOffset;


			GetTree().Root.AddChild(bullet);
			firingCooldown = 0;
		}
	}

    public override void _PhysicsProcess(double delta)
    {
		Vector2 moveDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		
		Velocity = moveDirection * Speed;
		MoveAndSlide();
	}
}
