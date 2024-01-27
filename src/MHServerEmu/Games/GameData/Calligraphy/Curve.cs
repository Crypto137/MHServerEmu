namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class Curve
    {
        public double[] Values { get; }
        private int _startPosition;
        private int _endPosition;     
        public int MinPosition { get => _startPosition; }
        public int MaxPosition { get =>  _endPosition; }

        public Curve(Stream stream)
        {
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = new(reader);
                _startPosition = reader.ReadInt32();
                _endPosition = reader.ReadInt32();

                Values = new double[_endPosition - _startPosition + 1];
                for (int i = 0; i < Values.Length; i++)
                    Values[i] = reader.ReadDouble();
            }
        }


    }
}
