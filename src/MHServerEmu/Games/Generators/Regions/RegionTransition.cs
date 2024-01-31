using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Regions
{
    public class ConnectionNodeList : List<TargetObject> {}
    public class TargetObject
    {
        public PrototypeGuid Entity { get; set; }
        public PrototypeId Area { get; set; }
        public PrototypeId Cell { get; set; }
        public PrototypeId TargetId { get; set; }
    }

    public class RegionTransition
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public RegionTransition() { }


        public static ConnectionNodeList BuildConnectionEdges(PrototypeId region)
        {
            var nodes = new ConnectionNodeList();

            HashSet<PrototypeId> regions = new (){region};
            RegionPrototype regionProto = GameDatabase.GetPrototype<RegionPrototype>(region);
            /* while (regionProto.ParentDataRef != (PrototypeId)1677652504589371837)
            {
                regions.Add(regionProto.ParentDataRef);
                regionProto = GameDatabase.GetPrototype<RegionPrototype>(regionProto.ParentDataRef);
            }      */      

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
                        if (regions.Contains(origin.Region))
                        {
                            //Logger.Debug($"[{GameDatabase.GetFormattedPrototypeName(origin.Area)}] {GameDatabase.GetFormattedPrototypeName(origin.Entity)} [{GameDatabase.GetFormattedPrototypeName(target.Area)}]");
                            nodes.Add(new TargetObject
                            {
                                Area = origin.Area,
                                Cell = GameDatabase.GetDataRefByAsset(origin.Cell),
                                Entity = GameDatabase.GetPrototypeGuid(origin.Entity),
                                TargetId = proto.Target
                            });
                        }
                        if (proto.Type == RegionTransitionDirectionality.BiDirectional && regions.Contains(target.Region))
                        {
                            ///Logger.Debug($"[{GameDatabase.GetFormattedPrototypeName(target.Area)}] {GameDatabase.GetFormattedPrototypeName(target.Entity)} [{GameDatabase.GetFormattedPrototypeName(origin.Area)}] ");
                            nodes.Add(new TargetObject
                            {                                
                                Area = target.Area,
                                Cell = GameDatabase.GetDataRefByAsset(target.Cell),
                                Entity = GameDatabase.GetPrototypeGuid(target.Entity),
                                TargetId = proto.Origin
                            });
                        }

                    }
                }
            }

            return nodes;
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

        public static bool GetDestination(WaypointPrototype waypointProto, out RegionConnectionTargetPrototype target)
        {            
            target = null;
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
