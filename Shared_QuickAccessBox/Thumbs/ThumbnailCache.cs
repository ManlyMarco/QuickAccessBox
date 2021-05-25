using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK_QuickAccessBox.Thumbs
{
    internal static class ThumbnailLoader
    {
        private static readonly Dictionary<string, Sprite> _thumbnailCache = new Dictionary<string, Sprite>();
        private static Dictionary<string, string> _pngNameCache;

        private static Sprite _thumbMissing;
        private static Sprite _thumbSound;

        public static Sprite GetThumbnail(ItemInfo info)
        {
            if (!_thumbnailCache.TryGetValue(info.CacheId, out var sprite))
            {
                if (_pngNameCache == null) return _thumbMissing;

                _pngNameCache.TryGetValue(info.CacheId, out var pngPath);
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

                _thumbnailCache.Add(info.CacheId, sprite);
            }

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

            var thumbMissing = Utils.LoadTexture(Utils.GetResourceBytes("thumb_missing.png")) ?? throw new ArgumentNullException(nameof(_thumbMissing));
            _thumbMissing = thumbMissing.ToSprite();
            var thumbSound = Utils.LoadTexture(Utils.GetResourceBytes("thumb_sfx.png")) ?? throw new ArgumentNullException(nameof(_thumbSound));
            _thumbSound = thumbSound.ToSprite();
        }

        public static void Dispose()
        {
#if DEBUG
            foreach (var thumb in _thumbnailCache.Values)
                Object.Destroy(thumb);
        
            _thumbnailCache.Clear();
            _pngNameCache = null;
#endif
        }

        public static bool CustomThumbnailAvailable(ItemInfo itemInfo)
        {
            return _pngNameCache != null && _pngNameCache.ContainsKey(itemInfo.CacheId);
        }
    }
}
