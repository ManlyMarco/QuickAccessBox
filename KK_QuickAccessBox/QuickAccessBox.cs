using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using DynamicTranslationLoader;
using KKAPI.Studio;
using StrayTech;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KK_QuickAccessBox
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInDependency(DynamicTranslator.GUID)]
    public class QuickAccessBox : BaseUnityPlugin
    {
        public const string GUID = "KK_QuickAccessBox";
        internal const string Version = "0.1";
        private InterfaceManager _interface;
        private IEnumerable<ItemInfo> _itemList;

        private bool _showBox;

        [Advanced(true)]
        public static SavedKeyboardShortcut GenerateThumbsKey { get; private set; }

        /// <summary>
        /// List of all studio items that can be added into the game
        /// </summary>
        public IEnumerable<ItemInfo> ItemList => _itemList ?? (_itemList = ItemInfo.GetItemList());

        [DisplayName("Search developer information")]
        [Description("The search box will search asset filenames, group/category/item ID numbers, manifests and other things from list files.")]
        public static ConfigWrapper<bool> SearchDeveloperInfo { get; private set; }

        public bool ShowBox
        {
            get => _showBox;
            set
            {
                if (value == _showBox)
                    return;

                _interface.SetVisible(value);

                if (value && !_showBox)
                    _interface.FocusSearchBox();

                _showBox = value;

                _interface.SetList(null);
            }
        }

        [DisplayName("Show quick access box")]
        public static SavedKeyboardShortcut ShowBoxKey { get; private set; }

        private static bool ItemMatchesSearch(ItemInfo x, string searchStr)
        {
            if (string.IsNullOrEmpty(searchStr)) return false;

            var splitSearchStr = searchStr.ToLowerInvariant().Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
            return splitSearchStr.All(s => x.SearchStr.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void OnDestroy()
        {
            _interface?.Dispose();
        }

        private void Start()
        {
            if (!StudioAPI.InsideStudio)
            {
                enabled = false;
                return;
            }

            ShowBoxKey = new SavedKeyboardShortcut(nameof(ShowBoxKey), this, new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftControl));
            SearchDeveloperInfo = new ConfigWrapper<bool>(nameof(SearchDeveloperInfo), this, false);

            // todo disable by default
            GenerateThumbsKey = new SavedKeyboardShortcut(nameof(GenerateThumbsKey), this, new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftControl, KeyCode.LeftShift));
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
            {
                StartCoroutine(ThumbnailGenerator.MakeThumbnail(ItemList, @"D:\thumb_background.png", "D:\\"));
                if (_interface == null)
                    _interface = new InterfaceManager(OnListItemClicked, OnSearchStringChanged);
            }
        }

        private void OnListItemClicked(ItemInfo info)
        {
            info.AddItem();
        }

        private void OnSearchStringChanged(string newStr)
        {
            if (string.IsNullOrEmpty(newStr))
                _interface.SetList(null);
            else
            {
                var filteredItems = ItemList.Where(info => ItemMatchesSearch(info, newStr)).Take(40).ToList();
                _interface.SetList(filteredItems);
            }
        }
    }

    internal class InterfaceListEntry : MonoBehaviour
    {
        public RawImage Icon;
        public Text TextCategory;
        public Text TextGroup;
        public Text TextItem;

        private ItemInfo _currentItem;

        public void SetItem(ItemInfo item, Action<ItemInfo> onClicked)
        {
            var listener = GetComponent<Button>().onClick;
            listener.RemoveAllListeners();
            if (onClicked != null)
                listener.AddListener(() => onClicked(_currentItem));

            if (item == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _currentItem = item;
            TextGroup.text = item.GroupName;
            TextCategory.text = item.CategoryName;
            TextItem.text = item.ItemName;

            gameObject.SetActive(true);
            //todo icon
        }
    }

    internal class InterfaceManager
    {
        private readonly GameObject _canvasRoot;
        private readonly InputField _inputField;

        private readonly List<InterfaceListEntry> _listEntries = new List<InterfaceListEntry>();
        private readonly Action<ItemInfo> _onClicked;
        private readonly InterfaceListEntry _templateListEntry;
        private readonly GameObject _textEmptyObj;
        private readonly GameObject _textHelpObj;
        private readonly GameObject _textMoreObj;

        /// <param name="onClicked">Fired when one of the list items is clicked</param>
        /// <param name="onSearchStringChanged">Fired when search string changes</param>
        public InterfaceManager(Action<ItemInfo> onClicked, Action<string> onSearchStringChanged)
        {
            _onClicked = onClicked;
            _canvasRoot = CreateCanvas();

            _inputField = _canvasRoot.transform.FindChildDeep("InputField").GetComponent<InputField>() ?? throw new ArgumentNullException(nameof(_inputField));
            _inputField.onValueChanged.AddListener(new UnityAction<string>(onSearchStringChanged));

            _textHelpObj = _canvasRoot.transform.FindChildDeep("TextHelp") ?? throw new ArgumentNullException(nameof(_textHelpObj));
            _textEmptyObj = _canvasRoot.transform.FindChildDeep("TextEmpty") ?? throw new ArgumentNullException(nameof(_textEmptyObj));
            // todo always has to be last
            _textMoreObj = _canvasRoot.transform.FindChildDeep("TextMore") ?? throw new ArgumentNullException(nameof(_textMoreObj));

            var listEntryObj = _canvasRoot.transform.FindChildDeep("ListEntry") ?? throw new ArgumentException("Couldn't find ListEntry");
            listEntryObj.SetActive(false);

            _templateListEntry = listEntryObj.AddComponent<InterfaceListEntry>();
            _templateListEntry.Icon = _templateListEntry.transform.FindChildDeep("Icon")?.GetComponent<RawImage>() ?? throw new ArgumentException("Couldn't find Icon");
            _templateListEntry.TextGroup = _templateListEntry.transform.FindChildDeep("TextGroup")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextGroup");
            _templateListEntry.TextCategory = _templateListEntry.transform.FindChildDeep("TextCategory")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextCategory");
            _templateListEntry.TextItem = _templateListEntry.transform.FindChildDeep("TextItem")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextItem");

            SetList(null);
        }

        public void Dispose()
        {
            Object.Destroy(_canvasRoot);
            _listEntries.Clear();
        }

        public void FocusSearchBox()
        {
            _inputField.Select();
        }

        /// <summary>
        /// Set items to null to show the help text, set them to empty collection to show "no results found" text.
        /// </summary>
        public void SetList(IEnumerable<ItemInfo> items)
        {
            foreach (var listEntry in _listEntries)
                Object.Destroy(listEntry.gameObject);

            _listEntries.Clear();

            if (items == null)
            {
                _textHelpObj.SetActive(true);
                _textEmptyObj.SetActive(false);
                _textMoreObj.SetActive(false);
                return;
            }

            foreach (var itemInfo in items)
            {
                var copy = Object.Instantiate(_templateListEntry.gameObject, _templateListEntry.transform.parent);
                var entry = copy.GetComponent<InterfaceListEntry>();
                entry.SetItem(itemInfo, _onClicked);
                _listEntries.Add(entry);
            }

            _textHelpObj.SetActive(false);
            if (_listEntries.Count == 0)
            {
                _textEmptyObj.SetActive(true);
                _textMoreObj.SetActive(false);
            }
            //todo
            else if (_listEntries.Count > 20)
            {
                _textEmptyObj.SetActive(false);
                _textMoreObj.SetActive(true);
            }
        }

        public void SetVisible(bool value)
        {
            _canvasRoot.SetActive(value);
        }

        private static GameObject CreateCanvas()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("quick_access_box_interface"));
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                var data = ReadFully(stream ?? throw new InvalidOperationException("The UI resource was not found"));
                var ab = AssetBundle.LoadFromMemory(data);

                var canvasObj = ab.LoadAsset<GameObject>("assets/QuickAccessBoxCanvas.prefab");
                if (canvasObj == null) throw new ArgumentException("Could not find QuickAccessBoxCanvas.prefab in loaded AB");

                canvasObj.SetActive(false);

                ab.Unload(false);

                return canvasObj;
            }
        }

        private static byte[] ReadFully(Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);
                return ms.ToArray();
            }
        }
    }
}
