using System.Drawing;
using System.Runtime.InteropServices;
using RAXUnpacker.Extensions;

namespace RAXUnpacker
{
    /// <summary>
    /// A section header in an RAX archive.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RAXSection
    {
        /// <summary>
        /// The signature of this <see cref="RAXSection"/>.<br/>
        /// Usually the extension of the file in uppercase.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public char[] Signature;

        /// <summary>
        /// The length of the entire <see cref="RAXSection"/> including the header of the section, and padding.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int SectionLength;

        /// <summary>
        /// The length of the file path of the data in the <see cref="RAXSection"/> including padding.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int NameLength;

        /// <summary>
        /// The length of the data in the <see cref="RAXSection"/>.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public int DataLength;

        /// <summary>
        /// Create a <see cref="RAXSection"/>.
        /// </summary>
        /// <param name="signature">The signature of the section. Will be uppercased and trimmed to 4 chars if necessary.</param>
        /// <param name="sectionLength">The length of the entire section including this header.</param>
        /// <param name="nameLength">The length of the file path to the data in the section.</param>
        /// <param name="dataLength">The length of the data in the section.</param>
        public RAXSection(string signature, int sectionLength, int nameLength, int dataLength)
        {
            Signature = new char[4];
            int length = signature.Length <= 4 ? signature.Length : 4;
            for (int i = 0; i < length; i++)
            {
                char c = signature[i];
                if (c != '\0')
                {
                    Signature[i] = char.ToUpper(c);
                }
            }

            SectionLength = sectionLength;
            NameLength = nameLength;
            DataLength = dataLength;
        }

        /// <summary>
        /// Read an <see cref="RAXSection"/>.
        /// </summary>
        /// <param name="br">A <see cref="BinaryReader"/>.</param>
        public RAXSection(BinaryReader br)
        {
            Signature = br.ReadChars(4);
            SectionLength = br.ReadInt32();
            NameLength = br.ReadInt32();
            DataLength = br.ReadInt32();
        }

        /// <summary>
        /// Write this <see cref="RAXSection"/>.
        /// </summary>
        /// <param name="bw">A <see cref="BinaryWriter"/>.</param>
        public readonly void Write(BinaryWriter bw)
        {
            bw.Write(Signature);
            bw.Write(SectionLength);
            bw.Write(NameLength);
            bw.Write(DataLength);
        }

        /// <summary>
        /// Converts this <see cref="RAXSection"/> into bytes.
        /// </summary>
        /// <returns>A byte array.</returns>
        public readonly byte[] GetBytes()
        {
            byte[] bytes = new byte[0x10];
            byte[] signatureBytes = [(byte)Signature[0], (byte)Signature[1], (byte)Signature[2], (byte)Signature[3]];
            byte[] sectionLengthBytes = BitConverter.GetBytes(SectionLength);
            byte[] nameLengthBytes = BitConverter.GetBytes(NameLength);
            byte[] dataLengthBytes = BitConverter.GetBytes(DataLength);

            Array.Copy(signatureBytes, 0, bytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(sectionLengthBytes);
                Array.Reverse(nameLengthBytes);
                Array.Reverse(dataLengthBytes);
            }

            Array.Copy(sectionLengthBytes, 0, bytes, 4, 4);
            Array.Copy(nameLengthBytes, 0, bytes, 8, 4);
            Array.Copy(dataLengthBytes, 0, bytes, 12, 4);
            return bytes;
        }
    }
}
