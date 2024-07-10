using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

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
        private static readonly Logger Logger = LogManager.CreateLogger();

        public RegionTransition() { }

        public static bool FindStartPosition(Region region, PrototypeId targetRef, out Vector3 targetPos, out Orientation targetRot)
        {
            targetPos = region.StartArea.RegionBounds.Center; // default
            targetRot = Orientation.Zero;
            RegionConnectionTargetPrototype targetDest = null;

            // Fall back to default start target for the region
            if (targetRef == PrototypeId.Invalid)
            {
                targetRef = region.RegionPrototype.StartTarget;
                Logger.Warn($"FindStartPosition(): invalid targetRef, falling back to {GameDatabase.GetPrototypeName(targetRef)}");
            }

            Prototype targetProto = GameDatabase.GetPrototype<Prototype>(targetRef);

            if (targetProto is WaypointPrototype waypointProto)
            {
                if (GetDestination(waypointProto, out RegionConnectionTargetPrototype targetDestination))
                {
                    targetRef = targetDestination.Entity;
                    targetDest = targetDestination;
                }
                else return false;
            }
            else if (targetProto is RegionConnectionTargetPrototype targetDestination)
            {
                targetRef = targetDestination.Entity;
                targetDest = targetDestination;
            }

            if (targetDest != null && region.FindTargetPosition(ref targetPos, ref targetRot, targetDest))
            {
                var teleportEntity = GameDatabase.GetPrototype<TransitionPrototype>(targetRef);
                if (teleportEntity != null && teleportEntity.SpawnOffset > 0) teleportEntity.CalcSpawnOffset(ref targetRot, ref targetPos);
                return true;
            }
            return false;
        }

        public static TargetObject GetTargetNode(ConnectionNodeList targets, PrototypeId area, PrototypeId cell, PrototypeGuid entity)
        {
            foreach (var targetNode in targets)
            {
                if (targetNode.Entity == entity
                    && (targetNode.Area == PrototypeId.Invalid || targetNode.Area == area)
                    && (targetNode.Cell == PrototypeId.Invalid || targetNode.Cell == cell))
                    return targetNode;
            }
            return null;
        }

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

        public static bool GetDestination(WaypointPrototype waypointProto, out RegionConnectionTargetPrototype target)
        {
            target = null;
            if (waypointProto == null || waypointProto.Destination == 0) return false;
            target = waypointProto.Destination.As<RegionConnectionTargetPrototype>();
            return target != null;
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
