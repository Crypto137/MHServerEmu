namespace MHServerEmu.GameServer.GameData.Calligraphy
{
    public class CurveDirectory
    {
        private readonly Dictionary<ulong, CurveRecord> _curveRecordDict = new();

        public int RecordCount { get => _curveRecordDict.Count; }

        public CurveRecord CreateCurveRecord(ulong id, byte flags)
        {
            CurveRecord record = new() { Flags = flags };
            _curveRecordDict.Add(id, record);
            return record;
        }

        public CurveRecord GetCurveRecord(ulong id)
        {
            if (_curveRecordDict.TryGetValue(id, out CurveRecord record))
                return record;

            return null;
        }

        public Curve GetCurve(ulong id)
        {
            if (_curveRecordDict.TryGetValue(id, out CurveRecord record))
                return record.Curve;

            return null;
        }
    }

    public class CurveRecord
    {
        public Curve Curve { get; set; }
        public byte Flags { get; set; }
    }
}
