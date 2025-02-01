// AudioManager.cs
//
// Manages playback of audio, such as shooting sounds, infected noises, etc.

using Godot;
using System;
using System.Linq;

public enum Sound {
	PistolShot,
	RifleShot,
	PistolReload,
	RifleReload,
	DryFire,
	Buy,
	Pill
}

public partial class AudioManager : Node
{
	/// <summary>The number of audio players to instantiate upon creation.</summary>
	[Export]
	public int AudioPlayerLimit = 10;

	private int currentAudioPlayerIdx = 0;

	private AudioStreamPlayer2D[] audioStreamPlayerArray = new AudioStreamPlayer2D[10];

	private SharedData Global;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Global = SharedData.Instance;

		for (int i = 0; i < AudioPlayerLimit; ++i) {
			audioStreamPlayerArray[i] = new AudioStreamPlayer2D 
				{ GlobalPosition = Global.PlayerNode.GlobalPosition };
			Global.PlayerNode.AddChild(audioStreamPlayerArray[i]);
		}
	}

	public void PlaySound(int soundID) {
		currentAudioPlayerIdx = (currentAudioPlayerIdx + 1) % AudioPlayerLimit;
		AudioStreamPlayer2D currentAudioPlayer2D = 
			audioStreamPlayerArray[currentAudioPlayerIdx];

		switch ((Sound)soundID) {
			case Sound.PistolShot:
				currentAudioPlayer2D.Stream = Global.PistolShot;
				break;
			case Sound.RifleShot:
				currentAudioPlayer2D.Stream = Global.RifleShot;
				break;
			case Sound.PistolReload:
				currentAudioPlayer2D.Stream = Global.PistolReload;
				break;
			case Sound.RifleReload:
				currentAudioPlayer2D.Stream = Global.RifleReload;
				break;
			case Sound.DryFire:
				currentAudioPlayer2D.Stream = Global.DryFire;
				break;
			case Sound.Buy:
				currentAudioPlayer2D.Stream = Global.BuySound;
				break;
			case Sound.Pill:
				currentAudioPlayer2D.Stream = Global.PillSound;
				break;
		}

		currentAudioPlayer2D.Play();
	}
}
