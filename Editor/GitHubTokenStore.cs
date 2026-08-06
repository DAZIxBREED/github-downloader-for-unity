using System;
using UnityEditor;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    internal static class GitHubTokenStore
    {
        public static string Load(string host)
        {
            return EditorPrefs.GetString(BuildKey(host), string.Empty);
        }

        public static void Save(string host, string token)
        {
            string key = BuildKey(host);
            if (string.IsNullOrWhiteSpace(token))
            {
                EditorPrefs.DeleteKey(key);
            }
            else
            {
                EditorPrefs.SetString(key, token.Trim());
            }
        }

        public static void Delete(string host)
        {
            EditorPrefs.DeleteKey(BuildKey(host));
        }

        private static string BuildKey(string host)
        {
            string project = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            string scope = project + "|" + (string.IsNullOrWhiteSpace(host) ? "github.com" : host.Trim());
            return "DAZIxBREED.GitHubDownloader.Token." + Sha256Utility.ComputeString(scope).Substring(0, 24);
        }
    }
}
