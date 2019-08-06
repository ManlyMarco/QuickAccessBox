using System;
using KK_QuickAccessBox.Thumbs;
using Studio;
using UnityEngine;

namespace KK_QuickAccessBox
{
    public sealed class ItemInfo
    {
        private readonly bool _initFinished;
        private readonly string _origFullname;

        public ItemInfo(int groupNo, int categoryNo, int itemNo, Info.ItemLoadInfo item = null)
        {
            GroupNo = groupNo;
            CategoryNo = categoryNo;
            ItemNo = itemNo;

            if (item == null) item = Info.Instance.dicItemLoadInfo[groupNo][categoryNo][itemNo];

            if (item == null) throw new ArgumentNullException(nameof(item), "Info.ItemLoadInfo is null in dicItemLoadInfo");

            DeveloperSearchString = $"{item.childRoot}\v{item.bundlePath}\v{item.fileName}\v{item.manifest}\v{GroupNo}\v{CategoryNo}\v{ItemNo}";
            CacheId = MakeCacheId(groupNo, categoryNo, item);

            if (!Info.Instance.dicItemGroupCategory.ContainsKey(GroupNo)) throw new ArgumentException("Invalid group number");
            var groupInfo = Info.Instance.dicItemGroupCategory[GroupNo];

            if (!groupInfo.dicCategory.ContainsKey(CategoryNo)) throw new ArgumentException("Invalid category number");
            var origCategoryName = groupInfo.dicCategory[CategoryNo];
            _origFullname = groupInfo.name + "/" + origCategoryName + "/" + item.name;

            ItemInfoLoader.TranslationCache.TryGetValue(CacheId, out var cachedTranslations);
            if (cachedTranslations != null)
            {
                CategoryName = cachedTranslations.CategoryName;
                GroupName = cachedTranslations.GroupName;
                ItemName = cachedTranslations.ItemName;
            }
            else
            {
                // Get translated versions of the relevant strings
                TranslationHelper.Translate(
                    groupInfo.name, s =>
                    {
                        GroupName = s;
                        if (_initFinished)
                            UpdateCompositeStrings();
                    });
                TranslationHelper.Translate(
                    origCategoryName, s =>
                    {
                        CategoryName = s;
                        if (_initFinished)
                            UpdateCompositeStrings();
                    });
                TranslationHelper.Translate(
                    item.name, s =>
                    {
                        ItemName = s;
                        if (_initFinished)
                            UpdateCompositeStrings();
                    });
            }

            UpdateCompositeStrings();
            _initFinished = true;
        }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string CategoryName { get; private set; }

        /// <summary>
        /// Under add/Item/Group
        /// </summary>
        public int CategoryNo { get; }

        /// <summary>
        /// Full translated (or original if not necessary/available) path of the item in the item tree
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string GroupName { get; private set; }

        /// <summary>
        /// Top level under add/Item menu
        /// </summary>
        public int GroupNo { get; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string ItemName { get; private set; }

        /// <summary>
        /// Index of the item in
        /// </summary>
        public int ItemNo { get; }

        /// <summary>
        /// String to search against
        /// </summary>
        internal string SearchString { get; private set; }

        /// <summary>
        /// String with developer info, used to build SearchString
        /// </summary>
        private string DeveloperSearchString;

        public Sprite Thumbnail => ThumbnailLoader.GetThumbnail(this);

        /// <summary>
        /// Item is a sound effect and should get the SFX thumbnail
        /// </summary>
        public bool IsSFX => GroupNo == 00000011;

        public string CacheId { get; }

        /// <summary>
        /// Spawn this item in studio
        /// </summary>
        public void AddItem()
        {
            try
            {
                Studio.Studio.Instance.AddItem(GroupNo, CategoryNo, ItemNo);
            }
            catch (NullReferenceException)
            {
                // Some modded items crash in Studio.OCIItem.UpdateColor()
            }
        }

        private void UpdateCompositeStrings()
        {
            FullName = GroupName + "/" + CategoryName + "/" + ItemName;

            var searchStr = FullName;

            if (!_origFullname.Equals(FullName, StringComparison.OrdinalIgnoreCase))
                searchStr = $"{searchStr}\v{_origFullname}";

            if (QuickAccessBox.SearchDeveloperInfo.Value)
                searchStr = $"{searchStr}\v{DeveloperSearchString}";

            SearchString = searchStr.ToLowerInvariant();
        }

        public static string MakeCacheId(int groupNo, int categoryNo, Info.ItemLoadInfo item)
        {
            // Can't use itemNo because it can change with sideloader
            return $"{groupNo:D8}-{categoryNo:D8}-{Utils.MakeValidFileName(item.name)}";
            // old - return $"{groupNo:D8}-{categoryNo:D8}-{item.name.GetHashCode():D32}";
        }

        public override int GetHashCode()
        {
            return _origFullname.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ItemInfo i && i._origFullname == _origFullname;
        }
    }
}
