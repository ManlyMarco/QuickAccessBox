using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using BepInEx;
using ChaCustom;
using MessagePack;
using Studio;
using UniRx;
using UniRx.Triggers;
using Timer = System.Timers.Timer;

namespace KK_QuickAccessBox
{
    internal static class ItemInfoLoader
    {
        private static readonly string _cachePath = Path.Combine(Paths.CachePath, "KK_QuickAccessBox.cache");

        internal static Dictionary<string, TranslationCacheEntry> TranslationCache { get; private set; }
        public static IEnumerable<ItemInfo> ItemList { get; private set; }

        public static void TriggerCacheSave()
        {
            _cacheSaveTimer.Stop();
            _cacheSaveTimer.Start();
        }

        public static void Dispose()
        {
            if(_cacheSaveTimer.Enabled)
                SaveTranslationCache();

            _cacheSaveTimer.Dispose();
        }

        private static Timer _cacheSaveTimer;
        private static void SetupCache()
        {
            LoadTranslationCache();

            void OnSave(object sender, ElapsedEventArgs args)
            {
                _cacheSaveTimer.Stop();
                SaveTranslationCache();
            }

            // Timeout has to be long enough to ensure people with potato internet can still get the translations in time
            _cacheSaveTimer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds);
            _cacheSaveTimer.Elapsed += OnSave;
            _cacheSaveTimer.AutoReset = false;
            _cacheSaveTimer.SynchronizingObject = ThreadingHelper.SynchronizingObject;

            // If a cache save is still pending on maker exit, run it immediately
            CustomBase.Instance.OnDestroyAsObservable().Subscribe(_ => { if (_cacheSaveTimer.Enabled) OnSave(null, null); });
        }

        public static void LoadItems()
        {
            QuickAccessBox.Logger.LogDebug("Starting item information load thread");

            SetupCache();

            var results = new List<ItemInfo>();

            // The instance should have been spawned by now
            var info = Info.Instance;

            var sw = Stopwatch.StartNew();
            foreach (var group in info.dicItemLoadInfo)
            {
                foreach (var category in @group.Value)
                {
                    foreach (var item in category.Value)
                    {
                        try
                        {
                            results.Add(new ItemInfo(@group.Key, category.Key, item.Key, item.Value));
                        }
                        catch (Exception e)
                        {
                            if (e is TargetInvocationException ie && ie.InnerException != null)
                                e = ie.InnerException;

                            QuickAccessBox.Logger.LogWarning($"Failed to load information about item: name={item.Value.name} group={@group.Key} category={category.Key} itemNo={item.Key} - {e.Message}");
                        }
                    }
                }
            }

            QuickAccessBox.Logger.LogDebug($"Item information loading finished in {sw.Elapsed.TotalMilliseconds:F0}ms - {results.Count} valid items found");
            results.Sort((x, y) => String.Compare(x.FullName, y.FullName, StringComparison.Ordinal));
            ItemList = results;

        }

        private static void LoadTranslationCache()
        {
            if (File.Exists(_cachePath))
            {
                try
                {
                    var cacheBytes = File.ReadAllBytes(_cachePath);
                    TranslationCache = MessagePackSerializer.Deserialize<Dictionary<string, TranslationCacheEntry>>(cacheBytes);
                }
                catch (Exception ex)
                {
                    QuickAccessBox.Logger.LogWarning("Failed to read cache: " + ex.Message);
                }
            }
            if (TranslationCache == null)
                TranslationCache = new Dictionary<string, TranslationCacheEntry>();
        }

        private static void SaveTranslationCache()
        {
            try
            {
                if (ItemList == null) return;

                var newCache = ItemList
                    .GroupBy(info => info.CacheId)
                    .Select(infos =>
                    {
                        if (infos.Count() != 1)
                            QuickAccessBox.Logger.LogWarning($"Cache collision on item {infos.Key}, please consider renaming it");

                        // Items have the same full names so translations can be reused for both of them
                        return infos.First();
                    })
                    .ToDictionary(info => info.CacheId, TranslationCacheEntry.FromItemInfo);

                var data = MessagePackSerializer.Serialize(newCache);
                File.WriteAllBytes(_cachePath, data);
            }
            catch (Exception ex)
            {
                QuickAccessBox.Logger.LogWarning("Failed to write cache: " + ex.Message);
            }
        }
    }
}
