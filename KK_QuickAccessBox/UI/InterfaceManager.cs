using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using StrayTech;
using UnityEngine;
using UnityEngine.Events;
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
        private readonly GameObject _textMoreObj;

        /// <param name="onClicked">Fired when one of the list items is clicked</param>
        /// <param name="onSearchStringChanged">Fired when search string changes</param>
        public InterfaceManager(Action<ItemInfo> onClicked, Action<string> onSearchStringChanged)
        {
            _canvasRoot = CreateCanvas();

            _inputField = _canvasRoot.transform.FindChildDeep("InputField").GetComponent<InputField>() ?? throw new ArgumentNullException(nameof(_inputField));
            _inputField.onValueChanged.AddListener(new UnityAction<string>(onSearchStringChanged));

            _textHelpObj = _canvasRoot.transform.FindChildDeep("TextHelp") ?? throw new ArgumentNullException(nameof(_textHelpObj));
            _textEmptyObj = _canvasRoot.transform.FindChildDeep("TextEmpty") ?? throw new ArgumentNullException(nameof(_textEmptyObj));
            _textMoreObj = _canvasRoot.transform.FindChildDeep("TextMore") ?? throw new ArgumentNullException(nameof(_textMoreObj));

            var scrollRect = _canvasRoot.GetComponentInChildren<ScrollRect>();
            _simpleVirtualList = scrollRect.gameObject.AddComponent<SimpleVirtualList>();
            _simpleVirtualList.ScrollRect = scrollRect;
            _simpleVirtualList.EntryTemplate = _canvasRoot.transform.FindChildDeep("ListEntry") ?? throw new ArgumentException("Couldn't find ListEntry");
            _simpleVirtualList.OnClicked = onClicked;
            _simpleVirtualList.Initialize();
        }

        public void Dispose()
        {
            _simpleVirtualList.Clear();
            Object.Destroy(_canvasRoot);
        }

        public void ClearAndFocusSearchBox()
        {
            _inputField.text = string.Empty;
            _inputField.Select();
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
                _textMoreObj.SetActive(false);
            }
            else
            {
                _textHelpObj.SetActive(false);
                if (itemInfos.Any())
                {
                    _textEmptyObj.SetActive(false);
                    _textMoreObj.SetActive(false);
                }
                else
                {
                    _textEmptyObj.SetActive(true);
                    _textMoreObj.SetActive(false);
                }
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

                var copy = Object.Instantiate(canvasObj);
                copy.SetActive(false);

                Object.Destroy(canvasObj);
                ab.Unload(false);

                return copy;
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
