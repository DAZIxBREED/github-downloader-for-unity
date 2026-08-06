using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    public sealed class GitHubDownloaderWindow : EditorWindow
    {
        private const string DefaultRepositoryUrl = "https://github.com/owner/repository";

        [SerializeField] private string sourceUrl = DefaultRepositoryUrl;
        [SerializeField] private GitHubSourceKind sourceKind = GitHubSourceKind.RepositoryArchive;
        [SerializeField] private string reference = "main";
        [SerializeField] private string releaseTag = string.Empty;
        [SerializeField] private string releaseAssetPattern = "*.zip";
        [SerializeField] private string expectedSha256 = string.Empty;
        [SerializeField] private GitHubInstallMode installMode = GitHubInstallMode.AssetsFolder;
        [SerializeField] private string targetPath = "Assets/GitHubDownload";
        [SerializeField] private string packageSubfolder = string.Empty;
        [SerializeField] private DirectoryInstallConflictMode conflictMode = DirectoryInstallConflictMode.Replace;
        [SerializeField] private bool extractArchive = true;
        [SerializeField] private bool stripSingleRoot = true;
        [SerializeField] private bool useCache = true;
        [SerializeField] private bool interactiveUnityPackageImport = true;
        [SerializeField] private bool rememberToken = true;

        private string token = string.Empty;
        private string tokenHost = "github.com";
        private Vector2 scroll;
        private CancellationTokenSource cancellation;
        private bool isBusy;
        private string status = "Ready.";
        private float progressValue;

        [MenuItem("Tools/DAZIxBREED/GitHub Downloader", priority = 1200)]
        public static void ShowWindow()
        {
            var window = GetWindow<GitHubDownloaderWindow>();
            window.titleContent = new GUIContent("GitHub Downloader");
            window.minSize = new Vector2(540f, 610f);
            window.Show();
        }

        private void OnEnable()
        {
            UpdateTokenScope();
        }

        private void OnDisable()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            EditorGUI.BeginDisabledGroup(isBusy);
            DrawSourceSection();
            DrawAuthenticationSection();
            DrawInstallSection();
            DrawIntegritySection();
            EditorGUI.EndDisabledGroup();
            DrawActionSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("GitHub Downloader for Unity", EditorStyles.largeLabel);
            EditorGUILayout.LabelField(
                "Repositories, releases, private assets, safe extraction, and transactional installs.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();
        }

        private void DrawSourceSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            sourceUrl = EditorGUILayout.TextField(
                sourceKind == GitHubSourceKind.DirectUrl ? "File URL" : "Repository URL",
                sourceUrl);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateTokenScope();
            }

            sourceKind = (GitHubSourceKind)EditorGUILayout.EnumPopup("Source Type", sourceKind);

            switch (sourceKind)
            {
                case GitHubSourceKind.RepositoryArchive:
                    reference = EditorGUILayout.TextField("Branch / Tag / Commit", reference);
                    break;
                case GitHubSourceKind.LatestReleaseAsset:
                    releaseAssetPattern = EditorGUILayout.TextField("Asset Pattern", releaseAssetPattern);
                    DrawBrowseReleasesButton();
                    break;
                case GitHubSourceKind.TaggedReleaseAsset:
                    releaseTag = EditorGUILayout.TextField("Release Tag", releaseTag);
                    releaseAssetPattern = EditorGUILayout.TextField("Asset Pattern", releaseAssetPattern);
                    DrawBrowseReleasesButton();
                    break;
                case GitHubSourceKind.DirectUrl:
                    EditorGUILayout.HelpBox("Direct URLs may point to ZIP files, .unitypackage files, or any downloadable file.", MessageType.Info);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBrowseReleasesButton()
        {
            if (GUILayout.Button("Browse Releases and Assets"))
            {
                if (!GitHubUrlParser.TryParseRepository(sourceUrl, out GitHubRepositoryReference repository))
                {
                    EditorUtility.DisplayDialog("GitHub Downloader", "Enter a valid repository URL first.", "OK");
                    return;
                }

                GitHubReleasePickerWindow.Open(
                    repository,
                    token,
                    GitHubDownloaderProjectSettings.instance.ApiBaseUrl,
                    GitHubDownloaderProjectSettings.instance.IncludePrereleases,
                    (tag, asset) =>
                    {
                        sourceKind = GitHubSourceKind.TaggedReleaseAsset;
                        releaseTag = tag;
                        releaseAssetPattern = asset;
                        Repaint();
                    });
            }
        }

        private void DrawAuthenticationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);
            token = EditorGUILayout.PasswordField("GitHub Token", token);
            rememberToken = EditorGUILayout.Toggle("Remember for this project", rememberToken);
            EditorGUILayout.LabelField("Token scope: " + tokenHost, EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "Use a fine-grained read-only token for private repositories. Tokens are stored in EditorPrefs and never written to the project.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Token"))
            {
                GitHubTokenStore.Save(tokenHost, token);
                status = string.IsNullOrWhiteSpace(token) ? "Saved token cleared." : "Token saved securely in EditorPrefs.";
            }

            if (GUILayout.Button("Forget Token"))
            {
                GitHubTokenStore.Delete(tokenHost);
                token = string.Empty;
                status = "Stored token removed.";
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawInstallSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Destination", EditorStyles.boldLabel);
            installMode = (GitHubInstallMode)EditorGUILayout.EnumPopup("Install Mode", installMode);

            if (installMode == GitHubInstallMode.UpmGit)
            {
                reference = EditorGUILayout.TextField("Git Reference", reference);
                packageSubfolder = EditorGUILayout.TextField("Package Subfolder", packageSubfolder);
                EditorGUILayout.HelpBox(
                    "Unity's system Git client handles authentication for private UPM installs. Configure a credential manager or SSH key; do not embed tokens in URLs.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            extractArchive = EditorGUILayout.Toggle("Extract ZIP", extractArchive);
            using (new EditorGUI.DisabledScope(!extractArchive))
            {
                stripSingleRoot = EditorGUILayout.Toggle("Strip Single Root Folder", stripSingleRoot);
            }

            if (installMode == GitHubInstallMode.AssetsFolder)
            {
                targetPath = EditorGUILayout.TextField("Assets Path", targetPath);
                packageSubfolder = EditorGUILayout.TextField("Source Subfolder", packageSubfolder);
                conflictMode = (DirectoryInstallConflictMode)EditorGUILayout.EnumPopup("Existing Files", conflictMode);
            }
            else if (installMode == GitHubInstallMode.EmbeddedPackage)
            {
                packageSubfolder = EditorGUILayout.TextField("Package Subfolder", packageSubfolder);
                conflictMode = (DirectoryInstallConflictMode)EditorGUILayout.EnumPopup("Existing Package", conflictMode);
            }
            else if (installMode == GitHubInstallMode.CustomDirectory)
            {
                EditorGUILayout.BeginHorizontal();
                targetPath = EditorGUILayout.TextField("Directory", targetPath);
                if (GUILayout.Button("Browse", GUILayout.Width(72f)))
                {
                    string selected = EditorUtility.OpenFolderPanel("Install Directory", targetPath, string.Empty);
                    if (!string.IsNullOrWhiteSpace(selected))
                    {
                        targetPath = selected;
                    }
                }
                EditorGUILayout.EndHorizontal();
                packageSubfolder = EditorGUILayout.TextField("Source Subfolder", packageSubfolder);
                conflictMode = (DirectoryInstallConflictMode)EditorGUILayout.EnumPopup("Existing Files", conflictMode);
            }
            else if (installMode == GitHubInstallMode.ImportUnityPackage)
            {
                interactiveUnityPackageImport = EditorGUILayout.Toggle("Interactive Import", interactiveUnityPackageImport);
                extractArchive = false;
            }

            useCache = EditorGUILayout.Toggle("Use Validated Cache", useCache);
            EditorGUILayout.EndVertical();
        }

        private void DrawIntegritySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Integrity", EditorStyles.boldLabel);
            expectedSha256 = EditorGUILayout.TextField("Expected SHA-256", expectedSha256);
            EditorGUILayout.HelpBox(
                "When supplied, installation stops unless the downloaded file exactly matches this SHA-256 hash.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawActionSection()
        {
            EditorGUILayout.Space();
            if (isBusy)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 22f);
                EditorGUI.ProgressBar(rect, progressValue, status);
                if (GUILayout.Button("Cancel"))
                {
                    cancellation?.Cancel();
                    status = "Cancelling…";
                }
            }
            else
            {
                if (GUILayout.Button(
                        installMode == GitHubInstallMode.DownloadOnly ? "Download" : "Download / Install",
                        GUILayout.Height(34f)))
                {
                    RunAsync();
                }

                EditorGUILayout.HelpBox(status, MessageType.Info);
            }
        }

        private async void RunAsync()
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;
            progressValue = 0f;
            status = "Starting…";
            cancellation = new CancellationTokenSource();
            try
            {
                if (rememberToken)
                {
                    GitHubTokenStore.Save(tokenHost, token);
                }

                if (installMode == GitHubInstallMode.UpmGit)
                {
                    GitHubRepositoryReference repository = GitHubUrlParser.ParseRepository(sourceUrl);
                    string packageId = GitHubUrlParser.BuildUpmGitUrl(repository, reference, packageSubfolder);
                    status = "Adding package through Unity Package Manager…";
                    string installed = await GitHubUpmInstaller.AddAsync(packageId, cancellation.Token);
                    status = "Installed " + installed;
                    EditorUtility.DisplayDialog("GitHub Downloader", status, "OK");
                    return;
                }

                GitHubDownloadRequest request = BuildRequest();
                var service = new GitHubDownloaderService();
                var reporter = new Progress<GitHubDownloadProgress>(value =>
                {
                    status = string.IsNullOrWhiteSpace(value.Detail)
                        ? value.Stage
                        : value.Stage + ": " + value.Detail;
                    progressValue = value.Progress;
                    Repaint();
                });

                GitHubDownloadResult result = await service.ExecuteAsync(request, reporter, cancellation.Token);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorMessage);
                }

                string finalPath = CompleteInstallation(result);
                status = "Complete: " + (finalPath ?? result.DownloadedFilePath);
                EditorUtility.DisplayDialog(
                    "GitHub Downloader",
                    $"Operation completed successfully.\n\nSHA-256:\n{result.Sha256}\n\nLocation:\n{finalPath ?? result.DownloadedFilePath}",
                    "OK");
            }
            catch (OperationCanceledException)
            {
                status = "Operation cancelled.";
            }
            catch (Exception exception)
            {
                status = "Failed: " + exception.Message;
                EditorUtility.DisplayDialog("GitHub Downloader", status, "OK");
            }
            finally
            {
                cancellation?.Dispose();
                cancellation = null;
                isBusy = false;
                progressValue = 0f;
                Repaint();
            }
        }

        private GitHubDownloadRequest BuildRequest()
        {
            GitHubRepositoryReference repository = null;
            if (sourceKind != GitHubSourceKind.DirectUrl)
            {
                repository = GitHubUrlParser.ParseRepository(sourceUrl);
            }

            string cacheRoot = GitHubDownloaderProjectSettings.instance.GetAbsoluteCacheDirectory();
            string cacheScope;
            if (repository != null)
            {
                cacheScope = Sha256Utility.ComputeString(
                    repository.Host + "/" + repository.Owner + "/" + repository.Name).Substring(0, 16);
            }
            else
            {
                cacheScope = "direct-" + Sha256Utility.ComputeString(sourceUrl).Substring(0, 16);
            }

            string cacheDirectory = Path.Combine(cacheRoot, cacheScope);
            Directory.CreateDirectory(cacheDirectory);
            return new GitHubDownloadRequest
            {
                SourceKind = sourceKind,
                Repository = repository,
                Reference = reference,
                ReleaseTag = releaseTag,
                ReleaseAssetPattern = releaseAssetPattern,
                DirectUrl = sourceKind == GitHubSourceKind.DirectUrl ? sourceUrl : null,
                DestinationDirectory = cacheDirectory,
                ExtractArchive = extractArchive,
                StripSingleRootDirectory = stripSingleRoot,
                UseCache = useCache,
                Overwrite = true,
                ExpectedSha256 = expectedSha256,
                Token = token,
                ApiBaseUrl = GitHubDownloaderProjectSettings.instance.ApiBaseUrl,
                ArchiveSafetyLimits = GitHubDownloaderProjectSettings.instance.CreateSafetyLimits()
            };
        }

        private string CompleteInstallation(GitHubDownloadResult result)
        {
            switch (installMode)
            {
                case GitHubInstallMode.DownloadOnly:
                    return GitHubProjectInstaller.SaveDownloadedFile(result.DownloadedFilePath);
                case GitHubInstallMode.ImportUnityPackage:
                    GitHubProjectInstaller.ImportUnityPackage(result.DownloadedFilePath, interactiveUnityPackageImport);
                    return result.DownloadedFilePath;
                case GitHubInstallMode.AssetsFolder:
                case GitHubInstallMode.EmbeddedPackage:
                case GitHubInstallMode.CustomDirectory:
                    return GitHubProjectInstaller.InstallDirectory(
                        result.ExtractedDirectoryPath,
                        installMode,
                        targetPath,
                        packageSubfolder,
                        conflictMode);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateTokenScope()
        {
            string newHost = "github.com";
            if (GitHubUrlParser.TryParseRepository(sourceUrl, out GitHubRepositoryReference repository))
            {
                newHost = repository.Host;
            }
            else if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri uri))
            {
                newHost = uri.Host;
            }

            if (!string.Equals(newHost, tokenHost, StringComparison.OrdinalIgnoreCase))
            {
                tokenHost = newHost;
                token = GitHubTokenStore.Load(tokenHost);
            }
            else if (string.IsNullOrEmpty(token))
            {
                token = GitHubTokenStore.Load(tokenHost);
            }
        }
    }
}
