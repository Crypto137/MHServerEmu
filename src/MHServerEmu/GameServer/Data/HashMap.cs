using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Data
{
    public class HashMap
    {
        private Dictionary<ulong, string> _forwardDict { get; } = new();
        private Dictionary<string, ulong> _reverseDict { get; } = new();

        public int Count { get => _forwardDict.Count; }

        public void Add(ulong key, string value)
        {
            _forwardDict.Add(key, value);
            _reverseDict.Add(value, key);
        }

        public string GetForward(ulong key) => _forwardDict[key];
        public ulong GetReverse(string value) => _reverseDict[value];
    }
}
