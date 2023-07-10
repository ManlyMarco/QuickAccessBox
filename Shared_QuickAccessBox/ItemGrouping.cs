using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace KK_QuickAccessBox
{
    internal sealed class ItemGrouping
    {
        private readonly Action _onChanged;
        public string SavePath { get; }

        private readonly Dictionary<string, HashSet<string>> _items = new Dictionary<string, HashSet<string>>();

        public ItemGrouping(string savePath, bool autoSaveLoad, Action onChanged)
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

        public bool Check(string guid, string itemId)
        {
            if (guid == null) guid = string.Empty;
            return _items.TryGetValue(guid, out var items) && items.Contains(itemId);
        }

        public bool RemoveMod(string guid)
        {
            if (guid == null) guid = string.Empty;
            var any = _items.Remove(guid);
            if (any) _onChanged?.Invoke();
            return any;
        }

        public bool RemoveItem(string guid, string itemId)
        {
            if (guid == null) guid = string.Empty;
            if (_items.TryGetValue(guid, out var items))
            {
                if (items.Remove(itemId))
                {
                    if (items.Count == 0)
                        _items.Remove(guid);

                    _onChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public bool AddMod(string guid)
        {
            if (guid == null) guid = string.Empty;
            if (!_items.TryGetValue(guid, out var items))
            {
                items = new HashSet<string>();
                _items.Add(guid, items);
            }

            var any = false;
            foreach (var item in ItemInfoLoader.ItemList.Where(x => (x.GUID ?? string.Empty) == guid))
            {
                if (items.Add(item.NewCacheId))
                    any = true;
            }

            if (any) _onChanged?.Invoke();
            return any;
        }

        public bool AddItem(string guid, string itemId)
        {
            if (guid == null) guid = string.Empty;
            if (!_items.TryGetValue(guid, out var items))
            {
                items = new HashSet<string>();
                _items.Add(guid, items);
            }

            var any = items.Add(itemId);

            if (any) _onChanged?.Invoke();
            return any;
        }

        public void TrySave()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                using (var fs = File.Create(SavePath))
                using (var wr = new StreamWriter(fs))
                {
                    foreach (var mod in _items)
                    {
                        foreach (var item in mod.Value)
                        {
                            wr.Write(mod.Key);
                            wr.Write('\0');
                            wr.Write(item);
                            wr.WriteLine();
                        }
                    }
                }

                QuickAccessBox.Logger.LogDebug($"Finished saving to [{Path.GetFileName(SavePath)}] in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                QuickAccessBox.Logger.LogWarning($"Failed to save item list to [{SavePath}] because of error: {ex}");
            }
        }

        public void TryLoad()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var sw = Stopwatch.StartNew();

                    var lines = File.ReadAllLines(SavePath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;

                        var s = line.Split(new char[] { '\0' }, 2, StringSplitOptions.None);
                        if (s.Length != 2) continue;

                        var guid = s[0];
                        var itemId = s[1];
                        if (itemId.Length == 0) continue;

                        if (!_items.TryGetValue(guid, out var items))
                        {
                            items = new HashSet<string>();
                            _items.Add(guid, items);
                        }
                        items.Add(itemId);
                    }

                    QuickAccessBox.Logger.LogDebug($"Finished reading from [{Path.GetFileName(SavePath)}] in {sw.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                QuickAccessBox.Logger.LogWarning($"Failed to load item list from [{SavePath}] because of error: {ex}");
            }
        }

        public bool Any()
        {
            return _items.Count > 0;
        }
    }
}
