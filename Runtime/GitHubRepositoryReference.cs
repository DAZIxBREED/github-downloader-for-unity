using System;

namespace DAZIxBREED.GitHubDownloader
{
    [Serializable]
    public sealed class GitHubRepositoryReference
    {
        public string Host;
        public string Owner;
        public string Name;
        public string Reference;

        public GitHubRepositoryReference()
        {
        }

        public GitHubRepositoryReference(string host, string owner, string name, string reference = null)
        {
            Host = string.IsNullOrWhiteSpace(host) ? "github.com" : host.Trim();
            Owner = owner?.Trim();
            Name = name?.Trim();
            Reference = reference?.Trim();
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(Owner) &&
            !string.IsNullOrWhiteSpace(Name);

        public bool IsGitHubDotCom =>
            string.Equals(Host, "github.com", StringComparison.OrdinalIgnoreCase);

        public string WebUrl => $"https://{Host}/{Owner}/{Name}";

        public string GitUrl => $"https://{Host}/{Owner}/{Name}.git";

        public override string ToString()
        {
            return IsValid ? $"{Owner}/{Name}" : "Invalid GitHub repository";
        }
    }
}
