using Godot;
using System;

public partial class SharedData : Node2D
{
	public static SharedData Instance {get; private set;}

	public int MyVariable {get; set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	/// <summary>
	/// Returns the root scene Node2D that ends with `Scene`.
	/// </summary>
	/// <returns></returns>
	public Node2D GetRootSceneNode() {
		foreach (Node2D n in GetTree().Root.GetChildren()) {
			if (((string)n.Name).EndsWith("Scene")) {
				return n;
			}
		}
		
		return null;
	}
}
