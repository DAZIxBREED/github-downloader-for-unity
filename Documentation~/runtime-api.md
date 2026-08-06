# Runtime API

The main entry point is `GitHubDownloaderService.ExecuteAsync`.

## Request

`GitHubDownloadRequest` selects a source, destination, extraction behavior, cache behavior, token, API base URL, expected SHA-256, and archive limits.

## Result

`GitHubDownloadResult` reports success, downloaded path, extracted path, source URL, SHA-256, byte count, cache use, HTTP status, and any error message.

## Releases

Use `GitHubDownloaderService.GetReleasesAsync` or `GitHubApiClient` to build custom release browsers.

## Cancellation

Pass a `CancellationToken`. Cancellation aborts active UnityWebRequest operations and returns a failed result with `Operation cancelled.`
