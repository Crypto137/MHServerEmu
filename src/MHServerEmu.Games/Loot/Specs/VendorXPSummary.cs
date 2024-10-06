using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Loot.Specs
{
    public readonly struct VendorXPSummary
    {
        public PrototypeId VendorProtoRef { get; }
        public int XPAmount { get; }

        public VendorXPSummary(PrototypeId vendorProtoRef, int xpAmount)
        {
            VendorProtoRef = vendorProtoRef;
            XPAmount = xpAmount;
        }

        public NetStructVendorXPSummary ToProtobuf()
        {
            return NetStructVendorXPSummary.CreateBuilder()
                .SetVendorProtoRef((ulong)VendorProtoRef)
                .SetXpAmount((uint)XPAmount)
                .Build();
        }

        public override string ToString()
        {
            return $"vendorProtoRef={VendorProtoRef.GetName()}, xpAmount={XPAmount}";
        }
    }
}
