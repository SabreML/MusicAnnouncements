using Menu.Remix.MixedUI;
using UnityEngine;

namespace MusicAnnouncements
{
	public class MusicAnnouncementsConfig : OptionInterface
	{
		public static Configurable<bool> pauseMenuText;

		public MusicAnnouncementsConfig()
		{
			pauseMenuText = config.Bind("pauseMenuText", true, new ConfigurableInfo("Show the name of the currently playing song in the top right of the pause menu.", tags: new object[]
			{
				"Show currently playing song in the pause menu"
			}));
		}

		// Called when the config menu is opened by the player. (I think)
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
			AddCheckbox();
		}

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

		private void AddTitle()
		{
			OpLabel title = new OpLabel(new Vector2(150f, 560f), new Vector2(300f, 30f), "Music Announcements Mod", bigText: true);
			OpLabel version = new OpLabel(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {MusicAnnouncementsMod.version}");

			Tabs[0].AddItems(new UIelement[]
			{
				title,
				version
			});
		}

		private void AddCheckbox()
		{
			OpCheckBox checkbox = new OpCheckBox(pauseMenuText, new Vector2(150f, 500f))
			{
				description = pauseMenuText.info.description
			};

			OpLabel checkboxLabel = new OpLabel(150f + 40f, 500f + 2f, pauseMenuText.info.Tags[0] as string)
			{
				description = pauseMenuText.info.description
			};

			Tabs[0].AddItems(new UIelement[]
			{
				checkbox,
				checkboxLabel
			});
		}
	}
}
