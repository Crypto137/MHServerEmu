using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions
{
    public class POISpiderNode
    {
        public RequiredCellBasePrototype Prototype;
        public List<Area> Reference;
        public List<POISpiderNode> Childrens;
        public POISpiderNode Parent;

        public POISpiderNode(RequiredCellBasePrototype protoNode, POISpiderNode parent)
        {
            Prototype = protoNode;
            Parent = parent;
            Childrens = new();
            Reference = new();

            if (IsList() && Prototype is RequiredCellBaseListPrototype baseList && baseList.RequiredCells.HasValue())
                AddChildrenFromList(baseList.RequiredCells);
        }

        public void AddChildrenFromList(RequiredCellBasePrototype[] cells)
        {
            foreach (var cell in cells) AddChild(cell);
        }

        public bool IsList()
        {
            return Prototype != null && Prototype is RequiredCellBaseListPrototype;
        }

        public bool IsAvailable()
        {
            return Prototype != null && Reference.Count == 0;
        }

        public POISpiderNode PickNode(Picker<POISpiderNode> picker, Area area)
        {
            if (Prototype == null || IsList())
            {
                picker.Clear();
                foreach (var node in Childrens)
                {
                    if (node != null && node.IsAvailable())
                        picker.Add(node);
                }

                if (!picker.Empty() && picker.Pick(out POISpiderNode pickedNode))
                {
                    if (pickedNode.IsList())
                    {
                        return pickedNode.PickNode(picker, area);
                    }
                    else
                    {
                        pickedNode.ReferenceArea(area);
                        return pickedNode;
                    }
                }
            }
            return null;
        }

        public void ReferenceArea(Area area)
        {
            if (Reference.Contains(area)) return;
            if (Prototype != null) Reference.Add(area);
            if (Parent != null && Parent.IsList()) Parent.ReferenceArea(area);
        }

        public void DereferenceArea(Area area)
        {
            Reference.RemoveAll(a => a == area);
            foreach (var node in Childrens) node.DereferenceArea(area);
        }

        public void AddChild(RequiredCellBasePrototype protoNode)
        {
            if (protoNode != null) Childrens.Add(new(protoNode, this));
        }

    }

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
                {
                    Logger.Error($"Area failed to resolve its Points of Interest.");
                    return false;
                }

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
            if (groupProto == 0) return;

            foreach (var group in _poiGroups)
                if (group.GetRef() == groupProto) return;

            RegionPOIPickerSpider spider = new(groupProto);
            _poiGroups.Add(spider);
        }

        public bool GetCellsForArea(Area area, GRandom random, List<Prototype> list)
        {
            bool ret = true;

            if (_poiGroups.Any())
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
            if (_poiGroups.Any())
                foreach (var spider in _poiGroups) spider.DereferenceArea(area);
        }

    }
}
