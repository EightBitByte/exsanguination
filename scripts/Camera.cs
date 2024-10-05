using Godot;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class Camera : Camera2D
{
	[Export]
	float SafeArea = 200;

	Node2D followTarget;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MakeCurrent();
		followTarget = GetNode<Node2D>("/root/main_scene/Character");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// If viewport was resized since last frame, update center
		Vector2 viewportCenter = GetViewportRect().Size / 2;

		Vector2 center = followTarget.Position;
		Vector2 mousePos = GetViewport().GetMousePosition() - viewportCenter;

		Vector2 calcPosition = center + mousePos;

		// If the camera's distance from the center is greater than the safe area, 
		// clamp to safe area
		if (Math.Abs(mousePos.X) >= SafeArea)
			calcPosition.X = mousePos.X > 0 ? center.X + SafeArea : center.X - SafeArea;

		if (Math.Abs(mousePos.Y) >= SafeArea)
			calcPosition.Y = mousePos.Y > 0 ? center.Y + SafeArea : center.Y - SafeArea;
			
		Position = calcPosition;	
	}
}
