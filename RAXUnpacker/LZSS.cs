using RAXUnpacker.Extensions;

namespace RAXUnpacker
{
    internal static class LZSS
    {
        internal static bool Is(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Is(fs);
        }

        internal static bool Is(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes, false);
            return Is(ms);
        }

        internal static bool Is(Stream stream)
            => stream.GetFixedString(4) == "LZSS";
    }
}
