using RAXUnpacker.Handlers;
using System.IO;

namespace RAXUnpacker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                var unpackTasks = new List<Task>();
                foreach (string arg in args)
                {
                    if (File.Exists(arg))
                    {
                        if (RAXReader.IsRead(arg, out RAXReader? reader))
                        {
                            unpackTasks.Add(UnpackRAXAsync(arg, reader));
                        }
                    }
                    else if (Directory.Exists(arg))
                    {
                        RepackRAX(arg);
                    }
                }

                await Task.WhenAll(unpackTasks);
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred:\n{ex.Message}\n{ex.StackTrace}");
                Console.ReadKey();
            }
#endif
        }

        static async Task UnpackRAXAsync(string path, RAXReader reader)
        {
            bool writeName = true;
            string directory = PathHandler.GetDirectoryName(path);
            string name = PathHandler.GetFileNameWithChangedExtensionChar(path, '-');
            string outputDirectory = PathHandler.CombineAndCreateDirectory(directory, name);
            var fileOrderLines = new List<string>(reader.FileCount);
            for (int i = 0; i < reader.FileCount; i++)
            {
                var fileDataInfo = await reader.ReadNextFileAsync();
                string outName = fileDataInfo.Path;
                if (string.IsNullOrWhiteSpace(outName))
                {
                    outName = $"{i}.{fileDataInfo.Type.ToLowerInvariant()}".TrimEnd('\0');
                    writeName = false;
                }
                else if (!writeName)
                {
                    throw new Exception("Some files had names while others didn't, please report this to the developer.");
                }

                string outputPath = PathHandler.CombineAndCreateDirectory(outputDirectory, outName);
                if (LZSS.Is(fileDataInfo.Bytes))
                {
                    outputPath += ".lzss";
                }

                await File.WriteAllBytesAsync(outputPath, fileDataInfo.Bytes);
                fileOrderLines.Add(outputPath);
            }

            // TODO: Make this whole thing better later.
            if (!writeName)
            {
                await File.WriteAllTextAsync(PathHandler.CombineAndCreateDirectory(outputDirectory, "nonames"), string.Empty);
            }

            await File.WriteAllLinesAsync(PathHandler.CombineAndCreateDirectory(outputDirectory, "fileorder.txt"), fileOrderLines);
        }

        static void RepackRAX(string directory)
        {
            string name = PathHandler.GetDirectoryNameWithoutPath(directory).Replace("-RAX", string.Empty);
            string outputPath = PathHandler.CombineAndCreateDirectory(PathHandler.GetDirectoryName(directory), name + ".RAX");
            if (File.Exists(outputPath))
            {
                string backupPath = outputPath + ".BAK";
                if (!File.Exists(backupPath))
                {
                    File.Move(outputPath, backupPath);
                }
            }

            string fileOrderPath = PathHandler.Combine(directory, "fileorder.txt");
            string noNamePath = PathHandler.Combine(directory, "noname");
            bool writeNames = File.Exists(noNamePath);

            if (File.Exists(fileOrderPath))
            {
                WriteFromPaths(outputPath, directory, File.ReadAllLines(fileOrderPath), writeNames);
            }
            else
            {
                WriteFromDirectory(outputPath, directory, writeNames);
            }
        }

        static void WriteFromDirectory(string outputPath, string directory, bool writeNames)
        {
            var writer = new RAXWriter(outputPath);
            writer.WritePaths = writeNames;
            var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
            int count = 0;

            // Get alignment size by checking the first file
            var enumerator = files.GetEnumerator();
            string firstFile = enumerator.Current;
            writer.Add(firstFile, PathHandler.GetRelativePathWithoutLeadingSlash(firstFile, directory));
            writer.AlignmentSize = LZSS.Is(firstFile) ? 4 : 0x10;
            enumerator.MoveNext();

            foreach (var file in files)
            {
                string archivePath = PathHandler.GetRelativePathWithoutLeadingSlash(file, directory);
                if (archivePath.EndsWith(".lzss"))
                {
                    archivePath = archivePath[..^5];
                }

                writer.Add(file, archivePath);
                count++;
            }

            writer.SetCapacity(count);
            writer.WriteAllFiles();
        }

        static void WriteFromPaths(string outputPath, string directory, IList<string> paths, bool writeNames)
        {
            int alignmentSize;
            if (paths.Count > 0)
            {
                alignmentSize = LZSS.Is(paths[0]) ? 4 : 0x10;
            }
            else
            {
                alignmentSize = 0x10;
            }

            RAXWriter writer = new RAXWriter(outputPath, paths.Count, alignmentSize);
            writer.WritePaths = writeNames;
            foreach (var path in paths)
            {
                string archivePath = PathHandler.GetRelativePathWithoutLeadingSlash(path, directory);
                if (archivePath.EndsWith(".lzss"))
                {
                    archivePath = archivePath[..^5];
                }

                writer.Add(path, archivePath);
            }

            writer.WriteAllFiles();
        }
    }
}
