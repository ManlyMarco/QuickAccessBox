using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using DynamicTranslationLoader;
using KKAPI.Studio;
using KK_QuickAccessBox.Thumbs;
using KK_QuickAccessBox.UI;
using Studio;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_QuickAccessBox
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(DynamicTranslator.GUID)]
    public class QuickAccessBox : BaseUnityPlugin
    {
        public const string GUID = "KK_QuickAccessBox";
        internal const string Version = "0.1";

        private InterfaceManager _interface;
        private bool _showBox;

        [DisplayName("Show quick access box")]
        public static SavedKeyboardShortcut KeyShowBox { get; private set; }

        [Advanced(true)]
        [DisplayName("Generate item thumbnails (auto)")]
        [Description("Automatically generate thumbnails for all items.\n\n" +
                     "Items with existing thumbnails are skipped. To re-do certain thumbnails just remove them from the folder.")]
        public static SavedKeyboardShortcut KeyGenerateThumbsAuto { get; private set; }

        [Advanced(true)]
        [DisplayName("Generate item thumbnails (manual)")]
        [Description("After adjusting the camera position to get the object in the middle of the screen press Shift to accept.\n\n" +
                     "Items with existing thumbnails are skipped. To re-do certain thumbnails just remove them from the folder.")]
        public static SavedKeyboardShortcut KeyGenerateThumbsManual { get; private set; }

        [Advanced(true)]
        [DisplayName("Generate item thumbnails (manual)")]
        [Description("After adjusting the camera position to get the object in the middle of the screen press Shift to accept.")]
        public static ConfigWrapper<string> ThumbStoreLocation { get; private set; }

        [DisplayName("Search developer information")]
        [Description("The search box will search asset filenames, group/category/item ID numbers, manifests and other things from list files.\n\n" +
                     "Requires studio restart to take effect.")]
        public static ConfigWrapper<bool> SearchDeveloperInfo { get; private set; }


        [Browsable(false)]
        public bool ShowBox
        {
            get => _showBox;
            set
            {
                if (value == _showBox)
                    return;

                if (value && _interface == null)
                    _interface = new InterfaceManager(OnListItemClicked, OnSearchStringChanged);

                _interface.SetVisible(value);

                // Focus search box right after showing the window
                if (value && !_showBox)
                    _interface.ClearAndFocusSearchBox();

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
            KeyGenerateThumbsAuto = new SavedKeyboardShortcut(nameof(KeyGenerateThumbsAuto), this, new KeyboardShortcut());
            KeyGenerateThumbsManual = new SavedKeyboardShortcut(nameof(KeyGenerateThumbsManual), this, new KeyboardShortcut());

            ThumbStoreLocation = new ConfigWrapper<string>(nameof(ThumbStoreLocation), this, string.Empty);
            SearchDeveloperInfo = new ConfigWrapper<bool>(nameof(SearchDeveloperInfo), this, false);

            if (StudioAPI.StudioLoaded)
                ItemList = GetItemList();
            else
                StudioAPI.StudioLoadedChanged += (sender, args) => ItemList = GetItemList();
        }

        private void OnDestroy()
        {
            _interface?.Dispose();
            Resources.UnloadUnusedAssets();
        }

        private void Update()
        {
            if (KeyShowBox.IsDown())
                ShowBox = !ShowBox;
            else if (KeyGenerateThumbsAuto.IsDown())
                StartCoroutine(ThumbnailGenerator.MakeThumbnail(ItemList, ThumbStoreLocation.Value, false));
            else if (KeyGenerateThumbsManual.IsDown())
                StartCoroutine(ThumbnailGenerator.MakeThumbnail(ItemList, ThumbStoreLocation.Value, true));

            if (ShowBox)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    ShowBox = false;
            }
        }

        private void OnListItemClicked(ItemInfo info)
        {
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

        //todo async load?
        private static List<ItemInfo> GetItemList()
        {
            var results = new List<ItemInfo>();

            foreach (var group in Info.Instance.dicItemLoadInfo)
            {
                foreach (var category in group.Value)
                {
                    foreach (var item in category.Value)
                    {
                        try
                        {
                            results.Add(new ItemInfo(group.Key, category.Key, item.Key, item.Value));
                        }
                        catch (Exception e)
                        {
                            Logger.Log(LogLevel.Warning, $"Failed to load information about item {item.Value.name} group={group.Key} category={category.Key} itemNo={item.Key} - {e}");
                        }
                    }
                }
            }

            results.Sort((x, y) => string.Compare(x.FullName, y.FullName, StringComparison.Ordinal));

            return results;
        }
    }
}
