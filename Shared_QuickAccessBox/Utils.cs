using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        static char[] _invalids;

        /// <summary>
        /// Replaces characters in <c>text</c> that are not allowed in file names with the specified replacement character.
        /// https://stackoverflow.com/questions/620605/how-to-make-a-valid-windows-filename-from-an-arbitrary-string/25223884#25223884
        /// </summary>
        /// <param name="text">Text to make into a valid filename. The same string is returned if it is valid already.</param>
        /// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
        /// <param name="fancy">Whether to replace quotes and slashes with the non-ASCII characters ” and ⁄.</param>
        /// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
        public static string MakeValidFileName(string text, char? replacement = '_', bool fancy = true)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            var invalids = _invalids ?? (_invalids = Path.GetInvalidFileNameChars());
            bool changed = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (invalids.Contains(c))
                {
                    changed = true;
                    var repl = replacement ?? '\0';
                    if (fancy)
                    {
                        if (c == '"') repl = '”'; // U+201D right double quotation mark
                        else if (c == '\'') repl = '’'; // U+2019 right single quotation mark
                        else if (c == '/') repl = '⁄'; // U+2044 fraction slash
                    }
                    if (repl != '\0')
                        sb.Append(repl);
                }
                else
                    sb.Append(c);
            }
            if (sb.Length == 0)
                return "_";
            return changed ? sb.ToString() : text;
        }

        public static Bounds? CalculateBounds(IEnumerable<Transform> targets)
        {
            Bounds? b = null;
            foreach (var renderer in targets.SelectMany(x => x.GetComponentsInChildren<Renderer>()))
            {
                if (b == null)
                    b = renderer.bounds;
                else
                    b.Value.Encapsulate(renderer.bounds);
            }

            return b;
        }
    }
}