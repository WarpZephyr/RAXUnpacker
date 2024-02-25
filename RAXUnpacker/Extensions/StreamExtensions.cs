using System.Text;

namespace RAXUnpacker.Extensions
{
    internal static class StreamExtensions
    {
        internal static byte[] ReadBytesSafe(this Stream stream, int count)
        {
            if (count == 0)
            {
                return [];
            }

            byte[] bytes = new byte[count];
            int result = stream.Read(bytes);
            if (result == -1)
            {
                throw new EndOfStreamException("Cannot read beyond the end of the stream.");
            }
            return bytes;
        }

        internal async static Task<byte[]> ReadBytesSafeAsync(this Stream stream, int count)
        {
            if (count == 0)
            {
                return [];
            }

            byte[] bytes = new byte[count];
            if (await stream.ReadAsync(bytes) == -1)
            {
                throw new EndOfStreamException("Cannot read beyond the end of the stream.");
            }
            return bytes;
        }

        internal static string ReadFixedString(this Stream stream, int length)
            => Encoding.ASCII.GetString(stream.ReadBytesSafe(length));

        internal async static Task<string> ReadFixedStringAsync(this Stream stream, int length)
            => Encoding.ASCII.GetString(await stream.ReadBytesSafeAsync(length));

        internal static string GetFixedString(this Stream stream, int length)
        {
            long position = stream.Position;
            string result = stream.ReadFixedString(length);
            stream.Position = position;
            return result;
        }
    }
}
