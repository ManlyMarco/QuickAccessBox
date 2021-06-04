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
        private readonly RectTransform _windowRootRt;
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

            _windowRootRt = _canvasRoot.GetComponentInChildren<Image>().GetComponent<RectTransform>();
            var savedPos = QuickAccessBox.WindowPosition.Value;
            if (savedPos != Vector2.zero)
            {
                var sd = _windowRootRt.sizeDelta;
                // Prevent window from getting stuck off-screen
                if (savedPos.x > 0 && savedPos.x + sd.x < 1920 && -savedPos.y > 0 && savedPos.y + sd.y < 1080)
                {
                    _windowRootRt.offsetMin = savedPos;
                    _windowRootRt.offsetMax = savedPos + sd;
                }
            }
            MovableWindow.MakeObjectDraggable(_windowRootRt, _windowRootRt, false);

            _inputField = _canvasRoot.transform.FindLoop("InputField").GetComponent<InputField>() ?? throw new ArgumentNullException(nameof(_inputField));
            _inputField.onValueChanged.AddListener(new UnityAction<string>(onSearchStringChanged));
            _inputField.textComponent.MarkXuaIgnored();

            _textHelpObj = _canvasRoot.transform.FindLoop("TextHelp").gameObject ?? throw new ArgumentNullException(nameof(_textHelpObj));
#if AI || HS2
            var helpText = _textHelpObj.GetComponentInChildren<Text>();
            // Get rid of the "use keyboard to navigate" part that doesn't work in AI
            helpText.text = helpText.text.Substring(0, helpText.text.LastIndexOf('-'));
#endif
            _textEmptyObj = _canvasRoot.transform.FindLoop("TextEmpty").gameObject ?? throw new ArgumentNullException(nameof(_textEmptyObj));
            _textEmptyObj.SetActive(false);

            var scrollRect = _canvasRoot.GetComponentInChildren<ScrollRect>();
            _simpleVirtualList = scrollRect.gameObject.AddComponent<SimpleVirtualList>();
            _simpleVirtualList.ScrollRect = scrollRect;
            _simpleVirtualList.EntryTemplate = _canvasRoot.transform.FindLoop("ListEntry").gameObject ?? throw new ArgumentException("Couldn't find ListEntry");
            _simpleVirtualList.OnClicked = onClicked;
            _simpleVirtualList.Initialize();

            CreateSearchMenuButton();
            CreateSearchToolbarButton();
        }

        public void Dispose()
        {
            if (_windowRootRt != null)
                QuickAccessBox.WindowPosition.Value = _windowRootRt.offsetMin;
#if DEBUG
            _simpleVirtualList.Clear();
            Object.Destroy(_canvasRoot);
            Object.Destroy(_searchMenuButton);
            Object.Destroy(_searchToolbarButton);
#endif
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
            if (itemInfos == null || itemInfos.Count == 0)
            {
                _simpleVirtualList.SetList(null);
                _textHelpObj.SetActive(true);
                _textEmptyObj.SetActive(false);
            }
            else
            {
                _simpleVirtualList.SetList(itemInfos);
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
                if (value)
                {
                    SelectSearchBox();

                    if (string.IsNullOrEmpty(_inputField.text))
                        _inputField.onValueChanged.Invoke("");
                }

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
#elif AI || HS2
            _searchMenuButton.GetComponentInChildren<TMPro.TMP_Text>().text = "Search...";
#endif
        }

        private void CreateSearchToolbarButton()
        {
            var iconTex = Utils.LoadTexture(ResourceUtils.GetEmbeddedResource("toolbar-icon.png"));
            _toolbarIcon = KKAPI.Studio.UI.CustomToolbarButtons.AddLeftToolbarToggle(iconTex, Visible, b => Visible = b);
        }
    }
}
