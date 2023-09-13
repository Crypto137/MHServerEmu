namespace MHServerEmu.GameServer.GameData
{
    public class HashMap
    {
        private readonly Dictionary<ulong, string> _forwardDict;
        private readonly Dictionary<string, ulong> _reverseDict;

        public int Count { get => _forwardDict.Count; }

        public HashMap()
        {
            _forwardDict = new();
            _reverseDict = new();
        }

        public HashMap(int capacity)
        {
            _forwardDict = new(capacity);
            _reverseDict = new(capacity);
        }

        public void Add(ulong key, string value)
        {
            _forwardDict.Add(key, value);
            _reverseDict.Add(value, key);
        }

        public string GetForward(ulong key) => _forwardDict[key];
        public bool TryGetForward(ulong key, out string value) => _forwardDict.TryGetValue(key, out value);
        public ulong GetReverse(string value) => _reverseDict[value];
        public bool TryGetReverse(string value, out ulong key) => _reverseDict.TryGetValue(value, out key);

        public ulong[] Enumerate()
        {
            ulong[] enumValues = new ulong[_forwardDict.Count];

            int i = 0;
            foreach (ulong key in _forwardDict.Keys)
            {
                enumValues[i] = key;
                i++;
            }

            Array.Sort(enumValues);

            return enumValues;
        }
    }
}
