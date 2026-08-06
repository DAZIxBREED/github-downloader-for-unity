using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAZIxBREED.GitHubDownloader.Internal;

namespace DAZIxBREED.GitHubDownloader
{
    public sealed class GitHubDownloaderService
    {
        public async Task<GitHubDownloadResult> ExecuteAsync(
            GitHubDownloadRequest request,
            IProgress<GitHubDownloadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                request.Validate();
                Directory.CreateDirectory(request.DestinationDirectory);
                progress?.Report(new GitHubDownloadProgress("Resolving source", 0f));

                var api = new GitHubApiClient(request.Token, request.ApiBaseUrl);
                ResolvedDownload resolved = await ResolveDownloadAsync(request, api, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                var downloader = new HttpFileDownloader();
                HttpFileDownloadResult downloaded = await downloader.DownloadAsync(
                    resolved.Url,
                    request.DestinationDirectory,
                    string.IsNullOrWhiteSpace(request.OutputFileName) ? resolved.FileName : request.OutputFileName,
                    request.UseCache,
                    request.Overwrite,
                    resolved.Headers,
                    progress,
                    cancellationToken);

                progress?.Report(new GitHubDownloadProgress("Verifying SHA-256", 0f));
                string sha256 = await Sha256Utility.ComputeFileAsync(downloaded.FilePath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(request.ExpectedSha256) &&
                    !Sha256Utility.EqualsNormalized(request.ExpectedSha256, sha256))
                {
                    throw new InvalidDataException(
                        $"SHA-256 mismatch. Expected {request.ExpectedSha256}, received {sha256}.");
                }

                string extractedDirectory = null;
                if (request.ExtractArchive && IsZip(downloaded.FilePath, downloaded.FileName))
                {
                    extractedDirectory = GetExtractionDirectory(downloaded.FilePath);
                    await SafeZipExtractor.ExtractAsync(
                        downloaded.FilePath,
                        extractedDirectory,
                        request.StripSingleRootDirectory,
                        request.ArchiveSafetyLimits,
                        progress,
                        cancellationToken);
                }

                return new GitHubDownloadResult
                {
                    Success = true,
                    DownloadedFilePath = downloaded.FilePath,
                    ExtractedDirectoryPath = extractedDirectory,
                    FileName = downloaded.FileName,
                    Sha256 = sha256,
                    BytesDownloaded = downloaded.BytesDownloaded,
                    FromCache = downloaded.FromCache,
                    HttpStatusCode = downloaded.HttpStatusCode,
                    SourceUrl = resolved.Url
                };
            }
            catch (OperationCanceledException)
            {
                return GitHubDownloadResult.Failure("Operation cancelled.");
            }
            catch (Exception exception)
            {
                return GitHubDownloadResult.Failure(exception.Message);
            }
        }

        public async Task<GitHubReleaseInfo[]> GetReleasesAsync(
            GitHubRepositoryReference repository,
            string token = null,
            string apiBaseUrl = null,
            bool includePrereleases = true,
            CancellationToken cancellationToken = default)
        {
            var client = new GitHubApiClient(token, apiBaseUrl);
            return await client.GetReleasesAsync(repository, false, includePrereleases, cancellationToken);
        }

        private static async Task<ResolvedDownload> ResolveDownloadAsync(
            GitHubDownloadRequest request,
            GitHubApiClient api,
            CancellationToken cancellationToken)
        {
            switch (request.SourceKind)
            {
                case GitHubSourceKind.RepositoryArchive:
                    string reference = string.IsNullOrWhiteSpace(request.Reference)
                        ? request.Repository.Reference
                        : request.Reference;
                    return new ResolvedDownload
                    {
                        Url = api.BuildRepositoryArchiveUrl(request.Repository, reference),
                        FileName = $"{request.Repository.Name}-{SanitizeReference(reference)}.zip",
                        Headers = api.CreateHeaders(true)
                    };

                case GitHubSourceKind.LatestReleaseAsset:
                {
                    GitHubReleaseInfo release = await api.GetLatestReleaseAsync(request.Repository, cancellationToken);
                    return ResolveReleaseAsset(release, request.ReleaseAssetPattern, api);
                }

                case GitHubSourceKind.TaggedReleaseAsset:
                {
                    GitHubReleaseInfo release = await api.GetReleaseByTagAsync(
                        request.Repository,
                        request.ReleaseTag,
                        cancellationToken);
                    return ResolveReleaseAsset(release, request.ReleaseAssetPattern, api);
                }

                case GitHubSourceKind.DirectUrl:
                    return new ResolvedDownload
                    {
                        Url = request.DirectUrl,
                        FileName = null,
                        // Never forward a GitHub token to an arbitrary direct URL.
                        Headers = null
                    };

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static ResolvedDownload ResolveReleaseAsset(
            GitHubReleaseInfo release,
            string pattern,
            GitHubApiClient api)
        {
            if (release == null)
            {
                throw new InvalidOperationException("Release information was not returned.");
            }

            GitHubReleaseAssetInfo[] assets = release.Assets ?? Array.Empty<GitHubReleaseAssetInfo>();
            GitHubReleaseAssetInfo asset = assets.FirstOrDefault(item =>
                item != null && WildcardMatcher.IsMatch(item.Name, pattern));
            if (asset == null)
            {
                string available = assets.Length == 0
                    ? "No assets are attached to the release."
                    : "Available assets: " + string.Join(", ", assets.Select(item => item.Name));
                throw new FileNotFoundException(
                    $"No release asset matched '{pattern}' for release '{release.TagName}'. {available}");
            }

            bool authenticated = !string.IsNullOrWhiteSpace(api.Token);
            return new ResolvedDownload
            {
                Url = authenticated && !string.IsNullOrWhiteSpace(asset.ApiUrl)
                    ? asset.ApiUrl
                    : asset.BrowserDownloadUrl,
                FileName = asset.Name,
                Headers = authenticated ? api.CreateHeaders(true) : null
            };
        }

        private static bool IsZip(string path, string fileName)
        {
            string candidate = string.IsNullOrWhiteSpace(fileName) ? path : fileName;
            return candidate.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetExtractionDirectory(string archivePath)
        {
            string directory = Path.GetDirectoryName(archivePath) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(archivePath);
            return Path.Combine(directory, fileName + "-extracted");
        }

        private static string SanitizeReference(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "HEAD" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalid, '-');
            }

            return result.Replace('/', '-').Replace('\\', '-');
        }

        private sealed class ResolvedDownload
        {
            public string Url;
            public string FileName;
            public IReadOnlyDictionary<string, string> Headers;
        }
    }
}
