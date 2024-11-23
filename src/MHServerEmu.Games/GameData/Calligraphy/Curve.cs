using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Contains a collection of numeric values.
    /// </summary>
    public class Curve
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly CurveId _curveId;
        private readonly float[] _values;

        public float this[int index] { get => _values[index]; }

        public int MinPosition { get; }    // m_startPosition
        public int MaxPosition { get; }    // m_endPosition

        public bool IsCurveZero { get; private set; } = true;

        /// <summary>
        /// Deserializes a new <see cref="Curve"/> instance from a <see cref="Stream"/>.
        /// </summary>
        public Curve(Stream stream, CurveId curveId)
        {
            _curveId = curveId;

            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = new(reader);

                MinPosition = reader.ReadInt32();
                MaxPosition = reader.ReadInt32();

                _values = new float[MaxPosition - MinPosition + 1];
                for (int i = 0; i < _values.Length; i++)
                {
                    double value = reader.ReadDouble();
                    _values[i] = (float)value;
                    IsCurveZero &= value == 0;
                }
            }
        }
        
        /// <summary>
        /// Returns the value at the specified position as <see cref="float"/>.
        /// </summary>
        public float GetAt(int position)
        {
            if (position < MinPosition)
                Logger.Warn($"GetAt(): Curve position {position} below min of {MinPosition}, curve {this}");

            if (position > MaxPosition)
                Logger.Warn($"GetAt(): Curve position {position} above max of {MaxPosition}, curve {this}");

            position = Math.Clamp(position, MinPosition, MaxPosition);
            int index = position - MinPosition;
            return _values[index];
        }

        /// <summary>
        /// Returns the value at the specified position as <see cref="int"/>.
        /// </summary>
        public int GetIntAt(int position)
        {
            return (int)MathF.Round(GetAt(position));
        }

        public bool GetIntAt(int position, out int value)
        {
            value = GetIntAt(position);
            return true;
        }

        /// <summary>
        /// Returns the value at the specified position as <see cref="long"/>.
        /// </summary>
        public long GetInt64At(int position)
        {
            return (long)MathF.Round(GetAt(position));
        }

        public bool GetInt64At(int position, out long value)
        {
            value = GetInt64At(position);
            return true;
        }

        /// <summary>
        /// Sums the values within specified range and returns the result as <see cref="float"/>.
        /// </summary>
        public float IntegrateDiscrete(int start, int end)
        {
            if (start < MinPosition)
                return Logger.WarnReturn(0f, $"IntegrateDiscrete(): Curve start {start} below min of {MinPosition}, curve {ToString()}");

            if (start > MaxPosition)
                return Logger.WarnReturn(0f, $"IntegrateDiscrete(): Curve start {start} above max of {MaxPosition}, curve {ToString()}");

            if (end < MinPosition)
                return Logger.WarnReturn(0f, $"IntegrateDiscrete(): Curve end {end} below min of {MinPosition}, curve {ToString()}");

            if (end > MaxPosition)
                return Logger.WarnReturn(0f, $"IntegrateDiscrete(): Curve end {end} above max of {MaxPosition}, curve {ToString()}");

            float result = 0;
            for (int i = start; i <= end; i++)
                result += _values[i - MinPosition];
            return result;
        }

        /// <summary>
        /// Sums the values within specified range and returns the result as <see cref="int"/>.
        /// </summary>
        public int IntegrateDiscreteInt(int start, int end)
        {
            return (int)MathF.Round(IntegrateDiscrete(start, end));
        }

        /// <summary>
        /// Checks if the specified index is within range of this <see cref="Curve"/>.
        /// </summary>
        public bool IndexInRange(int index)
        {
            return index >= MinPosition && index <= MaxPosition;
        }

        public override string ToString()
        {
            return GameDatabase.GetCurveName(_curveId);
        }
    }
}
