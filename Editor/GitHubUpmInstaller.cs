using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace DAZIxBREED.GitHubDownloader.Editor
{
    internal static class GitHubUpmInstaller
    {
        public static async Task<string> AddAsync(string packageId, CancellationToken cancellationToken)
        {
            AddRequest request = Client.Add(packageId);
            while (!request.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.Status == StatusCode.Failure)
            {
                throw new InvalidOperationException(request.Error?.message ?? "Unity Package Manager failed to add the package.");
            }

            return request.Result?.packageId ?? packageId;
        }
    }
}
