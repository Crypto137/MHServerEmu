using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public enum CalligraphyValueType : byte
    {
        A = 0x41,   // asset
        B = 0x42,   // bool
        C = 0x43,   // curve
        D = 0x44,   // double
        L = 0x4c,   // long
        P = 0x50,   // prototype
        R = 0x52,   // ??? (recursion?)
        S = 0x53,   // string
        T = 0x54    // type
    }

    public enum CalligraphyContainerType : byte
    {
        L = 0x4c,   // list (A P R T only)
        S = 0x53    // single
    }
}
