namespace RAXUnpacker.Handlers
{
    internal static class PathHandler
    {
        internal static string GetDirectoryName(string path)
        {
            string? directory = Path.GetDirectoryName(path) ?? path;
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException($"Provided path did not contain directory information: {path}");
            }

            return directory;
        }

        internal static string CorrectDirectorySeparatorChar(string path) => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        internal static string TrimLeadingDirectorySeparators(string path) => path.TrimStart('\\', '/', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        internal static string TrimTrailingDirectorySeparators(string path) => path.TrimEnd('\\', '/', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        internal static string TrimDirectorySeparators(string path) => path.Trim('\\', '/', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        internal static string CleanPath(string path) => CorrectDirectorySeparatorChar(TrimLeadingDirectorySeparators(path));
        internal static string[] CleanPaths(string[] paths)
        {
            string[] cleanedPaths = new string[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                cleanedPaths[i] = CleanPath(paths[i]);
            }
            return cleanedPaths;
        }

        internal static string Combine(string path1, string path2) => Path.Combine(CleanPath(path1), CleanPath(path2));
        internal static string Combine(string path1, string path2, string path3) => Path.Combine(CleanPath(path1), CleanPath(path2), CleanPath(path3));
        internal static string Combine(string path1, string path2, string path3, string path4) => Path.Combine(CleanPath(path1), CleanPath(path2), CleanPath(path3), CleanPath(path4));
        internal static string Combine(params string[] paths) => Path.Combine(CleanPaths(paths));

        internal static string CombineAndCreateDirectory(string path1, string path2)
        {
            string result = Combine(path1, path2);
            Directory.CreateDirectory(GetDirectoryName(result));
            return result;
        }

        internal static string CombineAndCreateDirectory(string path1, string path2, string path3)
        {
            string result = Combine(path1, path2, path3);
            Directory.CreateDirectory(GetDirectoryName(result));
            return result;
        }

        internal static string CombineAndCreateDirectory(string path1, string path2, string path3, string path4)
        {
            string result = Combine(path1, path2, path3, path4);
            Directory.CreateDirectory(GetDirectoryName(result));
            return result;
        }

        internal static string CombineAndCreateDirectory(params string[] paths)
        {
            string result = Combine(paths);
            Directory.CreateDirectory(GetDirectoryName(result));
            return result;
        }

        internal static string GetExtensionWithoutExtensionDot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.GetExtension(path).TrimStart('.');
        }

        internal static string GetExtensions(string path)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            int index = path.IndexOf('.');
            if (index > -1)
            {
                return path[index..];
            }
            return string.Empty;
        }

        internal static string GetCorrectedExtensions(string path, char dotReplaceChar = '-')
            => GetExtensions(path).Replace('.', dotReplaceChar);

        internal static string GetWithoutExtensions(string path)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            int index = path.IndexOf('.');
            if (index > -1)
            {
                return path[..index];
            }
            return path;
        }

        internal static string GetFileNameWithoutExtensions(string path) => GetWithoutExtensions(Path.GetFileName(path));

        internal static string GetFileNameWithChangedExtensionChar(string path, char dotReplaceChar = '-')
        {
            string name = Path.GetFileName(path);
            return GetWithoutExtensions(name) + GetCorrectedExtensions(name, dotReplaceChar);
        }

        internal static string GetDirectoryNameWithoutPath(string path) => Path.GetFileName(TrimTrailingDirectorySeparators(path));

        internal static string GetRelativePath(string path, string relativeFrom)
        {
            if (string.IsNullOrEmpty(relativeFrom))
            {
                return path;
            }

            StringExceptionHandler.ThrowIfNotStartsWith(path, relativeFrom, nameof(path), nameof(relativeFrom));
            return path[relativeFrom.Length..];
        }

        internal static string GetRelativePathWithoutLeadingSlash(string path, string relativeFrom)
            => TrimLeadingDirectorySeparators(GetRelativePath(path, relativeFrom));
    }
}
