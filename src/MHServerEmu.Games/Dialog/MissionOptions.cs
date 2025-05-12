using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;

namespace MHServerEmu.Games.Dialog
{
    public struct EntityObjectiveInfo
    {
        public PrototypeId MissionRef;
        public MissionState MissionState;
        public MissionObjectiveState ObjectiveState;
        public sbyte ObjectiveIndex;
        public BaseMissionOption Option;
        public PlayerHUDEnum Flags;

        public EntityObjectiveInfo(PrototypeId missionRef, MissionState missionState, MissionObjectiveState objectiveState, byte objectiveIndex,
            BaseMissionOption option, PlayerHUDEnum flags)
        {
            MissionRef = missionRef;
            MissionState = missionState;
            ObjectiveState = objectiveState;
            ObjectiveIndex = (sbyte)objectiveIndex;
            Option = option;
            Flags = flags;
        }
    }

    public class BaseMissionOption : InteractionOption
    {
        public MissionPrototype MissionProto { get; private set; }
        public MissionStateFlags MissionState { get; private set; }
        public sbyte ObjectiveIndex { get; private set; }
        public MissionObjectiveStateFlags ObjectiveState { get; private set; }
        public HashSet<PrototypeId> InterestRegions { get; private set; }
        public HashSet<PrototypeId> InterestAreas { get; private set; }
        public HashSet<PrototypeId> InterestCells { get; private set; }
        public bool HasObjective => ObjectiveIndex != -1;

        public BaseMissionOption()
        {
            MissionState = 0;
            InterestRegions = new();
            InterestAreas = new();
            InterestCells = new();
            ObjectiveIndex = -1;
            ObjectiveState = 0;
            Priority = 10;
        }

        public virtual void InitializeForMission(MissionPrototype missionProto, MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            MissionProto = missionProto;
            MissionState = state;
            ObjectiveIndex = objectiveIndex;
            ObjectiveState = objectiveState;
            OptionType = optionType;

            EntityFilterWrapper.FilterContextMissionRef = missionProto.DataRef;
        }

        public Mission GetMission(Player player)
        {
            MissionManager missionManger = MissionManager.FindMissionManagerForMission(player, player.GetRegion(), MissionProto.DataRef);
            return missionManger?.FindMissionByDataRef(MissionProto.DataRef);
        }

        public MissionObjective GetObjective(Mission mission)
        {
            if (mission == null) return null;
            if (MissionProto.DataRef == mission.PrototypeDataRef)
                return mission.GetObjectiveByPrototypeIndex((byte)ObjectiveIndex);
            return null;
        }

        public bool IsActiveForMissionAndEntity(Mission mission, WorldEntity interactee)
        {
            bool isActive = false;

            if (HasObjective == false)
            {
                MissionStateFlags missionState = (MissionStateFlags)(1 << (int)mission.State);
                if (MissionState.HasFlag(missionState))
                    isActive = true;
            }
            else
            {
                MissionObjective objective = GetObjective(mission);
                if (objective != null)
                {
                    MissionObjectiveState objectiveState = objective.State;
                    if (objectiveState == MissionObjectiveState.Active && interactee != null && objective.HasInteractedWithEntity(interactee))
                        objectiveState = MissionObjectiveState.Completed;

                    if (ObjectiveState.HasFlag((MissionObjectiveStateFlags)(1 << (int)objectiveState)))
                        isActive = true;
                }
                else
                {
                    if (mission.State == Missions.MissionState.Completed && ObjectiveState.HasFlag(MissionObjectiveStateFlags.Completed))
                        isActive = true;
                }
            }

            if (interactee != null && isActive)
                return EntityFilterWrapper.EvaluateEntity(interactee);

            return isActive;
        }

        public void SetInteractDataObjectiveFlags(Player player, ref InteractData outInteractData, Mission mission, BaseMissionOption option)
        {
            if (MissionProto == null) return;

            var flags = PlayerHUDEnum.HasObjectives;

            MissionObjectivePrototype objectiveProto;
            MissionObjective objective = null;
            if (HasObjective) objective = GetObjective(mission);

            if (option is MissionConditionMissionCompleteOption completeOption)
            {
                if (completeOption.ObjectiveFlagsAllowed() == false) return;

                if (GetMissionAndObjective(player, out Mission completeMission, out MissionObjective completeObjective))
                {
                    var completeProto = completeOption.Proto as MissionConditionMissionCompletePrototype;
                    switch (completeProto.ShowObjs)
                    {
                        case MissionShowObjsSettings.FromThisMission:
                            objectiveProto = completeObjective?.Prototype;
                            flags |= GetPlayerHUDFlags(completeMission.Prototype, objectiveProto);
                            outInteractData.PlayerHUDFlags |= flags;
                            outInteractData.PlayerHUDArrowDistanceOverride = Math.Max(outInteractData.PlayerHUDArrowDistanceOverride, GetPlayerHUDArrowDistanceOverride(objectiveProto));
                            outInteractData.InsertMissionObjective(completeMission, completeObjective, option, flags);
                            break;

                        case MissionShowObjsSettings.FromTargetMission:
                            objectiveProto = objective?.Prototype;
                            if (this is not MissionHintOption && objectiveProto != null && objectiveProto.ObjectiveHints.HasValue()) return;
                            flags |= GetPlayerHUDFlags(mission.Prototype, objectiveProto);
                            outInteractData.PlayerHUDFlags |= flags;
                            outInteractData.PlayerHUDArrowDistanceOverride = Math.Max(outInteractData.PlayerHUDArrowDistanceOverride, GetPlayerHUDArrowDistanceOverride(objectiveProto));
                            outInteractData.InsertMissionObjective(completeMission, completeObjective, option, flags);
                            break;

                        case MissionShowObjsSettings.SuppressAllObjs:
                            break;
                    }
                }
                return;
            }

            if (ObjectiveFlagsAllowed() == false) return;

            objectiveProto = objective?.Prototype;
            if (this is not MissionHintOption && objectiveProto != null && objectiveProto.ObjectiveHints.HasValue()) return;
            flags |= GetPlayerHUDFlags(mission.Prototype, objectiveProto);
            outInteractData.PlayerHUDFlags |= flags;
            outInteractData.PlayerHUDArrowDistanceOverride = Math.Max(outInteractData.PlayerHUDArrowDistanceOverride, GetPlayerHUDArrowDistanceOverride(objectiveProto));
            outInteractData.InsertMissionObjective(mission, objective, this, flags);
        }

        private static int GetPlayerHUDArrowDistanceOverride(MissionObjectivePrototype objectiveProto)
        {
            if (objectiveProto != null) return objectiveProto.PlayerHUDObjectiveArrowDistOvrde;
            return 0;
        }

        private PlayerHUDEnum GetPlayerHUDFlags(MissionPrototype missionProto, MissionObjectivePrototype objectiveProto)
        {
            PlayerHUDEnum flags = PlayerHUDEnum.None;
            if (missionProto == null) return flags; 

            if (this is MissionHintOption)
            {
                flags |= PlayerHUDEnum.Hint;
                flags |= PlayerHUDEnum.ShowObjs;
                flags |= PlayerHUDEnum.ShowObjsOnMap;
                flags |= PlayerHUDEnum.ShowObjsOnScreenEdge;
            }
            else if (missionProto.PlayerHUDShowObjs)
            {
                flags |= PlayerHUDEnum.ShowObjs;

                if (missionProto.PlayerHUDShowObjsOnMap)
                    flags |= PlayerHUDEnum.ShowObjsOnMap;
                if (missionProto.PlayerHUDShowObjsOnMapNoPing)
                    flags |= PlayerHUDEnum.ShowObjsOnMapNoPing;
                if (missionProto.PlayerHUDShowObjsOnScreenEdge)
                    flags |= PlayerHUDEnum.ShowObjsOnScreenEdge;
                if (missionProto.PlayerHUDShowObjsOnEntityFloor)
                    flags |= PlayerHUDEnum.ShowObjsOnEntityFloor;
                if (missionProto.PlayerHUDShowObjsOnEntityAbove)
                    flags |= PlayerHUDEnum.ShowObjsEntityAbove;

                if (objectiveProto != null)
                {
                    if (flags.HasFlag(PlayerHUDEnum.ShowObjsOnMap) && !objectiveProto.PlayerHUDShowObjsOnMap)
                        flags &= ~PlayerHUDEnum.ShowObjsOnMap;
                    if (flags.HasFlag(PlayerHUDEnum.ShowObjsOnMapNoPing) && !objectiveProto.PlayerHUDShowObjsOnMapNoPing)
                        flags &= ~PlayerHUDEnum.ShowObjsOnMapNoPing;
                    if (flags.HasFlag(PlayerHUDEnum.ShowObjsOnScreenEdge) && !objectiveProto.PlayerHUDShowObjsOnScreenEdge)
                        flags &= ~PlayerHUDEnum.ShowObjsOnScreenEdge;
                    if (flags.HasFlag(PlayerHUDEnum.ShowObjsOnEntityFloor) && !objectiveProto.PlayerHUDShowObjsOnEntityFloor)
                        flags &= ~PlayerHUDEnum.ShowObjsOnEntityFloor;
                    if (flags.HasFlag(PlayerHUDEnum.ShowObjsEntityAbove) && !objectiveProto.PlayerHUDShowObjsOnEntityAbove)
                        flags &= ~PlayerHUDEnum.ShowObjsEntityAbove;
                }
            }

            return flags;
        }

        public bool GetMissionAndObjective(Player player, out Mission mission, out MissionObjective objective)
        {
            mission = null;
            objective = null;

            var missionManager = MissionManager.FindMissionManagerForMission(player, player.GetRegion(), MissionProto);
            if (missionManager == null) return false;

            mission = missionManager.FindMissionByDataRef(MissionProto.DataRef);
            if (mission == null) return false;

            if (HasObjective)
            {
                objective = GetObjective(mission);
                if (objective == null) return false;
            }
            else
                objective = null;

            return true;
        }

        public bool ObjectiveFlagsAllowed()
        {
            if (OptionType.HasFlag(MissionOptionTypeFlags.Skip)) return false;

            if (MissionProto != null && OptionType.HasFlag(MissionOptionTypeFlags.ActivateCondition) 
                && MissionProto.PlayerHUDShowObjsNoActivateCond)
                return false;

            return true;
        }

        public bool IsLocationInteresting(Player player, PrototypeId regionRef, PrototypeId areaRef, PrototypeId cellRef)
        {
            if (InterestCells.Count > 0 && InterestCells.Contains(cellRef)) return true;
            if (InterestAreas.Count > 0 && InterestAreas.Contains(areaRef)) return true;
            if (InterestRegions.Count > 0 && InterestRegions.Contains(regionRef))
            {
                var region = player.GetRegion();
                if (region == null) return false;
                return region.PrototypeDataRef != regionRef;
            }
            return false;
        }
    }

    public class MissionHintOption : BaseMissionOption
    {
        public MissionObjectiveHintPrototype Proto { get; set; }  

        public MissionHintOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.Hint;
            EntityTrackingFlags |= EntityTrackingFlag.HUD;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (Proto != null && Proto.TargetEntity != null && Proto.TargetEntity.Evaluate(entity, new(MissionProto.DataRef))) 
            {
                map.Insert(MissionProto.DataRef, EntityTrackingFlag.HUD);
                return EntityTrackingFlag.HUD;                 
            }
            return EntityTrackingFlag.None;
        }
    }

    public class BaseMissionConditionOption : BaseMissionOption
    {
        public MissionConditionPrototype Proto { get; set; }

        public BaseMissionConditionOption()
        {
            EntityTrackingFlags |= EntityTrackingFlag.MissionCondition;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (Proto != null && Proto.NoTrackingOptimization)
                return EntityTrackingFlag.None;

            if (EntityFilterWrapper.EvaluateEntity(entity))
            {
                map.Insert(MissionProto.DataRef, EntityTrackingFlags);
                return EntityTrackingFlags;
            }
            return EntityTrackingFlag.None;
        }

    }

    public class MissionConditionMissionCompleteOption : BaseMissionConditionOption
    {
        public SortedVector<PrototypeId> CompleteMissionRefs { get; } = new();

        public override void InitializeForMission(MissionPrototype missionProto, MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            base.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, optionType);
            if (Proto is not MissionConditionMissionCompletePrototype missionComplete) return;

            PrototypeId missionCompleteRef = missionComplete.MissionPrototype;
            if (missionCompleteRef != PrototypeId.Invalid)
                CompleteMissionRefs.Add(missionCompleteRef);

            if (missionComplete.MissionKeyword != PrototypeId.Invalid)
            {
                foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    MissionPrototype mission = missionRef.As<MissionPrototype>();
                    if (mission != null && mission.Keywords.HasValue() && mission.Keywords.Contains(missionComplete.MissionKeyword))
                        CompleteMissionRefs.Add(missionRef);
                }
            }
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            EntityTrackingFlag trackingFlag = EntityTrackingFlag.None;

            if (checkList.Contains(this))
                return trackingFlag;
            else
                checkList.Add(this);

            var manager = GameDatabase.InteractionManager;
            foreach (var completeMissionRef in CompleteMissionRefs)
            {
                var missionData = manager.GetMissionData(completeMissionRef);
                if (missionData != null)
                    foreach (var option in missionData.Options)
                        trackingFlag |= option.InterestedInEntity(map, entity, checkList);
            }

            if (trackingFlag != EntityTrackingFlag.None)
                map.Insert(MissionProto.DataRef, trackingFlag);

            return trackingFlag;
        }
    }

    public class MissionConditionRegionOption : BaseMissionConditionOption
    {
        public MissionConditionRegionOption()
        {
            EntityTrackingFlags |= EntityTrackingFlag.TransitionRegion;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (entity is Transition transition && InterestRegions.Count > 0)
            {
                for (int i = 0; i < transition.Destinations.Count; i++)
                {
                    TransitionDestination destination = transition.Destinations[i];

                    if (destination.RegionRef != PrototypeId.Invalid && InterestRegions.Contains(destination.RegionRef))
                    {
                        map.Insert(MissionProto.DataRef, EntityTrackingFlags);
                        return EntityTrackingFlags;
                    }
                }
            }
            return EntityTrackingFlag.None;
        }
    }

    public class MissionConditionHotspotOption : BaseMissionConditionOption
    {
        public MissionConditionHotspotOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.ConditionHotspot;
            EntityTrackingFlags |= EntityTrackingFlag.Hotspot;
        }
    }

    public class MissionConditionEntityInteractOption : BaseMissionConditionOption
    {

    }

    public class MissionVisibilityOption : BaseMissionOption
    {
        public EntityVisibilitySpecPrototype Proto { get; set; }

        public MissionVisibilityOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.Visibility;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (Proto != null && Proto.EntityFilter != null && Proto.EntityFilter.Evaluate(entity, new(MissionProto.DataRef)))
            {
                map.Insert(MissionProto.DataRef, EntityTrackingFlag.Appearance);
                return EntityTrackingFlag.Appearance;
            }
            return EntityTrackingFlag.None;
        }
    }

    public class MissionDialogOption : BaseMissionOption
    {
        public MissionDialogTextPrototype Proto { get; set; }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (Proto != null && Proto.EntityFilter != null && Proto.EntityFilter.Evaluate(entity, new(MissionProto.DataRef)))
            {
                map.Insert(MissionProto.DataRef, EntityTrackingFlag.MissionDialog);
                return EntityTrackingFlag.MissionDialog;
            }
            return EntityTrackingFlag.None;
        }
    }

    public class MissionConnectionTargetEnableOption : BaseMissionOption
    {
        public ConnectionTargetEnableSpecPrototype Proto { get; set; }

        public MissionConnectionTargetEnableOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.ConnectionTargetEnable;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (Proto != null && Proto.ConnectionTarget != PrototypeId.Invalid)
            {
                if (entity is Transition transition)
                {
                    PrototypeId targetRef = Proto.ConnectionTarget;
                    for (int i = 0; i < transition.Destinations.Count; i++)
                    {
                        TransitionDestination destination = transition.Destinations[i];

                        if (destination.TargetRef == targetRef)
                        {
                            map.Insert(targetRef, EntityTrackingFlag.Appearance);
                            return EntityTrackingFlag.Appearance;
                        }
                    }
                }
            }
            return EntityTrackingFlag.None;
        }
    }

    public class MissionAppearanceOption : BaseMissionOption
    {
        public EntityAppearanceSpecPrototype Proto { get; set; }

        public MissionAppearanceOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.Appearance;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (Proto != null && Proto.EntityFilter != null && Proto.EntityFilter.Evaluate(entity, new(MissionProto.DataRef)))
            {
                map.Insert(MissionProto.DataRef, EntityTrackingFlag.Appearance);
                return EntityTrackingFlag.Appearance;
            }
            return EntityTrackingFlag.None;
        }
    }

    public class MissionActionEntityTargetOption: BaseMissionOption
    {
        public MissionActionEntityTargetPrototype Proto { get; set; }

        public MissionActionEntityTargetOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.ActionEntityTarget;
        }

        public override EntityTrackingFlag InterestedInEntity(EntityTrackingContextMap map, WorldEntity entity, HashSet<InteractionOption> checkList)
        {
            if (Proto != null && Proto.EntityFilter != null && Proto.EntityFilter.Evaluate(entity, new (MissionProto.DataRef)))
            {
                map.Insert(MissionProto.DataRef, EntityTrackingFlag.MissionAction);
                return EntityTrackingFlag.MissionAction;
            }
            return EntityTrackingFlag.None;
        }
    }
}
