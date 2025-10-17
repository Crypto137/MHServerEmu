using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.DRAG.Generators.Regions
{
    public class ConnectionNodeList : List<TargetObject> { }

    public class TargetObject
    {
        public PrototypeGuid Entity { get; set; }
        public PrototypeId Area { get; set; }
        public PrototypeId Cell { get; set; }
        public PrototypeId TargetId { get; set; }
    }

    public class RegionTransition
    {
        public RegionTransition() { }

        public static ConnectionNodeList BuildConnectionEdges(PrototypeId region)
        {
            var nodes = new ConnectionNodeList();

            void AddTargetNode(RegionConnectionTargetPrototype target, RegionConnectionTargetPrototype origin)
            {
                //Logger.Debug($"[{GameDatabase.GetFormattedPrototypeName(origin.Area)}] {GameDatabase.GetFormattedPrototypeName(origin.Entity)} [{GameDatabase.GetFormattedPrototypeName(target.Area)}]");
                nodes.Add(new TargetObject
                {
                    Area = origin.Area,
                    Cell = GameDatabase.GetDataRefByAsset(origin.Cell),
                    Entity = GameDatabase.GetPrototypeGuid(origin.Entity),
                    TargetId = target.DataRef
                });
            }

            RegionPrototype regionProto = GameDatabase.GetPrototype<RegionPrototype>(region);

            foreach (var protoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<RegionConnectionNodePrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var proto = protoRef.As<RegionConnectionNodePrototype>();
                if (proto != null)
                {
                    var target = proto.Target.As<RegionConnectionTargetPrototype>();
                    var origin = proto.Origin.As<RegionConnectionTargetPrototype>();

                    if (origin != null && target != null)
                    {
                        var originRegion = origin.Region.As<RegionPrototype>();
                        if (RegionPrototype.Equivalent(originRegion, regionProto))
                        {
                            AddTargetNode(target, origin);
                        }

                        var targetRegion = target.Region.As<RegionPrototype>();
                        if (proto.Type == RegionTransitionDirectionality.BiDirectional
                            && RegionPrototype.Equivalent(targetRegion, regionProto))
                        {
                            AddTargetNode(origin, target);
                        }
                    }
                }
            }

            return nodes;
        }

        public static bool GetRequiredTransitionData(PrototypeId regionRef, PrototypeId areaRef, ref List<RegionTransitionSpec> specList)
        {
            // ulong InvalidPrototype = 0;  This variable has no logic!!!
            bool found = false;

            // they check ALL prototypes... TODO: Rewrite!!!
            foreach (var protoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<RegionConnectionNodePrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                RegionConnectionNodePrototype proto = protoRef.As<RegionConnectionNodePrototype>();
                if (proto != null)
                {
                    var origin = proto.Origin.As<RegionConnectionTargetPrototype>();
                    var target = proto.Target.As<RegionConnectionTargetPrototype>();

                    if (origin != null && target != null)
                    {
                        if (regionRef == origin.Region && areaRef == origin.Area)
                        {
                            bool start = false; // (proto.Type == RegionTransitionDirectionality.BiDirectional && InvalidPrototype != 0 && target.Region == InvalidPrototype);
                            RegionTransitionSpec spec = new(origin.Cell, origin.Entity, start);

                            if (!specList.Contains(spec) && spec.Cell != 0)
                            {
                                // Logger.Debug($"[{GameDatabase.GetFormattedPrototypeName(origin.Area)}] {GameDatabase.GetFormattedPrototypeName(origin.Entity)}");
                                specList.Add(spec);
                                found = true;
                            }
                        }

                        else if (proto.Type == RegionTransitionDirectionality.BiDirectional && regionRef == target.Region && areaRef == target.Area)
                        {
                            bool start = false; // (InvalidPrototype != 0 && origin.Region == InvalidPrototype);
                            RegionTransitionSpec spec = new(target.Cell, target.Entity, start);

                            if (!specList.Contains(spec) && spec.Cell != 0)
                            {
                                // Logger.Debug($"[{GameDatabase.GetFormattedPrototypeName(target.Area)}] {GameDatabase.GetFormattedPrototypeName(target.Entity)}");
                                specList.Add(spec);
                                found = true;
                            }
                        }
                    }
                }
            }

            return found;
        }
    }

    public class RegionTransitionSpec
    {
        public AssetId Cell;
        public PrototypeId Entity;
        public bool Start;

        public RegionTransitionSpec() { }

        public RegionTransitionSpec(AssetId cell, PrototypeId entity, bool start)
        {
            Cell = cell;
            Entity = entity;
            Start = start;
        }

        public PrototypeId GetCellRef()
        {
            return GameDatabase.GetDataRefByAsset(Cell);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cell);
        }

        public override bool Equals(object obj)
        {
            if (obj is not RegionTransitionSpec other) return false;
            return Cell == other.Cell;
        }
    }
}
