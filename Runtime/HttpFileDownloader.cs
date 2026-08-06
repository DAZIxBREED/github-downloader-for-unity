using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DAZIxBREED.GitHubDownloader.Internal;
using UnityEngine;
using UnityEngine.Networking;

namespace DAZIxBREED.GitHubDownloader
{
    internal sealed class HttpFileDownloadResult
    {
        public string FilePath;
        public string FileName;
        public ulong BytesDownloaded;
        public bool FromCache;
        public long HttpStatusCode;
        public string ETag;
        public string LastModified;
    }

    internal sealed class HttpFileDownloader
    {
        public async Task<HttpFileDownloadResult> DownloadAsync(
            string url,
            string destinationDirectory,
            string preferredFileName,
            bool useCache,
            bool overwrite,
            IReadOnlyDictionary<string, string> headers,
            IProgress<GitHubDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            if (!GitHubUrlParser.IsHttpUrl(url))
            {
                throw new ArgumentException("A valid HTTP or HTTPS URL is required.", nameof(url));
            }

            Directory.CreateDirectory(destinationDirectory);
            string safeFileName = SanitizeFileName(
                string.IsNullOrWhiteSpace(preferredFileName)
                    ? InferFileName(url)
                    : preferredFileName);
            string destinationPath = Path.Combine(destinationDirectory, safeFileName);
            string metadataPath = destinationPath + ".github-downloader.json";
            DownloadCacheMetadata metadata = useCache ? LoadMetadata(metadataPath) : null;
            if (metadata != null && !string.Equals(metadata.url, url, StringComparison.Ordinal))
            {
                metadata = null;
            }
            string temporaryPath = destinationPath + ".partial-" + Guid.NewGuid().ToString("N");

            using (var request = new UnityWebRequest(url, "GET"))
            {
                request.downloadHandler = new DownloadHandlerFile(temporaryPath, true);
                request.redirectLimit = 8;
                request.disposeDownloadHandlerOnDispose = true;

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        if (!string.IsNullOrWhiteSpace(header.Key) && header.Value != null)
                        {
                            request.SetRequestHeader(header.Key, header.Value);
                        }
                    }
                }

                if (useCache && metadata != null && File.Exists(destinationPath))
                {
                    if (!string.IsNullOrWhiteSpace(metadata.etag))
                    {
                        request.SetRequestHeader("If-None-Match", metadata.etag);
                    }

                    if (!string.IsNullOrWhiteSpace(metadata.lastModified))
                    {
                        request.SetRequestHeader("If-Modified-Since", metadata.lastModified);
                    }
                }

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        TryDelete(temporaryPath);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    progress?.Report(new GitHubDownloadProgress(
                        "Downloading",
                        request.downloadProgress < 0f ? 0f : request.downloadProgress,
                        request.downloadedBytes,
                        safeFileName));
                    await Task.Yield();
                }

                if (request.responseCode == 304 && useCache && File.Exists(destinationPath))
                {
                    TryDelete(temporaryPath);
                    progress?.Report(new GitHubDownloadProgress("Downloading", 1f, 0, "Using validated cache"));
                    return new HttpFileDownloadResult
                    {
                        FilePath = destinationPath,
                        FileName = safeFileName,
                        BytesDownloaded = (ulong)new FileInfo(destinationPath).Length,
                        FromCache = true,
                        HttpStatusCode = 304,
                        ETag = metadata?.etag,
                        LastModified = metadata?.lastModified
                    };
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    TryDelete(temporaryPath);
                    throw new GitHubHttpException(request.responseCode, request.error, url);
                }

                if (File.Exists(destinationPath))
                {
                    if (!overwrite && !useCache)
                    {
                        TryDelete(temporaryPath);
                        throw new IOException($"Destination file already exists: {destinationPath}");
                    }

                    File.Delete(destinationPath);
                }

                File.Move(temporaryPath, destinationPath);
                string etag = request.GetResponseHeader("ETag");
                string lastModified = request.GetResponseHeader("Last-Modified");
                ulong length = (ulong)new FileInfo(destinationPath).Length;
                if (useCache)
                {
                    SaveMetadata(metadataPath, new DownloadCacheMetadata
                    {
                        url = url,
                        etag = etag,
                        lastModified = lastModified,
                        fileName = safeFileName,
                        length = (long)length,
                        updatedUtc = DateTime.UtcNow.ToString("O")
                    });
                }

                progress?.Report(new GitHubDownloadProgress("Downloading", 1f, length, safeFileName));
                return new HttpFileDownloadResult
                {
                    FilePath = destinationPath,
                    FileName = safeFileName,
                    BytesDownloaded = length,
                    FromCache = false,
                    HttpStatusCode = request.responseCode,
                    ETag = etag,
                    LastModified = lastModified
                };
            }
        }

        private static DownloadCacheMetadata LoadMetadata(string path)
        {
            try
            {
                return File.Exists(path)
                    ? JsonUtility.FromJson<DownloadCacheMetadata>(File.ReadAllText(path))
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static void SaveMetadata(string path, DownloadCacheMetadata metadata)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(metadata, true));
            }
            catch
            {
                // A cache metadata failure must not fail a successful download.
            }
        }

        private static string InferFileName(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                string file = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
                if (!string.IsNullOrWhiteSpace(file) && file.IndexOf('.') >= 0)
                {
                    return file;
                }
            }

            return "github-download.bin";
        }

        private static string SanitizeFileName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "github-download.bin" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(result) ? "github-download.bin" : result;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Cleanup is best effort.
            }
        }
    }
}
