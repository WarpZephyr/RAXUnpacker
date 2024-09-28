namespace RAXUnpacker
{
    /// <summary>
    /// A simple object for holding file path and bytes.
    /// </summary>
    /// <param name="Type">The type of file.</param>
    /// <param name="Path">A path to the file, may not be a full path, and may be entirely empty.</param>
    /// <param name="Bytes">The bytes of the file.</param>
    public record FileDataInfo(string Type, string Path, byte[] Bytes);
}
