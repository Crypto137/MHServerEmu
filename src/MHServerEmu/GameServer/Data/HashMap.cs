using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Data
{
    public class HashMap
    {
        public Dictionary<ulong, string> ForwardDict { get; } = new();
        public Dictionary<string, ulong> ReverseDict { get; } = new();

        public void Add(ulong key, string value)
        {
            ForwardDict.Add(key, value);
            ReverseDict.Add(value, key);
        }

        public string GetForward(ulong key) => ForwardDict[key];
        public ulong GetReverse(string value) => ReverseDict[value];
    }
}
