using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions.POIPickers
{
    public class RegionPOIPickerSpider
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly POISpiderNode _root;
        private readonly PrototypeId _poiGroupProto;

        public RegionPOIPickerSpider(PrototypeId groupProto)
        {
            _poiGroupProto = groupProto;
            _root = null;

            RequiredPOIGroupPrototype poiPicker = GameDatabase.GetPrototype<RequiredPOIGroupPrototype>(groupProto);
            if (poiPicker != null && poiPicker.RequiredCells.HasValue())
            {
                _root = new(null, null);
                _root.AddChildrenFromList(poiPicker.RequiredCells);
            }
        }

        public int GetAreaPicks(Area area)
        {
            if (area == null) return 0;

            RequiredPOIGroupPrototype proto = GameDatabase.GetPrototype<RequiredPOIGroupPrototype>(_poiGroupProto);
            if (proto != null && proto.Areas.HasValue())
            {
                foreach (var entry in proto.Areas)
                    if (entry != null && entry.Area == area.PrototypeDataRef) return entry.Picks;
            }

            return 0;
        }

        public bool GetCellsForArea(Area area, Picker<POISpiderNode> picker, List<Prototype> list)
        {
            if (area == null) return false;

            int picks = GetAreaPicks(area);
            for (int pick = 0; pick < picks; ++pick)
            {
                POISpiderNode node = _root.PickNode(picker, area);
                if (node == null)
                    Logger.ErrorReturn(false, $"GetCellsForArea(): Area failed to resolve its Points of Interest.");

                list.Add(node.Prototype);
            }

            return true;
        }

        public void DereferenceArea(Area area)
        {
            _root.DereferenceArea(area);
        }

        public PrototypeId GetRef()
        {
            return _poiGroupProto;
        }
    }
}
