using System;
using System.Linq;

namespace DAZIxBREED.GitHubDownloader
{
    public static class GitHubUrlParser
    {
        public static bool TryParseRepository(string input, out GitHubRepositoryReference repository)
        {
            repository = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string value = input.Trim();
            if (value.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
            {
                return TryParseSsh(value, out repository);
            }

            if (!value.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                value = "https://" + value.TrimStart('/');
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            string[] segments = uri.AbsolutePath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString)
                .ToArray();

            if (segments.Length < 2)
            {
                return false;
            }

            string owner = segments[0];
            string name = StripGitSuffix(segments[1]);
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string reference = null;
            if (segments.Length >= 4 &&
                (string.Equals(segments[2], "tree", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(segments[2], "commit", StringComparison.OrdinalIgnoreCase)))
            {
                reference = string.Join("/", segments.Skip(3));
            }
            else if (segments.Length >= 5 &&
                     string.Equals(segments[2], "releases", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(segments[3], "tag", StringComparison.OrdinalIgnoreCase))
            {
                reference = string.Join("/", segments.Skip(4));
            }

            repository = new GitHubRepositoryReference(uri.Host, owner, name, reference);
            return repository.IsValid;
        }

        public static GitHubRepositoryReference ParseRepository(string input)
        {
            if (!TryParseRepository(input, out GitHubRepositoryReference repository))
            {
                throw new FormatException($"Unable to parse a GitHub repository from '{input}'.");
            }

            return repository;
        }

        public static bool IsHttpUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out Uri uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static string BuildUpmGitUrl(
            GitHubRepositoryReference repository,
            string reference = null,
            string packageSubfolder = null)
        {
            if (repository == null || !repository.IsValid)
            {
                throw new ArgumentException("A valid repository is required.", nameof(repository));
            }

            string url = repository.GitUrl;
            if (!string.IsNullOrWhiteSpace(packageSubfolder))
            {
                string normalized = packageSubfolder.Trim().Replace('\\', '/').Trim('/');
                url += "?path=/" + Uri.EscapeDataString(normalized).Replace("%2F", "/");
            }

            string resolvedReference = string.IsNullOrWhiteSpace(reference)
                ? repository.Reference
                : reference.Trim();

            if (!string.IsNullOrWhiteSpace(resolvedReference))
            {
                url += "#" + resolvedReference;
            }

            return url;
        }

        private static bool TryParseSsh(string value, out GitHubRepositoryReference repository)
        {
            repository = null;
            int atIndex = value.IndexOf('@');
            int colonIndex = value.IndexOf(':', atIndex + 1);
            if (atIndex < 0 || colonIndex < 0 || colonIndex >= value.Length - 1)
            {
                return false;
            }

            string host = value.Substring(atIndex + 1, colonIndex - atIndex - 1);
            string path = value.Substring(colonIndex + 1).Trim('/');
            string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return false;
            }

            repository = new GitHubRepositoryReference(
                host,
                segments[0],
                StripGitSuffix(segments[1]));
            return repository.IsValid;
        }

        private static string StripGitSuffix(string value)
        {
            return value.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
                ? value.Substring(0, value.Length - 4)
                : value;
        }
    }
}
