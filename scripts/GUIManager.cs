// GUIManager.cs
//
// Manages the GUI elements, such as health, ammo, and purchase labels.

using Godot;

public partial class GUIManager : Node
{
	SharedData Global;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Global = SharedData.Instance;

		Global.hurtVignette.SetShaderParameter("inner_radius", 1.0);
		Global.vignetteBox.Visible = false;
		Global.reloadBar.Visible = false;
		Global.purchaseLabel.Visible = false;
	}

	/// <summary>Updates the points label.</summary>
	/// <param name="points">The number of points held by the player.</param>
	public void UpdatePoints(int points) {
		string cents = points % 100 == 0 ? "00" : (points % 100).ToString();
		Global.pointLabel.Text = $"${points/100}.{cents}";
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
			Global.vignetteBox.Visible = false;
		} else {
			Global.vignetteBox.Visible = true;
		}

		Global.hurtVignette.SetShaderParameter("inner_radius", strength);
	}


	/// <summary>
	/// Updates the ammo label with the amount in the magazine and in reserve.
	/// </summary>
	/// <param name="magazine">The number of bullets in the magazine of the current weapon.</param>
	/// <param name="reserve">The number of bullets in reserve currently.</param>
	public void UpdateAmmo (Ammo currentAmmo) {
		Global.ammoLabel.Text = $"{currentAmmo.AmmoInMagazine} / {currentAmmo.AmmoInReserve}";
	}


	/// <summary>
	/// Show the purchase label for the given barrier at <c>cost</c> cost.
	/// </summary>
	/// <param name="cost">The amount to display on the label.</param>
	public void ShowPurchaseLabel (int cost) {
		string cents = cost % 100 == 0 ? "00" : (cost % 100).ToString();

		Global.purchaseLabel.Text = $"[F] Clear for ${cost/100}.{cents}";
		Global.purchaseLabel.Visible = true;
	}


	public void HidePurchaseLabel () {
		Global.purchaseLabel.Visible = false;
	}


	public void ShowReloadBar () {
		Global.reloadBar.Visible = true;
	}


	public void HideReloadBar () {
		Global.reloadBar.Visible = false;
	}


	public void UpdateReloadBar (double percent) {
		Global.reloadBar.Value = percent;
	}


	public void UpdateGunLabel (string gunName) {
		Global.gunLabel.Text = gunName;
	}


	public void UpdateInfectionBar (double progress) {
		Global.infectionBar.Value = progress;
	}


	public void ShowLabel(string text) {
		Global.purchaseLabel.Visible = true;
		Global.purchaseLabel.Text = text;
	}


	public void HideLabel() {
		Global.purchaseLabel.Visible = false;
	}


	public void ShowGameOver() {
		Global.gameOverScreen.Visible = true;
	}

}
