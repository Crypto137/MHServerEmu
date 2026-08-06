using System.Text.Json;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.RateLimiting;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess.Json
{
    /// <summary>
    /// Serializes <see cref="DBAccount"/> instances to JSON.
    /// </summary>
    public class DBAccountJsonSerializer
    {
        private readonly JsonSerializerOptions _options = new();
        private readonly TimeLeakyBucketCollection<ulong> _rateLimiter = new(TimeSpan.FromMinutes(30), 5);

        public static DBAccountJsonSerializer Instance { get; } = new();

        private DBAccountJsonSerializer()
        {
            _options.Converters.Add(new DBEntityCollectionJsonConverter());
        }

        public bool TrySerializeAccount(DBAccount account, bool checkRateLimit, out string json)
        {
            json = string.Empty;

            if (!Verify.IsNotNull(account)) return false;

            if (checkRateLimit)
            {
                lock (_rateLimiter)
                {
                    if (_rateLimiter.AddTime((ulong)account.Id) == false)
                        return false;
                }
            }

            try
            {
                json = JsonSerializer.Serialize(account, _options);
            }
            catch (Exception e)
            {
                Verify.IsTrue(false, LoggingLevel.Error, $"Failed to serialize account {account}: {e.Message}");
                return false;
            }

            return true;
        }
    }
}
