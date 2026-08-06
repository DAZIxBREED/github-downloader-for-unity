using System;

namespace DAZIxBREED.GitHubDownloader.Internal
{
    [Serializable]
    internal sealed class ReleaseDto
    {
        public long id;
        public string name;
        public string tag_name;
        public string body;
        public string html_url;
        public string published_at;
        public bool draft;
        public bool prerelease;
        public AssetDto[] assets;

        public GitHubReleaseInfo ToPublic()
        {
            AssetDto[] sourceAssets = assets ?? Array.Empty<AssetDto>();
            var converted = new GitHubReleaseAssetInfo[sourceAssets.Length];
            for (int i = 0; i < sourceAssets.Length; i++)
            {
                converted[i] = sourceAssets[i].ToPublic();
            }

            return new GitHubReleaseInfo
            {
                Id = id,
                Name = name,
                TagName = tag_name,
                Body = body,
                HtmlUrl = html_url,
                PublishedAt = published_at,
                IsDraft = draft,
                IsPrerelease = prerelease,
                Assets = converted
            };
        }
    }

    [Serializable]
    internal sealed class AssetDto
    {
        public long id;
        public string name;
        public string url;
        public string browser_download_url;
        public string content_type;
        public long size;
        public string updated_at;

        public GitHubReleaseAssetInfo ToPublic()
        {
            return new GitHubReleaseAssetInfo
            {
                Id = id,
                Name = name,
                ApiUrl = url,
                BrowserDownloadUrl = browser_download_url,
                ContentType = content_type,
                Size = size,
                UpdatedAt = updated_at
            };
        }
    }

    [Serializable]
    internal sealed class ReleaseArrayWrapper
    {
        public ReleaseDto[] items;
    }

    [Serializable]
    internal sealed class DownloadCacheMetadata
    {
        public string url;
        public string etag;
        public string lastModified;
        public string fileName;
        public string sha256;
        public long length;
        public string updatedUtc;
    }
}
