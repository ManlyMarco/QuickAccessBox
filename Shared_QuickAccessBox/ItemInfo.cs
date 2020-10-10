using System;
using System.IO;
using System.Linq;
using KK_QuickAccessBox.Thumbs;
using MessagePack;
using Sideloader.AutoResolver;
using Studio;
using UnityEngine;

namespace KK_QuickAccessBox
{
    [MessagePackObject]
    public sealed class ItemInfo
    {
        private readonly bool _initFinished;

        private ItemInfo()
        {
            _initFinished = true;
        }

        public ItemInfo(int groupNo, int categoryNo, int itemNo, Info.ItemLoadInfo item = null)
        {
            GroupNo = groupNo;
            CategoryNo = categoryNo;
            ItemNo = itemNo;

            if (item == null) item = Info.Instance.dicItemLoadInfo[groupNo][categoryNo][itemNo];

            if (item == null) throw new ArgumentNullException(nameof(item), "Info.ItemLoadInfo is null in dicItemLoadInfo");

#if KK
            DeveloperSearchString = $"{item.childRoot}\v{item.bundlePath}\v{item.fileName}\v{item.manifest}\v{GroupNo}\v{CategoryNo}\v{ItemNo}";
#elif AI || HS2
            DeveloperSearchString = $"{item.bundlePath}\v{item.fileName}\v{item.manifest}\v{GroupNo}\v{CategoryNo}\v{ItemNo}";
#endif
            var studioResolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.Slot == itemNo);
            if (studioResolveInfo != null)
            {
                GUID = studioResolveInfo.GUID;
                DeveloperSearchString += "\v" + GUID;
                if (Sideloader.Sideloader.ZipArchives.TryGetValue(studioResolveInfo.GUID, out var filename))
                {
                    FileName = Path.GetFileName(filename);
                    DeveloperSearchString += "\v" + FileName;
                }
            }

            CacheId = MakeCacheId(groupNo, categoryNo, item);

            if (!Info.Instance.dicItemGroupCategory.ContainsKey(GroupNo)) throw new ArgumentException("Invalid group number");
            var groupInfo = Info.Instance.dicItemGroupCategory[GroupNo];

            if (!groupInfo.dicCategory.ContainsKey(CategoryNo)) throw new ArgumentException("Invalid category number");
#if KK
            var origCategoryName = groupInfo.dicCategory[CategoryNo];
#elif AI || HS2
            var origCategoryName = groupInfo.dicCategory[CategoryNo].name;
#endif
            OriginalItemName = groupInfo.name + "/" + origCategoryName + "/" + item.name;

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
        [Key(nameof(ItemName))]
        public string CategoryName { get; private set; }

        /// <summary>
        /// Under add/Item/Group
        /// </summary>
        [Key(nameof(ItemName))]
        public int CategoryNo { get; private set; }

        /// <summary>
        /// Full translated (or original if not necessary/available) path of the item in the item tree
        /// </summary>
        [Key(nameof(ItemName))]
        public string FullName { get; private set; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        [Key(nameof(ItemName))]
        public string GroupName { get; private set; }

        /// <summary>
        /// Top level under add/Item menu
        /// </summary>
        [Key(nameof(ItemName))]
        public int GroupNo { get; private set; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        [Key(nameof(ItemName))]
        public string ItemName { get; private set; }

        [Key(nameof(OriginalItemName))]
        private string OriginalItemName { get; set; }

        /// <summary>
        /// Index of the item in
        /// </summary>
        [Key(nameof(ItemNo))]
        public int ItemNo { get; private set; }

        /// <summary>
        /// String to search against
        /// </summary>
        [Key(nameof(SearchString))]
        internal string SearchString { get; private set; }

        /// <summary>
        /// String with developer info, used to build SearchString
        /// </summary>
        [Key(nameof(DeveloperSearchString))]
        internal string DeveloperSearchString { get; private set; }

        [IgnoreMember]
        public Sprite Thumbnail => ThumbnailLoader.GetThumbnail(this);

        /// <summary>
        /// Item is a sound effect and should get the SFX thumbnail
        /// </summary>
        [IgnoreMember]
        public bool IsSFX =>
#if KK
            GroupNo == 00000011;
#elif AI || HS2
            GroupNo == 00000009;
#endif

        [Key(nameof(CacheId))]
        public string CacheId { get; private set; }

        /// <summary>
        /// If this item is from a zipmod, GUID of the zipmod. Otherwise null.
        /// </summary>
        [Key(nameof(GUID))]
        public string GUID { get; private set; }

        /// <summary>
        /// If this item is from a zipmod, name of the .zipmod file. Otherwise null.
        /// </summary>
        [Key(nameof(FileName))]
        public string FileName { get; private set; }

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

            if (!OriginalItemName.Equals(FullName, StringComparison.OrdinalIgnoreCase))
                searchStr = $"{searchStr}\v{OriginalItemName}";

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
            if (OriginalItemName == null) return 0;
            return OriginalItemName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ItemInfo i && i.OriginalItemName == OriginalItemName;
        }
    }
}
