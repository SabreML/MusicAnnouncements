using BepInEx;
using HUD;
using Music;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace MusicAnnouncements
{
	[BepInPlugin("sabreml.musicannouncements", "MusicAnnouncements", VERSION)]
	public class MusicAnnouncementsMod : BaseUnityPlugin
	{
		// The current mod version. (Stored here as a const so that I don't have to update it in as many places.)
		public const string VERSION = "1.3.0";

		// The name of the song to announce. (Also used to display the track name in the pause menu)
		public static string SongToAnnounce;

		// The number of attempts to make to try and announce the music.
		public static int AnnounceAttempts;

		// Dictionary of "Background Music" tracks, and their respective community-made "display names".
		// (The display names are from Rain Land Society's 'The Hidden Songs' youtube playlist.)
		private static readonly Dictionary<string, string> backgroundMusicNames = new Dictionary<string, string>
		{
			// Basegame.
			{ "BM_CC_CANOPY",		"Clairvoyant Canopy" },
			{ "BM_DS_GATE",			"Drainage Duct" },
			{ "BM_HI_GATE",			"Indusrial Atrium" },
			{ "BM_SB_FILTER",		"Filtration System" },
			{ "BM_SB_SUBWAY",		"Desolate Subway" },
			{ "BM_SH_CRYPTS",		"Memory Crypts" },
			{ "BM_SI_STRUT",		"Serpentine Struts" },
			{ "BM_SL_SHORE",		"Gate to the Shoreline" },
			{ "BM_SS_DOOR",			"Entrance of a Superstructure" },
			{ "BM_UW_UNDERHANG",	"Underhang" },
			{ "BM_UW_UPPERWALL",	"Upperwall" },
			{ "BM_UW_WALL",			"The Wall" },

			// Watcher
			{ "BM_RWTW_ASCENT",				"Ascent" },
			{ "BM_RWTW_BLUEENVOI",			"Blue Envoi" },
			{ "BM_RWTW_COLLAPSEDEARTH",		"Collapsed Earth" },
			{ "BM_RWTW_IMPACTSUB",			"Impact Sub" },
			{ "BM_RWTW_INANNA",				"Inanna" },
			{ "BM_RWTW_MOURNFULECLIPSE",	"Mournful Eclipse" },
			{ "BM_RWTW_UNDISTURBED",		"Undisturbed" },
			{ "BM_RWTW_UNDSQUEAK",			"Undsqueak" },
			{ "BM_RWTW_VERTUNE",			"Vertune" },
			{ "BM_RWTW_VIOLETABYSS",		"Violet Abyss" },
		};

		public void OnEnable()
		{
			On.RainWorld.OnModsInit += RainWorld_OnModsInitHK;

			// In-game announcement hooks.
			On.Music.Song.ctor += SongHK;
			On.Music.MusicPiece.StopAndDestroy += MusicPiece_StopAndDestroyHK;
			On.Music.MusicPlayer.Update += MusicPlayer_UpdateHK;
			On.HUD.TextPrompt.Draw += TextPrompt_DrawHK;

			// Pause menu hooks.
			PauseMenuText.SetUpHooks();
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
			if (self.GetType() != typeof(Song)) // Skip over other types of music too. (Ending cutscenes, music pearl, etc.)
			{
				return;
			}

			// Get the track name.
			string displayName = ParseTrackName(name);
			if (displayName == string.Empty)
			{
				return;
			}
			SongToAnnounce = displayName;

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

		private string ParseTrackName(string fullTrackName)
		{
			// Background music without a track name. (E.g. "BM_CC_CANOPY")
			if (fullTrackName.StartsWith("BM_"))
			{
				// If the user doesn't have the BGM setting enabled, then don't announce this at all.
				if (!MusicAnnouncementsConfig.ShowBackgroundMusic.Value)
				{
					Debug.Log($"(MusicAnnouncements) Skipping '{fullTrackName}' announcement due to config");
					return string.Empty;
				}

				// Otherwise, try to return the track's associated display name from `backgroundMusicNames`.
				if (backgroundMusicNames.TryGetValue(fullTrackName, out string displayName))
				{
					return displayName;
				}
				else
				{
					// If there isn't an associated display name, make a log to explain why the announcement might look weird.
					Debug.Log($"(MusicAnnouncements) No custom display name exists for '{fullTrackName}'");
					// Return the full track name since the setting is enabled.
					return fullTrackName;
				}
			}

			// If it's not background music and contains a dash, then the full name will likely be along the lines of "RW_24 - Kayava".
			if (fullTrackName.Contains(" - "))
			{
				// We want the part after the dash. (In the above case this would be "Kayava".)
				return Regex.Split(fullTrackName, " - ")[1];
			}

			// If it's not background music or regular music, then it might be something added by a mod.
			// Just announce its full track name in this case.
			return fullTrackName;
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
					Debug.Log($"(MusicAnnouncements) Announcing '{SongToAnnounce}'");
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
