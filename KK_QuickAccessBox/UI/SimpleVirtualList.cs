using System;
using System.Collections.Generic;
using System.Linq;
using StrayTech;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_QuickAccessBox.UI
{
    internal class SimpleVirtualList : MonoBehaviour
    {
        private readonly List<InterfaceListEntry> _cachedEntries = new List<InterfaceListEntry>();
        private readonly List<ItemInfo> _items = new List<ItemInfo>();

        public GameObject EntryTemplate;
        public Action<ItemInfo> OnClicked;
        public ScrollRect ScrollRect;

        private bool _dirty;
        private int _lastItemsAboveViewRect;

        private int _paddingBot;
        private int _paddingTop;
        private float _singleItemHeight;

        private VerticalLayoutGroup _verticalLayoutGroup;

        public void Initialize()
        {
            if (ScrollRect == null) throw new ArgumentNullException(nameof(ScrollRect));

            _verticalLayoutGroup = ScrollRect.content.GetComponent<VerticalLayoutGroup>();
            if (_verticalLayoutGroup == null) throw new ArgumentNullException(nameof(_verticalLayoutGroup));

            _paddingTop = _verticalLayoutGroup.padding.top;
            _paddingBot = _verticalLayoutGroup.padding.bottom;

            SetupEntryTemplate();

            PopulateEntryCache();

            Destroy(EntryTemplate);

            Clear();
        }

        private void SetupEntryTemplate()
        {
            if (EntryTemplate == null) throw new ArgumentNullException(nameof(EntryTemplate));

            EntryTemplate.SetActive(false);

            var listEntry = EntryTemplate.AddComponent<InterfaceListEntry>();
            listEntry.Icon = listEntry.transform.FindChildDeep("Icon")?.GetComponent<RawImage>() ?? throw new ArgumentException("Couldn't find Icon");
            listEntry.TextGroup = listEntry.transform.FindChildDeep("TextGroup")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextGroup");
            listEntry.TextCategory = listEntry.transform.FindChildDeep("TextCategory")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextCategory");
            listEntry.TextItem = listEntry.transform.FindChildDeep("TextItem")?.GetComponent<Text>() ?? throw new ArgumentException("Couldn't find TextItem");
            listEntry.SetItem(null, true);

            var templateRectTransform = EntryTemplate.GetComponent<RectTransform>();
            _singleItemHeight = templateRectTransform.rect.height + _verticalLayoutGroup.spacing;
        }

        private void PopulateEntryCache()
        {
            var viewportHeight = ScrollRect.GetComponent<RectTransform>().rect.height;
            var visibleEntryCount = Mathf.CeilToInt(viewportHeight / _singleItemHeight) + 1;

            for (var i = 0; i < visibleEntryCount; i++)
            {
                var copy = Instantiate(EntryTemplate, EntryTemplate.transform.parent);
                var entry = copy.GetComponent<InterfaceListEntry>();
                _cachedEntries.Add(entry);
                entry.SetVisible(false);
                entry.SetOnClicked(OnClicked);
            }
        }

        public void Clear()
        {
            SetList(null);
        }

        public void SetList(IEnumerable<ItemInfo> items)
        {
            _items.Clear();
            if (items != null)
                _items.AddRange(items);

            _dirty = true;
        }

        private void Update()
        {
            var scrollPosition = ScrollRect.content.localPosition.y;
            // How many items are not visible in current view
            var offscreenItemCount = Mathf.Max(0, _items.Count - _cachedEntries.Count);
            // How many items are above current view rect and not visible
            var itemsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / _singleItemHeight, 0, offscreenItemCount));

            if (_lastItemsAboveViewRect == itemsAboveViewRect && !_dirty)
                return;

            _lastItemsAboveViewRect = itemsAboveViewRect;
            _dirty = false;

            // Store selected item to preserve selection when moving the list with mouse
            var selectedItem = EventSystem.current != null 
                ? _cachedEntries.Find(x => x.gameObject == EventSystem.current.currentSelectedGameObject)?.CurrentItem 
                : null;

            var count = 0;
            foreach (var item in _items.Skip(itemsAboveViewRect))
            {
                if (_cachedEntries.Count <= count) break;

                var cachedEntry = _cachedEntries[count];

                count++;

                cachedEntry.SetItem(item, false);
                cachedEntry.SetVisible(true);

                if (ReferenceEquals(selectedItem, item))
                    EventSystem.current?.SetSelectedGameObject(cachedEntry.gameObject);
            }

            // If there are less items than cached list entries, disable unused cache entries
            if (_cachedEntries.Count > _items.Count)
            {
                foreach (var cacheEntry in _cachedEntries.Skip(_items.Count))
                    cacheEntry.SetVisible(false);
            }

            RecalculateOffsets(itemsAboveViewRect);

            // Needed after changing _verticalLayoutGroup.padding since it doesn't make the object dirty
            LayoutRebuilder.MarkLayoutForRebuild(_verticalLayoutGroup.GetComponent<RectTransform>());

            //Logger.Log(LogLevel.Info, $"items={_items.Count} scroll={scrollPosition} topItems={topOffsetItems} top={_verticalLayoutGroup.padding.top} bottom={_verticalLayoutGroup.padding.bottom}");
        }

        private void RecalculateOffsets(int itemsAboveViewRect)
        {
            var topOffset = Mathf.RoundToInt(itemsAboveViewRect * _singleItemHeight);
            _verticalLayoutGroup.padding.top = _paddingTop + topOffset;

            var totalHeight = _items.Count * _singleItemHeight;
            var cacheEntriesHeight = _cachedEntries.Count * _singleItemHeight;
            var trailingHeight = totalHeight - cacheEntriesHeight - topOffset;
            _verticalLayoutGroup.padding.bottom = Mathf.FloorToInt(Mathf.Max(0, trailingHeight) + _paddingBot);
        }
    }
}
