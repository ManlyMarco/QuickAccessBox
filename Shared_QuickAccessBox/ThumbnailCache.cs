﻿using System;
using System.Collections.Generic;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_QuickAccessBox.Thumbs
{
    internal static class ThumbnailLoader
    {
        public static List<QuickAccessBox.ThumbnailProvider> ThumbnailProviders { get; } = new List<QuickAccessBox.ThumbnailProvider>();

        private static readonly Dictionary<ItemInfo, Sprite> _thumbnailCache = new Dictionary<ItemInfo, Sprite>();
        private static Dictionary<string, string> _pngNameCache;

        private static Sprite _thumbMissing;
        private static Sprite _thumbSound;

        public static Sprite GetThumbnail(ItemInfo info)
        {
            if (_thumbnailCache.TryGetValue(info, out var sprite)) return sprite;
            if (_pngNameCache == null) return _thumbMissing;

            foreach (var thumbnailProvider in ThumbnailProviders)
            {
                try
                {
                    var thumb = thumbnailProvider(info);
                    if (thumb != null)
                    {
                        _thumbnailCache.Add(info, thumb);
                        return thumb;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            _pngNameCache.TryGetValue(info.NewCacheId, out var pngPath);
            if (pngPath == null)
            {
                // Fall back to old thumbnail names in case the mod wasn't updated
#pragma warning disable CS0612
                _pngNameCache.TryGetValue(info.OldCacheId, out pngPath);
                if (pngPath == null)
                    _pngNameCache.TryGetValue(info.NewCacheIdShort, out pngPath);
#pragma warning restore CS0612
            }

            var tex = Sideloader.Sideloader.GetPng(pngPath, TextureFormat.DXT5, false);
            if (tex != null)
            {
                sprite = tex.ToSprite();
            }
            else
            {
                if (info.IsSFX)
                    sprite = _thumbSound;
                else
                    sprite = _thumbMissing;
            }

            _thumbnailCache.Add(info, sprite);

            return sprite;
        }

        public static void LoadAssetBundle()
        {
            if (_thumbMissing != null) return;

            _pngNameCache = new Dictionary<string, string>();

            var pathSeparators = new[] { '/', '\\' };
            foreach (var pngName in Sideloader.Sideloader.GetPngNames())
            {
                //var name = Path.GetFileNameWithoutExtension(pngName); - order of magnitude slower
                var i = pngName.LastIndexOfAny(pathSeparators) + 1;
                var d = pngName.LastIndexOf('.');
                var name = pngName.Substring(i, d - i);

                // Prefer thumbs with longer paths to let mod authors override defaults
                if (_pngNameCache.TryGetValue(name, out var existing) && existing.Length > pngName.Length)
                    continue;
                _pngNameCache[name] = pngName;
            }

            _thumbMissing = ResourceUtils.GetEmbeddedResource("thumb_missing.png").LoadTexture().ToSprite();
            _thumbSound = ResourceUtils.GetEmbeddedResource("thumb_sfx.png").LoadTexture().ToSprite();
        }

        public static void Dispose()
        {
#if DEBUG
            foreach (var thumb in _thumbnailCache.Values)
                UnityEngine.Object.Destroy(thumb);
        
            _thumbnailCache.Clear();
            _pngNameCache = null;
#endif
        }

        public static bool CustomThumbnailAvailable(ItemInfo itemInfo)
        {
#pragma warning disable CS0612
            return _pngNameCache != null && (_pngNameCache.ContainsKey(itemInfo.NewCacheId) || _pngNameCache.ContainsKey(itemInfo.OldCacheId));
#pragma warning restore CS0612
        }
    }
}
