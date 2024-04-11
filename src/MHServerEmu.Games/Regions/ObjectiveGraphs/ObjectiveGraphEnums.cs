using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Regions.ObjectiveGraphs
{
    [AssetEnum((int)Off)]
    public enum ObjectiveGraphMode         // Regions/EnumObjectiveGraphMode.type
    {
        Off,
        PathDistance,
        PathNavi,
    }

    public enum ObjectiveGraphType
    {
        Invalid = 0,
        Objective = 1,
        Avatar = 2,
        Layout = 3,
        Portal = 4
    }
}
