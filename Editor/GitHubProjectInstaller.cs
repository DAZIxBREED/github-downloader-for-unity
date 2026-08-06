using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    internal static class GitHubProjectInstaller
    {
        public static string InstallDirectory(
            string sourceDirectory,
            GitHubInstallMode mode,
            string targetPath,
            string packageSubfolder,
            DirectoryInstallConflictMode conflictMode)
        {
            string source = ResolveSourceDirectory(sourceDirectory, packageSubfolder);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string destination;

            switch (mode)
            {
                case GitHubInstallMode.AssetsFolder:
                {
                    string relative = NormalizeRelativeProjectPath(
                        string.IsNullOrWhiteSpace(targetPath) ? "Assets/GitHubDownload" : targetPath,
                        "Assets");
                    destination = Path.Combine(projectRoot, relative);
                    break;
                }
                case GitHubInstallMode.EmbeddedPackage:
                {
                    string packageJson = Path.Combine(source, "package.json");
                    if (!File.Exists(packageJson))
                    {
                        throw new FileNotFoundException(
                            "Embedded package installation requires package.json at the selected source root. Use Package Subfolder when the package is nested.",
                            packageJson);
                    }

                    GitHubPackageManifest manifest = GitHubPackageManifest.Load(packageJson);
                    destination = Path.Combine(projectRoot, "Packages", manifest.name);
                    break;
                }
                case GitHubInstallMode.CustomDirectory:
                    if (string.IsNullOrWhiteSpace(targetPath))
                    {
                        throw new ArgumentException("A custom destination directory is required.");
                    }
                    destination = Path.GetFullPath(targetPath);
                    break;
                default:
                    throw new InvalidOperationException($"Install mode {mode} does not accept a directory source.");
            }

            TransactionalDirectoryInstallResult result = TransactionalDirectoryInstaller.Install(
                source,
                destination,
                conflictMode,
                GitHubDownloaderProjectSettings.instance.KeepBackups);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return result.InstalledPath;
        }

        public static string SaveDownloadedFile(string downloadedFilePath)
        {
            string extension = Path.GetExtension(downloadedFilePath).TrimStart('.');
            string chosen = EditorUtility.SaveFilePanel(
                "Save GitHub Download",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Path.GetFileName(downloadedFilePath),
                extension);
            if (string.IsNullOrWhiteSpace(chosen))
            {
                return null;
            }

            File.Copy(downloadedFilePath, chosen, true);
            return chosen;
        }

        public static void ImportUnityPackage(string downloadedFilePath, bool interactive)
        {
            if (!downloadedFilePath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The selected release asset is not a .unitypackage file.");
            }

            AssetDatabase.ImportPackage(downloadedFilePath, interactive);
        }

        private static string ResolveSourceDirectory(string sourceDirectory, string packageSubfolder)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException("The download did not produce an extracted directory.");
            }

            string source = Path.GetFullPath(sourceDirectory);
            if (string.IsNullOrWhiteSpace(packageSubfolder))
            {
                return source;
            }

            string relative = packageSubfolder.Trim().Replace('\\', '/').Trim('/');
            string resolved = Path.GetFullPath(Path.Combine(source, relative));
            string sourceRoot = source.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            StringComparison comparison = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!resolved.StartsWith(sourceRoot, comparison) || !Directory.Exists(resolved))
            {
                throw new DirectoryNotFoundException(
                    $"Package subfolder '{packageSubfolder}' was not found inside the extracted download.");
            }

            return resolved;
        }

        private static string NormalizeRelativeProjectPath(string value, string requiredRoot)
        {
            string normalized = value.Trim().Replace('\\', '/').Trim('/');
            if (!normalized.Equals(requiredRoot, StringComparison.OrdinalIgnoreCase) &&
                !normalized.StartsWith(requiredRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = requiredRoot + "/" + normalized;
            }

            if (normalized.IndexOf("../", StringComparison.Ordinal) >= 0 ||
                normalized.EndsWith("/..", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Project-relative install paths may not contain '..'.");
            }

            return normalized;
        }
    }
}
