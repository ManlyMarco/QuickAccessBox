using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using DynamicTranslationLoader;
using KKAPI.Studio;
using KK_QuickAccessBox.UI;
using UnityEngine;

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
        public static SavedKeyboardShortcut ShowBoxKey { get; private set; }

        [Advanced(true)]
        public static SavedKeyboardShortcut GenerateThumbsKey { get; private set; }

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

                //_interface.SetList(null);
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

            ShowBoxKey = new SavedKeyboardShortcut(nameof(ShowBoxKey), this, new KeyboardShortcut(KeyCode.Space, KeyCode.LeftControl));
            SearchDeveloperInfo = new ConfigWrapper<bool>(nameof(SearchDeveloperInfo), this, false);

            // todo disable by default
            GenerateThumbsKey = new SavedKeyboardShortcut(nameof(GenerateThumbsKey), this, new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftControl, KeyCode.LeftShift));

            if (StudioAPI.StudioLoaded)
                ItemList = ItemInfo.GetItemList();
            else
                StudioAPI.StudioLoadedChanged += (sender, args) => ItemList = ItemInfo.GetItemList();
        }

        private void OnDestroy()
        {
            _interface?.Dispose();
            Resources.UnloadUnusedAssets();
        }

        private void Update()
        {
            if (ShowBoxKey.IsDown())
                ShowBox = !ShowBox;

            if (ShowBox)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    ShowBox = false;
            }

            if (GenerateThumbsKey.IsDown())
                StartCoroutine(ThumbnailGenerator.MakeThumbnail(ItemList, @"D:\thumb_background.png", "D:\\"));
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
    }
}
