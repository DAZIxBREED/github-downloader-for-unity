using System;
using System.IO;
using NUnit.Framework;

namespace DAZIxBREED.GitHubDownloader.Tests
{
    public sealed class TransactionalDirectoryInstallerTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "github-downloader-transaction-" + Guid.NewGuid().ToString("N"));
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
        public void ReplacesDestinationAtomically()
        {
            string source = Path.Combine(root, "source");
            string destination = Path.Combine(root, "destination");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(source, "new.txt"), "new");
            File.WriteAllText(Path.Combine(destination, "old.txt"), "old");

            TransactionalDirectoryInstaller.Install(
                source,
                destination,
                DirectoryInstallConflictMode.Replace);

            Assert.That(File.Exists(Path.Combine(destination, "new.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(destination, "old.txt")), Is.False);
        }

        [Test]
        public void MergePreservesUnchangedDestinationFiles()
        {
            string source = Path.Combine(root, "source");
            string destination = Path.Combine(root, "destination");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(source, "new.txt"), "new");
            File.WriteAllText(Path.Combine(destination, "keep.txt"), "keep");

            TransactionalDirectoryInstaller.Install(
                source,
                destination,
                DirectoryInstallConflictMode.Merge);

            Assert.That(File.Exists(Path.Combine(destination, "new.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(destination, "keep.txt")), Is.True);
        }
    }
}
