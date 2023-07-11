using System;
using System.Text;
using KK_QuickAccessBox.Thumbs;
using Sideloader.AutoResolver;
using Studio;
using UnityEngine;
#pragma warning disable CS0612

namespace KK_QuickAccessBox
{
    public sealed class ItemInfo
    {
        private readonly bool _initFinished;

        public ItemInfo(int groupNo, int categoryNo, int localSlot, Info.ItemLoadInfo studioInfo, StudioResolveInfo zipmodInfo, string zipmodFilename)
        {
            GroupNo = groupNo;
            CategoryNo = categoryNo;
            LocalSlot = localSlot;

            if (studioInfo == null) throw new ArgumentNullException(nameof(studioInfo), "Info.ItemLoadInfo is null in dicItemLoadInfo");

            Bundle = studioInfo.bundlePath;
            Asset = studioInfo.fileName;

#if KK || KKS
            DeveloperSearchString = $"{studioInfo.childRoot}\v{studioInfo.bundlePath}\v{studioInfo.fileName}\v{studioInfo.manifest}\v{GroupNo}\v{CategoryNo}\v{LocalSlot}";
#elif AI || HS2
            DeveloperSearchString = $"{studioInfo.bundlePath}\v{studioInfo.fileName}\v{studioInfo.manifest}\v{GroupNo}\v{CategoryNo}\v{LocalSlot}";
#endif

            OldCacheId = MakeOldCacheId(groupNo, categoryNo, studioInfo);

            if (zipmodInfo != null)
            {
                GUID = zipmodInfo.GUID;
                ZipmodSlot = zipmodInfo.Slot;
                DeveloperSearchString += "\v" + ZipmodSlot;
                NewCacheId = MakeNewCacheId(groupNo, categoryNo, ZipmodSlot);
            }
            else
            {
                ZipmodSlot = -1;
                NewCacheId = MakeNewCacheId(groupNo, categoryNo, LocalSlot);
            }

            if (zipmodFilename != null)
            {
                FileName = zipmodFilename;
                DeveloperSearchString += "\v" + FileName;
            }

            if (!Info.Instance.dicItemGroupCategory.ContainsKey(GroupNo)) throw new ArgumentException("Invalid group number");
            var groupInfo = Info.Instance.dicItemGroupCategory[GroupNo];

            if (!groupInfo.dicCategory.ContainsKey(CategoryNo)) throw new ArgumentException("Invalid category number");
#if KK || KKS
            var origCategoryName = groupInfo.dicCategory[CategoryNo];
#elif AI || HS2
            var origCategoryName = groupInfo.dicCategory[CategoryNo].name;
#endif
            OriginalItemName = groupInfo.name + "/" + origCategoryName + "/" + studioInfo.name;

            // Use old cache ID since it contains the name, which is translated here.
            // This way duplicate names will not have duplicate entries. Also old caches can still be used.
            ItemInfoLoader.TranslationCache.TryGetValue(OldCacheId, out var cachedTranslations);
            if (cachedTranslations != null)
            {
                CategoryName = cachedTranslations.CategoryName;
                GroupName = cachedTranslations.GroupName;
                ItemName = cachedTranslations.ItemName;
            }
            else
            {
                // Get translated versions of the relevant strings
                Translate(groupInfo.name, s =>
                {
                    GroupName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
                Translate(origCategoryName, s =>
                {
                    CategoryName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
                Translate(studioInfo.name, s =>
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

        private string OriginalItemName { get; }

        /// <summary>
        /// Index of the item as used by the current game instance. Changes between game restarts for zipmods.
        /// </summary>
        public int LocalSlot { get; }

        /// <summary>
        /// Original index of the item if it was loaded from a zipmod, -1 otherwise. Stays constant between game restarts.
        /// </summary>
        public int ZipmodSlot { get; }

        /// <summary>
        /// String to search against
        /// </summary>
        internal string SearchString { get; private set; }

        /// <summary>
        /// String with developer info, used to build SearchString
        /// </summary>
        internal string DeveloperSearchString { get; }

        public Sprite Thumbnail => ThumbnailLoader.GetThumbnail(this);

        /// <summary>
        /// Item is a sound effect and should get the SFX thumbnail
        /// </summary>
        public bool IsSFX =>
#if KK || KKS
            GroupNo == 00000011; // stock 3d sfx
#elif AI || HS2
            GroupNo == 00000009 || // stock 3d sfx
            GroupNo == 2171; // dirty's 3dsfx
#endif

        [Obsolete("Will be removed", true)]
        public string CacheId => OldCacheId;
        internal string NewCacheId { get; }
        [Obsolete]
        internal string OldCacheId { get; }

        /// <summary>
        /// If this item is from a zipmod, GUID of the zipmod. Otherwise null.
        /// </summary>
        public string GUID { get; }

        /// <summary>
        /// If this item is from a zipmod, name of the .zipmod file. Otherwise null.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Relative path to the asset bundle that contains this item
        /// </summary>
        public string Bundle { get; }

        /// <summary>
        /// Name of this item's asset inside of the asset bundle
        /// </summary>
        public string Asset { get; }

        /// <summary>
        /// Spawn this item in studio
        /// </summary>
        public void AddItem()
        {
            try
            {
                Studio.Studio.Instance.AddItem(GroupNo, CategoryNo, LocalSlot);
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

            if (!string.IsNullOrEmpty(GUID))
                searchStr += "\v" + GUID;

            SearchString = searchStr.Replace(' ', '_').ToLowerInvariant();
        }

        private static string MakeNewCacheId(int groupNo, int categoryNo, int slotNo)
        {
            // Sideloader generated local slot numbers start at 100000000, so real slot numbers should have at most 8 digits
            return $"{groupNo:D8}-{categoryNo:D8}-{slotNo:D8}";
        }

        [Obsolete]
        private string MakeOldCacheId(int groupNo, int categoryNo, Info.ItemLoadInfo item)
        {
            // Can't use itemNo because it can change with sideloader
            return $"{groupNo:D8}-{categoryNo:D8}-{Utils.MakeValidFileName(item.name)}";
            // even older - return $"{groupNo:D8}-{categoryNo:D8}-{item.name.GetHashCode():D32}";
        }

        public override int GetHashCode()
        {
            return string.Concat(GroupNo, '/', CategoryNo, '/', LocalSlot).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ItemInfo i && i.GroupNo == GroupNo && i.CategoryNo == CategoryNo && i.LocalSlot == LocalSlot;
        }

        public override string ToString()
        {
            return $"Name=\"{FullName}\" GroupNo={GroupNo} CategoryNo={CategoryNo} LocalSlot={LocalSlot} ZipmodSlot={ZipmodSlot} Bundle=\"{Bundle}\" Asset=\"{Asset}\" GUID=\"{GUID}\" Zipmod=\"{FileName}\"";
        }

        public string ToDescriptionString()
        {
            var isZipmod = ZipmodSlot >= 0;

            var sb = new StringBuilder();
            sb.AppendLine(FullName);
            
            sb.Append($"Group = {GroupNo}  Category = {CategoryNo}");
            if (isZipmod) sb.Append($"  LocalSlot = {LocalSlot}  ZipmodSlot = {ZipmodSlot}");
            else sb.Append($"  Slot = {LocalSlot}");
            sb.AppendLine();

            sb.AppendLine($"Bundle = \"{Bundle}\"  Asset = \"{Asset}\"");

            if (isZipmod)
                sb.Append($"GUID = \"{GUID}\"  Zipmod = \"{FileName}\"");
            else
                sb.Append("This is a base game item or a hardmod");

            return sb.ToString();
        }

        private static void Translate(string input, Action<string> updateAction)
        {
            if (KKAPI.Utilities.TranslationHelper.AutoTranslatorInstalled)
            {
                var didFire = false;
                KKAPI.Utilities.TranslationHelper.TranslateAsync(input, s =>
                {
                    updateAction(s);
                    didFire = true;
                    ItemInfoLoader.TriggerCacheSave();
                });
                if (didFire) return;
            }

            // Make sure there's a valid value set
            updateAction(input);
            ItemInfoLoader.TriggerCacheSave();
        }
    }
}
