using System;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    [Serializable]
    internal sealed class GitHubPackageManifest
    {
        public string name;
        public string version;
        public string displayName;

        public static GitHubPackageManifest Load(string packageJsonPath)
        {
            string json = System.IO.File.ReadAllText(packageJsonPath);
            GitHubPackageManifest manifest = JsonUtility.FromJson<GitHubPackageManifest>(json);
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.name))
            {
                throw new InvalidOperationException("package.json does not contain a valid package name.");
            }

            return manifest;
        }
    }
}
