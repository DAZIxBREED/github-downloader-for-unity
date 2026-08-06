using System;

namespace DAZIxBREED.GitHubDownloader
{
    [Serializable]
    public sealed class GitHubArchiveSafetyLimits
    {
        public int MaxEntries = 100000;
        public long MaxUncompressedBytes = 4L * 1024L * 1024L * 1024L;
        public double MaxCompressionRatio = 1000d;

        public static GitHubArchiveSafetyLimits Default => new GitHubArchiveSafetyLimits();

        public GitHubArchiveSafetyLimits Clone()
        {
            return new GitHubArchiveSafetyLimits
            {
                MaxEntries = MaxEntries,
                MaxUncompressedBytes = MaxUncompressedBytes,
                MaxCompressionRatio = MaxCompressionRatio
            };
        }
    }
}
