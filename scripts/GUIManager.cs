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

		Global.HurtVignette.SetShaderParameter("inner_radius", 1.0);
		Global.VignetteBox.Visible = false;
		Global.ReloadBar.Visible = false;
		Global.ActiveLabel.Visible = false;
		Global.StaminaBar.Visible = false;
	}


	/// <summary>Updates the points label.</summary>
	/// <param name="points">The number of points held by the player.</param>
	public void UpdatePoints(int points) {
		string cents = points % 100 == 0 ? "00" : (points % 100).ToString();
		Global.PointLabel.Text = $"${points/100}.{cents}";
	}


	/// <summary>
	/// Adjusts the hurt vignette (red around edges of screen) on the GUI.
	/// </summary>
	public void AdjustHurtVignette (double percentHP) {
		double strength;

		if (percentHP < 0.34) {
			strength = 0.1;
		} else {
			// NOTE: What the actual heck does this do? Can we make this more clear?
			strength = 0.01 * (100 * percentHP) - 0.274;
		}

		if (percentHP == 1.0) {
			Global.VignetteBox.Visible = false;
		} else {
			Global.VignetteBox.Visible = true;
		}

		Global.HurtVignette.SetShaderParameter("inner_radius", strength);
	}


	/// <summary>
	/// Updates the ammo label with the amount in the magazine and in reserve.
	/// </summary>
	/// <param name="magazine">The number of bullets in the magazine of the current weapon.</param>
	/// <param name="reserve">The number of bullets in reserve currently.</param>
	public void UpdateAmmo (int ammoInMagazine, int ammoInReserve) {
		Global.AmmoLabel.Text = $"{ammoInMagazine} / {ammoInReserve}";
	}


	public void ShowActiveLabel (int cost) {
		string cents = cost % 100 == 0 ? "00" : (cost % 100).ToString();

		Global.ActiveLabel.Text = $"[F] Clear for ${cost/100}.{cents}";
		Global.ActiveLabel.Visible = true;
	}

	public void ShowActiveLabel (string activeText) {
		Global.ActiveLabel.Text = activeText;
		Global.ActiveLabel.Visible = true;
	}


	public void HideActiveLabel () {
		Global.ActiveLabel.Visible = false;
	}


	public void ToggleReloadBar (bool enabled) {
		Global.ReloadBar.Visible = enabled;
	}


	public void UpdateReloadBar (double reloadProgress) {
		Global.ReloadBar.Value = reloadProgress;
	}


	public void UpdateWeaponLabel (string weaponName) {
		Global.WeaponLabel.Text = weaponName;
	}


	public void UpdateInfectionBar (double infectionProgress) {
		Global.InfectionBar.Value = infectionProgress;
	}

	public void UpdateStaminaBar (float stamina) {
		Global.StaminaBar.Value = stamina * 100;

		if (stamina > 0.9) {
			Color modulate = Global.StaminaBar.Modulate;
			modulate.A = -10 * stamina + 10;
			Global.StaminaBar.Modulate = modulate;
		} else if (stamina == 1.0) {
			Global.StaminaBar.Visible = false;
		} else {
			Global.StaminaBar.Visible = true;
		}
	}


	public void ToggleStaminaBar (bool isExhausted) {
		if (isExhausted) {
			Global.StaminaBar.TextureUnder = Global.ExhaustedStaminaBarUnderTexture;
			Global.StaminaBar.TextureProgress = Global.ExhaustedStaminaBarProgressTexture;
		} else {
			Global.StaminaBar.TextureUnder = Global.StaminaBarUnderTexture;
			Global.StaminaBar.TextureProgress = Global.StaminaBarProgressTexture;
		}
	}


	public void ShowGameOver () {
		Global.GameOverScreen.Visible = true;
	}

}
