using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DAZIxBREED.GitHubDownloader
{
    public static class SafeZipExtractor
    {
        private const int UnixFileTypeMask = 0xF000;
        private const int UnixSymbolicLink = 0xA000;

        public static async Task ExtractAsync(
            string archivePath,
            string destinationDirectory,
            bool stripSingleRootDirectory,
            GitHubArchiveSafetyLimits limits,
            IProgress<GitHubDownloadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                throw new FileNotFoundException("ZIP archive not found.", archivePath);
            }

            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new ArgumentException("A destination directory is required.", nameof(destinationDirectory));
            }

            limits ??= GitHubArchiveSafetyLimits.Default;
            string fullDestination = Path.GetFullPath(destinationDirectory);
            string stagingDirectory = fullDestination + ".extracting-" + Guid.NewGuid().ToString("N");

            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, true);
            }

            Directory.CreateDirectory(stagingDirectory);
            try
            {
                await Task.Run(() => ExtractInternal(
                    archivePath,
                    stagingDirectory,
                    limits,
                    progress,
                    cancellationToken), cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                if (Directory.Exists(fullDestination))
                {
                    Directory.Delete(fullDestination, true);
                }

                if (stripSingleRootDirectory && TryGetSingleRootDirectory(stagingDirectory, out string rootDirectory))
                {
                    Directory.Move(rootDirectory, fullDestination);
                    Directory.Delete(stagingDirectory, true);
                }
                else
                {
                    Directory.Move(stagingDirectory, fullDestination);
                }

                progress?.Report(new GitHubDownloadProgress("Extracting", 1f, 0, fullDestination));
            }
            catch
            {
                TryDeleteDirectory(stagingDirectory);
                throw;
            }
        }

        private static void ExtractInternal(
            string archivePath,
            string stagingDirectory,
            GitHubArchiveSafetyLimits limits,
            IProgress<GitHubDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                if (archive.Entries.Count > limits.MaxEntries)
                {
                    throw new InvalidDataException(
                        $"Archive contains {archive.Entries.Count} entries; the configured limit is {limits.MaxEntries}.");
                }

                long totalExpandedBytes = 0;
                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ZipArchiveEntry entry = archive.Entries[i];
                    ValidateEntry(entry, limits, ref totalExpandedBytes);
                    ExtractEntry(entry, stagingDirectory, cancellationToken);

                    float amount = archive.Entries.Count == 0
                        ? 1f
                        : (i + 1f) / archive.Entries.Count;
                    progress?.Report(new GitHubDownloadProgress("Extracting", amount, 0, entry.FullName));
                }
            }
        }

        private static void ValidateEntry(
            ZipArchiveEntry entry,
            GitHubArchiveSafetyLimits limits,
            ref long totalExpandedBytes)
        {
            int unixMode = (entry.ExternalAttributes >> 16) & 0xFFFF;
            if ((unixMode & UnixFileTypeMask) == UnixSymbolicLink)
            {
                throw new InvalidDataException($"Archive contains a symbolic link: {entry.FullName}");
            }

            if (entry.Length < 0)
            {
                throw new InvalidDataException($"Archive entry has an invalid length: {entry.FullName}");
            }

            checked
            {
                totalExpandedBytes += entry.Length;
            }

            if (totalExpandedBytes > limits.MaxUncompressedBytes)
            {
                throw new InvalidDataException(
                    $"Archive expands beyond the configured {limits.MaxUncompressedBytes} byte limit.");
            }

            if (entry.CompressedLength > 0 && entry.Length > 0)
            {
                double ratio = (double)entry.Length / entry.CompressedLength;
                if (ratio > limits.MaxCompressionRatio)
                {
                    throw new InvalidDataException(
                        $"Archive entry '{entry.FullName}' exceeds the allowed compression ratio.");
                }
            }
        }

        private static void ExtractEntry(
            ZipArchiveEntry entry,
            string stagingDirectory,
            CancellationToken cancellationToken)
        {
            string relativePath = entry.FullName.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            if (relativePath.StartsWith("/", StringComparison.Ordinal) ||
                relativePath.StartsWith("\\", StringComparison.Ordinal) ||
                Path.IsPathRooted(relativePath))
            {
                throw new InvalidDataException($"Archive contains an absolute path: {entry.FullName}");
            }

            string destinationPath = Path.GetFullPath(Path.Combine(stagingDirectory, relativePath));
            string root = Path.GetFullPath(stagingDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            StringComparison comparison = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!destinationPath.StartsWith(root, comparison))
            {
                throw new InvalidDataException($"Archive path escapes the destination: {entry.FullName}");
            }

            bool isDirectory = relativePath.EndsWith("/", StringComparison.Ordinal) ||
                               string.IsNullOrEmpty(entry.Name);
            if (isDirectory)
            {
                Directory.CreateDirectory(destinationPath);
                return;
            }

            string parent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            using (Stream source = entry.Open())
            using (var destination = new FileStream(
                       destinationPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       1024 * 128,
                       FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[1024 * 128];
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    destination.Write(buffer, 0, read);
                }
            }

            if (entry.LastWriteTime.Year >= 1980)
            {
                File.SetLastWriteTimeUtc(destinationPath, entry.LastWriteTime.UtcDateTime);
            }
        }

        private static bool TryGetSingleRootDirectory(string directory, out string rootDirectory)
        {
            rootDirectory = null;
            string[] directories = Directory.GetDirectories(directory);
            string[] files = Directory.GetFiles(directory);
            if (directories.Length != 1 || files.Length != 0)
            {
                return false;
            }

            rootDirectory = directories[0];
            return true;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                // Cleanup is best effort. The original exception remains authoritative.
            }
        }
    }
}
