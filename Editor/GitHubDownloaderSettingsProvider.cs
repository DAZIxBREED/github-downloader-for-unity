using UnityEditor;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    internal static class GitHubDownloaderSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/DAZIxBREED/GitHub Downloader", SettingsScope.Project)
            {
                label = "GitHub Downloader",
                guiHandler = DrawSettings,
                keywords = new[] { "GitHub", "Downloader", "UPM", "Token", "Cache", "ZIP" }
            };
        }

        private static void DrawSettings(string searchContext)
        {
            GitHubDownloaderProjectSettings settings = GitHubDownloaderProjectSettings.instance;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Download Cache", EditorStyles.boldLabel);
            settings.CacheDirectory = EditorGUILayout.TextField("Cache Directory", settings.CacheDirectory);
            EditorGUILayout.HelpBox(
                "Relative paths are resolved from the Unity project root. Cache metadata contains URLs and validators, never tokens.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GitHub API", EditorStyles.boldLabel);
            settings.ApiBaseUrl = EditorGUILayout.TextField("API Base Override", settings.ApiBaseUrl);
            settings.IncludePrereleases = EditorGUILayout.Toggle("Include Prereleases", settings.IncludePrereleases);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Archive Safety", EditorStyles.boldLabel);
            settings.MaxArchiveEntries = EditorGUILayout.IntField("Maximum Entries", settings.MaxArchiveEntries);
            settings.MaxExpandedArchiveBytes = EditorGUILayout.LongField(
                "Maximum Expanded Bytes",
                settings.MaxExpandedArchiveBytes);
            settings.MaxCompressionRatio = EditorGUILayout.DoubleField(
                "Maximum Compression Ratio",
                settings.MaxCompressionRatio);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Installation", EditorStyles.boldLabel);
            settings.KeepBackups = EditorGUILayout.Toggle("Keep Replaced Backups", settings.KeepBackups);

            if (EditorGUI.EndChangeCheck())
            {
                settings.SaveSettings();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Reveal Cache Directory"))
            {
                string cache = settings.GetAbsoluteCacheDirectory();
                System.IO.Directory.CreateDirectory(cache);
                EditorUtility.RevealInFinder(cache);
            }
        }
    }
}
