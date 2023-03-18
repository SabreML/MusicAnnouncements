using Menu.Remix.MixedUI;
using UnityEngine;

namespace MusicAnnouncements
{
	public class MusicAnnouncementsConfig : OptionInterface
	{
		public static Configurable<bool> IngameText;
		public static Configurable<bool> PauseMenuText;

		public MusicAnnouncementsConfig()
		{
			IngameText = config.Bind("ingameText", true, new ConfigurableInfo("Announce newly playing songs at the bottom left of the screen in-game.", tags: new object[]
			{
				"Announce new songs in-game"
			}));
			PauseMenuText = config.Bind("pauseMenuText", true, new ConfigurableInfo("Show the name of the currently playing song in the top right of the pause menu.", tags: new object[]
			{
				"Show currently playing song in the pause menu"
			}));
		}

		// Called when the config menu is opened by the player.
		public override void Initialize()
		{
			base.Initialize();
			Tabs = new OpTab[]
			{
				new OpTab(this, "Options")
			};

			AddDivider(593f);
			AddTitle();
			AddDivider(540f);
			AddCheckbox(IngameText, 500f);
			AddCheckbox(PauseMenuText, 460f);
		}

		// Combines two flipped 'LinearGradient200's together to make a fancy looking divider.
		private void AddDivider(float y)
		{
			OpImage dividerLeft = new OpImage(new Vector2(300f, y), "LinearGradient200");
			dividerLeft.sprite.SetAnchor(0.5f, 0f);
			dividerLeft.sprite.rotation = 270f;

			OpImage dividerRight = new OpImage(new Vector2(300f, y), "LinearGradient200");
			dividerRight.sprite.SetAnchor(0.5f, 0f);
			dividerRight.sprite.rotation = 90f;

			Tabs[0].AddItems(new UIelement[]
			{
				dividerLeft,
				dividerRight
			});
		}

		// Adds the mod name and version to the interface.
		private void AddTitle()
		{
			OpLabel title = new OpLabel(new Vector2(150f, 560f), new Vector2(300f, 30f), "Music Announcements Mod", bigText: true);
			OpLabel version = new OpLabel(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {MusicAnnouncementsMod.Version}");

			Tabs[0].AddItems(new UIelement[]
			{
				title,
				version
			});
		}

		// Adds a checkbox tied to the config setting passed through `optionText`, as well as a label next to it with a description.
		private void AddCheckbox(Configurable<bool> optionText, float y)
		{
			OpCheckBox checkbox = new OpCheckBox(optionText, new Vector2(150f, y))
			{
				description = optionText.info.description
			};

			OpLabel checkboxLabel = new OpLabel(150f + 40f, y + 2f, optionText.info.Tags[0] as string)
			{
				description = optionText.info.description
			};

			Tabs[0].AddItems(new UIelement[]
			{
				checkbox,
				checkboxLabel
			});
		}
	}
}
