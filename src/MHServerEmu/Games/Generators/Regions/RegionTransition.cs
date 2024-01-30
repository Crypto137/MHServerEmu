using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Regions
{
    public class ConnectionNodeDict : Dictionary<PrototypeId, Dictionary<PrototypeGuid, PrototypeId>>
    {

    }

    public struct TargetObject
    {
        public PrototypeGuid Entity { get; set; }
        public PrototypeId Area { get; set; }
        public PrototypeId TargetId { get; set; }
    }

    public class RegionTransition
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public RegionTransition() { }


        public static ConnectionNodeDict BuildConnectionEdges(PrototypeId region)
        {
            var items = new ConnectionNodeDict();
            var nodes = new List<TargetObject>();

            var iterateProtos = GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(RegionConnectionNodePrototype), PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly);
            foreach (var itrProtoId in iterateProtos)
            {
                RegionConnectionNodePrototype proto = GameDatabase.GetPrototype<RegionConnectionNodePrototype>(itrProtoId);
                if (proto != null)
                {
                    var target = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(proto.Target);
                    var origin = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(proto.Origin);

                    if (origin != null && target != null)
                    {
                        if (region == origin.Region)
                        {
                            Logger.Debug($"[{GameDatabase.GetFormattedPrototypeName(origin.Area)}] {GameDatabase.GetFormattedPrototypeName(origin.Entity)} [{GameDatabase.GetFormattedPrototypeName(target.Area)}]");
                            nodes.Add(new TargetObject
                            {
                                Area = origin.Area,
                                Entity = GameDatabase.GetPrototypeGuid(origin.Entity),
                                TargetId = proto.Target
                            });
                        }
                        if (proto.Type == RegionTransitionDirectionality.BiDirectional && region == target.Region)
                        {
                            Logger.Debug($"[{GameDatabase.GetFormattedPrototypeName(target.Area)}] {GameDatabase.GetFormattedPrototypeName(target.Entity)} [{GameDatabase.GetFormattedPrototypeName(origin.Area)}] ");
                            nodes.Add(new TargetObject
                            {                                
                                Area = target.Area,
                                Entity = GameDatabase.GetPrototypeGuid(target.Entity),
                                TargetId = proto.Origin
                            });
                        }

                    }
                }
            }
            //foreach (var node in nodes) Logger.Warn($"{node.area}, {node.entity}, {node.targetId}"); 

            var groupedNodes = nodes.GroupBy(node => node.Area);
            foreach (var group in groupedNodes)
            {
                var groupItems = new Dictionary<PrototypeGuid, PrototypeId>();

                foreach (var node in group)
                    groupItems[node.Entity] = node.TargetId;

                items[group.Key] = groupItems;
            }

            return items;
        }

        public static bool GetRequiredTransitionData(PrototypeId regionRef, PrototypeId areaRef, ref List<RegionTransitionSpec> specList)
        {
            var iterateProtos = GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(RegionConnectionNodePrototype), PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly);

            // ulong InvalidPrototype = 0;  This variable has no logic!!!
            bool found = false;

            foreach (var itrProtoId in iterateProtos) // they check ALL prototypes... TODO: Rewrite!!!
            {
                RegionConnectionNodePrototype proto = GameDatabase.GetPrototype<RegionConnectionNodePrototype>(itrProtoId);
                if (proto != null)
                {
                    var origin = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(proto.Origin);
                    var target = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(proto.Target);

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

        public static bool GetDestination(PrototypeId waypointDataRef, out RegionConnectionTargetPrototype target)
        {            
            target = null;
            if (waypointDataRef == 0) return false;
            WaypointPrototype waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(waypointDataRef);
            if (waypointProto == null || waypointProto.Destination == 0) return false;
            target = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(waypointProto.Destination);
            return (target != null);
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
