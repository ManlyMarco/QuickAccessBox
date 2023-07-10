using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MessagePack;

namespace KK_QuickAccessBox
{
    internal class ItemRecents
    {
        private readonly Action _onChanged;
        public string SavePath { get; }

        private Dictionary<string, DateTime> _recents = new Dictionary<string, DateTime>();

        public ItemRecents(string savePath, bool autoSaveLoad, Action onChanged)
        {
            SavePath = savePath ?? throw new ArgumentNullException(nameof(savePath));
            if (autoSaveLoad)
            {
                _onChanged = onChanged + TrySave;
                TryLoad();
            }
            else
            {
                _onChanged = onChanged;
            }
        }

        public void TrySave()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                File.WriteAllBytes(SavePath, MessagePackSerializer.Serialize(_recents));

                QuickAccessBox.Logger.LogDebug($"Finished saving to [{Path.GetFileName(SavePath)}] in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                QuickAccessBox.Logger.LogWarning("Failed to save recents: " + ex.Message);
            }
        }

        public void TryLoad()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var sw = Stopwatch.StartNew();

                    var bytes = File.ReadAllBytes(SavePath);
                    _recents = MessagePackSerializer.Deserialize<Dictionary<string, DateTime>>(bytes);
                    TrimRecents();

                    QuickAccessBox.Logger.LogDebug($"Finished reading from [{Path.GetFileName(SavePath)}] in {sw.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                QuickAccessBox.Logger.LogWarning("Failed to read recents: " + ex.Message);
            }
        }

        private int TrimRecents()
        {
            var toTrim = _recents.OrderByDescending(x => x.Value).Skip(QuickAccessBox.RecentsCount.Value).ToList();

            foreach (var toRemove in toTrim)
                _recents.Remove(toRemove.Key);

            return toTrim.Count;
        }

        public void BumpItemLastUseDate(string newCacheId)
        {
            _recents[newCacheId] = DateTime.UtcNow;
            TrimRecents();
            _onChanged?.Invoke();
        }

        public bool TryGetLastUseDate(string newCacheId, out DateTime lastUseDate)
        {
            return _recents.TryGetValue(newCacheId, out lastUseDate);
        }
    }
}
