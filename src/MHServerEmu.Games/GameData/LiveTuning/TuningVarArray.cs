namespace MHServerEmu.Games.GameData.LiveTuning
{
    /// <summary>
    /// A wrapper around <see cref="Array"/> for storing live tuning var values.
    /// </summary>
    public class TuningVarArray
    {
        private readonly float[] _data;

        public float this[int index] { get => _data[index]; set => _data[index] = value; }

        public TuningVarArray(int size)
        {
            _data = new float[size];
            Clear();
        }

        public TuningVarArray(TuningVarArray other)
        {
            _data = new float[other._data.Length];
            Copy(other);
        }

        public void Clear()
        {
            for (int i = 0; i < _data.Length; i++)
                _data[i] = LiveTuningData.DefaultTuningVarValue;
        }

        public void Copy(TuningVarArray other)
        {
            if (_data.Length != other._data.Length)
                throw new InvalidOperationException("TuningVarArray size mismatch.");

            for (int i = 0; i < _data.Length; i++)
                _data[i] = other._data[i];
        }

    }
}
