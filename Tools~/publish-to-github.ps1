$ErrorActionPreference = 'Stop'

$Owner = if ($env:GITHUB_OWNER) { $env:GITHUB_OWNER } else { 'DAZIxBREED' }
$Repository = if ($env:GITHUB_REPOSITORY_NAME) { $env:GITHUB_REPOSITORY_NAME } else { 'github-downloader-for-unity' }
$Visibility = if ($env:GITHUB_VISIBILITY) { $env:GITHUB_VISIBILITY } else { 'public' }
$Tag = if ($env:GITHUB_RELEASE_TAG) { $env:GITHUB_RELEASE_TAG } else { 'v1.0.0' }

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'git is required.' }
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'GitHub CLI is required: https://cli.github.com/' }

gh auth status | Out-Null
if (-not (Test-Path '.git')) { throw 'Run this script from the repository root.' }

$origin = git remote get-url origin 2>$null
if (-not $origin) {
    gh repo create "$Owner/$Repository" "--$Visibility" `
        --description 'Production-oriented GitHub repository and release downloader for Unity 2022.3 LTS+.' `
        --source . --remote origin --push
}

git push -u origin main
git push origin $Tag

$archive = "../GitHub-Downloader-for-Unity-$Tag.zip"
git archive --format=zip --output $archive $Tag

& gh release view $Tag --repo "$Owner/$Repository" *> $null
if ($LASTEXITCODE -eq 0) {
    gh release upload $Tag $archive --clobber --repo "$Owner/$Repository"
} else {
    gh release create $Tag $archive --repo "$Owner/$Repository" `
        --title 'GitHub Downloader for Unity 1.0.0' --notes-file CHANGELOG.md
}

Write-Host "Published https://github.com/$Owner/$Repository"
