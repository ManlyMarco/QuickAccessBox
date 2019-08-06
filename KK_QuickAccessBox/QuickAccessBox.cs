using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using DynamicTranslationLoader;
using KKAPI.Studio;
using KK_QuickAccessBox.Thumbs;
using KK_QuickAccessBox.UI;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_QuickAccessBox
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(DynamicTranslator.GUID)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    public class QuickAccessBox : BaseUnityPlugin
    {
        public const string GUID = "KK_QuickAccessBox";
        internal const string Version = "1.0";

        private InterfaceManager _interface;
        private bool _showBox;

        [DisplayName("!Show quick access box")]
        [Description("Toggles the item search box on and off.")]
        public static SavedKeyboardShortcut KeyShowBox { get; private set; }

        [DisplayName("Search developer information")]
        [Description("The search box will search asset filenames, group/category/item ID numbers, manifests and other things from list files.\n\n" +
                     "Requires studio restart to take effect.")]
        public static ConfigWrapper<bool> SearchDeveloperInfo { get; private set; }

        [Advanced(true)]
        [Category("Thumbnail generation")]
        [DisplayName("Generate item thumbnails")]
        [Description("Automatically generate thumbnails for all items.\n\n" +
                     "Items with existing thumbnails are skipped. To re-do certain thumbnails just remove them from the folder.")]
        public static SavedKeyboardShortcut ThumbGenerateKey { get; private set; }

        [Advanced(true)]
        [Category("Thumbnail generation")]
        [DisplayName("Output directory")]
        [Description("After adjusting the camera position to get the object in the middle of the screen press Shift to accept.")]
        public static ConfigWrapper<string> ThumbStoreLocation { get; private set; }

        [Advanced(true)]
        [Category("Thumbnail generation")]
        [DisplayName("Dark background")]
        [Description("Only use for items that are impossible to see on light background like flares.")]
        public static ConfigWrapper<bool> ThumbDarkBackground { get; private set; }

        [Advanced(true)]
        [Category("Thumbnail generation")]
        [DisplayName("Manual mode - adjust by hand")]
        [Description("After spawning the object, generator will wait for you to adjust the camera position to get the object in " +
                     "the middle of the screen. Press left Shift to accept and move to the next item.")]
        public static ConfigWrapper<bool> ThumbManualAdjust { get; private set; }

        [Browsable(false)]
        public bool ShowBox
        {
            get => _showBox;
            set
            {
                if (value == _showBox)
                    return;

                _interface.SetVisible(value);

                // Focus search box right after showing the window
                if (value && !_showBox)
                    _interface.SelectSearchBox();

                _showBox = value;
            }
        }

        /// <summary>
        /// List of all studio items that can be added into the game
        /// </summary>
        public IEnumerable<ItemInfo> ItemList { get; private set; }

        private void Start()
        {
            if (!StudioAPI.InsideStudio)
            {
                enabled = false;
                return;
            }

            KeyShowBox = new SavedKeyboardShortcut(nameof(KeyShowBox), this, new KeyboardShortcut(KeyCode.Space, KeyCode.LeftControl));

            SearchDeveloperInfo = new ConfigWrapper<bool>(nameof(SearchDeveloperInfo), this, false);

            ThumbGenerateKey = new SavedKeyboardShortcut(nameof(ThumbGenerateKey), this, new KeyboardShortcut());
            ThumbManualAdjust = new ConfigWrapper<bool>(nameof(ThumbManualAdjust), this, false);
            ThumbDarkBackground = new ConfigWrapper<bool>(nameof(ThumbDarkBackground), this, false);
            ThumbStoreLocation = new ConfigWrapper<string>(nameof(ThumbStoreLocation), this, string.Empty);

            StartCoroutine(LoadingCo());
        }

        private void OnDestroy()
        {
            _interface?.Dispose();
            ThumbnailLoader?.Dispose();
        }

        private void Update()
        {
            if (KeyShowBox.IsDown())
            {
                if (IsLoaded())
                    ShowBox = !ShowBox;
            }
            else if (ThumbGenerateKey.IsDown())
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

            // Wait for DTL to finish loading translations before starting the thread to avoid accessing TL dicts as they get populated
            yield return null;

            ItemInfoLoader.StartLoadItemsThread(result => ItemList = result);

            _interface = new InterfaceManager(OnListItemClicked, OnSearchStringChanged);
            _interface.SetVisible(false);

            ThumbnailLoader.LoadAssetBundle();
        }

        private void OnListItemClicked(ItemInfo info)
        {
            Logger.Log(LogLevel.Debug, $"[KK_QuickAccessBox] Creating item {info.FullName} - {info.CacheId}");
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
            return splitSearchStr.All(s => item.SearchStr.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
