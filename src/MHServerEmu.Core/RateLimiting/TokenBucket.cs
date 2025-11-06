using System.Diagnostics;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.RateLimiting
{
    /// <summary>
    /// A rate limiter based on the token bucket algorithm.
    /// </summary>
    public class TokenBucket
    {
        private readonly int _ticksPerToken;
        private readonly int _maxTokens;

        private int _currentTokens;
        private TimeSpan _lastRefillTime;

        /// <summary>
        /// Constructs a <see cref="TokenBucket"/> with the specified settings.
        /// </summary>
        public TokenBucket(float tokensPerSecond, int maxTokens)
        {
            Debug.Assert(tokensPerSecond > 0);
            Debug.Assert(maxTokens > 0);

            _ticksPerToken = (int)(TimeSpan.TicksPerSecond / tokensPerSecond);
            _maxTokens = maxTokens;

            _currentTokens = _maxTokens;
        }

        /// <summary>
        /// Refills this <see cref="TokenBucket"/> and attempts to consume the specified number of tokens from it.
        /// </summary>
        public bool CheckLimit(int numTokens = 1)
        {
            Refill();

            int numTokensAfterConsumption = _currentTokens - numTokens;
            if (numTokensAfterConsumption < 0)
                return false;

            _currentTokens = numTokensAfterConsumption;
            return true;
        }

        /// <summary>
        /// Refills this <see cref="TokenBucket"/> based on the amount of time that passed.
        /// </summary>
        private void Refill()
        {
            TimeSpan currentTime = Clock.ElapsedTime;
            TimeSpan elapsed = currentTime - _lastRefillTime;

            int tokensGained = (int)(elapsed.Ticks / _ticksPerToken);
            if (tokensGained > 0)
            {
                _currentTokens = Math.Min(_currentTokens + tokensGained, _maxTokens);
                _lastRefillTime = currentTime;
            }
        }
    }
}
