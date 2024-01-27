using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Regions
{
    public class RegionTransition
    {
        public RegionTransition() { }

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
