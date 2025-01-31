// SharedData.cs
//
// Implements an autoload singleton for loading all shared assets and functions.

using Godot;
using System;

/// <summary>
// Implements an autoload singleton for loading all shared assets and functions.
/// </summary>
public partial class SharedData : Node2D
{
	/// <summary>
	// The public instance of SharedData, which preloads and holds all of the assets.
	/// </summary>
	public static SharedData Instance {get; private set;}

	public RichTextLabel pointLabel, gunLabel, ammoLabel, purchaseLabel;
	public TextureProgressBar reloadBar, infectionBar;
	public ColorRect vignetteBox, gameOverScreen;
	public ShaderMaterial hurtVignette;
	public TextureButton gameOverButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

		Node2D SceneNode = GetRootSceneNode();

		pointLabel = SceneNode.GetNode<RichTextLabel>("./GUI/Point Label");
		gunLabel = SceneNode.GetNode<RichTextLabel>("./GUI/Gun Label");
		ammoLabel = SceneNode.GetNode<RichTextLabel>("./GUI/Ammo Label");
		purchaseLabel = SceneNode.GetNode<RichTextLabel>("./GUI/Purchase Label");

		reloadBar = SceneNode.GetNode<TextureProgressBar>("./Player/Reload Bar");
		infectionBar = SceneNode.GetNode<TextureProgressBar>("./GUI/Infection Bar");
		vignetteBox = SceneNode.GetNode<ColorRect>("./GUI/Vignette");
		hurtVignette = (ShaderMaterial)vignetteBox.Material;
		gameOverScreen = SceneNode.GetNode<ColorRect>("./GUI/Game Over");
		gameOverButton = SceneNode.GetNode<TextureButton>("./GUI/Game Over/TextureButton");
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
}