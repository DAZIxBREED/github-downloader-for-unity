using System;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    internal sealed class GitHubReleasePickerWindow : EditorWindow
    {
        private GitHubReleaseInfo[] releases = Array.Empty<GitHubReleaseInfo>();
        private Action<string, string> onSelected;
        private Vector2 scroll;
        private string filter = string.Empty;

        public static async void Open(
            GitHubRepositoryReference repository,
            string token,
            string apiBaseUrl,
            bool includePrereleases,
            Action<string, string> onSelected)
        {
            var window = CreateInstance<GitHubReleasePickerWindow>();
            window.titleContent = new GUIContent("GitHub Releases");
            window.minSize = new Vector2(560f, 360f);
            window.onSelected = onSelected;
            window.ShowUtility();

            try
            {
                var service = new GitHubDownloaderService();
                window.releases = await service.GetReleasesAsync(
                    repository,
                    token,
                    apiBaseUrl,
                    includePrereleases,
                    CancellationToken.None);
                window.Repaint();
            }
            catch (Exception exception)
            {
                window.Close();
                EditorUtility.DisplayDialog("GitHub Release Browser", exception.Message, "OK");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Select a release asset", EditorStyles.boldLabel);
            filter = EditorGUILayout.TextField("Filter", filter);
            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (GitHubReleaseInfo release in releases)
            {
                if (release == null)
                {
                    continue;
                }

                GitHubReleaseAssetInfo[] assets = release.Assets ?? Array.Empty<GitHubReleaseAssetInfo>();
                bool releaseMatches = string.IsNullOrWhiteSpace(filter) ||
                                      (release.TagName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                                      assets.Any(asset =>
                                          (asset.Name?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
                if (!releaseMatches)
                {
                    continue;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                string releaseTitle = string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name;
                EditorGUILayout.LabelField(releaseTitle, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"Tag: {release.TagName}" + (release.IsPrerelease ? "  •  Prerelease" : string.Empty),
                    EditorStyles.miniLabel);

                foreach (GitHubReleaseAssetInfo asset in assets)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(asset.Name, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField(FormatBytes(asset.Size), GUILayout.Width(88f));
                    if (GUILayout.Button("Select", GUILayout.Width(68f)))
                    {
                        onSelected?.Invoke(release.TagName, asset.Name);
                        Close();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (assets.Length == 0)
                {
                    EditorGUILayout.LabelField("No attached assets.", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private static string FormatBytes(long value)
        {
            if (value < 1024) return value + " B";
            if (value < 1024L * 1024L) return (value / 1024d).ToString("0.0") + " KB";
            if (value < 1024L * 1024L * 1024L) return (value / (1024d * 1024d)).ToString("0.0") + " MB";
            return (value / (1024d * 1024d * 1024d)).ToString("0.0") + " GB";
        }
    }
}
