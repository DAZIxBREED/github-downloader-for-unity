namespace DAZIxBREED.GitHubDownloader
{
    public readonly struct GitHubDownloadProgress
    {
        public GitHubDownloadProgress(string stage, float progress, ulong bytesReceived = 0, string detail = null)
        {
            Stage = stage ?? string.Empty;
            Progress = progress < 0f ? 0f : progress > 1f ? 1f : progress;
            BytesReceived = bytesReceived;
            Detail = detail ?? string.Empty;
        }

        public string Stage { get; }
        public float Progress { get; }
        public ulong BytesReceived { get; }
        public string Detail { get; }
    }
}
