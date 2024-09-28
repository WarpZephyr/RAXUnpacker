using System.Text;

namespace RAXUnpacker.Extensions
{
    internal static class BinaryWriterExtensions
    {
        internal static void Pad(this BinaryWriter bw, long alignment)
        {
            long remaining = bw.BaseStream.Position.Remaining(alignment);
            if (remaining > 0)
            {
                bw.BaseStream.Write(new byte[remaining]);
            }
        }

        internal async static Task WriteBytesAsync(this BinaryWriter bw, byte[] bytes)
            => await bw.BaseStream.WriteAsync(bytes);

        internal static void WriteFixedString(this BinaryWriter bw, string value)
            => bw.Write(Encoding.ASCII.GetBytes(value));
    }
}
