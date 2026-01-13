using System.Security.Cryptography;

namespace MHServerEmu.Core.Network.Web
{
    /// <summary>
    /// Generates tokens bound to <typeparamref name="T"/> values for accessing restricted web API endpoints. This class is thread-safe.
    /// </summary>
    public class WebTokenManager<T>
    {
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private readonly Dictionary<string, T> _lookup = new();

        private readonly byte[] _buffer;

        /// <summary>
        /// Constructs a new <see cref="WebTokenManager{T}"/> with the specified token size in bytes.
        /// </summary>
        public WebTokenManager(int tokenSize = 16)
        {
            _buffer = new byte[tokenSize];
        }

        /// <summary>
        /// Generates a new token for the provided <typeparamref name="T"/> value.
        /// </summary>
        public string GenerateToken(T lookupValue)
        {
            lock (_lookup)
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
        /// Removes the provided token. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveToken(string token)
        {
            lock (_lookup)
                return _lookup.Remove(token);
        }

        /// <summary>
        /// Retrieves the <typeparamref name="T"/> value associated with the provided token. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetValue(string token, out T value)
        {
            lock (_lookup)
                return _lookup.TryGetValue(token, out value);
        }
    }
}
