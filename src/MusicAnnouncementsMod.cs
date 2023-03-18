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
	[BepInPlugin("sabreml.musicannouncements", "MusicAnnouncements", "1.2.0")]
	public class MusicAnnouncementsMod : BaseUnityPlugin
	{
		// The current mod version. (Stored here as a variable so that I don't have to update it in as many places.)
		public static string Version;

		// The name of the song to announce. (Also used to display the track name in the pause menu)
		public static string SongToAnnounce;

		// The number of attempts to make to try and announce the music.
		public static int AnnounceAttempts;

		public void OnEnable()
		{
			// Take the version number that was given to `BepInPlugin()` above.
			Version = Info.Metadata.Version.ToString();

			On.RainWorld.OnModsInit += RainWorld_OnModsInitHK;

			// In-game announcement hooks.
			On.Music.Song.ctor += SongHK;
			On.Music.MusicPiece.StopAndDestroy += MusicPiece_StopAndDestroyHK;
			On.Music.MusicPlayer.Update += MusicPlayer_UpdateHK;

			// Pause menu hooks.
			PauseMenuText.SetupHooks();
		}

		private void RainWorld_OnModsInitHK(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			// Set up the remix menu.
			MachineConnector.SetRegisteredOI(Info.Metadata.GUID, new MusicAnnouncementsConfig());
		}

		// Called when a new song is instantiated.
		// If it's the correct type of song (playing as an event ingame), `SongToAnnounce` and `AnnounceAttempts` are set.
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
			if (self.GetType() != typeof(Song)) // Skip over other types of music too. (Ending cutscenes, music pearl, etc.)
			{
				return;
			}

			// The full `name` will be something like "RW_24 - Kayava". We want the part after the dash.
			SongToAnnounce = Regex.Split(name, " - ")[1];
			
			if (MusicAnnouncementsConfig.IngameText.Value) // Gameplay announcements are enabled.
			{
				AnnounceAttempts = 500; // 500 attempts
			}
			else
			{
				Debug.Log("(MusicAnnouncements) Skipping gameplay announcement due to config");
			}
		}

		// Called when a song ends (or is otherwise deleted).
		private void MusicPiece_StopAndDestroyHK(On.Music.MusicPiece.orig_StopAndDestroy orig, MusicPiece self)
		{
			orig(self);
			if (self.GetType() == typeof(Song))
			{
				SongToAnnounce = null;
			}
		}

		// Every time `MusicPlayer` updates ingame, try to show the player the announcement (If there is one).
		// Code mostly taken from `Music.MultiplayerDJ.Update()`.
		private void MusicPlayer_UpdateHK(On.Music.MusicPlayer.orig_Update orig, MusicPlayer self)
		{
			orig(self);

			if (AnnounceAttempts < 1)
			{
				return; // If we're out of attempts, don't go any further.
			}
			AnnounceAttempts--;

			if (self.manager.currentMainLoop is RainWorldGame gameLoop)
			{
				if (gameLoop.cameras[0]?.room?.ReadyForPlayer != null && gameLoop.cameras[0].hud?.textPrompt?.messages?.Count == 0)
				{
					Debug.Log("(MusicAnnouncements) Announcing " + SongToAnnounce);
					AddMusicMessage_HideHUD(gameLoop.cameras[0].hud.textPrompt, SongToAnnounce, 240);
					AnnounceAttempts = 0;
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
