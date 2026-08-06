using System;

namespace DAZIxBREED.GitHubDownloader
{
    [Serializable]
    public sealed class GitHubReleaseInfo
    {
        public long Id;
        public string Name;
        public string TagName;
        public string Body;
        public string HtmlUrl;
        public string PublishedAt;
        public bool IsDraft;
        public bool IsPrerelease;
        public GitHubReleaseAssetInfo[] Assets = Array.Empty<GitHubReleaseAssetInfo>();
    }

    [Serializable]
    public sealed class GitHubReleaseAssetInfo
    {
        public long Id;
        public string Name;
        public string ApiUrl;
        public string BrowserDownloadUrl;
        public string ContentType;
        public long Size;
        public string UpdatedAt;
    }
}
