using RAXUnpacker.Extensions;
using RAXUnpacker.Handlers;
using System.Text;

namespace RAXUnpacker
{
    /// <summary>
    /// Writes an RAX archive on-demand.
    /// </summary>
    public class RAXWriter : IDisposable
    {
        /// <summary>
        /// The underlying <see cref="BinaryWriter"/>.
        /// </summary>
        private readonly BinaryWriter _bw;

        /// <summary>
        /// Whether or not this <see cref="RAXWriter"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The amount to align things by in the RAX archive.
        /// </summary>
        public int AlignmentSize { get; set; }

        /// <summary>
        /// The number of files written so far.
        /// </summary>
        public int Written { get; private set; } = 0;

        /// <summary>
        /// The files to write currently.
        /// </summary>
        private Queue<FileArchivePathInfo> _files;

        /// <summary>
        /// The current reserved capacity.
        /// </summary>
        private int _capacity;

        /// <summary>
        /// The position of the file count.
        /// </summary>
        private long _count_position;

        /// <summary>
        /// Creates a new <see cref="RAXWriter"/>.
        /// </summary>
        /// <param name="outputPath">The path to write the RAX archive to.</param>
        /// <param name="capacity">The amount of files that is to be written.</param>
        /// <param name="alignment">The amount to align things by in the RAX archive.</param>
        public RAXWriter(string outputPath, int capacity = default, int alignment = 0x10) : this(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read), capacity, alignment, false) { }

        /// <summary>
        /// Creates a new <see cref="RAXWriter"/>.
        /// </summary>
        /// <param name="raxStream">The stream to write the RAX archive to.</param>
        /// <param name="capacity">The amount of files that is to be written.</param>
        /// <param name="alignment">The amount to align things by in the RAX archive.</param>
        /// <param name="leaveOpen">Whether or not to leave the <see cref="Stream"/> open after this <see cref="RAXWriter"/> is no longer being used.</param>
        public RAXWriter(Stream raxStream, int capacity = default, int alignment = 0x10, bool leaveOpen = false)
        {
            _capacity = capacity;
            AlignmentSize = alignment;
            _bw = new BinaryWriter(raxStream, Encoding.ASCII, leaveOpen);
            _files = new Queue<FileArchivePathInfo>(capacity);
            WriteHeader();
        }

        #region Factory Methods

        /// <summary>
        /// Creates a new <see cref="RAXWriter"/> with all files in the input directory.
        /// </summary>
        /// <param name="raxStream">The stream to write the RAX archive to.</param>
        /// <param name="inputDirectory">The directory with files to write into the RAX archive.</param>
        /// <param name="leaveOpen">Whether or not to leave the <see cref="Stream"/> open after this <see cref="RAXWriter"/> is no longer being used.</param>
        /// <returns>A new <see cref="RAXWriter"/>.</returns>
        public static RAXWriter FromDirectory(Stream raxStream, string inputDirectory, bool leaveOpen = false)
        {
            var filesQueue = new Queue<FileArchivePathInfo>();
            var files = Directory.EnumerateFiles(inputDirectory, "*", SearchOption.AllDirectories);

            // Get alignment size by checking the first file
            var enumerator = files.GetEnumerator();
            string firstFile = enumerator.Current;
            filesQueue.Enqueue(new FileArchivePathInfo(firstFile, PathHandler.GetRelativePathWithoutLeadingSlash(firstFile, inputDirectory)));
            int alignment = LZSS.Is(firstFile) ? 4 : 0x10;
            enumerator.MoveNext();

            foreach (var file in files)
            {
                filesQueue.Enqueue(new FileArchivePathInfo(file, PathHandler.GetRelativePathWithoutLeadingSlash(file, inputDirectory)));
            }

            var writer = new RAXWriter(raxStream, filesQueue.Count, alignment, leaveOpen)
            {
                _files = filesQueue
            };

            return writer;
        }

        /// <summary>
        /// Creates a new <see cref="RAXWriter"/> with all files in the input directory.
        /// </summary>
        /// <param name="outputPath">The path to write the RAX archive to.</param>
        /// <param name="inputDirectory">The directory with files to write into the RAX archive.</param>
        /// <returns>A new <see cref="RAXWriter"/>.</returns>
        public static RAXWriter FromDirectory(string outputPath, string inputDirectory)
            => FromDirectory(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read), inputDirectory, false);

        /// <summary>
        /// Writes an RAX archive to the specified <see cref="Stream"/> with all files in the input directory.
        /// </summary>
        /// <param name="raxStream">The stream to write the RAX archive to.</param>
        /// <param name="inputDirectory">The directory with files to write into the RAX archive.</param>
        public static void WriteFromDirectory(Stream raxStream, string inputDirectory)
            => FromDirectory(raxStream, inputDirectory).WriteAllFiles();

        /// <summary>
        /// Writes an RAX archive to the specified path with all files in the input directory.
        /// </summary>
        /// <param name="outputPath">The path to write the RAX archive to.</param>
        /// <param name="inputDirectory">The directory with files to write into the RAX archive.</param>
        public static void WriteFromDirectory(string outputPath, string inputDirectory)
            => FromDirectory(outputPath, inputDirectory).WriteAllFiles();

        #endregion

        #region Internal Write Methods

        /// <summary>
        /// Writes the header of the RAX archive.
        /// </summary>
        private void WriteHeader()
        {
            _bw.WriteFixedString("RAX\0");
            _bw.Write(0x10);
            _bw.Write((short)0);
            _bw.Write((short)2);

            _count_position = _bw.BaseStream.Position;
            _bw.Write(_capacity);
        }

        /// <summary>
        /// Writes the next file section.
        /// </summary>
        /// <param name="file">The file to write.</param>
        private void WriteSection(FileArchivePathInfo file)
        {
            string path = file.ArchivePath;

            bool extendedNamePadding;
            int nameLength = path.Length.Align(AlignmentSize);
            if ((path.Length % AlignmentSize) == 0)
            {
                nameLength += AlignmentSize;
                extendedNamePadding = true;
            }
            else
            {
                extendedNamePadding = false;
            }

            string signature = PathHandler.GetExtensionWithoutExtensionDot(path).ToUpper();

            byte[] data = File.ReadAllBytes(file.FullPath);
            int dataLength = data.Length;
            int sectionLength = 16 + nameLength + dataLength.Align(AlignmentSize);

            var section = new RAXSection(signature, sectionLength, nameLength, dataLength);
            section.Write(_bw);
            _bw.WriteFixedString(path.Replace('\\', '/'));
            _bw.Pad(AlignmentSize);
            if (extendedNamePadding)
            {
                _bw.Write(new byte[AlignmentSize]);
            }

            _bw.Write(data);
            _bw.Pad(AlignmentSize);
            Written++;
        }

        #endregion

        #region Write

        /// <summary>
        /// Writes the next file.
        /// </summary>
        /// <exception cref="InvalidOperationException">The writer is disposed or there are no files in the queue to write.</exception>
        public void WriteNextFile()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("The writer is disposed.");
            }

            if (_files.Count == 0)
            {
                throw new InvalidOperationException("There are no files in the queue to write.");
            }

            if (Written > _capacity)
            {
                SetCapacity(Written);
            }

            WriteSection(_files.Dequeue());
        }

        /// <summary>
        /// Writes the specified number of files next in the file queue.
        /// </summary>
        /// <param name="count">The number of files to write.</param>
        /// <exception cref="InvalidOperationException">The writer is disposed.</exception>
        /// <exception cref="ArgumentException">Count must not be greater than the number of remaining files.</exception>
        public void WriteNextFiles(int count)
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("The writer is disposed.");
            }

            if (count > _files.Count)
            {
                throw new ArgumentException("Count must not be greater than the number of remaining files.", nameof(count));
            }

            for (int i = 0; i < count; i++)
            {
                WriteSection(_files.Dequeue());
            }

            if (Written > _capacity)
            {
                SetCapacity(Written);
            }
        }

        /// <summary>
        /// Writes all files present in the file queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">The writer is disposed or there are no files in the queue to write.</exception>
        public void WriteAllFiles()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("The writer is disposed.");
            }

            if (_files.Count == 0)
            {
                throw new InvalidOperationException("There are no files in the queue to write.");
            }

            int count = _files.Count;
            for (int i = 0; i < count; i++)
            {
                WriteSection(_files.Dequeue());
            }

            if (Written > _capacity)
            {
                SetCapacity(Written);
            }
        }

        #endregion

        #region Queue Helpers

        /// <summary>
        /// Adds the given file to the file queue.
        /// </summary>
        /// <param name="path">The file path to the file to add.</param>
        /// <param name="archivePath">The path to write in the archive for this file.</param>
        public void Add(string path, string archivePath)
        {
            _files.Enqueue(new FileArchivePathInfo(path, archivePath));
        }

        /// <summary>
        /// Adds a file to the file queue.
        /// </summary>
        /// <param name="rootDirectory">The root directory of the path to the file so that it may be removed if necessary.</param>
        /// <param name="path">The path of the file to add.</param>
        public void AddRelative(string rootDirectory, string path)
        {
            _files.Enqueue(new FileArchivePathInfo(path, PathHandler.GetRelativePathWithoutLeadingSlash(path, rootDirectory)));
        }

        /// <summary>
        /// Adds the given list of files to the file queue.
        /// </summary>
        /// <param name="rootDirectory">The root directory of all paths so that it may be removed if necessary.</param>
        /// <param name="paths">The paths to the files to add.</param>
        public void AddRangeRelative(string rootDirectory, IList<string> paths)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                _files.Enqueue(new FileArchivePathInfo(path, PathHandler.GetRelativePathWithoutLeadingSlash(path, rootDirectory)));
            }
        }

        /// <summary>
        /// Set the capacity of the RAX archive being written.
        /// </summary>
        /// <param name="count">The capacity to set.</param>
        /// <exception cref="InvalidOperationException">Cannot set capacity to value lower than the amount of files already written.</exception>
        public void SetCapacity(int count)
        {
            if (count < Written)
            {
                throw new InvalidOperationException("Cannot set capacity to value lower than the amount of files already written.");
            }

            _capacity = count;
            long position = _bw.BaseStream.Position;
            _bw.BaseStream.Position = _count_position;
            _bw.Write(_capacity);
            _bw.BaseStream.Position = position;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose of this <see cref="RAXWriter"/>.
        /// </summary>
        /// <param name="disposing">Whether or not to dispose of managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (_files.Count != 0)
                    {
                        throw new InvalidOperationException("The file queue is not empty.");
                    }

                    if (Written < _capacity)
                    {
                        throw new InvalidOperationException("Set capacity has not been filled.");
                    }

                    _bw.Dispose();
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
