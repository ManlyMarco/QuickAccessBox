using System;
using System.Collections.Generic;
using System.Linq;
using IllusionUtility.GetUtility;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KK_QuickAccessBox.UI
{
    internal class InterfaceManager
    {
        private readonly GameObject _canvasRoot;
        private readonly InputField _inputField;

        private readonly SimpleVirtualList _simpleVirtualList;

        private readonly GameObject _textEmptyObj;
        private readonly GameObject _textHelpObj;

        private GameObject _searchMenuButton;
        private GameObject _searchToolbarButton;
        private Image _toolbarIcon;

        /// <param name="onClicked">Fired when one of the list items is clicked</param>
        /// <param name="onSearchStringChanged">Fired when search string changes</param>
        public InterfaceManager(Action<ItemInfo> onClicked, Action<string> onSearchStringChanged)
        {
            _canvasRoot = CreateCanvas();

            _inputField = _canvasRoot.transform.FindLoop("InputField").GetComponent<InputField>() ?? throw new ArgumentNullException(nameof(_inputField));
            _inputField.onValueChanged.AddListener(new UnityAction<string>(onSearchStringChanged));
            _inputField.textComponent.MarkXuaIgnored();

            _textHelpObj = _canvasRoot.transform.FindLoop("TextHelp") ?? throw new ArgumentNullException(nameof(_textHelpObj));
            _textEmptyObj = _canvasRoot.transform.FindLoop("TextEmpty") ?? throw new ArgumentNullException(nameof(_textEmptyObj));
            _textEmptyObj.SetActive(false);

            var scrollRect = _canvasRoot.GetComponentInChildren<ScrollRect>();
            _simpleVirtualList = scrollRect.gameObject.AddComponent<SimpleVirtualList>();
            _simpleVirtualList.ScrollRect = scrollRect;
            _simpleVirtualList.EntryTemplate = _canvasRoot.transform.FindLoop("ListEntry") ?? throw new ArgumentException("Couldn't find ListEntry");
            _simpleVirtualList.OnClicked = onClicked;
            _simpleVirtualList.Initialize();

            CreateSearchMenuButton();
            CreateSearchToolbarButton();
        }

        public void Dispose()
        {
            _simpleVirtualList.Clear();
            Object.Destroy(_canvasRoot);
            Object.Destroy(_searchMenuButton);
            Object.Destroy(_searchToolbarButton);
        }

        public void SelectSearchBox()
        {
            _inputField.Select();
        }

        public bool IsSearchBoxSelected()
        {
            return EventSystem.current.currentSelectedGameObject == _inputField.gameObject;
        }

        public void SelectFirstItem()
        {
            _simpleVirtualList.SelectFirstItem();
        }

        /// <summary>
        /// Set items to null to show the help text, set them to empty collection to show "no results found" text.
        /// </summary>
        public void SetList(IEnumerable<ItemInfo> items)
        {
            var itemInfos = items?.ToList();

            _simpleVirtualList.SetList(itemInfos);

            if (itemInfos == null)
            {
                _textHelpObj.SetActive(true);
                _textEmptyObj.SetActive(false);
            }
            else
            {
                _textHelpObj.SetActive(false);
                _textEmptyObj.SetActive(!itemInfos.Any());
            }
        }

        public bool Visible
        {
            get => _canvasRoot.activeSelf;
            set
            {
                if (value == _canvasRoot.activeSelf)
                    return;

                _canvasRoot.SetActive(value);

                // Focus search box right after showing the window
                if (value && !_canvasRoot.activeSelf)
                    SelectSearchBox();

                _toolbarIcon.color = value ? Color.green : Color.white;
            }
        }

        private static GameObject CreateCanvas()
        {
            var data = Utils.GetResourceBytes("quick_access_box_interface");
            var ab = AssetBundle.LoadFromMemory(data);

            var canvasObj = ab.LoadAsset<GameObject>("assets/QuickAccessBoxCanvas.prefab");
            if (canvasObj == null) throw new ArgumentException("Could not find QuickAccessBoxCanvas.prefab in loaded AB");

            var copy = Object.Instantiate(canvasObj);
            copy.SetActive(false);

            Object.Destroy(canvasObj);
            ab.Unload(false);

            return copy;
        }

        private void CreateSearchMenuButton()
        {
            var origButton = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/Scroll View Add Group/Viewport/Content/Frame");

            _searchMenuButton = Object.Instantiate(origButton, origButton.transform.parent);
            _searchMenuButton.name = "QuickSearchBoxBtn";

            var btn = _searchMenuButton.GetComponent<Button>();
            btn.onClick.ActuallyRemoveAllListeners();
            btn.onClick.AddListener(() => Visible = !Visible);

#if KK
            _searchMenuButton.GetComponentInChildren<Text>().text = "Search...";
#elif AI
            _searchMenuButton.GetComponentInChildren<TMPro.TMP_Text>().text = "Search...";
#endif
        }

        private void CreateSearchToolbarButton()
        {
            var existingRt = GameObject.Find("StudioScene/Canvas System Menu/01_Button/Button Center").GetComponent<RectTransform>();

            _searchToolbarButton = Object.Instantiate(existingRt.gameObject, existingRt.parent);
            var copyRt = _searchToolbarButton.GetComponent<RectTransform>();
            copyRt.localScale = existingRt.localScale;
            copyRt.anchoredPosition = existingRt.anchoredPosition + new Vector2(0f, 120f);

            var iconTex = Utils.LoadTexture(ResourceUtils.GetEmbeddedResource("toolbar-icon.png"));
            var iconSprite = Sprite.Create(iconTex, new Rect(0f, 0f, 32f, 32f), new Vector2(16f, 16f));

            var copyBtn = copyRt.GetComponent<Button>();
            copyBtn.onClick.ActuallyRemoveAllListeners();
            copyBtn.onClick.AddListener(() => Visible = !Visible);

            _toolbarIcon = copyBtn.image;
            _toolbarIcon.sprite = iconSprite;
            _toolbarIcon.color = Color.white;
        }
    }
}
