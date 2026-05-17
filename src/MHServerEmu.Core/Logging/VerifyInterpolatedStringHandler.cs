using System.Runtime.CompilerServices;

namespace MHServerEmu.Core.Logging
{
    [InterpolatedStringHandler]
    public ref struct VerifyInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _defaultHandler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VerifyInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool isEnabled)
        {
            if (condition == false)
            {
                _defaultHandler = new(literalLength, formattedCount);
                isEnabled = true;
            }
            else
            {
                _defaultHandler = default;
                isEnabled = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VerifyInterpolatedStringHandler(int literalLength, int formattedCount, object instance, out bool isEnabled)
        {
            if (instance == null)
            {
                _defaultHandler = new(literalLength, formattedCount);
                isEnabled = true;
            }
            else
            {
                _defaultHandler = default;
                isEnabled = false;
            }
        }

        public override string ToString()
        {
            return _defaultHandler.ToString();
        }

        public string ToStringAndClear()
        {
            return _defaultHandler.ToStringAndClear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLiteral(string value)
        {
            _defaultHandler.AppendLiteral(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted<T>(T value)
        {
            _defaultHandler.AppendFormatted(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted<T>(T value, string format)
        {
            _defaultHandler.AppendFormatted(value, format);
        }
    }
}
