using MessagePack;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace KK_QuickAccessBox
{
    /// <summary>
    /// Has to be public for MessagePack, do not use outside QAB.
    /// </summary>
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
