using System.Security.Cryptography;

namespace MHServerEmu.Core.System
{
    /// <summary>
    /// Generates random cryptographic tokens bound to <typeparamref name="T"/> values for accessing restricted APIs. This class is thread-safe.
    /// </summary>
    public class TokenManager<T>
    {
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private readonly Dictionary<string, T> _lookup = new();

        private readonly byte[] _buffer;

        /// <summary>
        /// Constructs a new <see cref="TokenManager{T}"/> with the specified token size in bytes.
        /// </summary>
        public TokenManager(int tokenSize = 16)
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
        /// Clears all previously added tokens.
        /// </summary>
        public void Clear()
        {
            lock (_lookup)
                _lookup.Clear();
        }

        /// <summary>
        /// Replaces tokens with the contents of the provided <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        public void Import(IReadOnlyList<KeyValuePair<string, T>> tokens)
        {
            lock (_lookup)
            {
                Clear();
                for (int i = 0; i < tokens.Count; i++)
                {
                    var kvp = tokens[i];
                    _lookup.Add(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Copies tokens to the provided <see cref="List{T}"/>.
        /// </summary>
        public void Export(List<KeyValuePair<string, T>> tokens)
        {
            lock (_lookup)
                tokens.AddRange(_lookup);
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
