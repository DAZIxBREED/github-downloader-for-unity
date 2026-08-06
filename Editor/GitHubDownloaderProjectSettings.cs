using System;
using UnityEditor;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    [FilePath("ProjectSettings/GitHubDownloaderSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class GitHubDownloaderProjectSettings : ScriptableSingleton<GitHubDownloaderProjectSettings>
    {
        [SerializeField] private string cacheDirectory = "Library/GitHubDownloader/Cache";
        [SerializeField] private string apiBaseUrl = string.Empty;
        [SerializeField] private bool includePrereleases = true;
        [SerializeField] private bool keepBackups;
        [SerializeField] private int maxArchiveEntries = 100000;
        [SerializeField] private long maxExpandedArchiveBytes = 4L * 1024L * 1024L * 1024L;
        [SerializeField] private double maxCompressionRatio = 1000d;

        public string CacheDirectory
        {
            get => cacheDirectory;
            set => cacheDirectory = value;
        }

        public string ApiBaseUrl
        {
            get => apiBaseUrl;
            set => apiBaseUrl = value;
        }

        public bool IncludePrereleases
        {
            get => includePrereleases;
            set => includePrereleases = value;
        }

        public bool KeepBackups
        {
            get => keepBackups;
            set => keepBackups = value;
        }

        public int MaxArchiveEntries
        {
            get => maxArchiveEntries;
            set => maxArchiveEntries = Math.Max(1, value);
        }

        public long MaxExpandedArchiveBytes
        {
            get => maxExpandedArchiveBytes;
            set => maxExpandedArchiveBytes = Math.Max(1024L * 1024L, value);
        }

        public double MaxCompressionRatio
        {
            get => maxCompressionRatio;
            set => maxCompressionRatio = Math.Max(1d, value);
        }

        public string GetAbsoluteCacheDirectory()
        {
            string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            return System.IO.Path.IsPathRooted(cacheDirectory)
                ? System.IO.Path.GetFullPath(cacheDirectory)
                : System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, cacheDirectory));
        }

        public GitHubArchiveSafetyLimits CreateSafetyLimits()
        {
            return new GitHubArchiveSafetyLimits
            {
                MaxEntries = MaxArchiveEntries,
                MaxUncompressedBytes = MaxExpandedArchiveBytes,
                MaxCompressionRatio = MaxCompressionRatio
            };
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }
}
