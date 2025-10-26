using System.Security.Cryptography;

namespace MHServerEmu.Core.Network.Web
{
    /// <summary>
    /// Generates tokens bound to <typeparamref name="T"/> values for accessing restricted web API endpoints. This class is thread-safe.
    /// </summary>
    public class WebTokenManager<T>
    {
        private const int TokenSize = 16;

        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private readonly byte[] _buffer = new byte[TokenSize];
        private readonly Dictionary<string, T> _lookup = new();

        private readonly object _lock = new();

        /// <summary>
        /// Generates a new token for the specified session id.
        /// </summary>
        public string GenerateToken(T lookupValue)
        {
            lock (_lock)
            {
                string token;

                // The probability of generating the same token should be low, but it's still possible in theory.
                do
                {
                    _rng.GetBytes(_buffer);
                    token = Convert.ToHexString(_buffer);
                }
                while (_lookup.TryAdd(token, lookupValue) == false);

                Array.Clear(_buffer);
                return token;
            }
        }

        /// <summary>
        /// Removes the provided token.
        /// </summary>
        public bool RemoveToken(string token)
        {
            lock (_lock)
                return _lookup.Remove(token);
        }

        /// <summary>
        /// Retrieves the value for the provided token.
        /// </summary>
        public bool TryGetValue(string token, out T value)
        {
            lock (_lock)
                return _lookup.TryGetValue(token, out value);
        }
    }
}
