using BepInEx;
using Music;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace MusicAnnouncements
{
	[BepInPlugin("sabreml.musicannouncements", "MusicAnnouncements", "1.1.0")]
	public partial class MusicAnnouncementsMod : BaseUnityPlugin
	{
		// The current mod version. (Set here as a variable so that I don't have to update it in as many places.)
		public static readonly string version = "1.1.0";

		// The name of the song to announce. (Also used to display the track name in the pause menu)
		private string songToAnnounce;

		// The number of attempts to make to try and announce the music.
		private int announceAttempts;

		public void OnEnable()
		{
			On.RainWorld.OnModsInit += RainWorld_OnModsInitHK;

			// In-game announcement hooks.
			On.Music.Song.ctor += SongHK;
			On.Music.MusicPiece.StopAndDestroy += MusicPiece_StopAndDestroyHK;
			On.Music.MusicPlayer.Update += MusicPlayer_UpdateHK;

			// Pause menu hooks.
			SetupPauseMenuHooks();
		}

		private void RainWorld_OnModsInitHK(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			// Set up the remix menu.
			MachineConnector.SetRegisteredOI("sabreml.musicannouncements", new MusicAnnouncementsConfig());
		}

		// Called when a new song is instantiated.
		// If it's the correct type of song (playing as a random event ingame), `songToAnnounce` and `announceAttempts` are set.
		private void SongHK(On.Music.Song.orig_ctor orig, Song self, MusicPlayer musicPlayer, string name, MusicPlayer.MusicContext context)
		{
			orig(self, musicPlayer, name, context);
			if (context != MusicPlayer.MusicContext.StoryMode) // Ingame music only.
			{
				return;
			}
			if (!name.Contains(" - ")) // Probably background music. (E.g. 'BM_CC_CANOPY')
			{
				return;
			}
			if (self.GetType() != typeof(Song)) // Skip over other types of BGM too.
			{
				return;
			}

			// The full `name` will be something like "RW_24 - Kayava". We only want to announce the part after the dash.
			songToAnnounce = Regex.Split(name, " - ")[1];
			announceAttempts = 500; // 500 attempts
		}

		private void MusicPiece_StopAndDestroyHK(On.Music.MusicPiece.orig_StopAndDestroy orig, MusicPiece self)
		{
			orig(self);
			if (self.GetType() == typeof(Song))
			{
				songToAnnounce = null;
			}
		}

		// Every time `MusicPlayer` updates ingame, try to show the player the announcement (If there is one).
		// Code mostly taken from `Music.MultiplayerDJ.Update()`.
		private void MusicPlayer_UpdateHK(On.Music.MusicPlayer.orig_Update orig, MusicPlayer self)
		{
			orig(self);

			if (announceAttempts < 1)
			{
				return; // If we're out of attempts, don't go any further.
			}
			announceAttempts--;

			if (self.manager.currentMainLoop is RainWorldGame gameLoop)
			{
				if (gameLoop.cameras[0]?.room?.ReadyForPlayer != null && gameLoop.cameras[0].hud?.textPrompt?.messages?.Count == 0)
				{
					Debug.Log("(MusicAnnouncements) Announcing " + songToAnnounce);
					AddMusicMessage_HideHUD(gameLoop.cameras[0].hud.textPrompt, songToAnnounce, 240);
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
