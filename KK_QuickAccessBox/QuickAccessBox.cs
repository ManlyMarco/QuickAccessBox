using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
    [BepInDependency(KoikatuAPI.GUID, "1.4")]
    [BepInDependency(Sideloader.Sideloader.GUID, "11.2.1")]
    [BepInProcess("CharaStudio")]
    public class QuickAccessBox : BaseUnityPlugin
    {
        public const string GUID = "KK_QuickAccessBox";
        internal const string Version = "2.1";
        internal new static ManualLogSource Logger;

        private InterfaceManager _interface;

        private const string DESCRIPTION_DEVINFO = "The search box will search asset filenames, group/category/item ID numbers, manifests and other things from list files.\nRequires studio restart to take effect.";
        private const string DESCRIPTION_THUMBGENKEY = "Automatically generate thumbnails for all items. Hold the Esc key to abort.\n\n" +
                                                       "Items with existing thumbnails in zipmods are skipped. Items with thumbnails already present in the output folder are skipped too. " +
                                                       "To re-do certain thumbnails just remove them from the folder. If they are in a zipmod you have remove them from the zipmod.";
        private const string DESCRIPTION_DARKBG = "Only use for items that are impossible to see on light background like flares.";
        private const string DESCRIPTION_MANUALADJUST = "After spawning the object, generator will wait for you to adjust the camera position to get the object in " +
                                                    "the middle of the screen. Press left Shift to accept and move to the next item.";
        private const string DESCRIPTION_THUMBDIR = "Directory to save newly generated thumbs into. The directory must exist. Existing thumbs are not overwritten, remove them to re-create.";
        
        public static ConfigEntry<KeyboardShortcut> KeyShowBox { get; private set; }
        public static ConfigEntry<bool> SearchDeveloperInfo { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ThumbGenerateKey { get; private set; }
        public static ConfigEntry<string> ThumbStoreLocation { get; private set; }
        public static ConfigEntry<bool> ThumbDarkBackground { get; private set; }
        public static ConfigEntry<bool> ThumbManualAdjust { get; private set; }

        [Browsable(false)]
        public bool ShowBox
        {
            get => _interface.Visible;
            set => _interface.Visible = value;
        }

        /// <summary>
        /// List of all studio items that can be added into the game
        /// </summary>
        public IEnumerable<ItemInfo> ItemList { get; private set; }

        private void Start()
        {
            Logger = base.Logger;

            KeyShowBox = Config.Bind("General", "Show quick access box", new KeyboardShortcut(KeyCode.Space, KeyCode.LeftControl), "Toggles the item search box on and off.");

            SearchDeveloperInfo = Config.Bind("General", "Search developer information", false, new ConfigDescription(DESCRIPTION_DEVINFO, null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            ThumbGenerateKey = Config.Bind("Thumbnail generation", "Generate item thumbnails", new KeyboardShortcut(), new ConfigDescription(DESCRIPTION_THUMBGENKEY, null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ThumbManualAdjust = Config.Bind("Thumbnail generation", "Manual mode", false, new ConfigDescription(DESCRIPTION_MANUALADJUST, null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ThumbDarkBackground = Config.Bind("Thumbnail generation", "Dark background", false, new ConfigDescription(DESCRIPTION_DARKBG, null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ThumbStoreLocation = Config.Bind("Thumbnail generation", "Output directory", string.Empty, new ConfigDescription(DESCRIPTION_THUMBDIR, null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            StartCoroutine(LoadingCo());
        }

        private void OnDestroy()
        {
            _interface?.Dispose();
            ThumbnailLoader.Dispose();
            ItemInfoLoader.SaveTranslationCache(ItemList);
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
            if (ItemList == null)
            {
                Logger.Log(LogLevel.Message, "Item list is still loading, please try again in a few seconds");
                return false;
            }
            return true;
        }

        private IEnumerator LoadingCo()
        {
            yield return new WaitUntil(() => StudioAPI.StudioLoaded);
            // Wait until fully loaded
            yield return null;

            ItemInfoLoader.StartLoadItemsThread(result => ItemList = result);

            _interface = new InterfaceManager(OnListItemClicked, OnSearchStringChanged);
            _interface.Visible = false;

            ThumbnailLoader.LoadAssetBundle();
        }

        private void OnListItemClicked(ItemInfo info)
        {
            Logger.Log(LogLevel.Debug, $"Creating item {info.FullName} - {info.CacheId}");
            Logger.Log(LogLevel.Debug, info.DeveloperSearchString);
            info.AddItem();
        }

        private void OnSearchStringChanged(string newStr)
        {
            _interface.SetList(string.IsNullOrEmpty(newStr) ? null : ItemList.Where(info => ItemMatchesSearch(info, newStr)));
        }

        private static bool ItemMatchesSearch(ItemInfo item, string searchStr)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (searchStr == null) throw new ArgumentNullException(nameof(searchStr));

            var splitSearchStr = searchStr.ToLowerInvariant().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return splitSearchStr.All(s => item.SearchString.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
