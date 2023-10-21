namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class ReplacementDirectory
    {
        private Dictionary<ulong, ReplacementRecord> _replacementDict = new();

        public int RecordCount { get => _replacementDict.Count; }

        public void AddReplacementRecord(ulong oldGuid, ulong newGuid, string name)
        {
            ReplacementRecord record = new(oldGuid, newGuid, name);
            _replacementDict.Add(oldGuid, record);
        }

        public ReplacementRecord GetReplacementRecord(ulong guid)
        {
            if (_replacementDict.TryGetValue(guid, out ReplacementRecord record))
                return record;

            return null;
        }
    }

    public class ReplacementRecord
    {
        public ulong OldGuid { get; }
        public ulong NewGuid { get; }
        public string Name { get; }

        public ReplacementRecord(ulong oldGuid, ulong newGuid, string name)
        {
            OldGuid = oldGuid;
            NewGuid = newGuid;
            Name = name;
        }
    }
}
