#!/usr/bin/env bash
set -euo pipefail

OWNER="${GITHUB_OWNER:-DAZIxBREED}"
REPOSITORY="${GITHUB_REPOSITORY_NAME:-github-downloader-for-unity}"
VISIBILITY="${GITHUB_VISIBILITY:-public}"
TAG="${GITHUB_RELEASE_TAG:-v1.0.0}"

command -v git >/dev/null 2>&1 || { echo "git is required." >&2; exit 1; }
command -v gh >/dev/null 2>&1 || { echo "GitHub CLI is required: https://cli.github.com/" >&2; exit 1; }
gh auth status >/dev/null

if [[ ! -d .git ]]; then
  echo "Run this script from the repository root." >&2
  exit 1
fi

if git remote get-url origin >/dev/null 2>&1; then
  echo "Using existing origin: $(git remote get-url origin)"
else
  gh repo create "${OWNER}/${REPOSITORY}" \
    --${VISIBILITY} \
    --description "Production-oriented GitHub repository and release downloader for Unity 2022.3 LTS+." \
    --source . \
    --remote origin \
    --push
fi

git push -u origin main
git push origin "${TAG}"

ARCHIVE="../GitHub-Downloader-for-Unity-${TAG}.zip"
git archive --format=zip --output "${ARCHIVE}" "${TAG}"

if gh release view "${TAG}" --repo "${OWNER}/${REPOSITORY}" >/dev/null 2>&1; then
  gh release upload "${TAG}" "${ARCHIVE}" --clobber --repo "${OWNER}/${REPOSITORY}"
else
  gh release create "${TAG}" "${ARCHIVE}" \
    --repo "${OWNER}/${REPOSITORY}" \
    --title "GitHub Downloader for Unity 1.0.0" \
    --notes-file CHANGELOG.md
fi

echo "Published https://github.com/${OWNER}/${REPOSITORY}"
