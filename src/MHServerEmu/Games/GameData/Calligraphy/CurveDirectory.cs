namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class CurveDirectory
    {
        private readonly Dictionary<CurveId, CurveRecord> _curveRecordDict = new();

        public int RecordCount { get => _curveRecordDict.Count; }

        public CurveRecord CreateCurveRecord(CurveId id, CurveRecordFlags flags)
        {
            CurveRecord record = new() { Flags = flags };
            _curveRecordDict.Add(id, record);
            return record;
        }

        public CurveRecord GetCurveRecord(CurveId id)
        {
            if (_curveRecordDict.TryGetValue(id, out CurveRecord record) == false)
                return null;

            return record;
        }

        public Curve GetCurve(CurveId id)
        {
            if (_curveRecordDict.TryGetValue(id, out CurveRecord record) == false)
                return null;

            return record.Curve;
        }

        public class CurveRecord
        {
            public Curve Curve { get; set; }
            public CurveRecordFlags Flags { get; set; }
        }
    }
}
