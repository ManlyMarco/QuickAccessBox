using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KK_QuickAccessBox
{
    internal static class Utils
    {
        public static byte[] GetResourceBytes(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourceFileName));

            using (var stream = assembly.GetManifestResourceStream(resourceName))
                return ReadFully(stream ?? throw new InvalidOperationException($"The resource {resourceFileName} was not found"));
        }

        public static Texture2D LoadTexture(byte[] texData)
        {
            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            tex.LoadImage(texData);
            return tex;
        }

        public static Sprite ToSprite(this Texture2D texture)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        private static byte[] ReadFully(Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);
                return ms.ToArray();
            }
        }

        internal static void MarkXuaIgnored(this Component target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.gameObject.name += "(XUAIGNORE)";
        }
    }
}