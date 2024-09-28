namespace RAXUnpacker
{
    /// <summary>
    /// An object for holding pathing information for a file in an archive.
    /// </summary>
    /// <param name="FullPath">The full path to the file on disk.</param>
    /// <param name="ArchivePath">The path in archives.</param>
    internal record FileArchivePathInfo(string FullPath, string ArchivePath);
}
