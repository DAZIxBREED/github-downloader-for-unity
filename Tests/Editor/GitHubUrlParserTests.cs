using NUnit.Framework;

namespace DAZIxBREED.GitHubDownloader.Tests
{
    public sealed class GitHubUrlParserTests
    {
        [TestCase("https://github.com/owner/repo", "github.com", "owner", "repo", null)]
        [TestCase("https://github.com/owner/repo.git", "github.com", "owner", "repo", null)]
        [TestCase("git@github.com:owner/repo.git", "github.com", "owner", "repo", null)]
        [TestCase("https://github.com/owner/repo/tree/feature/test", "github.com", "owner", "repo", "feature/test")]
        [TestCase("https://git.example.com/team/tool/tree/v2", "git.example.com", "team", "tool", "v2")]
        public void ParsesRepositoryUrls(
            string input,
            string host,
            string owner,
            string name,
            string reference)
        {
            Assert.That(GitHubUrlParser.TryParseRepository(input, out GitHubRepositoryReference repository), Is.True);
            Assert.That(repository.Host, Is.EqualTo(host));
            Assert.That(repository.Owner, Is.EqualTo(owner));
            Assert.That(repository.Name, Is.EqualTo(name));
            Assert.That(repository.Reference, Is.EqualTo(reference));
        }

        [Test]
        public void BuildsUpmUrlWithSubfolderAndReference()
        {
            var repository = new GitHubRepositoryReference("github.com", "owner", "repo");
            string value = GitHubUrlParser.BuildUpmGitUrl(repository, "v1.2.3", "Packages/Tool");
            Assert.That(value, Is.EqualTo("https://github.com/owner/repo.git?path=/Packages/Tool#v1.2.3"));
        }
    }
}
