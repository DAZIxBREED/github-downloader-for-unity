# Manual

## Open the tool

Use **Tools → DAZIxBREED → GitHub Downloader**.

## Source types

- **Repository Archive** downloads a ZIP snapshot at a branch, tag, or commit.
- **Latest Release Asset** resolves the latest non-draft GitHub release and downloads the first wildcard match.
- **Tagged Release Asset** resolves a specific release tag.
- **Direct URL** downloads an arbitrary HTTP or HTTPS file.

## Install modes

- **Download Only** saves the downloaded file to a location you choose.
- **Assets Folder** installs extracted files under `Assets/`.
- **Embedded Package** requires a `package.json` and installs under `Packages/<package-name>`.
- **Custom Directory** performs the same transactional directory install outside the Unity project.
- **Import Unity Package** passes a downloaded `.unitypackage` to Unity's importer.
- **UPM Git** asks Unity Package Manager to add a Git repository directly.

## Nested packages

Set **Package Subfolder** when the package root is not the repository root. For UPM Git mode, this becomes Unity's `?path=/subfolder` Git dependency syntax.

## Cache

The package stores files under `Library/GitHubDownloader/Cache` by default. HTTP validators are sent on subsequent downloads. GitHub may return `304 Not Modified`, allowing the existing file to be reused.

## Rollback

Directory installs are staged beside the destination. Existing content is renamed to a backup immediately before the staged directory is moved into place. When the final move fails, the previous directory is restored.
