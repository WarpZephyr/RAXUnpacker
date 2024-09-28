using System.Diagnostics.CodeAnalysis;
using System.Text;
using RAXUnpacker.Extensions;

namespace RAXUnpacker
{
    /// <summary>
    /// Reads an RAX archive on-demand.
    /// </summary>
    public class RAXReader : IDisposable
    {
        /// <summary>
        /// The underlying <see cref="BinaryReader"/>.
        /// </summary>
        private readonly BinaryReader _br;

        /// <summary>
        /// Whether or not this <see cref="RAXReader"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The total number of files in this RAX archive.
        /// </summary>
        public int FileCount { get; private set; }

        /// <summary>
        /// The number of files remaining in the <see cref="RAXReader"/>.
        /// </summary>
        public int Remaining { get; private set; }

        /// <summary>
        /// Create a <see cref="RAXReader"/> and read the header of a RAX archive.
        /// </summary>
        /// <param name="path">The file path to the RAX archive.</param>
        /// <exception cref="InvalidDataException">The file was not an RAX file.</exception>
        /// <exception cref="Exception">There was an unexpected value.</exception>
        public RAXReader(string path) : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), false){}

        /// <summary>
        /// Create a <see cref="RAXReader"/> and read the header of a RAX archive.
        /// </summary>
        /// <param name="bytes">The bytes of the entire RAX archive.</param>
        /// <exception cref="InvalidDataException">The file was not an RAX file.</exception>
        /// <exception cref="Exception">There was an unexpected value.</exception>
        public RAXReader(byte[] bytes) : this(new MemoryStream(bytes, false), false){}

        /// <summary>
        /// Create a <see cref="RAXReader"/> and read the header of a RAX archive.
        /// </summary>
        /// <param name="stream">The stream positioned at the start of the RAX archive.</param>
        /// <param name="leaveOpen">Whether or not to leave the <see cref="Stream"/> open after this <see cref="RAXReader"/> is no longer being used.</param>
        /// <exception cref="InvalidDataException">The file was not an RAX file.</exception>
        /// <exception cref="Exception">There was an unexpected value.</exception>
        public RAXReader(Stream stream, bool leaveOpen = false)
        {
            _br = new BinaryReader(stream, Encoding.ASCII, leaveOpen);

            string magic = _br.ReadFixedString(4);
            if (magic != "RAX\0")
            {
                throw new InvalidDataException($"File magic is not RAX: {magic}");
            }

            if (_br.ReadInt32() != 16)
            {
                throw new Exception($"Unexpected header value, please notify the developer about this.");
            }

            if (_br.ReadInt16() != 0)
            {
                throw new Exception($"Unexpected header value, please notify the developer about this.");
            }

            if (_br.ReadInt16() != 2)
            {
                throw new Exception($"Unexpected header value, please notify the developer about this.");
            }

            FileCount = _br.ReadInt32();
            Remaining = FileCount;
        }

        #region Read

        /// <summary>
        /// Read the next file.
        /// </summary>
        /// <returns>The next file.</returns>
        /// <exception cref="InvalidOperationException">The reader is disposed or there are no files left to read.</exception>
        public FileDataInfo ReadNextFile()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("The reader is disposed.");
            }

            if (Remaining == 0)
            {
                throw new InvalidOperationException("There are no files left to read.");
            }

            long nextPosition = _br.BaseStream.Position;
            var section = new RAXSection(_br);
            nextPosition += section.SectionLength;

            var fileDataInfo = new FileDataInfo(_br.ReadFixedString(section.NameLength).TrimEnd('\0'), _br.ReadBytesSafe(section.DataLength));
            _br.BaseStream.Position = nextPosition;
            Remaining--;
            return fileDataInfo;
        }

        /// <summary>
        /// Read the next file asynchronously.
        /// </summary>
        /// <returns>The next file.</returns>
        /// <exception cref="InvalidOperationException">The reader is disposed or there are no files left to read.</exception>
        public async Task<FileDataInfo> ReadNextFileAsync()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("The reader is disposed.");
            }

            if (Remaining == 0)
            {
                throw new InvalidOperationException("There are no files left to read.");
            }

            long nextPosition = _br.BaseStream.Position;
            var section = new RAXSection(_br);
            nextPosition += section.SectionLength;

            var name = await _br.ReadFixedStringAsync(section.NameLength);
            var fileDataInfo = new FileDataInfo(name.TrimEnd('\0'), await _br.ReadBytesSafeAsync(section.DataLength));
            _br.BaseStream.Position = nextPosition;
            Remaining--;
            return fileDataInfo;
        }

        #endregion

        #region IsRead

        /// <summary>
        /// If the specified file is an RAX archive, return <see langword="true"/> and create a <see cref="RAXReader"/>.
        /// </summary>
        /// <param name="path">The file path to the file.</param>
        /// <param name="result">A <see cref="RAXReader"/> for the archive if valid, or <see langword="null"/>.</param>
        /// <returns>Whether or not the file was an RAX archive.</returns>
        public static bool IsRead(string path, [NotNullWhen(true)] out RAXReader? result)
            => IsRead(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), out result, false);

        /// <summary>
        /// If the specified byte array is an RAX archive, return <see langword="true"/> and create a <see cref="RAXReader"/>.
        /// </summary>
        /// <param name="bytes">A byte array.</param>
        /// <param name="result">A <see cref="RAXReader"/> for the archive if valid, or <see langword="null"/>.</param>
        /// <returns>Whether or not the byte array was an RAX archive.</returns>
        public static bool IsRead(byte[] bytes, [NotNullWhen(true)] out RAXReader? result)
            => IsRead(new MemoryStream(bytes, false), out result, false);

        /// <summary>
        /// If the specified <see cref="Stream"/> at the current position begins with an RAX archive, return <see langword="true"/> and create a <see cref="RAXReader"/>.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> with an RAX archive at the current position.</param>
        /// <param name="result">A <see cref="RAXReader"/> for the archive if valid, or <see langword="null"/>.</param>
        /// <param name="leaveOpen">Whether or not to leave the <see cref="Stream"/> open after this <see cref="RAXReader"/> is no longer being used.</param>
        /// <returns>Whether or not the data at the current position in the <see cref="Stream"/> was an RAX archive.</returns>
        public static bool IsRead(Stream stream, [NotNullWhen(true)] out RAXReader? result, bool leaveOpen = false)
        {
            if (stream.GetFixedString(4) == "RAX\0")
            {
                result = new RAXReader(stream, leaveOpen);
                return true;
            }

            result = null;
            return false;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose of this <see cref="RAXReader"/>.
        /// </summary>
        /// <param name="disposing">Whether or not to dispose of managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _br.Dispose();
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
