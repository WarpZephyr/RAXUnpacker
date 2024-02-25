using RAXUnpacker.Handlers;

namespace RAXUnpacker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
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
                }

                await Task.WhenAll(unpackTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred:\n{ex.Message}\n{ex.StackTrace}");
                Console.ReadKey();
            }
        }

        static async Task UnpackRAXAsync(string path, RAXReader reader)
        {
            string directory = PathHandler.GetDirectoryName(path);
            string name = PathHandler.GetFileNameWithCorrectedExtensions(path);
            for (int i = 0; i < reader.FileCount; i++)
            {
                var fileDataInfo = await reader.ReadNextFileAsync();
                await File.WriteAllBytesAsync(PathHandler.CombineAndCreateDirectory(directory, name, fileDataInfo.Path), fileDataInfo.Bytes);
            }
        }
    }
}
