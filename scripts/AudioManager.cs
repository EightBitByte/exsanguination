// AudioManager.cs
//
// Manages playback of audio, such as shooting sounds, infected noises, etc.

using Godot;
using System;

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
	private AudioStreamWav pistolShot, rifleShot, dryFire, pillSound;
	private AudioStreamMP3 pistolReload, rifleReload, buySound;

	private Player player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		pistolShot = GD.Load<AudioStreamWav>("res://assets/pistolshot.wav");
		rifleShot = GD.Load<AudioStreamWav>("res://assets/rifleshot.wav");
		pistolReload = GD.Load<AudioStreamMP3>("res://assets/pistolreload.mp3");
		rifleReload = GD.Load<AudioStreamMP3>("res://assets/riflereload.mp3");
		dryFire = GD.Load<AudioStreamWav>("res://assets/dryfire.wav");
		buySound = GD.Load<AudioStreamMP3>("res://assets/buy.mp3");
		pillSound = GD.Load<AudioStreamWav>("res://assets/pill.wav");

		player = GetNode<Player>("/root/MainScene/Player");

		for (int i = 0; i < AudioPlayerLimit; ++i) {
			audioStreamPlayerArray[i] = new AudioStreamPlayer2D();
			audioStreamPlayerArray[i].GlobalPosition = player.GlobalPosition;
			player.AddChild(audioStreamPlayerArray[i]);
		}
	}

	public void PlaySound(Sound sound) {
		currentAudioPlayerIdx = (currentAudioPlayerIdx + 1) % AudioPlayerLimit;
		AudioStreamPlayer2D currentAudioPlayer2D = audioStreamPlayerArray[currentAudioPlayerIdx];

		switch (sound) {
			case Sound.PistolShot:
				currentAudioPlayer2D.Stream = pistolShot;
				break;
			case Sound.RifleShot:
				currentAudioPlayer2D.Stream = rifleShot;
				break;
			case Sound.PistolReload:
				currentAudioPlayer2D.Stream = pistolReload;
				break;
			case Sound.RifleReload:
				currentAudioPlayer2D.Stream = rifleReload;
				break;
			case Sound.DryFire:
				currentAudioPlayer2D.Stream = dryFire;
				break;
			case Sound.Buy:
				currentAudioPlayer2D.Stream = buySound;
				break;
			case Sound.Pill:
				currentAudioPlayer2D.Stream = pillSound;
				break;
		}

		currentAudioPlayer2D.Play();
	}
}
