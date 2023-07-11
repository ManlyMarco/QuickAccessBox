using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_QuickAccessBox.UI
{
    internal class SimpleListEntry : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color _favoriteColor = new Color(1f, 0.85f, 0.93f);
        private static readonly Color _blacklistColor = new Color(0.7f, 0.7f, 0.7f);

        public Image Icon;
        public Text TextCategory;
        public Text TextGroup;
        public Text TextItem;
        public Image Background;

        private ItemInfo _currentItem;

        private Action<ItemInfo, PointerEventData> _onClicked;

        public ItemInfo CurrentItem
        {
            get => _currentItem;
            set => SetItem(value, false);
        }

        private void Start()
        {
            var listener = GetComponent<Button>().onClick;
            listener.RemoveAllListeners();
        }

        public void SetItem(ItemInfo item, bool force)
        {
            if (!force && ReferenceEquals(item, _currentItem)) return;

            _currentItem = item;

            if (item != null)
            {
                TextGroup.text = item.GroupName;
                TextCategory.text = item.CategoryName;
                TextItem.text = item.ItemName;
                Icon.sprite = item.Thumbnail;

                if (item.IsFavorited())
                    Background.color = _favoriteColor;
                else if (item.IsBlacklisted())
                    Background.color = _blacklistColor;
                else
                    Background.color = Color.white;
            }
            else
            {
                TextGroup.text = string.Empty;
                TextCategory.text = string.Empty;
                TextItem.text = string.Empty;
                Icon.sprite = null;

                Background.color = Color.white;
            }
        }

        public void SetOnClicked(Action<ItemInfo, PointerEventData> onClicked)
        {
            _onClicked = onClicked;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (_onClicked != null && CurrentItem != null)
                _onClicked(CurrentItem, eventData);
        }
    }
}
