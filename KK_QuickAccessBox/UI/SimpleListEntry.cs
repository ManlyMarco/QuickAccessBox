using System;
using UnityEngine;
using UnityEngine.UI;

namespace KK_QuickAccessBox.UI
{
    internal class SimpleListEntry : MonoBehaviour
    {
        public RawImage Icon;
        public Text TextCategory;
        public Text TextGroup;
        public Text TextItem;

        private ItemInfo _currentItem;

        public ItemInfo CurrentItem
        {
            get => _currentItem;
            set => SetItem(value, false);
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
                Icon.texture = item.Thumbnail;
            }
            else
            {
                TextGroup.text = string.Empty;
                TextCategory.text = string.Empty;
                TextItem.text = string.Empty;
                Icon.texture = null;
            }
        }

        public void SetOnClicked(Action<ItemInfo> onClicked)
        {
            var listener = GetComponent<Button>().onClick;
            listener.RemoveAllListeners();
            if (onClicked != null)
                listener.AddListener(() => onClicked(CurrentItem));
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}
