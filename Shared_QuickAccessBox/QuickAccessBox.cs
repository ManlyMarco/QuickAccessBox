using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Studio;
using KK_QuickAccessBox.Thumbs;
using KK_QuickAccessBox.UI;
using UnityEngine;
using BepInEx.Configuration;
using KeyboardShortcut = BepInEx.Configuration.KeyboardShortcut;

namespace KK_QuickAccessBox
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInProcess(KoikatuAPI.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInDependency(Screencap.ScreenshotManager.GUID, Screencap.ScreenshotManager.Version)]
    [BepInDependency("KK_OrthographicCamera", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("gravydevsupreme.xunity.autotranslator", BepInDependency.DependencyFlags.SoftDependency)]
    public partial class QuickAccessBox : BaseUnityPlugin
    {
        public const string Version = "3.0";

        internal static new ManualLogSource Logger;
        internal static QuickAccessBox Instance;

        internal InterfaceManager Interface { get; set; }

        private static readonly string _blacklistPath = Path.Combine(Paths.CachePath, "KK_QuickAccessBox.blacklist");
        private static readonly string _favesPath = Path.Combine(Paths.CachePath, "KK_QuickAccessBox.faves");
        private static readonly string _recentsPath = Path.Combine(Paths.CachePath, "KK_QuickAccessBox.recents");
        internal ItemGrouping Blacklisted { get; private set; }
        internal ItemGrouping Favorited { get; private set; }
        internal ItemRecents Recents { get; private set; }

        private const string DESCRIPTION_RECENTS = "How many items that were recently opened by using the search box should be stored. Recent items are displayed when search box is empty, ordered by date of last use. Set to 0 to disable the feature.";
        private const string DESCRIPTION_DEVINFO = "The search box will search asset filenames, group/category/item ID numbers, manifests and other things from list files.\nRequires studio restart to take effect.";
        private const string DESCRIPTION_THUMBGENKEY = "Automatically generate thumbnails for all items. Hold the Esc key to abort.\n\n" +
                                                       "Items with existing thumbnails in zipmods are skipped. Items with thumbnails already present in the output folder are skipped too. " +
                                                       "To re-do certain thumbnails just remove them from the folder. If they are in a zipmod you have remove them from the zipmod.";
        private const string DESCRIPTION_DARKBG = "Only use for items that are impossible to see on light background like flares.";
        private const string DESCRIPTION_MANUALADJUST = "After spawning the object, generator will wait for you to adjust the camera position to get the object in " +
                                                    "the middle of the screen. Press left Shift to accept and move to the next item.";
        private const string DESCRIPTION_THUMBDIR = "Directory to save newly generated thumbs into. The directory must exist. Existing thumbs are not overwritten, remove them to re-create.";
        private const string DESCRIPTION_WINPOS = "Position at which the search window first opens. Can be changed by dragging the edges of the window.";

        public static ConfigEntry<KeyboardShortcut> KeyShowBox { get; private set; }
        public static ConfigEntry<bool> SearchDeveloperInfo { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ThumbGenerateKey { get; private set; }
        public static ConfigEntry<string> ThumbStoreLocation { get; private set; }
        public static ConfigEntry<bool> ThumbDarkBackground { get; private set; }
        public static ConfigEntry<bool> ThumbManualAdjust { get; private set; }
        public static ConfigEntry<int> RecentsCount { get; private set; }

        public static ConfigEntry<Vector2> WindowPosition { get; private set; }
        public static ConfigEntry<float> InterfaceScale { get; private set; }

        [Browsable(false)]
        public bool ShowBox
        {
            get => Interface.Visible;
            set => Interface.Visible = value;
        }

        /// <summary>
        /// List of all studio items that can be added into the game
        /// </summary>
        [Browsable(false)]
        public IEnumerable<ItemInfo> ItemList => ItemInfoLoader.ItemList;

        /// <summary>
        /// Register a custom provider for item thumbnails used in the item list.
        /// It should return a thumbnail sprite or a null. If it returns null, the next provider in the list is called.
        /// Thumbnail providers are called only once when the item is first shown in the list. The result is cached and never rechecked.
        /// If no provider returns a thumbnail, zipmods will be searched, and if that fails the item will have a placeholder thumbnail.
        /// </summary>
        public static void RegisterThumbnailProvider(ThumbnailProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            ThumbnailLoader.ThumbnailProviders.Add(provider);
        }
        public delegate Sprite ThumbnailProvider(ItemInfo item);

        private void Start()
        {
            Logger = base.Logger;
            Instance = this;

            var advanced = new ConfigurationManagerAttributes { IsAdvanced = true };

            KeyShowBox = Config.Bind("General", "Show quick access box", new KeyboardShortcut(KeyCode.Space, KeyCode.LeftControl), "Toggles the item search box on and off.");
            RecentsCount = Config.Bind("General", "Number of recents to remember", 20, new ConfigDescription(DESCRIPTION_RECENTS, new AcceptableValueRange<int>(0, 200)));
            SearchDeveloperInfo = Config.Bind("General", "Search developer information", false, new ConfigDescription(DESCRIPTION_DEVINFO, null, advanced));

            ThumbGenerateKey = Config.Bind("Thumbnail generation", "Generate item thumbnails", new KeyboardShortcut(), new ConfigDescription(DESCRIPTION_THUMBGENKEY, null, advanced));
            ThumbManualAdjust = Config.Bind("Thumbnail generation", "Manual mode", false, new ConfigDescription(DESCRIPTION_MANUALADJUST, null, advanced));
            ThumbDarkBackground = Config.Bind("Thumbnail generation", "Dark background", false, new ConfigDescription(DESCRIPTION_DARKBG, null, advanced));
            ThumbStoreLocation = Config.Bind("Thumbnail generation", "Output directory", string.Empty, new ConfigDescription(DESCRIPTION_THUMBDIR, null, advanced));

            WindowPosition = Config.Bind("General", "Initial window position", Vector2.zero, new ConfigDescription(DESCRIPTION_WINPOS, null, new ConfigurationManagerAttributes { Browsable = false }));
            InterfaceScale = Config.Bind("General", "Interface scale", 1f, new ConfigDescription("Scale of the search window compared to normal.", new AcceptableValueRange<float>(0.5f, 1.5f)));
            InterfaceScale.SettingChanged += (sender, args) => Interface?.SetScale(InterfaceScale.Value);

            StartCoroutine(LoadingCo());
        }

        private void OnDestroy()
        {
            // Must run Dispose on these to save caches and settings
            // todo: don't rely on game closing cleanly
            Interface?.Dispose();
            ThumbnailLoader.Dispose();
            ItemInfoLoader.Dispose();
        }

        private void Update()
        {
            if (KeyShowBox.Value.IsDown())
            {
                if (IsLoaded())
                    ShowBox = !ShowBox;
            }
            else if (ThumbGenerateKey.Value.IsDown())
            {
                if (IsLoaded())
                    StartCoroutine(ThumbnailGenerator.MakeThumbnail(ItemList, ThumbStoreLocation.Value, ThumbManualAdjust.Value, ThumbDarkBackground.Value));
            }
        }

        private bool IsLoaded()
        {
            if (ItemList == null || Interface == null)
            {
                Logger.LogMessage("Item list is still loading, please try again in a few seconds");
                return false;
            }
            return true;
        }

        private IEnumerator LoadingCo()
        {
            yield return new WaitUntil(() => StudioAPI.StudioLoaded);
            // Wait until fully loaded
            yield return null;

            ThreadingHelper.Instance.StartAsyncInvoke(() =>
            {
                // Runs async
                ItemInfoLoader.LoadItems();
                Recents = new ItemRecents(_recentsPath, true, null);
                Favorited = new ItemGrouping(_blacklistPath, true, RefreshList);
                Blacklisted = new ItemGrouping(_favesPath, true, RefreshList);
                return () =>
                {
                    // Runs sync
                    Interface = new InterfaceManager();
                    Interface.Visible = false;
                    Interface.SetScale(InterfaceScale.Value);

                    ThumbnailLoader.LoadAssetBundle();
                };
            });
        }

        internal void CreateItem(ItemInfo info, bool parented)
        {
            //todo parenting
            Logger.LogDebug("Creating item " + info);
            info.AddItem();

            Recents.BumpItemLastUseDate(info.NewCacheId);
        }

        public void RefreshList()
        {
            var searchStrings = GetSearchStrings();
            switch (Interface.ListFilteringType)
            {
                case ListVisibilityType.Filtered:
                    if (searchStrings.Key.Count == 0 && searchStrings.Value.Count == 0)
                    {
                        Interface.SetList(ItemList.Select(i => new { i, isRecent = Recents.TryGetLastUseDate(i.NewCacheId, out var date), date, isfav = Favorited.Check(i.GUID, i.NewCacheId) })
                                                   .Where(x => x.isfav || x.isRecent)
                                                   .OrderByDescending(x => x.date)
                                                   .ThenByDescending(x => x.i.ItemName)
                                                   .Select(x => x.i));
                    }
                    else
                    {
                        Interface.SetList(ItemList.Where(info => !Blacklisted.Check(info.GUID, info.NewCacheId) && ItemMatchesSearch(info, searchStrings.Key, searchStrings.Value)));
                    }
                    break;
                case ListVisibilityType.Favorites:
                    Interface.SetList(ItemList.Where(info => Favorited.Check(info.GUID, info.NewCacheId) && ItemMatchesSearch(info, searchStrings.Key, searchStrings.Value)));
                    break;
                case ListVisibilityType.Hidden:
                    Interface.SetList(ItemList.Where(info => Blacklisted.Check(info.GUID, info.NewCacheId) && ItemMatchesSearch(info, searchStrings.Key, searchStrings.Value)));
                    break;
                case ListVisibilityType.All:
                    Interface.SetList(ItemList.Where(info => ItemMatchesSearch(info, searchStrings.Key, searchStrings.Value)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(Interface.ListFilteringType.ToString());
            }
        }

        private KeyValuePair<List<string>, List<string>> GetSearchStrings()
        {
            var val = new KeyValuePair<List<string>, List<string>>(new List<string>(4), new List<string>(4));

            var searchStr = Interface.SearchString;
            if (string.IsNullOrEmpty(searchStr))
                return val;

            var splitSearchStr = searchStr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var str in splitSearchStr)
            {
                if (string.IsNullOrEmpty(str))
                    continue;

                var negative = str[0] == '-';
                if (negative)
                {
                    if (str.Length == 1)
                        continue;
                    val.Value.Add(str.Substring(1));
                }
                else
                {
                    val.Key.Add(str);
                }
            }

            return val;
        }

        private static bool ItemMatchesSearch(ItemInfo item, List<string> positiveSearchStrings, List<string> negativeSearchStrings)
        {
            var matchString = item.SearchString;
            for (var i = 0; i < positiveSearchStrings.Count; i++)
            {
                var str = positiveSearchStrings[i];
                var match = matchString.IndexOf(str, StringComparison.Ordinal) >= 0;
                if (!match) return false;
            }

            for (var i = 0; i < negativeSearchStrings.Count; i++)
            {
                var str = negativeSearchStrings[i];
                var match = matchString.IndexOf(str, StringComparison.Ordinal) >= 0;
                if (match) return false;
            }

            return true;
        }
    }
}
