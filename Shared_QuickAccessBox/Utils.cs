using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_QuickAccessBox
{
    internal static class Utils
    {
        private static char[] _invalids;

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

        public static bool IsFavorited(this ItemInfo item) => QuickAccessBox.Instance.Favorited.Check(item.GUID, item.NewCacheId);
        public static bool IsBlacklisted(this ItemInfo item) => QuickAccessBox.Instance.Blacklisted.Check(item.GUID, item.NewCacheId);
    }
}