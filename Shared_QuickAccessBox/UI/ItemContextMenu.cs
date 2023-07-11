using System;
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

            var template = _menuRoot.Find("MenuButton") ?? throw new ArgumentException("MenuButton is missing");

            var canvasGroup = _menuRoot.GetComponent<CanvasGroup>() ?? throw new ArgumentException("MenuButton/CanvasGroup is missing");

            _menuVisible.Subscribe(visible =>
            {
                canvasGroup.alpha = visible ? 1 : 0;
                canvasGroup.blocksRaycasts = visible;
            });
            _menuVisible.OnNext(false);

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

            var favorited = QuickAccessBox.Instance.Favorited;
            CreateButton("Favorite this item",                 () => favorited.AddItem(_currentItem),        () => !_itemIsFav);
            CreateButton("Favorite all items from this mod",   () => favorited.AddMod(_currentItem.GUID),    () => !_itemIsFav);
            CreateButton("Unfavorite this item",               () => favorited.RemoveItem(_currentItem),     () => _itemIsFav);
            CreateButton("Unfavorite all items from this mod", () => favorited.RemoveMod(_currentItem.GUID), () => _itemIsFav);

            var blacklisted = QuickAccessBox.Instance.Blacklisted;
            CreateButton("Hide this item",                 () => blacklisted.AddItem(_currentItem),        () => !_itemIsBlk);
            CreateButton("Hide all items from this mod",   () => blacklisted.AddMod(_currentItem.GUID),    () => !_itemIsBlk);
            CreateButton("Unhide this item",               () => blacklisted.RemoveItem(_currentItem),     () => _itemIsBlk);
            CreateButton("Unhide all items from this mod", () => blacklisted.RemoveMod(_currentItem.GUID), () => _itemIsBlk);
            
            CreateButton("Print item info", () => QuickAccessBox.Logger.LogMessage(_currentItem.ToDescriptionString()), null);

            UnityEngine.Object.Destroy(template.gameObject);

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
            var xPosition = Input.mousePosition.x + 15;
            var yPosition = Input.mousePosition.y - 10 - _menuRoot.rect.height + 33;
            _menuRoot.position = new Vector3(xPosition, yPosition, 0);
        }

        public void SetMenuVisibility(bool visible)
        {
            _menuVisible.OnNext(visible);
        }
    }
}
