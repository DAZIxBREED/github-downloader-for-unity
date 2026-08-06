# Security Policy

## Reporting

Please report security issues privately to the repository owner rather than opening a public issue when the report includes exploitable details.

## Token Safety

- Never commit a personal access token.
- Prefer fine-grained, read-only tokens.
- Do not embed tokens in player builds.
- Do not place tokens in Git URLs.

## Archive Safety

The extractor rejects path traversal, absolute paths, symbolic links, excessive entry counts, excessive expanded size, and extreme compression ratios. Applications should still download only from trusted repositories and validate published SHA-256 hashes whenever available.
