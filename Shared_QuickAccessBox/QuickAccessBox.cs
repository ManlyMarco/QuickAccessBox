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
using MessagePack;
using KeyboardShortcut = BepInEx.Configuration.KeyboardShortcut;

namespace KK_QuickAccessBox
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInDependency("KK_OrthographicCamera", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("gravydevsupreme.xunity.autotranslator", BepInDependency.DependencyFlags.SoftDependency)]
    public partial class QuickAccessBox : BaseUnityPlugin
    {
        public const string Version = "2.4";

        internal static new ManualLogSource Logger;

        private InterfaceManager _interface;

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

        [Browsable(false)]
        public bool ShowBox
        {
            get => _interface.Visible;
            set => _interface.Visible = value;
        }

        /// <summary>
        /// List of all studio items that can be added into the game
        /// </summary>
        public IEnumerable<ItemInfo> ItemList => ItemInfoLoader.ItemList;

        private void Start()
        {
            Logger = base.Logger;

            var advanced = new ConfigurationManagerAttributes { IsAdvanced = true };

            KeyShowBox = Config.Bind("General", "Show quick access box", new KeyboardShortcut(KeyCode.Space, KeyCode.LeftControl), "Toggles the item search box on and off.");
            RecentsCount = Config.Bind("General", "Number of recents to remember", 20, new ConfigDescription(DESCRIPTION_RECENTS, new AcceptableValueRange<int>(0, 200)));
            SearchDeveloperInfo = Config.Bind("General", "Search developer information", false, new ConfigDescription(DESCRIPTION_DEVINFO, null, advanced));

            ThumbGenerateKey = Config.Bind("Thumbnail generation", "Generate item thumbnails", new KeyboardShortcut(), new ConfigDescription(DESCRIPTION_THUMBGENKEY, null, advanced));
            ThumbManualAdjust = Config.Bind("Thumbnail generation", "Manual mode", false, new ConfigDescription(DESCRIPTION_MANUALADJUST, null, advanced));
            ThumbDarkBackground = Config.Bind("Thumbnail generation", "Dark background", false, new ConfigDescription(DESCRIPTION_DARKBG, null, advanced));
            ThumbStoreLocation = Config.Bind("Thumbnail generation", "Output directory", string.Empty, new ConfigDescription(DESCRIPTION_THUMBDIR, null, advanced));

            WindowPosition = Config.Bind("General", "Initial window position", Vector2.zero, new ConfigDescription(DESCRIPTION_WINPOS, null, new ConfigurationManagerAttributes { Browsable = false }));

            StartCoroutine(LoadingCo());
        }

        private void OnDestroy()
        {
            _interface?.Dispose();
            ThumbnailLoader.Dispose();
            ItemInfoLoader.Dispose();
            //SaveRecents();
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
            if (ItemList == null || _interface == null)
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
                ItemInfoLoader.LoadItems();
                LoadRecents();
                return () =>
                {
                    _interface = new InterfaceManager(OnListItemClicked, OnSearchStringChanged);
                    _interface.Visible = false;
                    ThumbnailLoader.LoadAssetBundle();
                };
            });
        }

        private void OnListItemClicked(ItemInfo info)
        {
            Logger.LogDebug($"Creating item {info.FullName} - GUID:{info.GUID} GroupNo:{info.GroupNo} CategoryNo:{info.CategoryNo} ItemNo:{info.ItemNo} FileName:{info.FileName}");
            info.AddItem();

            _recents[info.CacheId] = DateTime.UtcNow;
            TrimRecents();
            SaveRecents();
        }

        private void OnSearchStringChanged(string newStr)
        {
            // If no search string, show recents if any
            _interface.SetList(!string.IsNullOrEmpty(newStr)
                ? ItemList.Where(info => ItemMatchesSearch(info, newStr))
                : _recents.OrderByDescending(x => x.Value).Select(x => ItemList.FirstOrDefault(i => i.CacheId == x.Key)).Where(x => x != null));
        }

        private static bool ItemMatchesSearch(ItemInfo item, string searchStr)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (searchStr == null) throw new ArgumentNullException(nameof(searchStr));

            var matchString = item.SearchString;
            var splitSearchStr = searchStr.ToLowerInvariant().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return splitSearchStr.All(s => matchString.IndexOf(s, StringComparison.Ordinal) >= 0);
        }

        #region Recents

        private static readonly string _recentsPath = Path.Combine(Paths.CachePath, "KK_QuickAccessBox.recents");
        private Dictionary<string, DateTime> _recents = new Dictionary<string, DateTime>();

        private void SaveRecents()
        {
            try
            {
                File.WriteAllBytes(_recentsPath, MessagePackSerializer.Serialize(_recents));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to save recents: " + ex.Message);
            }
        }

        private void LoadRecents()
        {
            if (File.Exists(_recentsPath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(_recentsPath);
                    _recents = MessagePackSerializer.Deserialize<Dictionary<string, DateTime>>(bytes);
                    TrimRecents();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Failed to read recents: " + ex.Message);
                }
            }
        }

        private void TrimRecents()
        {
            foreach (var toRemove in _recents.OrderByDescending(x => x.Value).Skip(RecentsCount.Value))
                _recents.Remove(toRemove.Key);
        }

        #endregion
    }
}
