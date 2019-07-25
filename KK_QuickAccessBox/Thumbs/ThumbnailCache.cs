using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK_QuickAccessBox.Thumbs
{
    internal static class ThumbnailLoader
    {
        private static readonly Dictionary<string, Sprite> _thumbnailCache = new Dictionary<string, Sprite>();
        private static AssetBundle _thumbnailBundle;
        private static Sprite _thumbMissing;
        private static Sprite _thumbSound;

        public static Sprite GetThumbnail(ItemInfo info)
        {
            LoadAssetBundle();

            var cacheId = info.CacheId;

            if (!_thumbnailCache.TryGetValue(cacheId, out var sprite))
            {
                var getName = $"assets/thumbnails/{cacheId}.png";
                var tex = _thumbnailBundle.LoadAsset<Texture2D>(getName);
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

                _thumbnailCache.Add(cacheId, sprite);
            }

            return sprite;
        }

        public static void LoadAssetBundle()
        {
            if (_thumbnailBundle != null) return;

            var res = Utils.GetResourceBytes("quick_access_box_thumbs");
            var ab = AssetBundle.LoadFromMemory(res) ?? throw new Exception("Failed to load thumbnail bundle");
            _thumbnailBundle = ab;

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
            _thumbnailBundle.Unload(true);
        }
    }
}
