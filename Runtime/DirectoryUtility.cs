using System;
using System.IO;

namespace DAZIxBREED.GitHubDownloader
{
    public static class DirectoryUtility
    {
        public static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
            }

            Directory.CreateDirectory(destinationDirectory);
            foreach (string directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = GetRelativePath(sourceDirectory, directory);
                Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
            }

            foreach (string file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = GetRelativePath(sourceDirectory, file);
                string destinationFile = Path.Combine(destinationDirectory, relative);
                string parent = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, destinationFile, overwrite);
            }
        }

        public static string GetRelativePath(string basePath, string fullPath)
        {
            string normalizedBase = Path.GetFullPath(basePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalizedFull = Path.GetFullPath(fullPath);
            var baseUri = new Uri(normalizedBase);
            var fullUri = new Uri(normalizedFull);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        public static void DeleteDirectoryRobust(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(path, true);
        }
    }
}
