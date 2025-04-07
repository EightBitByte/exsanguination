// AudioManager.cs
//
// Manages playback of audio, such as shooting sounds, infected noises, etc.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

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
	private AudioStreamPlayer2D reloadAudioPlayer;

	private SharedData Global;

	private Dictionary<Sound, AudioStreamWav> soundMap;
	private HashSet<Sound> reloadSounds;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Global = SharedData.Instance;
        reloadAudioPlayer = new()
			{ GlobalPosition = Global.PlayerNode.GlobalPosition };
        Global.PlayerNode.AddChild(reloadAudioPlayer);

		for (int i = 0; i < AudioPlayerLimit; ++i) {
			audioStreamPlayerArray[i] = new AudioStreamPlayer2D 
				{ GlobalPosition = Global.PlayerNode.GlobalPosition };
			Global.PlayerNode.AddChild(audioStreamPlayerArray[i]);
		}

		soundMap = new() {
			{ Sound.PistolShot, Global.PistolShot },
			{ Sound.RifleShot, Global.RifleShot },
			{ Sound.PistolReload, Global.PistolReload },
			{ Sound.RifleReload, Global.RifleReload },
			{ Sound.DryFire, Global.DryFire },
			{ Sound.Buy, Global.BuySound },
			{ Sound.Pill, Global.PillSound }
		};

		reloadSounds = new() {
			Sound.PistolReload,
			Sound.RifleReload
		};
	}


	public void PlaySound(int soundID) {
		Sound id = (Sound) soundID;

		if (!soundMap.TryGetValue(id, out var stream)) {
			GD.PrintErr($"PlaySound: Cannot find sound with ID {soundID}.");
			return;
		}

		if (reloadSounds.Contains(id)) {
			reloadAudioPlayer.Stream = soundMap[(Sound)soundID];
			reloadAudioPlayer.Play();
		} else {
			PlayFromAudioStreamPool(soundMap[id]);
		}
	}


	public void PlayFromAudioStreamPool(AudioStreamWav wav) {
		currentAudioPlayerIdx = (currentAudioPlayerIdx + 1) % AudioPlayerLimit;
		AudioStreamPlayer2D currentAudioPlayer2D = 
			audioStreamPlayerArray[currentAudioPlayerIdx];
		currentAudioPlayer2D.Stream = wav;
		currentAudioPlayer2D.Play();
	}

	public void CancelReloadSound() {
		reloadAudioPlayer.Stop();
	}
}
