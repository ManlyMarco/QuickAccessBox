using System;
using System.IO;
using System.Linq;
using System.Reflection;

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
    }
}