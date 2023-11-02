using MHServerEmu.Games.Generators.Prototypes;

namespace MHServerEmu.Games.Generators.Regions
{
    public class POIPickerSpider
    {
        public POIPickerSpider() { }
    }

    public class RegionPOIPickerCollection
    {
        public RegionPOIPickerCollection(RegionGeneratorPrototype regionGenerator)
        {
            if (regionGenerator != null && regionGenerator.POIGroups != null)
            {
                foreach (var group in regionGenerator.POIGroups)
                {
                    RegisterPOIGroup(group);
                }
            }
        }
        public void RegisterPOIGroup(ulong group) { 
            // TODO
        }
    }
}
