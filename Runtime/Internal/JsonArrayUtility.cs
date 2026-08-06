using System;
using UnityEngine;

namespace DAZIxBREED.GitHubDownloader.Internal
{
    internal static class JsonArrayUtility
    {
        public static T[] FromJsonArray<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<T>();
            }

            string wrapped = "{\"items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper?.items ?? Array.Empty<T>();
        }

        [Serializable]
        private sealed class Wrapper<T>
        {
            public T[] items;
        }
    }
}
