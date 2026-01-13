using System.Text.Json;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System;

namespace MHServerEmu.WebFrontend.Network
{
    public enum WebApiKeyVerificationResult
    {
        Success,
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
            if (keys == null)
            {
                Logger.Warn("LoadKeys(): Failed to deserialize web API keys");
                return;
            }

            foreach (var kvp in keys)
            {
                string key = kvp.Key;
                WebApiKeyData keyData = kvp.Value;

                if (_keys.ImportToken(key, keyData) == false)
                {
                    Logger.Warn($"LoadKeys(): Failed to import web API key [{keyData}]");
                    continue;
                }

                Logger.Info($"Loaded web API key [{keyData}]");
            }
        }

        public void SaveKeys()
        {
            var keys = ListPool<KeyValuePair<string, WebApiKeyData>>.Instance.Get();
            _keys.ExportTokens(keys);

            FileHelper.SerializeJson(KeyFilePath, keys, JsonOptions);

            ListPool<KeyValuePair<string, WebApiKeyData>>.Instance.Return(keys);
        }

        public string CreateKey(string name, WebApiAccessType access)
        {
            string key = null;

            if (string.IsNullOrWhiteSpace(name))
                return Logger.WarnReturn(key, $"CreateKey(): Invalid key name '{name}'");

            if (access <= WebApiAccessType.None || access >= WebApiAccessType.NumTypes)
                return Logger.WarnReturn(key, $"CreateKey(): Invalid access type {access}");

            WebApiKeyData keyData = new(name, access, DateTime.UtcNow);
            key = _keys.GenerateToken(keyData);

            SaveKeys();

            return key;
        }

        public WebApiKeyVerificationResult VerifyKey(string key, WebApiAccessType requiredAccess, out string keyName)
        {
            keyName = string.Empty;

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
