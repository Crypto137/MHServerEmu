using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public enum CalligraphyValueType : byte
    {
        A = 0x41,   // asset?
        B = 0x42,
        C = 0x43,   // curve?
        D = 0x44,
        L = 0x4c,
        P = 0x50,   // prototype?
        R = 0x52,
        S = 0x53,
        T = 0x54    // type?
    }

    public enum CalligraphyContainerType : byte
    {
        L = 0x4c,   // list?
        S = 0x53    // single?
    }
}
