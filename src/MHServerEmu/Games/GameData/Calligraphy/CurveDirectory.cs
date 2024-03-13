using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class CurveDirectory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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
            // Look for a record for the specified id
            if (_curveRecordDict.TryGetValue(id, out CurveRecord record) == false)
                return Logger.WarnReturn<Curve>(null, $"Failed to get curve id {id}");

            // Load the curve if needed
            if (record.Curve == null)
            {
                string filePath = $"Calligraphy/{GameDatabase.GetCurveName(id)}";
                using (MemoryStream ms = PakFileSystem.Instance.LoadFromPak(filePath, PakFileId.Calligraphy))
                    record.Curve = new(ms, id);
            }
            
            return record.Curve;
        }

        public class CurveRecord
        {
            public Curve Curve { get; set; }
            public CurveRecordFlags Flags { get; set; }
        }
    }
}
