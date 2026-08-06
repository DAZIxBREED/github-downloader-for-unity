using System;
using System.Threading;
using DAZIxBREED.GitHubDownloader;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Samples
{
    public sealed class RuntimeDownloadExample : MonoBehaviour
    {
        [SerializeField] private string repositoryUrl = "https://github.com/owner/repository";
        [SerializeField] private string reference = "main";
        [SerializeField] private bool downloadOnStart;

        private CancellationTokenSource cancellation;

        private void Start()
        {
            if (downloadOnStart)
            {
                Download();
            }
        }

        [ContextMenu("Download Repository")]
        public async void Download()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();

            try
            {
                GitHubRepositoryReference repository = GitHubUrlParser.ParseRepository(repositoryUrl);
                var request = new GitHubDownloadRequest
                {
                    SourceKind = GitHubSourceKind.RepositoryArchive,
                    Repository = repository,
                    Reference = reference,
                    DestinationDirectory = Application.persistentDataPath,
                    ExtractArchive = true,
                    StripSingleRootDirectory = true,
                    UseCache = true
                };

                var service = new GitHubDownloaderService();
                GitHubDownloadResult result = await service.ExecuteAsync(
                    request,
                    new Progress<GitHubDownloadProgress>(value =>
                        Debug.Log($"{value.Stage}: {value.Progress:P0} {value.Detail}")),
                    cancellation.Token);

                if (!result.Success)
                {
                    Debug.LogError(result.ErrorMessage);
                    return;
                }

                Debug.Log($"Downloaded to {result.DownloadedFilePath}\nExtracted to {result.ExtractedDirectoryPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnDestroy()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
        }
    }
}
