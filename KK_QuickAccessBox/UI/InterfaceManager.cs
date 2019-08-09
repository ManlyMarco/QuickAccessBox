using System;
using System.Collections.Generic;
using System.Linq;
using StrayTech;
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
        private static GameObject _searchButton;

        /// <param name="onClicked">Fired when one of the list items is clicked</param>
        /// <param name="onSearchStringChanged">Fired when search string changes</param>
        public InterfaceManager(Action<ItemInfo> onClicked, Action<string> onSearchStringChanged, Action onToggleVisible)
        {
            _canvasRoot = CreateCanvas();

            _inputField = _canvasRoot.transform.FindChildDeep("InputField").GetComponent<InputField>() ?? throw new ArgumentNullException(nameof(_inputField));
            _inputField.onValueChanged.AddListener(new UnityAction<string>(onSearchStringChanged));
            _inputField.textComponent.MarkXuaIgnored();

            _textHelpObj = _canvasRoot.transform.FindChildDeep("TextHelp") ?? throw new ArgumentNullException(nameof(_textHelpObj));
            _textEmptyObj = _canvasRoot.transform.FindChildDeep("TextEmpty") ?? throw new ArgumentNullException(nameof(_textEmptyObj));
            _textEmptyObj.SetActive(false);

            var scrollRect = _canvasRoot.GetComponentInChildren<ScrollRect>();
            _simpleVirtualList = scrollRect.gameObject.AddComponent<SimpleVirtualList>();
            _simpleVirtualList.ScrollRect = scrollRect;
            _simpleVirtualList.EntryTemplate = _canvasRoot.transform.FindChildDeep("ListEntry") ?? throw new ArgumentException("Couldn't find ListEntry");
            _simpleVirtualList.OnClicked = onClicked;
            _simpleVirtualList.Initialize();

            CreateSearchButton(onToggleVisible);
        }

        public void Dispose()
        {
            _simpleVirtualList.Clear();
            Object.Destroy(_canvasRoot);
            Object.Destroy(_searchButton);
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

        public void SetVisible(bool value)
        {
            _canvasRoot.SetActive(value);
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

        private static void CreateSearchButton(Action onToggleVisible)
        {
            var origButton = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/Scroll View Add Group/Viewport/Content/Frame");
            _searchButton = Object.Instantiate(origButton, origButton.transform.parent);
            _searchButton.name = "QuickSearchBoxBtn";
            var btn = _searchButton.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
            btn.onClick.AddListener(() => onToggleVisible());
            _searchButton.GetComponentInChildren<Text>().text = "Search items...";
        }
    }
}
