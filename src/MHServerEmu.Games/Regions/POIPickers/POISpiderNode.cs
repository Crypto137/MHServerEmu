using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions.POIPickers
{
    public class POISpiderNode
    {
        public RequiredCellBasePrototype Prototype;
        public List<Area> Reference;
        public List<POISpiderNode> Children;
        public POISpiderNode Parent;

        public POISpiderNode(RequiredCellBasePrototype protoNode, POISpiderNode parent)
        {
            Prototype = protoNode;
            Parent = parent;
            Children = new();
            Reference = new();

            if (IsList() && Prototype is RequiredCellBaseListPrototype baseList && baseList.RequiredCells.HasValue())
                AddChildrenFromList(baseList.RequiredCells);
        }

        public void AddChildrenFromList(RequiredCellBasePrototype[] cells)
        {
            foreach (RequiredCellBasePrototype cell in cells)
                AddChild(cell);
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
                foreach (var node in Children)
                {
                    if (node != null && node.IsAvailable())
                        picker.Add(node);
                }

                if (picker.Empty() == false && picker.Pick(out POISpiderNode pickedNode))
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
            if (Reference.Contains(area))
                return;
            
            if (Prototype != null)
                Reference.Add(area);
            
            if (Parent != null && Parent.IsList())
                Parent.ReferenceArea(area);
        }

        public void DereferenceArea(Area area)
        {
            Reference.RemoveAll(a => a == area);
            foreach (POISpiderNode node in Children)
                node.DereferenceArea(area);
        }

        public void AddChild(RequiredCellBasePrototype protoNode)
        {
            if (protoNode != null)
                Children.Add(new(protoNode, this));
        }
    }
}
