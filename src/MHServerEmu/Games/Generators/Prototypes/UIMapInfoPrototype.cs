using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class HUDEntitySettingsPrototype : Prototype
    {
        public int FloorEffect;
        public int OverheadIcon;
        public ulong MapIcon;
        public ulong EdgeIcon;

        public HUDEntitySettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HUDEntitySettingsPrototype), proto); }
    }

}
