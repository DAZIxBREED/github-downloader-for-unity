using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DAZIxBREED.GitHubDownloader.Tests
{
    public sealed class SafeZipExtractorTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "github-downloader-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root))
            {
                DirectoryUtility.DeleteDirectoryRobust(root);
            }
        }

        [Test]
        public async Task ExtractsAndStripsSingleRootDirectory()
        {
            string archive = Path.Combine(root, "valid.zip");
            using (ZipArchive zip = ZipFile.Open(archive, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = zip.CreateEntry("repo-root/Runtime/Test.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("hello");
                }
            }

            string destination = Path.Combine(root, "output");
            await SafeZipExtractor.ExtractAsync(
                archive,
                destination,
                true,
                GitHubArchiveSafetyLimits.Default);

            Assert.That(File.Exists(Path.Combine(destination, "Runtime", "Test.txt")), Is.True);
        }

        [Test]
        public void RejectsZipSlip()
        {
            string archive = Path.Combine(root, "malicious.zip");
            using (ZipArchive zip = ZipFile.Open(archive, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = zip.CreateEntry("../escaped.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("nope");
                }
            }

            string destination = Path.Combine(root, "output");
            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await SafeZipExtractor.ExtractAsync(
                    archive,
                    destination,
                    false,
                    GitHubArchiveSafetyLimits.Default));
        }
    }
}
