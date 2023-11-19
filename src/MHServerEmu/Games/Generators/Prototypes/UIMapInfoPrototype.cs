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

    public class ObjectiveInfoPrototype : Prototype
    {
        public ulong EdgeColor;
        public bool EdgeEnabled;
        public int EdgeRange;
        public bool EdgeOnlyInArea;
        public bool FloorRingAnimation;
        public bool MapEnabled;
        public int MapRange;
        public bool ShowToSummonerOnly;
        public bool TrackAfterDiscovery;
        public ObjectiveVisibility Visibility;

        public ObjectiveInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ObjectiveInfoPrototype), proto); }
    }

    public enum ObjectiveVisibility
    {
	    VisibleOnlyByMission,
	    VisibleWhenFound,
	    VisibleAlways,
	    VisibleToParty,
    }

}
