# GitHub Downloader for Unity

**GitHub Downloader for Unity** is a production-oriented Unity package for downloading and installing content from GitHub. It supports repository snapshots, branches, tags, commits, release assets, direct URLs, private repositories, SHA-256 validation, conditional HTTP caching, safe ZIP extraction, transactional installs, embedded UPM packages, `.unitypackage` imports, and Git-based UPM installs.

Built by **DAZIxBREED** for Unity **2022.3 LTS and newer**.

## Features

- Download a repository at a branch, tag, or commit.
- Download the latest release asset or a specific tagged release asset.
- Match release assets with wildcard patterns such as `*-unity.zip`.
- Authenticate to private repositories with a GitHub personal access token.
- Cache downloads with `ETag` and `Last-Modified` revalidation.
- Verify downloaded files with SHA-256.
- Defend against ZIP-slip paths, symbolic links, oversized archives, and extreme compression ratios.
- Strip a repository archive's generated top-level directory.
- Install into `Assets/`, `Packages/`, a custom folder, or a user-selected download location.
- Import downloaded `.unitypackage` files.
- Add UPM packages directly from Git URLs, including package subfolders.
- Back up and roll back replaced folders when installation fails.
- Use the same download core from runtime C# code.
- Cancel active downloads from the Editor window.

## Install

### Unity Package Manager — Git URL

Open **Window → Package Manager**, click **+**, choose **Add package from git URL**, and enter:

```text
https://github.com/DAZIxBREED/github-downloader-for-unity.git#v1.0.0
```

Until the first GitHub release is published, use:

```text
https://github.com/DAZIxBREED/github-downloader-for-unity.git
```

### Local package

Copy this repository into your project's `Packages/com.dazixbreed.github-downloader` directory, or use Package Manager's **Add package from disk** and select `package.json`.

## Editor Usage

Open:

```text
Tools → DAZIxBREED → GitHub Downloader
```

1. Paste a GitHub repository URL or direct file URL.
2. Choose repository archive, latest release asset, tagged release asset, direct URL, or UPM Git install.
3. Enter a branch/tag/commit or release asset wildcard when applicable.
4. Select the install destination.
5. Optionally enter an expected SHA-256 hash.
6. Click **Download / Install**.

Tokens are stored in `EditorPrefs`, scoped to the current Unity project and GitHub host. They are not written to project files or logs.

## Runtime API

```csharp
using System.Threading;
using DAZIxBREED.GitHubDownloader;
using UnityEngine;

public sealed class DownloadExample : MonoBehaviour
{
    private async void Start()
    {
        var repository = GitHubUrlParser.ParseRepository(
            "https://github.com/owner/repository");

        var request = new GitHubDownloadRequest
        {
            SourceKind = GitHubSourceKind.RepositoryArchive,
            Repository = repository,
            Reference = "main",
            DestinationDirectory = Application.persistentDataPath,
            ExtractArchive = true,
            StripSingleRootDirectory = true,
            UseCache = true
        };

        var service = new GitHubDownloaderService();
        GitHubDownloadResult result = await service.ExecuteAsync(
            request,
            new Progress<GitHubDownloadProgress>(p =>
                Debug.Log($"{p.Stage}: {p.Progress:P0}")),
            CancellationToken.None);

        Debug.Log(result.Success
            ? result.ExtractedDirectoryPath
            : result.ErrorMessage);
    }
}
```

## Private Repositories

Use a fine-grained personal access token with only the repository permissions required to read contents and releases. The Editor token field is intentionally masked. Runtime tokens should be supplied at execution time and should never be committed into a project or embedded in a shipped client.

For private UPM Git installs, Git authentication is handled by the system Git client used by Unity. Configure a credential manager or SSH key rather than placing a token in the Git URL.

## Repository Layout

```text
Runtime/        Download API, GitHub API client, cache, checksum, safe extraction
Editor/         Editor window, settings, installers, UPM integration
Tests/          EditMode tests for parsing, extraction, and transactions
Samples~/       Runtime usage sample
Documentation~/ Package documentation
```

## License

MIT. See [LICENSE.md](LICENSE.md).
