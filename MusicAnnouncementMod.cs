using BepInEx;
using Music;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace MusicAnnouncement
{
	[BepInPlugin("sabreml.musicannouncement", "MusicAnnouncement", "1.0")]
    public class MusicAnnouncementMod : BaseUnityPlugin
    {
		private ProcessManager mainLoopCache; // For debugging

		// The name of the song to announce.
		private string songToAnnounce;

		// The number of attempts to make to try and announce the music.
		private int announceAttempts;

		public void OnEnable()
		{
			On.Music.MusicPlayer.ctor += MusicPlayerHK;
			On.Music.Song.ctor += SongHK;
			On.Music.MusicPlayer.Update += MusicPlayer_UpdateHK;
		}

		public void Update() // For debugging
		{
			if (Input.GetKeyDown(KeyCode.Keypad1))
			{
				StopMusicEvent stopMusicEvent = new StopMusicEvent
				{
					type = StopMusicEvent.Type.AllSongs,
					fadeOutTime = 120f
				};
				mainLoopCache.musicPlayer.GameRequestsSongStop(stopMusicEvent);
				Debug.Log("Stopping current music");
			}
			if (Input.GetKeyDown(KeyCode.Keypad2))
			{
				MusicEvent musicEvent = new MusicEvent
				{
					stopAtDeath = false,
					stopAtGate = false,
					oneSongPerCycle = false,
					cyclesRest = 0,
					songName = "RW_42 - Kayava"
				};
				mainLoopCache.musicPlayer.GameRequestsSong(musicEvent);
			}
			if (Input.GetKeyDown(KeyCode.Keypad3))
			{
				(mainLoopCache.currentMainLoop as RainWorldGame).cameras[0].hud.textPrompt.AddMessage("Wiggling around quickly might startle this creature.", 30, 250, true, true);
			}
		}

		private void MusicPlayerHK(On.Music.MusicPlayer.orig_ctor orig, MusicPlayer self, ProcessManager manager)
		{
			orig(self, manager);
			mainLoopCache = manager;
		}

		// Called when a new song is instantiated.
		// If it's the correct type of song (playing as a random event ingame), `songToAnnounce` and `announceAttempts` are set.
		private void SongHK(On.Music.Song.orig_ctor orig, Music.Song self, Music.MusicPlayer musicPlayer, string name, Music.MusicPlayer.MusicContext context)
		{
			orig(self, musicPlayer, name, context);
			if (context != MusicPlayer.MusicContext.StoryMode) // Ingame music only.
			{
				return;
			}
			if (self.GetType() != typeof(Song)) // Skip over `SSSong`. (Iterator background music)
			{
				Debug.Log("skipping");
				return;
			}

			// The full `name` will be something like "RW_24 - Kayava". We only want to announce the part after the dash.
			songToAnnounce = Regex.Split(name, " - ")[1];
			announceAttempts = 500; // 500 attempts
		}

		// Every time `MusicPlayer` updates ingame, try to show the player the announcement (If there is one).
		private void MusicPlayer_UpdateHK(On.Music.MusicPlayer.orig_Update orig, MusicPlayer self)
		{
			orig(self);
			if (songToAnnounce == null)
			{
				return;
			}

			announceAttempts--;
			if (announceAttempts < 1)
			{
				songToAnnounce = null;
				return; // If we're out of attempts, give up.
			}

			if (self.manager.currentMainLoop is RainWorldGame gameLoop)
			{
				if (gameLoop.cameras[0]?.room?.ReadyForPlayer != null && gameLoop.cameras[0].hud?.textPrompt?.messages?.Count == 0)
				{
					Debug.Log("Announcing " + songToAnnounce);
					AddMusicMessage_HideHUD(gameLoop.cameras[0].hud.textPrompt, songToAnnounce, 240);
					songToAnnounce = null;
					announceAttempts = 0;
				}
			}
		}

		// Exactly the same as `HUD.TextPrompt.AddMusicMessage()`, but with the `hideHud` argument set to `true`.
		// Without this it can look pretty weird if the HUD is up.
		private void AddMusicMessage_HideHUD(HUD.TextPrompt self, string text, int time)
		{
			self.messages.Add(new HUD.TextPrompt.MusicMessage("    ~ " + text, 0, time, false, true));
			if (self.messages.Count == 1)
			{
				self.InitNextMessage();
			}
		}
	}
}
