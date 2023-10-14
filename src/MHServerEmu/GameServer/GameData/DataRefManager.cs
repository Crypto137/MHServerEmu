namespace MHServerEmu.GameServer.GameData
{
    public class DataRefManager
    {
        private readonly Dictionary<ulong, string> _referenceDict = new();

        public void AddDataRef(ulong value, string name)
        {
            _referenceDict.Add(value, name);
        }

        public ulong GetDataRefByName(string name)
        {
            foreach (var kvp in _referenceDict)
                if (kvp.Value == name) return kvp.Key;

            return 0;
        }

        public string GetReferenceName(ulong id)
        {
            if (_referenceDict.TryGetValue(id, out string name) == false)
                return string.Empty;

            return name;
        }        
    }
}
