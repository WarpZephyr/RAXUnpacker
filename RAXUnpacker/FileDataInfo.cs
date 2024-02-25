namespace RAXUnpacker
{
    /// <summary>
    /// A simple object for holding file path and bytes.
    /// </summary>
    /// <param name="Path">A path to the file, may not be a full path.</param>
    /// <param name="Bytes">The bytes of the file.</param>
    public record FileDataInfo(string Path, byte[] Bytes);
}
