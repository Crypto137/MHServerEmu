namespace MHServerEmu.Games.GameData.LiveTuning
{
    /// <summary>
    /// A wrapper around <see cref="Array"/> for storing live tuning var values.
    /// </summary>
    public class TuningVarArray
    {
        private readonly float[] _data;

        public float this[int index] { get => _data[index]; set => _data[index] = value; }

        /// <summary>
        /// Constructs a new <see cref="TuningVarArray"/> and fills it with default values.
        /// </summary>
        public TuningVarArray(int size)
        {
            _data = new float[size];
            Clear();
        }

        /// <summary>
        /// Constructs a new <see cref="TuningVarArray"/> and copies all values from the provided instance.
        /// </summary>
        public TuningVarArray(TuningVarArray other)
        {
            _data = new float[other._data.Length];
            Copy(other);
        }

        /// <summary>
        /// Sets all values in this <see cref="TuningVarArray"/> to the default value.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _data.Length; i++)
                _data[i] = LiveTuningData.DefaultTuningVarValue;
        }

        /// <summary>
        /// Copies all values from the provided <see cref="TuningVarArray"/>
        /// </summary>
        public void Copy(TuningVarArray other)
        {
            if (_data.Length != other._data.Length)
                throw new InvalidOperationException("TuningVarArray size mismatch.");

            for (int i = 0; i < _data.Length; i++)
                _data[i] = other._data[i];
        }

    }
}
