using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class Curve
    {
        public double[] Values { get; }
        private int _startPosition;
        private int _endPosition;     
        public int MinPosition { get => _startPosition; }
        public int MaxPosition { get =>  _endPosition; }

        public Curve(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();
                _startPosition = reader.ReadInt32();
                _endPosition = reader.ReadInt32();

                Values = new double[_endPosition - _startPosition + 1];
                for (int i = 0; i < Values.Length; i++)
                    Values[i] = reader.ReadDouble();
            }
        }


    }
}
