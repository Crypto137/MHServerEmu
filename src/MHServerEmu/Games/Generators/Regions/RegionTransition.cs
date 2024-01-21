using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Regions
{
    public class RegionTransition
    {
        public RegionTransition() { }

        public static bool GetRequiredTransitionData(ulong regionRef, ulong areaRef, out List<RegionTransitionSpec> specList)
        {
            specList = new ();

            IEnumerable<Prototype> iterateProtos = GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(RegionConnectionNodePrototype), 2 | 4);

            // ulong InvalidPrototype = 0;  This variable has no logic!!!
            bool found = false;

            foreach (var itrProto in iterateProtos) // they check ALL prototypes... TODO: Rewrite!!!
            {
                if (itrProto is RegionConnectionNodePrototype proto)
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

        public static bool GetDestination(ulong waypointDataRef, out RegionConnectionTargetPrototype target)
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
        public ulong Cell; // assetRef
        public ulong Entity;
        public bool Start;

        public RegionTransitionSpec() { }

        public RegionTransitionSpec(ulong cell, ulong entity, bool start)
        {
            Cell = cell;
            Entity = entity;
            Start = start;
        }

        public ulong GetCellRef()
        {
            return GameDatabase.GetDataRefByAsset(Cell);
        }
    }
}
