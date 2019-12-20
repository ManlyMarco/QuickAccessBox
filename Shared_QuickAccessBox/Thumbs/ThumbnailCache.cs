using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
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

            ThreadingHelper.Instance.StartAsyncInvoke(
                () =>
                {
                    // Make sure the keys are unique. In case of duplicates allow mods to override the stock thumbs by picking the longest filename from the dupes
                    _pngNameCache = Sideloader.Sideloader.GetPngNames()
                        .GroupBy(Path.GetFileNameWithoutExtension)
                        .ToDictionary(gr => gr.Key, gr => gr.OrderByDescending(s => s.Length).First());
                    return null;
                });

            var thumbMissing = Utils.LoadTexture(Utils.GetResourceBytes("thumb_missing.png")) ?? throw new ArgumentNullException(nameof(_thumbMissing));
            _thumbMissing = thumbMissing.ToSprite();
            var thumbSound = Utils.LoadTexture(Utils.GetResourceBytes("thumb_sfx.png")) ?? throw new ArgumentNullException(nameof(_thumbSound));
            _thumbSound = thumbSound.ToSprite();
        }

        public static void Dispose()
        {
            foreach (var thumb in _thumbnailCache.Values)
                UnityEngine.Object.Destroy(thumb);

            _thumbnailCache.Clear();
            _pngNameCache = null;
        }

        public static bool CustomThumbnailAvailable(ItemInfo itemInfo)
        {
            return _pngNameCache != null && _pngNameCache.ContainsKey(itemInfo.CacheId);
        }
    }
}
