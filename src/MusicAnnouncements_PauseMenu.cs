using Menu;
using UnityEngine;

namespace MusicAnnouncements
{
	public partial class MusicAnnouncementsMod
	{
		// The 'Currently Playing: x' text in the top right corner.
		private MenuLabel menuCurrentlyPlaying;
		// The music note icon next to the text.
		private FSprite menuMusicSprite;


		private void SetupPauseMenuHooks()
		{
			On.Menu.PauseMenu.ctor += PauseMenuHK;
			On.Menu.PauseMenu.ShutDownProcess += PauseMenu_ShutDownProcessHK;
			On.HUD.TextPrompt.Draw += TextPrompt_DrawHK;
		}

		// Called when the pause menu is opened.
		// If there's a song currently playing, this adds a label in the top right corner of the screen displaying its name.
		private void PauseMenuHK(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
		{
			orig(self, manager, game);
			if (songToAnnounce == null)
			{
				return;
			}
			if (!MusicAnnouncementsConfig.PauseMenuText.Value) // If the player has unticked `pauseMenuText` in the mod's config.
			{
				Debug.Log("(MusicAnnouncements) Skipping pause menu text due to config");
				return;
			}

			float posX = game.rainWorld.options.ScreenSize.x - 10.01f;
			float posY = game.rainWorld.options.ScreenSize.y - 13f;

			menuCurrentlyPlaying = new MenuLabel(self, self.pages[0], $" ~ Currently Playing: {songToAnnounce}", new Vector2(posX, posY), Vector2.zero, true);
			menuCurrentlyPlaying.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
			menuCurrentlyPlaying.label.alignment = FLabelAlignment.Right; // Align on the right so that longer song names don't go off the screen.
			menuCurrentlyPlaying.label.alpha = 0f;

			menuMusicSprite = new FSprite("musicSymbol", true)
			{
				x = posX - menuCurrentlyPlaying.label.textRect.width - 9.01f,
				y = posY - 2.01f,
				color = menuCurrentlyPlaying.label.color,
				alpha = 0f
			};

			self.pages[0].subObjects.Add(menuCurrentlyPlaying);
			self.pages[0].Container.AddChild(menuMusicSprite);
			Debug.Log($"(MusicAnnouncements) Currently Playing: {songToAnnounce}");
		}

		private void PauseMenu_ShutDownProcessHK(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
		{
			if (menuCurrentlyPlaying != null)
			{
				RemoveCurrentlyPlaying(); // Called before `orig()` so that it doesn't waste time trying to remove itself twice.
			}
			orig(self);
		}

		// Called when the "Paused" text updates its appearance. (Every frame ish)
		// This is used to steal its alpha value and fade in along with it, or to smoothly fade out if the song ends.
		private void TextPrompt_DrawHK(On.HUD.TextPrompt.orig_Draw orig, HUD.TextPrompt self, float timeStacker)
		{
			orig(self, timeStacker);
			if (menuCurrentlyPlaying == null) // No text to modify.
			{
				return;
			}

			float newAlpha;
			if (songToAnnounce != null)
			{
				newAlpha = self.label.alpha; // Fade in.
			}
			else // If the text exists but there's no song, then it ended while the menu was open.
			{
				newAlpha = RWCustom.Custom.LerpAndTick(menuCurrentlyPlaying.label.alpha, 0f, 0.02f, 0.02f); // Fade out.
				if (newAlpha <= 0f)
				{
					Debug.Log("(MusicAnnouncements) Song ended, removing menu text");
					RemoveCurrentlyPlaying();
					return;
				}
			}

			menuCurrentlyPlaying.label.alpha = newAlpha;
			menuMusicSprite.alpha = newAlpha;
		}

		// Clean up our variables.
		private void RemoveCurrentlyPlaying()
		{
			menuCurrentlyPlaying.RemoveSprites();
			menuCurrentlyPlaying.owner.RemoveSubObject(menuCurrentlyPlaying);
			menuCurrentlyPlaying = null;

			menuMusicSprite.RemoveFromContainer();
			menuMusicSprite = null;
		}
	}
}
