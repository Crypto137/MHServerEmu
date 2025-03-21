using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions.POIPickers
{
    public class RegionPOIPickerCollection
    {
        private readonly List<RegionPOIPickerSpider> _poiGroups;

        public RegionPOIPickerCollection(RegionGeneratorPrototype regionGenerator)
        {
            _poiGroups = new();

            if (regionGenerator != null && regionGenerator.POIGroups.HasValue())
                foreach (var group in regionGenerator.POIGroups) RegisterPOIGroup(group);
        }

        public void RegisterPOIGroup(PrototypeId groupProto)
        {
            if (groupProto == PrototypeId.Invalid) return;

            foreach (RegionPOIPickerSpider group in _poiGroups)
            {
                if (group.GetRef() == groupProto)
                    return;
            }

            RegionPOIPickerSpider spider = new(groupProto);
            _poiGroups.Add(spider);
        }

        public bool GetCellsForArea(Area area, GRandom random, List<Prototype> list)
        {
            bool ret = true;

            if (_poiGroups.Count > 0)
            {
                Picker<POISpiderNode> poiPicker = new(random);
                foreach (var spider in _poiGroups)
                {
                    poiPicker.Clear();
                    ret &= spider.GetCellsForArea(area, poiPicker, list);
                }
            }

            return ret;
        }

        public void DereferenceArea(Area area)
        {
            if (_poiGroups.Count > 0)
            {
                foreach (var spider in _poiGroups)
                    spider.DereferenceArea(area);
            }
        }
    }
}
