using BepInEx;
using HUD;
using Music;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace MusicAnnouncements
{
	[BepInPlugin("sabreml.musicannouncements", "MusicAnnouncements", "1.2.4")]
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
			On.HUD.TextPrompt.Draw += TextPrompt_DrawHK;

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
			if (context == MusicPlayer.MusicContext.Menu) // Ingame music only.
			{
				return;
			}
			if (name.StartsWith("BM_")) // Background music. (E.g. 'BM_CC_CANOPY')
			{
				return;
			}
			if (self.GetType() != typeof(Song)) // Skip over other types of music too. (Ending cutscenes, music pearl, etc.)
			{
				return;
			}

			if (name.Contains(" - "))
			{
				// The full `name` will be something like "RW_24 - Kayava". We want the part after the dash.
				SongToAnnounce = Regex.Split(name, " - ")[1];
			}
			else
			{
				// If it's not background music or regular music, then it might be something added by a mod.
				// Just announce its full track name in this case.
				SongToAnnounce = name;
			}

			// Arena mode announces the song name in the bottom left by itself already, so there's no need to do it here with `AnnounceAttempts`.
			// (This is checked after `SongToAnnounce` is set so that the pause menu text still works.)
			if (context == MusicPlayer.MusicContext.Arena)
			{
				return;
			}

			if (MusicAnnouncementsConfig.IngameText.Value) // Gameplay announcements are enabled.
			{
				AnnounceAttempts = 500; // 500 frames worth of attempts
			}
			else // Gameplay announcements are disabled.
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
				// Reset everything back to default.
				SongToAnnounce = null;
				AnnounceAttempts = 0;
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
		private void AddMusicMessage_HideHUD(TextPrompt self, string text, int time)
		{
			self.messages.Add(new TextPrompt.MusicMessage("    ~ " + text, 0, time, false, true));
			if (self.messages.Count == 1)
			{
				self.InitNextMessage();
			}
		}

		// This is here mainly to hide the little quaver/quarter note 'music sprite' if there isn't currently a music announcement on the screen.
		// Without this, if another message of a higher priority overrides it (Pause menu, game over, etc.), the music sprite will still be visible overlapping with the new text.
		private void TextPrompt_DrawHK(On.HUD.TextPrompt.orig_Draw orig, TextPrompt self, float timeStacker)
		{
			orig(self, timeStacker);
			// If the current message is a music announcement, but it's being overridden by another type of `TextPrompt`.
			if ((self.musicSprite != null && self.messages.Count > 0 && self.messages[0] is TextPrompt.MusicMessage) && self.currentlyShowing != TextPrompt.InfoID.Message)
			{
				// Force the music sprite to be invisible.
				self.musicSprite.alpha = 0f;

				// Also, if the message override was caused by the pause menu being opened, force the black letterbox border at the top to be visible.
				// It only seems to disappear in Arena mode for whatever reason, but it happens consistently and the pause menu text looks weird without it.
				if (self.currentlyShowing == TextPrompt.InfoID.Paused)
				{
					float currentAlpha = Mathf.Lerp(self.lastShow, self.show, timeStacker);
					self.sprites[0].y = self.hud.rainWorld.screenSize.y - (30f + self.hud.rainWorld.options.SafeScreenOffset.y) * RWCustom.Custom.SCurve(Mathf.InverseLerp(0f, 0.5f, currentAlpha), 0.5f);
				}
			}
		}
	}
}
