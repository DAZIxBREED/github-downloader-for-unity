namespace DAZIxBREED.GitHubDownloader
{
    public sealed class GitHubDownloadResult
    {
        public bool Success { get; internal set; }
        public string DownloadedFilePath { get; internal set; }
        public string ExtractedDirectoryPath { get; internal set; }
        public string FileName { get; internal set; }
        public string Sha256 { get; internal set; }
        public ulong BytesDownloaded { get; internal set; }
        public bool FromCache { get; internal set; }
        public long HttpStatusCode { get; internal set; }
        public string SourceUrl { get; internal set; }
        public string ErrorMessage { get; internal set; }

        internal static GitHubDownloadResult Failure(string message)
        {
            return new GitHubDownloadResult
            {
                Success = false,
                ErrorMessage = message ?? "The download failed."
            };
        }
    }
}
