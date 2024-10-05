using Godot;
using System;

public partial class Character : CharacterBody2D
{
	[Export]
	public float Speed = 400;

	private float rotation;
	private double viewOffset = Math.PI / 2;

	Sprite2D characterSprite;
	CollisionShape2D characterCollider;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		characterSprite = GetChild<Sprite2D>(0);
		characterCollider = GetChild<CollisionShape2D>(1);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2 viewportCenter = GetViewportRect().Size / 2;
		Vector2 mousePos = GetViewport().GetMousePosition() - viewportCenter;
		double viewAngle = Math.Atan2(mousePos.Y, mousePos.X);

		GD.Print("mousePos: ", mousePos);
		GD.Print("viewAngle: ", viewAngle);
		characterSprite.Rotation = (float)viewAngle + (float)viewOffset;
		characterCollider.Rotation = characterSprite.Rotation;
	}

    public override void _PhysicsProcess(double delta)
    {
		Vector2 moveDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		
		Velocity = moveDirection * Speed;
		MoveAndSlide();

	}
}
