using System.Text;

namespace RAXUnpacker.Extensions
{
    internal static class BinaryReaderExtensions
    {
        internal static byte[] ReadBytesSafe(this BinaryReader br, int count)
        {
            if (count == 0)
            {
                return [];
            }

            byte[] result = br.ReadBytes(count);
            if (result.Length != count)
            {
                throw new EndOfStreamException("Cannot read beyond the end of the stream.");
            }
            return result;
        }

        internal async static Task<byte[]> ReadBytesSafeAsync(this BinaryReader br, int count)
        {
            if (count == 0)
            {
                return [];
            }

            byte[] bytes = new byte[count];
            if (await br.BaseStream.ReadAsync(bytes) == -1)
            {
                throw new EndOfStreamException("Cannot read beyond the end of the stream.");
            }
            return bytes;
        }

        internal static string ReadFixedString(this BinaryReader br, int length)
            => Encoding.ASCII.GetString(br.ReadBytesSafe(length));

        internal async static Task<string> ReadFixedStringAsync(this BinaryReader br, int length)
            => Encoding.ASCII.GetString(await br.ReadBytesSafeAsync(length));
    }
}
