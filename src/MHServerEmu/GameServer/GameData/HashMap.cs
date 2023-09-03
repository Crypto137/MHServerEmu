using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.GameData
{
    public class HashMap
    {
        private Dictionary<ulong, string> _forwardDict { get; } = new();
        private Dictionary<string, ulong> _reverseDict { get; } = new();

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
        public ulong GetReverse(string value) => _reverseDict[value];

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
