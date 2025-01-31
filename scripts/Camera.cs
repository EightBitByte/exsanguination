// Camera.cs
//
// Defines a camera node that follows the player and moves with the mouse, 
// allowing the player to "peek" further.

using Godot;
using System;

public partial class Camera : Camera2D
{
	[Export]
	float SafeArea = 200;

	Node2D followTarget;

	public override void _Ready()
	{
		Node2D SceneNode = SharedData.Instance.GetRootSceneNode();
		MakeCurrent();
		followTarget = SceneNode.GetNode<Node2D>("./Player");
	}

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
