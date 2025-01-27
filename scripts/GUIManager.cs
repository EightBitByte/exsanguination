// GUIManager.cs
//
// Manages the GUI elements, such as health, ammo, and purchase labels.

using Godot;
using System;

public partial class GUIManager : Node
{

	private RichTextLabel pointLabel, gunLabel, ammoLabel, purchaseLabel;
	private TextureProgressBar reloadBar, infectionBar;
	private ColorRect vignetteBox, gameOverScreen;
	private ShaderMaterial hurtVignette;
	private TextureButton gameOverButton;

	private string rootPath = "/root/MainScene/";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Load == 
		pointLabel = GetNode<RichTextLabel>(rootPath + "GUI/Point Label");
		gunLabel = GetNode<RichTextLabel>(rootPath + "GUI/Gun Label");
		ammoLabel = GetNode<RichTextLabel>(rootPath + "GUI/Ammo Label");
		purchaseLabel = GetNode<RichTextLabel>(rootPath + "GUI/Purchase Label");

		reloadBar = GetNode<TextureProgressBar>(rootPath + "Player/Reload Bar");
		infectionBar = GetNode<TextureProgressBar>(rootPath + "GUI/Infection Bar");
		vignetteBox = GetNode<ColorRect>(rootPath + "GUI/Vignette");
		hurtVignette = (ShaderMaterial)vignetteBox.Material;
		gameOverScreen = GetNode<ColorRect>(rootPath + "GUI/Game Over");
		gameOverButton = GetNode<TextureButton>(rootPath + "GUI/Game Over/TextureButton");

		// Set up ==
		hurtVignette.SetShaderParameter("inner_radius", 1.0);
		vignetteBox.Visible = false;
		reloadBar.Visible = false;
		purchaseLabel.Visible = false;
	}

	/// <summary>Updates the points label.</summary>
	/// <param name="points">The number of points held by the player.</param>
	public void UpdatePoints(int points) {
		string cents = points % 100 == 0 ? "00" : (points % 100).ToString();
		pointLabel.Text = $"${points/100}.{cents}";
	}


	/// <summary>Adjusts the hurt vignette (red around edges of screen) on the GUI.</summary>
	public void AdjustHurtVignette (double percentHP) {
		double strength;

		if (percentHP < 0.34) {
			strength = 0.1;
		} else {
			// NOTE: What the actual heck does this do? Can we make this more clear?
			strength = 0.01 * (100 * percentHP) - 0.274;
		}

		if (percentHP == 1.0) {
			vignetteBox.Visible = false;
		} else {
			vignetteBox.Visible = true;
		}

		hurtVignette.SetShaderParameter("inner_radius", strength);
	}


	/// <summary>
	/// Updates the ammo label with the amount in the magazine and in reserve.
	/// </summary>
	/// <param name="magazine">The number of bullets in the magazine of the current weapon.</param>
	/// <param name="reserve">The number of bullets in reserve currently.</param>
	public void UpdateAmmo (Ammo currentAmmo) {
		ammoLabel.Text = $"{currentAmmo.AmmoInMagazine} / {currentAmmo.AmmoInReserve}";
	}


	/// <summary>
	/// Show the purchase label for the given barrier at <c>cost</c> cost.
	/// </summary>
	/// <param name="cost">The amount to display on the label.</param>
	public void ShowPurchaseLabel (int cost) {
		string cents = cost % 100 == 0 ? "00" : (cost % 100).ToString();

		purchaseLabel.Text = $"[F] Clear for ${cost/100}.{cents}";
		purchaseLabel.Visible = true;
	}


	public void HidePurchaseLabel () {
		purchaseLabel.Visible = false;
	}


	public void ShowReloadBar () {
		reloadBar.Visible = true;
	}


	public void HideReloadBar () {
		reloadBar.Visible = false;
	}


	public void UpdateReloadBar (double percent) {
		reloadBar.Value = percent;
	}


	public void UpdateGunLabel (string gunName) {
		gunLabel.Text = gunName;
	}


	public void UpdateInfectionBar (double progress) {
		infectionBar.Value = progress;
	}


	public void ShowLabel(string text) {
		purchaseLabel.Visible = true;
		purchaseLabel.Text = text;
	}


	public void HideLabel() {
		purchaseLabel.Visible = false;
	}


	public void ShowGameOver() {
		gameOverScreen.Visible = true;
	}

}
