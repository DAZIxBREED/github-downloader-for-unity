# Publishing

The repository is already initialized on `main` and tagged `v1.0.0` in the downloadable project bundle.

After installing and authenticating GitHub CLI, run one of these from the repository root:

```bash
./Tools~/publish-to-github.sh
```

```powershell
./Tools~/publish-to-github.ps1
```

The script creates `DAZIxBREED/github-downloader-for-unity` as a public repository when no `origin` exists, pushes `main` and `v1.0.0`, creates a clean Git archive, and publishes the GitHub release.

Environment variables can override `GITHUB_OWNER`, `GITHUB_REPOSITORY_NAME`, `GITHUB_VISIBILITY`, and `GITHUB_RELEASE_TAG`.
