using System.Collections;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.PowerCollections
{
    public class PowerCollection : IEnumerable<KeyValuePair<PrototypeId, PowerCollectionRecord>>
    {
        private const int MaxNumRecordsToSerialize = 256;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly SortedDictionary<PrototypeId, PowerCollectionRecord> _powerDict = new();
        private readonly Stack<Power> _condemnedPowers = new();
        private readonly WorldEntity _owner;
 
        public Power ThrowablePower { get; private set; }
        public Power ThrowableCancelPower { get; private set; }

        public int PowerCount { get => _powerDict.Count; }

        public PowerCollection(WorldEntity owner)
        {
            _owner = owner;
        }

        public static bool SerializeRecordCount(Archive archive, PowerCollection powerCollection, ref uint numberOfRecords)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                // TODO: archive.IsPersistent
                if (archive.IsReplication && archive.HasReplicationPolicy(AOINetworkPolicyValues.AOIChannelProximity))
                {
                    numberOfRecords = 0;
                    if (powerCollection != null)
                    {
                        foreach (PowerCollectionRecord record in powerCollection._powerDict.Values)
                        {
                            if (record.ShouldSerializeRecordForPacking(archive))
                            {
                                if (numberOfRecords >= MaxNumRecordsToSerialize)
                                {
                                    Logger.Warn("SerializeRecordCount(): numberOfRecords >= MaxNumRecordsToSerialize");
                                    break;
                                }

                                numberOfRecords++;
                            }
                        }
                    }
                    success &= Serializer.Transfer(archive, ref numberOfRecords);
                }
            }
            else
            {
                // TODO: archive.IsPersistent
                if (archive.IsReplication && archive.HasReplicationPolicy(AOINetworkPolicyValues.AOIChannelProximity))
                    success &= Serializer.Transfer(archive, ref numberOfRecords);
            }

            return success;
        }

        public static bool SerializeTo(Archive archive, PowerCollection powerCollection, uint numberOfRecords)
        {
            // TODO: Also check for replication mode
            if (archive.IsPacking == false) return Logger.WarnReturn(false, "SerializeTo(): archive.IsPacking == false");

            bool success = true;

            PowerCollectionRecord previousRecord = null;
            foreach (PowerCollectionRecord record in powerCollection._powerDict.Values)
            {
                if (record.ShouldSerializeRecordForPacking(archive))
                {
                    success &= record.SerializeTo(archive, previousRecord);
                    previousRecord = record;
                    numberOfRecords--;
                }
            }

            if (numberOfRecords != 0) return Logger.ErrorReturn(false, "SerializeTo(): numberOfRecords != 0");
            return success;
        }

        public static bool SerializeFrom(Archive archive, PowerCollection powerCollection, uint numberOfRecords)
        {
            if (archive.IsUnpacking == false) return Logger.WarnReturn(false, "SerializeFrom(): archive.IsUnpacking == false");

            bool success = true;

            if (powerCollection != null && powerCollection._powerDict.Count > 0)
            {
                Logger.Error("SerializeFrom(): When preparing to unpack a serialized PowerCollection, there was already data in the receiving _powerDict");
                powerCollection._powerDict.Clear();
            }

            PowerCollectionRecord previousRecord = null;
            for (uint i = 0; i < numberOfRecords; i++)
            {
                PowerCollectionRecord record = new();
                success &= record.SerializeFrom(archive, previousRecord);
                if (powerCollection != null)
                    powerCollection._powerDict.Add(record.PowerPrototypeRef, record);
                previousRecord = record;
            }

            return success;
        }

        // IEnumerable implementation
        public IEnumerator<KeyValuePair<PrototypeId, PowerCollectionRecord>> GetEnumerator() => _powerDict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Power GetPower(PrototypeId powerProtoRef)
        {
            if (_powerDict.TryGetValue(powerProtoRef, out PowerCollectionRecord record) == false)
                return null;

            return record.Power;
        }

        public IEnumerable<Power> GetPowersMatchingAnyKeyword(IEnumerable<PrototypeId> powerKeywordRefs)
        {
            foreach (PowerCollectionRecord record in _powerDict.Values)
            {
                Power power = record.Power;
                if (power == null) continue;

                foreach (PrototypeId powerKeywordProtoRef in powerKeywordRefs)
                {
                    if (power.HasKeyword(powerKeywordProtoRef.As<KeywordPrototype>()))
                    {
                        yield return power;
                        break;
                    }
                }
            }
        }

        public bool ContainsPower(PrototypeId powerProtoRef) => GetPowerRecordByRef(powerProtoRef) != null;

        public bool ContainsPowerProgressionPower(PrototypeId powerProtoRef)
        {
            PowerCollectionRecord record = GetPowerRecordByRef(powerProtoRef);
            return record != null && record.IsPowerProgressionPower;
        }

        public Power AssignPower(PrototypeId powerProtoRef, PowerIndexProperties indexProps, PrototypeId triggeringPowerRef = PrototypeId.Invalid, bool sendPowerAssignmentToClients = true)
        {
            var powerProto = powerProtoRef.As<PowerPrototype>();
            if (powerProto == null) return Logger.WarnReturn<Power>(null, "AssignPower(): powerProto == null");

            if (Power.IsComboEffect(powerProto) == false && (_owner == null || _owner.IsInWorld == false))
                return Logger.WarnReturn<Power>(null, "AssignPower(): PowerCollection only supports Assign() of powers while the owner is in world!");

            return AssignPowerInternal(powerProtoRef, indexProps, triggeringPowerRef, sendPowerAssignmentToClients);
        }

        public bool UnassignPower(PrototypeId powerProtoRef, bool sendPowerUnassignToClients = true)
        {
            if (_owner == null || _owner.IsInWorld == false)
                return Logger.WarnReturn(false, "UnassignPower(): PowerCollection only supports Unassign() of powers while the owner is in world!");

            return UnassignPowerInternal(powerProtoRef, sendPowerUnassignToClients);
        }

        public void DeleteCondemnedPowers()
        {
            while (_condemnedPowers.Count > 0)
            {
                Power condemnedPower = _condemnedPowers.Pop();
                condemnedPower.OnDeallocate();
            }
        }

        /// <summary>
        /// Sends all assigned powers to the provided <see cref="Player"/>.
        /// </summary>
        public bool SendEntireCollection(Player player)
        {
            // NOTE: This is for when other players enter your area of interest,
            // your own powers are sent one by one as they are assigned when
            // your avatar enters world.

            if (_owner == null) return Logger.WarnReturn(false, "SendEntireCollection(): _owner == null");

            // Make sure the provided player is actually interested in our owner
            AreaOfInterest aoi = player.PlayerConnection.AOI;
            if (aoi.InterestedInEntity(_owner.Id, AOINetworkPolicyValues.AOIChannelProximity) == false)
                return Logger.WarnReturn(false, "SendEntireCollection(): Attempting to send an entire power collection to a player not interested in this collection's owner");

            var assignCollectionBuilder = NetMessageAssignPowerCollection.CreateBuilder();

            foreach (PowerCollectionRecord record in _powerDict.Values)
            {
                assignCollectionBuilder.AddPower(NetMessagePowerCollectionAssignPower.CreateBuilder()
                    .SetEntityId(_owner.Id)
                    .SetPowerProtoId((ulong)record.PowerPrototypeRef)
                    .SetPowerRank(record.IndexProps.PowerRank)
                    .SetCharacterLevel(record.IndexProps.CharacterLevel)
                    .SetCombatLevel(record.IndexProps.CombatLevel)
                    .SetItemLevel(record.IndexProps.ItemLevel)
                    .SetItemVariation(record.IndexProps.ItemVariation));
            }

            player.SendMessage(assignCollectionBuilder.Build());

            return true;
        }

        public void OnOwnerEnteredWorld()
        {
            // Notify powers of the owner entering world
            foreach (PowerCollectionRecord record in _powerDict.Values)
                record.Power?.OnOwnerEnteredWorld();
        }

        public void OnOwnerExitedWorld()
        {
            // Notify powers of the owner exiting world
            foreach (PowerCollectionRecord record in _powerDict.Values)
                record.Power?.OnOwnerExitedWorld();

            // Convert dict to array to be able to remove entries while iterating
            foreach (var kvp in _powerDict.ToArray())
            {
                Power power = kvp.Value.Power;

                // Simply remove records that have no valid powers
                if (power == null)
                {
                    Logger.Warn("OnOwnerExitedWorld(): power == null");
                    _powerDict.Remove(kvp.Key);
                    continue;
                }

                // Combo effects are unassigned separately
                if (power.IsComboEffect()) continue;

                // Unassign power
                UnassignPower(kvp.Value.PowerPrototypeRef, false);
            }
        }

        public void OnOwnerCastSpeedChange(PrototypeId keywordProtoRef)
        {
            // Filter by keyword if there is a valid ref
            if (keywordProtoRef != PrototypeId.Invalid)
            {
                var keywordProto = GameDatabase.GetPrototype<PowerKeywordPrototype>(keywordProtoRef);
                if (keywordProto != null)
                {
                    foreach (PowerCollectionRecord record in _powerDict.Values)
                    {
                        if (record.Power == null || record.Power.HasKeyword(keywordProto) == false)
                            continue;

                        record.Power.OnOwnerCastSpeedChange();
                    }

                    return;
                }
            }

            foreach (PowerCollectionRecord record in _powerDict.Values)
                record.Power?.OnOwnerCastSpeedChange();
        }

        public void OnOwnerDeallocate()
        {
            foreach (var kvp in _powerDict)
                kvp.Value.Power.OnDeallocate();

            DeleteCondemnedPowers();
        }

        private PowerCollectionRecord GetPowerRecordByRef(PrototypeId powerProtoRef)
        {
            if (_powerDict.TryGetValue(powerProtoRef, out PowerCollectionRecord record) == false)
                return null;

            return record;
        }

        private Power AssignPowerInternal(PrototypeId powerProtoRef, PowerIndexProperties indexProps, PrototypeId triggeringPowerRef, bool sendPowerAssignmentToClients)
        {
            // Do pre-assignment validation, this check combines and inlines PowerCollection::preAssignPowerInternal() and PowerCollection::validatePowerData()
            if (GameDatabase.DataDirectory.PrototypeIsApproved(powerProtoRef) == false)
                return Logger.WarnReturn<Power>(null, $"AssignPowerInternal(): Power is not approved for use ({GameDatabase.GetPrototypeName(powerProtoRef)})");

            // Send power assignment message to interested clients
            if (sendPowerAssignmentToClients && _owner != null && _owner.IsInGame)
            {
                var assignPowerMessage = NetMessagePowerCollectionAssignPower.CreateBuilder()
                    .SetEntityId(_owner.Id)
                    .SetPowerProtoId((ulong)powerProtoRef)
                    .SetPowerRank(indexProps.PowerRank)
                    .SetCharacterLevel(indexProps.CharacterLevel)
                    .SetCombatLevel(indexProps.CombatLevel)
                    .SetItemLevel(indexProps.ItemLevel)
                    .SetItemVariation(indexProps.ItemVariation)
                    .Build();

                _owner.Game.NetworkManager.SendMessageToInterested(assignPowerMessage, _owner, AOINetworkPolicyValues.AOIChannelProximity);
            }

            // See if the power we are trying to assign is already in this collection
            PowerCollectionRecord powerRecord = GetPowerRecordByRef(powerProtoRef);
            if (powerRecord == null)
            {
                // Determine source flags for this power
                // (TODO: it would probably be cleaner to do this as a separate method with early returns)
                bool isPowerProgressionPower = false;
                bool isTeamUpPassiveWhileAway = false;

                // Inherit the flags from the triggering power if we have one
                PowerCollectionRecord triggeringPowerRecord = GetPowerRecordByRef(triggeringPowerRef);
                if (triggeringPowerRecord != null)
                {
                    isPowerProgressionPower = triggeringPowerRecord.IsPowerProgressionPower;
                    isTeamUpPassiveWhileAway = triggeringPowerRecord.IsTeamUpPassiveWhileAway;
                }
                else
                {
                    if (_owner != null)
                    {
                        if (_owner is Agent agentOwner)
                        {
                            isPowerProgressionPower = agentOwner.HasPowerInPowerProgression(powerProtoRef);

                            if (isPowerProgressionPower == false)
                            {
                                var avatarOwner = _owner.GetMostResponsiblePowerUser<Avatar>();
                                if (avatarOwner != null)
                                {
                                    Agent teamUpAgent = avatarOwner.CurrentTeamUpAgent;
                                    if (teamUpAgent != null)
                                    {
                                        teamUpAgent.GetPowerProgressionInfo(powerProtoRef, out var info);
                                        if (info.IsForTeamUp)
                                        {
                                            isPowerProgressionPower = true;
                                            isTeamUpPassiveWhileAway = info.IsPassivePowerOnAvatarWhileAway;
                                        }
                                    }

                                }
                            }
                        }
                        else
                        {
                            isTeamUpPassiveWhileAway = _owner.Properties[PropertyEnum.IsTeamUpAwaySource];
                        }
                    }
                    else
                    {
                        Logger.Warn("AssignPowerInternal(): _owner == null");
                    }
                }

                powerRecord = CreatePowerRecord(powerProtoRef, indexProps, triggeringPowerRef, isPowerProgressionPower, isTeamUpPassiveWhileAway);
                if (powerRecord == null)
                    return Logger.WarnReturn<Power>(null, "AssignPowerInternal(): powerRecord == null");
            }
            else
            {
                if (powerRecord.Power == null)
                    return Logger.WarnReturn<Power>(null, "AssignPowerInternal(): powerRecord.Power == null");

                if (powerRecord.PowerPrototypeRef != powerProtoRef)
                    return Logger.WarnReturn<Power>(null, "AssignPowerInternal(): powerRecord.PowerPrototypeRef != powerProtoRef");

                // Only proc and combo effects can be assigned multiple times
                bool isProcEffect = powerRecord.Power.IsProcEffect();
                bool isComboEffect = powerRecord.Power.IsComboEffect() && powerRecord.Power.GetActivationType() != PowerActivationType.Passive;

                if (isProcEffect == false && isComboEffect == false)
                {
                    return Logger.WarnReturn<Power>(null, string.Format(
                        "AssignPowerInternal(): The following power being assigned multiple times to a PowerCollection is not a Combo or Proc effect power, which is not allowed!\nOwner: [{0}]\nPower: [{1}]",
                        _owner != null ? _owner.ToString() : "Unknown",
                        powerRecord.Power.ToString()));
                }

                // Increment power ref count
                powerRecord.PowerRefCount++;
            }

            return powerRecord.Power;
        }

        private PowerCollectionRecord CreatePowerRecord(PrototypeId powerProtoRef, PowerIndexProperties indexProps, PrototypeId triggeringPowerRef,
            bool isAvatarPowerProgressionPower, bool isTeamUpPassivePowerWhileAway)
        {
            Power power = CreatePower(powerProtoRef, indexProps, triggeringPowerRef, isTeamUpPassivePowerWhileAway);
            if (power == null) return Logger.WarnReturn<PowerCollectionRecord>(null, "CreatePowerRecord(): power == null");

            // Here we have a custom Initialize() method not present in the client to clean up record initialization
            PowerCollectionRecord record = new();
            record.Initialize(power, powerProtoRef, indexProps, 1, isAvatarPowerProgressionPower, isTeamUpPassivePowerWhileAway);
            _powerDict.Add(record.PowerPrototypeRef, record);   // PowerCollection::addPowerRecord()

            FinishAssignPower(power);
            return record;
        }

        private Power CreatePower(PrototypeId powerProtoRef, PowerIndexProperties indexProps, PrototypeId triggeringPowerRef, bool isTeamUpPassivePowerWhileAway)
        {
            if (powerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn<Power>(null, "CreatePower(): powerProtoRef == PrototypeId.Invalid");
            if (_owner == null) return Logger.WarnReturn<Power>(null, "CreatePower(): _owner == null");
            if (_owner.Game == null) return Logger.WarnReturn<Power>(null, "CreatePower(): _owner.Game == null");

            Power power = _owner.Game.AllocatePower(powerProtoRef);

            // Assemble property values passed as arguments into a collection
            PropertyCollection initializeProperties = new();

            initializeProperties[PropertyEnum.PowerRank] = indexProps.PowerRank;
            initializeProperties[PropertyEnum.CharacterLevel] = indexProps.CharacterLevel;
            initializeProperties[PropertyEnum.CombatLevel] = indexProps.CombatLevel;
            initializeProperties[PropertyEnum.ItemLevel] = indexProps.ItemLevel;

            if (triggeringPowerRef != PrototypeId.Invalid)
                initializeProperties[PropertyEnum.TriggeringPowerRef, powerProtoRef] = triggeringPowerRef;

            power.Initialize(_owner, isTeamUpPassivePowerWhileAway, initializeProperties);

            return power;
        }

        private void FinishAssignPower(Power power)
        {
            if (power.GetPowerCategory() == PowerCategoryType.ThrowablePower)
            {
                if (ThrowablePower != null)
                    Logger.Warn("FinishAssignPower(): Trying to assign a throwable power when this entity already has a throwable power in its power collection");

                ThrowablePower = power;
            }
            else if (power.GetPowerCategory() == PowerCategoryType.ThrowableCancelPower)
            {
                if (ThrowableCancelPower != null)
                    Logger.Warn("FinishAssignPower(): Trying to assign a throwable cancel power when this entity already has a throwable cancel power in its power collection");

                ThrowableCancelPower = power;
            }

            AssignTriggeredPowers(power);
            _owner.OnPowerAssigned(power);
            power.OnAssign();
        }

        private bool AssignTriggeredPowers(Power power)
        {
            if (_owner == null) return Logger.WarnReturn(false, "AssignTriggeredPowers(): _owner == null");

            PowerPrototype powerProto = power.Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "AssignTriggeredPowers(): powerProto == null");

            PrototypeId powerProtoRef = power.PrototypeDataRef;
            if (powerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "AssignTriggeredPowers(): powerProtoRef == PrototypeId.Invalid");

            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return true;

            if (_owner is not Agent && _owner.Prototype is not HotspotPrototype)
            {
                return Logger.WarnReturn(false, $"AssignTriggeredPowers(): The following entity can't cast combo powers, but is assigning a power"
                    + " that specifies PowerEventActions, which are only supported for agents/hotspots:\n[{_owner}]\n[{powerProto}]");
            }

            PowerIndexProperties indexProps = new(power.Properties[PropertyEnum.PowerRank], power.Properties[PropertyEnum.CharacterLevel],
                power.Properties[PropertyEnum.CombatLevel], power.Properties[PropertyEnum.ItemLevel]);

            int assignedPowers = 0;
            List<PrototypeId> triggeredPowerRefList = new();    // NOTE: We reuse the same list for all iterations intead of allocating a new one each time

            foreach (PowerEventActionPrototype triggeredPowerEventProto in powerProto.ActionsTriggeredOnPowerEvent)
            {
                PowerEventType powerEventType = triggeredPowerEventProto.PowerEvent;
                if (powerEventType == PowerEventType.None)
                {
                    Logger.Warn($"AssignTriggeredPowers(): This power contains a triggered power event with a null type\nPower: [{powerProto}]");
                    continue;
                }

                // Check if this power event has triggered powers that need assignment
                switch (triggeredPowerEventProto.EventAction)
                {
                    case PowerEventActionType.UsePower:
                    case PowerEventActionType.ScheduleActivationAtPercent:
                    case PowerEventActionType.ScheduleActivationInSeconds:
                        if (triggeredPowerEventProto.Power == PrototypeId.Invalid)
                        {
                            Logger.Warn($"AssignTriggeredPowers(): Power [{power}] for agent [{_owner}] has a triggered power event with no power specified");
                            continue;
                        }
                        triggeredPowerRefList.Add(triggeredPowerEventProto.Power);
                        break;

                    case PowerEventActionType.TransformModeStart:
                        var contextProto = triggeredPowerEventProto.PowerEventContext as PowerEventContextTransformModePrototype;
                        if (contextProto == null)
                        {
                            Logger.Warn("AssignTriggeredPowers(): contextProto == null");
                            continue;
                        }

                        var transformModeProto = contextProto.TransformMode.As<TransformModePrototype>();
                        if (transformModeProto == null)
                        {
                            Logger.Warn("AssignTriggeredPowers(): transformModeProto == null");
                            continue;
                        }

                        if (transformModeProto.EnterTransformModePower == PrototypeId.Invalid)
                        {
                            Logger.Warn($"AssignTriggeredPowers(): Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no EnterTransformMode power specified");
                            continue;
                        }

                        if (transformModeProto.ExitTransformModePower == PrototypeId.Invalid)
                        {
                            Logger.Warn($"AssignTriggeredPowers(): Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no ExitTransformModePower power specified");
                            continue;
                        }

                        triggeredPowerRefList.Add(transformModeProto.EnterTransformModePower);
                        triggeredPowerRefList.Add(transformModeProto.ExitTransformModePower);

                        break;

                    case PowerEventActionType.RemoveSummonedAgentsWithKeywords:
                        if (triggeredPowerEventProto.Power != PrototypeId.Invalid)
                            triggeredPowerRefList.Add(triggeredPowerEventProto.Power);
                        break;

                    default:
                        continue;
                }

                // Make sure this power doesn't assign more than one OnTargetKill / OnPowerEnded triggered powers
                if (powerEventType == PowerEventType.OnTargetKill || (powerEventType == PowerEventType.OnPowerEnd && triggeredPowerEventProto.HasEvalEventTriggerChance == false))
                {
                    if (MathHelper.BitTest(assignedPowers, 1 << (int)powerEventType))
                    {
                        Logger.Warn($"AssignTriggeredPowers(): This power contains multiple powers of type OnTargetKill or OnPowerEnded\nPower: [{powerProto}]");
                        continue;
                    }
                }

                MathHelper.BitSet(ref assignedPowers, 1 << (int)powerEventType);

                // Assign triggered powers we determined we need to assign
                foreach (PrototypeId triggeredPowerRef in triggeredPowerRefList)
                {
                    if (triggeredPowerRef == PrototypeId.Invalid)
                    {
                        Logger.Warn("AssignTriggeredPowers(): triggeredPowerRef == PrototypeId.Invalid");
                        continue;
                    }

                    // Some powers apparently can trigger themselves, no need to assign them again
                    if (triggeredPowerRef == powerProtoRef)
                        continue;

                    //Logger.Trace($"AssignTriggeredPowers(): {GameDatabase.GetPrototypeName(triggeredPowerRef)} for {powerProto}");

                    if (AssignPower(triggeredPowerRef, indexProps, powerProtoRef, false) == null)
                        return Logger.WarnReturn(false, "AssignTriggeredPowers(): AssignPower() == null");
                }

                triggeredPowerRefList.Clear();
            }

            return true;
        }

        private bool UnassignPowerInternal(PrototypeId powerProtoRef, bool sendPowerUnassignToClients)
        {
            if (_owner == null) return Logger.WarnReturn(false, "UnassignPowerInternal(): _owner == null");
            if (_owner.Game == null) return Logger.WarnReturn(false, "UnassignPowerInternal(): _owner.Game == null");

            // Find and validate the record for our powerProtoRef
            PowerCollectionRecord powerRecord = GetPowerRecordByRef(powerProtoRef);
            if (powerRecord == null) return Logger.WarnReturn(false, "UnassignPowerInternal(): powerRecord == null");
            if (powerRecord.Power == null) return Logger.WarnReturn(false, "UnassignPowerInternal(): powerRecord.Power == null");

            // Start by subtracting from the PowerRefCount
            if (powerRecord.PowerRefCount < 1) return Logger.WarnReturn(false, "UnassignPowerInternal(): powerRecord.PowerRefCount < 1");
            powerRecord.PowerRefCount--;

            // Remove the record when our PowerRefCount reaches 0
            if (powerRecord.PowerRefCount == 0)
            {
                FinishUnassignPower(powerRecord.Power);

                _owner.Game.EntityManager.RegisterEntityForCondemnedPowerDeletion(_owner.Id);
                _condemnedPowers.Push(powerRecord.Power);

                DestroyPowerRecord(powerRecord.PowerPrototypeRef);
            }

            // Send power unassignment message to interested clients
            if (sendPowerUnassignToClients && _owner.IsInGame && _owner.IsInWorld)
            {
                var unassignPowerMessage = NetMessagePowerCollectionUnassignPower.CreateBuilder()
                    .SetEntityId(_owner.Id)
                    .SetPowerProtoId((ulong)powerProtoRef)
                    .Build();

                _owner.Game.NetworkManager.SendMessageToInterested(unassignPowerMessage, _owner, AOINetworkPolicyValues.AOIChannelProximity);
            }

            return true;
        }

        private bool DestroyPowerRecord(PrototypeId powerProtoRef)
        {
            // Is this extra validation worth the performance cost of looking the record up again?
            if (_powerDict.TryGetValue(powerProtoRef, out PowerCollectionRecord powerRecord) == false)
                return false;

            if (powerRecord.PowerRefCount != 0)
                Logger.Warn("DestroyPowerRecord(): Power record is not empty");

            return _powerDict.Remove(powerProtoRef);
        }

        private void FinishUnassignPower(Power power)
        {
            if (power.GetPowerCategory() == PowerCategoryType.ThrowablePower)
            {
                if (ThrowablePower == null)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable power when this entity does not have a throwable power in its power collection");

                if (ThrowablePower != power)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable power that isn't the same as this power collection's throwable power");

                ThrowablePower = null;
            }
            else if (power.GetPowerCategory() == PowerCategoryType.ThrowableCancelPower)
            {
                if (ThrowableCancelPower == null)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable cancel power when this entity does not have a throwable cancel power in its power collection");

                if (ThrowableCancelPower != power)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable cancel power that isn't the same as this power collection's throwable cancel power");

                ThrowableCancelPower = null;
            }

            if (_owner.IsDestroyed == false)
            {
                _owner.OnPowerUnassigned(power);
                power.OnUnassign();
            }

            UnassignTriggeredPowers(power);
        }

        private bool UnassignTriggeredPowers(Power power)
        {
            // NOTE: This is very similar to AssignTriggeredPowers()

            if (_owner == null) return Logger.WarnReturn(false, "UnassignTriggeredPowers(): _owner == null");

            PowerPrototype powerProto = power.Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "UnassignTriggeredPowers(): powerProto == null");

            PrototypeId powerProtoRef = power.PrototypeDataRef;
            if (powerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "UnassignTriggeredPowers(): powerProtoRef == PrototypeId.Invalid");

            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return true;

            List<PrototypeId> triggeredPowerRefList = new();    // NOTE: We reuse the same list for all iterations intead of allocating a new one each time

            foreach (PowerEventActionPrototype triggeredPowerEventProto in powerProto.ActionsTriggeredOnPowerEvent)
            {
                PowerEventType powerEventType = triggeredPowerEventProto.PowerEvent;
                if (powerEventType == PowerEventType.None)
                {
                    Logger.Warn($"UnassignTriggeredPowers(): This power contains a triggered power event with a null type\nPower: [{powerProto}]");
                    continue;
                }

                // Check if this power event has triggered powers that need assignment
                switch (triggeredPowerEventProto.EventAction)
                {
                    case PowerEventActionType.UsePower:
                    case PowerEventActionType.ScheduleActivationAtPercent:
                    case PowerEventActionType.ScheduleActivationInSeconds:
                        if (triggeredPowerEventProto.Power == PrototypeId.Invalid)
                        {
                            Logger.Warn($"UnassignTriggeredPowers(): Power [{power}] for agent [{_owner}] has a triggered power event with no power specified");
                            continue;
                        }
                        triggeredPowerRefList.Add(triggeredPowerEventProto.Power);
                        break;

                    case PowerEventActionType.TransformModeStart:
                        var contextProto = triggeredPowerEventProto.PowerEventContext as PowerEventContextTransformModePrototype;
                        if (contextProto == null)
                        {
                            Logger.Warn("UnassignTriggeredPowers(): contextProto == null");
                            continue;
                        }

                        var transformModeProto = contextProto.TransformMode.As<TransformModePrototype>();
                        if (transformModeProto == null)
                        {
                            Logger.Warn("UnassignTriggeredPowers(): transformModeProto == null");
                            continue;
                        }

                        // NOTE: Assignment happens for any type of owner, but unassignment is for avatars only. Is this intended?
                        if (_owner is Avatar avatar)
                        {
                            PrototypeId currentTransformMode = avatar.CurrentTransformMode;

                            if (currentTransformMode == PrototypeId.Invalid || currentTransformMode != transformModeProto.DataRef)
                            {
                                if (transformModeProto.EnterTransformModePower == PrototypeId.Invalid)
                                {
                                    Logger.Warn($"UnassignTriggeredPowers(): Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no EnterTransformMode power specified");
                                    continue;
                                }

                                if (transformModeProto.ExitTransformModePower == PrototypeId.Invalid)
                                {
                                    Logger.Warn($"UnassignTriggeredPowers(): Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no ExitTransformModePower power specified");
                                    continue;
                                }

                                triggeredPowerRefList.Add(transformModeProto.EnterTransformModePower);
                                triggeredPowerRefList.Add(transformModeProto.ExitTransformModePower);
                            }
                        }

                        break;

                    case PowerEventActionType.RemoveSummonedAgentsWithKeywords:
                        if (triggeredPowerEventProto.Power != PrototypeId.Invalid)
                            triggeredPowerRefList.Add(triggeredPowerEventProto.Power);
                        break;

                    default:
                        continue;
                }

                // Assign triggered powers we determined we need to assign
                foreach (PrototypeId triggeredPowerRef in triggeredPowerRefList)
                {
                    if (triggeredPowerRef == PrototypeId.Invalid)
                    {
                        Logger.Warn("UnassignTriggeredPowers(): triggeredPowerRef == PrototypeId.Invalid");
                        continue;
                    }

                    // Some powers apparently can trigger themselves, no need to unassign them
                    if (triggeredPowerRef == powerProtoRef)
                        continue;

                    //Logger.Trace($"UnassignTriggeredPowers(): {GameDatabase.GetPrototypeName(triggeredPowerRef)} for {powerProto}");

                    UnassignPower(triggeredPowerRef, false);
                }

                triggeredPowerRefList.Clear();
            }

            return true;
        }
    }
}
