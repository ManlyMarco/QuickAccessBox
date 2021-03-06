﻿using MessagePack;

namespace KK_QuickAccessBox
{
    [MessagePackObject]
    public sealed class TranslationCacheEntry
    {
        public static TranslationCacheEntry FromItemInfo(ItemInfo info)
        {
            return new TranslationCacheEntry {CategoryName = info.CategoryName, GroupName = info.GroupName, ItemName = info.ItemName};
        }

        [Key(0)]
        public string CategoryName;
        [Key(1)]
        public string GroupName;
        [Key(2)]
        public string ItemName;
    }
}
