using System.Text.Json;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System;

namespace MHServerEmu.Core.Network.Web
{
    public enum WebApiKeyVerificationResult
    {
        Success,
        InvalidKey,
        KeyNotFound,
        AccessMismatch,
    }

    /// <summary>
    /// Singleton implementation of <see cref="TokenManager{T}"/> for managing <see cref="WebApiKeyData"/> instances.
    /// </summary>
    public class WebApiKeyManager
    {
        private static readonly string KeyFilePath = Path.Combine(FileHelper.DataDirectory, "Web", "ApiKeys.json");
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly TokenManager<WebApiKeyData> _keys = new();

        public static WebApiKeyManager Instance { get; } = new();

        private WebApiKeyManager() { }

        public void LoadKeys()
        {
            _keys.Clear();

            if (File.Exists(KeyFilePath) == false)
                return;

            var keys = FileHelper.DeserializeJson<List<KeyValuePair<string, WebApiKeyData>>>(KeyFilePath);
            if (!Verify.IsNotNull(keys))
                return;

            foreach (var kvp in keys)
            {
                string key = kvp.Key;
                WebApiKeyData keyData = kvp.Value;

                if (!Verify.IsTrue(_keys.ImportToken(key, keyData), $"Failed to import web API key [{keyData}]"))
                    continue;

                Logger.Info($"Loaded web API key [{keyData}]");
            }
        }

        public void SaveKeys()
        {
            using var keysHandle = ListPool<KeyValuePair<string, WebApiKeyData>>.Get(out var keys);
            _keys.ExportTokens(keys);

            FileHelper.SerializeJson(KeyFilePath, keys, JsonOptions);
        }

        public string CreateKey(string name, WebApiAccessType access)
        {
            if (!Verify.IsTrue(string.IsNullOrWhiteSpace(name) == false, $"Invalid key name '{name}'"))
                return null;

            if (!Verify.IsTrue(access >= WebApiAccessType.None && access < WebApiAccessType.NumTypes, $"Invalid access type {access}"))
                return null;

            WebApiKeyData keyData = new(name, access, DateTime.UtcNow);
            string key = _keys.GenerateToken(keyData);

            SaveKeys();

            return key;
        }

        public WebApiKeyVerificationResult VerifyKey(string key, WebApiAccessType requiredAccess, out string keyName)
        {
            keyName = string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                return WebApiKeyVerificationResult.InvalidKey;

            if (_keys.TryGetValue(key, out WebApiKeyData keyData) == false)
                return WebApiKeyVerificationResult.KeyNotFound;

            if (keyData.Access != requiredAccess)
                return WebApiKeyVerificationResult.AccessMismatch;

            keyName = keyData.Name;
            return WebApiKeyVerificationResult.Success;
        }

        private class WebApiKeyData(string name, WebApiAccessType access, DateTime creationTime)
        {
            public string Name { get; init; } = name;
            public WebApiAccessType Access { get; init; } = access;
            public DateTime CreationTime { get; init; } = creationTime;

            public override string ToString()
            {
                return $"{Name} ({Access})";
            }
        }
    }
}
