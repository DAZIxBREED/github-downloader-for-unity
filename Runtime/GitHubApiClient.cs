using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAZIxBREED.GitHubDownloader.Internal;
using UnityEngine;
using UnityEngine.Networking;

namespace DAZIxBREED.GitHubDownloader
{
    public sealed class GitHubApiClient
    {
        public const string DefaultUserAgent = "DAZIxBREED-GitHub-Downloader-for-Unity/1.0.0";

        public GitHubApiClient(string token = null, string apiBaseUrl = null)
        {
            Token = token;
            ApiBaseUrl = apiBaseUrl;
        }

        public string Token { get; set; }
        public string ApiBaseUrl { get; set; }
        public string UserAgent { get; set; } = DefaultUserAgent;

        public async Task<GitHubReleaseInfo[]> GetReleasesAsync(
            GitHubRepositoryReference repository,
            bool includeDrafts = false,
            bool includePrereleases = true,
            CancellationToken cancellationToken = default)
        {
            ValidateRepository(repository);
            string url = $"{GetApiBase(repository)}/repos/{Escape(repository.Owner)}/{Escape(repository.Name)}/releases?per_page=100";
            string json = await GetJsonAsync(url, cancellationToken);
            ReleaseDto[] dtos = JsonArrayUtility.FromJsonArray<ReleaseDto>(json);
            return dtos
                .Where(item => item != null)
                .Select(item => item.ToPublic())
                .Where(item => (includeDrafts || !item.IsDraft) && (includePrereleases || !item.IsPrerelease))
                .ToArray();
        }

        public async Task<GitHubReleaseInfo> GetLatestReleaseAsync(
            GitHubRepositoryReference repository,
            CancellationToken cancellationToken = default)
        {
            ValidateRepository(repository);
            string url = $"{GetApiBase(repository)}/repos/{Escape(repository.Owner)}/{Escape(repository.Name)}/releases/latest";
            string json = await GetJsonAsync(url, cancellationToken);
            ReleaseDto dto = JsonUtility.FromJson<ReleaseDto>(json);
            if (dto == null)
            {
                throw new InvalidOperationException("GitHub returned an empty latest release response.");
            }

            return dto.ToPublic();
        }

        public async Task<GitHubReleaseInfo> GetReleaseByTagAsync(
            GitHubRepositoryReference repository,
            string tag,
            CancellationToken cancellationToken = default)
        {
            ValidateRepository(repository);
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("A release tag is required.", nameof(tag));
            }

            string url = $"{GetApiBase(repository)}/repos/{Escape(repository.Owner)}/{Escape(repository.Name)}/releases/tags/{EscapePathSegment(tag)}";
            string json = await GetJsonAsync(url, cancellationToken);
            ReleaseDto dto = JsonUtility.FromJson<ReleaseDto>(json);
            if (dto == null)
            {
                throw new InvalidOperationException($"GitHub returned an empty response for release '{tag}'.");
            }

            return dto.ToPublic();
        }

        public string BuildRepositoryArchiveUrl(GitHubRepositoryReference repository, string reference)
        {
            ValidateRepository(repository);
            string resolvedReference = string.IsNullOrWhiteSpace(reference)
                ? "HEAD"
                : reference.Trim();
            return $"{GetApiBase(repository)}/repos/{Escape(repository.Owner)}/{Escape(repository.Name)}/zipball/{EscapePathSegment(resolvedReference)}";
        }

        public Dictionary<string, string> CreateHeaders(bool binary = false)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Accept"] = binary ? "application/octet-stream" : "application/vnd.github+json",
                ["X-GitHub-Api-Version"] = "2022-11-28",
                ["User-Agent"] = string.IsNullOrWhiteSpace(UserAgent) ? DefaultUserAgent : UserAgent
            };

            if (!string.IsNullOrWhiteSpace(Token))
            {
                headers["Authorization"] = "Bearer " + Token.Trim();
            }

            return headers;
        }

        public string GetApiBase(GitHubRepositoryReference repository)
        {
            if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                return ApiBaseUrl.Trim().TrimEnd('/');
            }

            return repository.IsGitHubDotCom
                ? "https://api.github.com"
                : $"https://{repository.Host}/api/v3";
        }

        private async Task<string> GetJsonAsync(string url, CancellationToken cancellationToken)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.redirectLimit = 8;
                foreach (KeyValuePair<string, string> header in CreateHeaders(false))
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string body = request.downloadHandler?.text;
                    throw new GitHubHttpException(
                        request.responseCode,
                        string.IsNullOrWhiteSpace(body) ? request.error : body,
                        url);
                }

                return request.downloadHandler.text;
            }
        }

        private static void ValidateRepository(GitHubRepositoryReference repository)
        {
            if (repository == null || !repository.IsValid)
            {
                throw new ArgumentException("A valid GitHub repository is required.", nameof(repository));
            }
        }

        private static string Escape(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty);
        }

        private static string EscapePathSegment(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty).Replace("%2F", "/");
        }
    }

    public sealed class GitHubHttpException : Exception
    {
        public GitHubHttpException(long statusCode, string message, string url)
            : base($"GitHub request failed with HTTP {statusCode}: {message}")
        {
            StatusCode = statusCode;
            Url = url;
        }

        public long StatusCode { get; }
        public string Url { get; }
    }
}
