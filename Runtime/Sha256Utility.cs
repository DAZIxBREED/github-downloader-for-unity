using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DAZIxBREED.GitHubDownloader
{
    public static class Sha256Utility
    {
        public static async Task<string> ComputeFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A file path is required.", nameof(filePath));
            }

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var stream = new FileStream(
                           filePath,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.Read,
                           1024 * 128,
                           FileOptions.SequentialScan))
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(stream);
                    cancellationToken.ThrowIfCancellationRequested();
                    return ToLowerHex(hash);
                }
            }, cancellationToken);
        }

        public static string ComputeString(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return ToLowerHex(hash);
            }
        }

        public static bool EqualsNormalized(string expected, string actual)
        {
            string left = Normalize(expected);
            string right = Normalize(actual);
            if (left.Length != right.Length || left.Length == 0)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace("sha256:", string.Empty)
                .Replace("SHA256:", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
