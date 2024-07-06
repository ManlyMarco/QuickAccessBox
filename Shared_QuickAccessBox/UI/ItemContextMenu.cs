using System;
using System.Linq;
using Illusion.Extensions;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace KK_QuickAccessBox.UI
{
    /// <summary>
    /// Menu inspired by KK_Plugins/ItemBlacklist
    /// </summary>
    internal class ItemContextMenu
    {
        private readonly RectTransform _menuRoot;

        private readonly Subject<bool> _menuVisible = new Subject<bool>();
        private ItemInfo _currentItem;
        private bool _itemIsFav;
        private bool _itemIsBlk;

        public ItemContextMenu(RectTransform contextMenu)
        {
            _menuRoot = contextMenu;

            var canvasGroup = _menuRoot.GetComponent<CanvasGroup>() ?? throw new ArgumentException("MenuButton/CanvasGroup is missing");
            _menuVisible.Subscribe(visible =>
            {
                canvasGroup.alpha = visible ? 1 : 0;
                canvasGroup.blocksRaycasts = visible;
            });
            _menuVisible.OnNext(false);

            var template = _menuRoot.Find("MenuButton") ?? throw new ArgumentException("MenuButton is missing");
            void CreateButton(string text, Action onClick, Func<bool> isVisible)
            {
                var copy = UnityEngine.Object.Instantiate(template.gameObject, _menuRoot);

                copy.GetComponentInChildren<Text>().text = text;

                copy.GetComponent<Button>().onClick.AddListener(() => onClick());

                _menuVisible.Subscribe(visible =>
                {
                    if (visible)
                        copy.SetActiveIfDifferent(isVisible == null || isVisible());
                });
            }

            var separator = _menuRoot.Find("Separator") ?? throw new ArgumentException("Separator is missing");
            void CreateSeparator()
            {
                UnityEngine.Object.Instantiate(separator.gameObject, _menuRoot);
            }

            CreateButton("Spawn item", () => QuickAccessBox.Instance.CreateItem(_currentItem, false), null);
            CreateButton("Spawn and parent to selection", () => QuickAccessBox.Instance.CreateItem(_currentItem, true), () => Studio.Studio.Instance.treeNodeCtrl.selectNodes.Any());
            CreateButton("Print item info", () => QuickAccessBox.Logger.LogMessage(_currentItem.ToDescriptionString()), null);

            CreateSeparator();

            CreateButton("Search by this category", () => QuickAccessBox.Instance.Interface.SearchString = $"{_currentItem.GroupName}/{_currentItem.CategoryName}".Replace(' ', '_'), null);
            CreateButton("Search by this zipmod",   () => QuickAccessBox.Instance.Interface.SearchString = _currentItem.GUID.Replace(' ', '_'), () => !string.IsNullOrEmpty(_currentItem.FileName));
            
            CreateSeparator();

            var favorited = QuickAccessBox.Instance.Favorited;
            CreateButton("Favorite this item",                 () => favorited.AddItem(_currentItem),        () => !_itemIsFav);
            CreateButton("Favorite all items from this mod",   () => favorited.AddMod(_currentItem.GUID),    () => !_itemIsFav);
            CreateButton("Unfavorite this item",               () => favorited.RemoveItem(_currentItem),     () => _itemIsFav);
            CreateButton("Unfavorite all items from this mod", () => favorited.RemoveMod(_currentItem.GUID), () => _itemIsFav);

            CreateSeparator();
            
            var blacklisted = QuickAccessBox.Instance.Blacklisted;
            CreateButton("Hide this item",                 () => blacklisted.AddItem(_currentItem),        () => !_itemIsBlk);
            CreateButton("Hide all items from this mod",   () => blacklisted.AddMod(_currentItem.GUID),    () => !_itemIsBlk);
            CreateButton("Unhide this item",               () => blacklisted.RemoveItem(_currentItem),     () => _itemIsBlk);
            CreateButton("Unhide all items from this mod", () => blacklisted.RemoveMod(_currentItem.GUID), () => _itemIsBlk);

            UnityEngine.Object.Destroy(template.gameObject);
            UnityEngine.Object.Destroy(separator.gameObject);

            _menuRoot.UpdateAsObservable().Subscribe(_ =>
            {
                if (canvasGroup && canvasGroup.blocksRaycasts)
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        SetMenuVisibility(false);
                    }
                }
            });

            _menuRoot.OnRectTransformDimensionsChangeAsObservable().Subscribe(_ => SnapPositionToCursor());
        }

        public void ShowMenu(ItemInfo item)
        {
            if (item == null)
            {
                SetMenuVisibility(false);
                return;
            }

            _currentItem = item;
            var guid = item.GUID;
            var itemId = item.NewCacheId;
            _itemIsFav = QuickAccessBox.Instance.Favorited.Check(guid, itemId);
            _itemIsBlk = QuickAccessBox.Instance.Blacklisted.Check(guid, itemId);

            SetMenuVisibility(true);
            
            SnapPositionToCursor();
        }

        private void SnapPositionToCursor()
        {
            var height = _menuRoot.rect.height * _menuRoot.lossyScale.y;
            var xPosition = Input.mousePosition.x + 15;
            var yPosition = Input.mousePosition.y < height ? Input.mousePosition.y - 15 : Input.mousePosition.y - height - 15;
            _menuRoot.position = new Vector3(xPosition, yPosition, 0);
        }

        public void SetMenuVisibility(bool visible)
        {
            _menuVisible.OnNext(visible);
        }

        public void SetScale(Vector3 newScale)
        {
            _menuRoot.localScale = newScale;
        }
    }
}
