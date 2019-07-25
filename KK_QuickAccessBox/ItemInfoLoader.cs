using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using Studio;

namespace KK_QuickAccessBox
{
    internal static class ItemInfoLoader
    {
        public static void StartLoadItemsThread(Action<List<ItemInfo>> onFinished)
        {
            Logger.Log(LogLevel.Debug, "[KK_QuickAccessBox] Starting item information load thread");

            var t = new Thread(LoadItemsThread)
            {
                IsBackground = true,
                Name = "Collect item info",
                Priority = ThreadPriority.BelowNormal
            };
            t.Start(onFinished);
        }

        private static void LoadItemsThread(object obj)
        {
            var onFinished = (Action<List<ItemInfo>>)obj;

            var sw = Stopwatch.StartNew();

            var results = new List<ItemInfo>();

            // The instance has to be spawned by now to avoid crashing from crossthreaded FindObjectOfType
            var info = Info.Instance;
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

                            Logger.Log(LogLevel.Warning, $"Failed to load information about item: name={item.Value.name} group={@group.Key} category={category.Key} itemNo={item.Key} - {e.Message}");
                        }
                    }
                }
            }

            results.Sort((x, y) => String.Compare(x.FullName, y.FullName, StringComparison.Ordinal));

            sw.Stop();
            Logger.Log(LogLevel.Debug, $"[KK_QuickAccessBox] Item information load thread finished in {sw.Elapsed.TotalMilliseconds:F0}ms - {results.Count} valid items found");

            onFinished(results);
        }
    }
}