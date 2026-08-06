using System;
using System.IO;

namespace DAZIxBREED.GitHubDownloader
{
    public enum DirectoryInstallConflictMode
    {
        Fail = 0,
        Replace = 1,
        Merge = 2
    }

    public sealed class TransactionalDirectoryInstallResult
    {
        public string InstalledPath { get; internal set; }
        public string BackupPath { get; internal set; }
        public bool ReplacedExistingDirectory { get; internal set; }
    }

    public static class TransactionalDirectoryInstaller
    {
        public static TransactionalDirectoryInstallResult Install(
            string sourceDirectory,
            string destinationDirectory,
            DirectoryInstallConflictMode conflictMode,
            bool keepBackup = false)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"Install source not found: {sourceDirectory}");
            }

            string destination = Path.GetFullPath(destinationDirectory);
            string parent = Path.GetDirectoryName(destination);
            if (string.IsNullOrWhiteSpace(parent))
            {
                throw new InvalidOperationException("The destination must have a parent directory.");
            }

            Directory.CreateDirectory(parent);
            string operationId = Guid.NewGuid().ToString("N");
            string staging = destination + ".staging-" + operationId;
            string backup = destination + ".backup-" + operationId;
            bool destinationExisted = Directory.Exists(destination);

            if (destinationExisted && conflictMode == DirectoryInstallConflictMode.Fail)
            {
                throw new IOException($"Destination already exists: {destination}");
            }

            try
            {
                if (conflictMode == DirectoryInstallConflictMode.Merge && destinationExisted)
                {
                    DirectoryUtility.CopyDirectory(destination, staging, true);
                    DirectoryUtility.CopyDirectory(sourceDirectory, staging, true);
                }
                else
                {
                    DirectoryUtility.CopyDirectory(sourceDirectory, staging, true);
                }

                if (destinationExisted)
                {
                    Directory.Move(destination, backup);
                }

                Directory.Move(staging, destination);

                string retainedBackup = backup;
                if (destinationExisted && !keepBackup)
                {
                    try
                    {
                        DirectoryUtility.DeleteDirectoryRobust(backup);
                        retainedBackup = null;
                    }
                    catch
                    {
                        // Installation succeeded. Preserve the backup path for manual cleanup.
                    }
                }

                return new TransactionalDirectoryInstallResult
                {
                    InstalledPath = destination,
                    BackupPath = retainedBackup,
                    ReplacedExistingDirectory = destinationExisted
                };
            }
            catch
            {
                TryDelete(staging);
                if (!Directory.Exists(destination) && Directory.Exists(backup))
                {
                    Directory.Move(backup, destination);
                }

                throw;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                DirectoryUtility.DeleteDirectoryRobust(path);
            }
            catch
            {
                // Cleanup is best effort.
            }
        }
    }
}
