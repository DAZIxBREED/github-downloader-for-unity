# Security

## Tokens

Editor tokens are stored in `EditorPrefs`, scoped by Unity project and GitHub host. The package never writes a token into project settings, cache metadata, logs, download filenames, or URLs.

Runtime tokens are held only by the request object. Shipping a long-lived GitHub token inside a client is unsafe.

## ZIP archives

The extractor rejects:

- absolute paths;
- `..` path traversal;
- symbolic links;
- entries outside the normalized destination root;
- entry counts beyond the configured limit;
- expanded archives beyond the configured byte limit;
- entries beyond the configured compression ratio.

SHA-256 verification should be enabled when the publisher provides a trusted digest.
