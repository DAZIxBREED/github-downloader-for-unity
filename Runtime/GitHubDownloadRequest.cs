using System;

namespace DAZIxBREED.GitHubDownloader
{
    [Serializable]
    public sealed class GitHubDownloadRequest
    {
        public GitHubSourceKind SourceKind = GitHubSourceKind.RepositoryArchive;
        public GitHubRepositoryReference Repository;
        public string Reference = "main";
        public string ReleaseTag;
        public string ReleaseAssetPattern = "*.zip";
        public string DirectUrl;
        public string DestinationDirectory;
        public string OutputFileName;
        public bool ExtractArchive = true;
        public bool StripSingleRootDirectory = true;
        public bool UseCache = true;
        public bool Overwrite = true;
        public string ExpectedSha256;
        public string Token;
        public string ApiBaseUrl;
        public GitHubArchiveSafetyLimits ArchiveSafetyLimits = GitHubArchiveSafetyLimits.Default;

        public void Validate()
        {
            if (SourceKind == GitHubSourceKind.DirectUrl)
            {
                if (!GitHubUrlParser.IsHttpUrl(DirectUrl))
                {
                    throw new ArgumentException("DirectUrl must be an absolute HTTP or HTTPS URL.");
                }
            }
            else if (Repository == null || !Repository.IsValid)
            {
                throw new ArgumentException("A valid GitHub repository is required.");
            }

            if ((SourceKind == GitHubSourceKind.LatestReleaseAsset ||
                 SourceKind == GitHubSourceKind.TaggedReleaseAsset) &&
                string.IsNullOrWhiteSpace(ReleaseAssetPattern))
            {
                throw new ArgumentException("A release asset name or wildcard pattern is required.");
            }

            if (SourceKind == GitHubSourceKind.TaggedReleaseAsset &&
                string.IsNullOrWhiteSpace(ReleaseTag))
            {
                throw new ArgumentException("A release tag is required for tagged release downloads.");
            }

            if (string.IsNullOrWhiteSpace(DestinationDirectory))
            {
                throw new ArgumentException("DestinationDirectory is required.");
            }
        }
    }
}
