using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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
    public class PowerCollection
    {
        private const int MaxNumRecordsToSerialize = 256;

        private readonly SortedDictionary<PrototypeId, PowerCollectionRecord> _powers = new();
        private readonly Stack<Power> _condemnedPowers = new();
        private readonly WorldEntity _owner;
 
        public Power ThrowablePower { get; private set; }
        public Power ThrowableCancelPower { get; private set; }

        public int PowerCount { get => _powers.Count; }

        public PowerCollection(WorldEntity owner)
        {
            _owner = owner;
        }

        public static bool SerializeRecordCount(Archive archive, PowerCollection powerCollection, ref uint numberOfRecords)
        {
            bool success = true;

            // In very old versions of the game (before archive version 15) power collections were serialized to persistent archives.
            // We don't need a code path for persistent archives here like the client does because we don't have this kind of legacy data.
            if (archive.IsPacking)
            {
                if (archive.IsReplication && archive.HasReplicationPolicy(AOINetworkPolicyValues.AOIChannelProximity))
                {
                    numberOfRecords = 0;
                    if (powerCollection != null)
                    {
                        foreach (PowerCollectionRecord record in powerCollection._powers.Values)
                        {
                            if (record.ShouldSerializeRecordForPacking(archive) == false)
                                continue;

                            if (!Verify.IsTrue(numberOfRecords < MaxNumRecordsToSerialize))
                                break;

                            numberOfRecords++;
                        }
                    }
                    success &= Serializer.Transfer(archive, ref numberOfRecords);
                }
            }
            else
            {
                if (archive.IsReplication && archive.HasReplicationPolicy(AOINetworkPolicyValues.AOIChannelProximity))
                    success &= Serializer.Transfer(archive, ref numberOfRecords);
            }

            return success;
        }

        public static bool SerializeTo(Archive archive, PowerCollection powerCollection, uint numberOfRecords)
        {
            if (!Verify.IsTrue(archive.IsPacking && archive.IsReplication)) return false;

            bool success = true;

            PowerCollectionRecord previousRecord = null;
            foreach (PowerCollectionRecord record in powerCollection._powers.Values)
            {
                if (record.ShouldSerializeRecordForPacking(archive))
                {
                    success &= record.SerializeTo(archive, previousRecord);
                    previousRecord = record;
                    numberOfRecords--;
                }
            }

            if (!Verify.IsTrue(numberOfRecords == 0)) return false;
            return success;
        }

        public static bool SerializeFrom(Archive archive, PowerCollection powerCollection, uint numberOfRecords)
        {
            if (!Verify.IsTrue(archive.IsUnpacking)) return false;

            bool success = true;

            if (powerCollection != null && powerCollection._powers.Count > 0)
            {
                Verify.IsTrue(false, "When preparing to unpack a serialized PowerCollection, there was already data in the receiving _powers structure");
                powerCollection._powers.Clear();
            }

            PowerCollectionRecord previousRecord = null;
            for (uint i = 0; i < numberOfRecords; i++)
            {
                PowerCollectionRecord record = new();
                success &= record.SerializeFrom(archive, previousRecord);
                powerCollection?._powers.Add(record.PowerPrototypeRef, record);
                previousRecord = record;
            }

            return success;
        }
        
        public SortedDictionary<PrototypeId, PowerCollectionRecord>.Enumerator GetEnumerator()
        {
            return _powers.GetEnumerator();
        }

        public Power GetPower(PrototypeId powerProtoRef)
        {
            if (_powers.TryGetValue(powerProtoRef, out PowerCollectionRecord record) == false)
                return null;

            return record.Power;
        }

        public void GetPowersMatchingAnyKeyword(List<Power> powers, PrototypeId[] keywords)
        {
            foreach (PowerCollectionRecord record in _powers.Values)
            {
                Power power = record.Power;
                if (power == null)
                    continue;

                foreach (PrototypeId keywordProtoRef in keywords)
                {
                    if (power.HasKeyword(keywordProtoRef.As<KeywordPrototype>()))
                    {
                        powers.Add(power);
                        break;
                    }
                }
            }
        }

        public bool ContainsPower(PrototypeId powerProtoRef, bool excludeNonPowerProgressionPowers = false)
        {
            PowerCollectionRecord record = GetPowerRecordByRef(powerProtoRef);
            return record != null && (excludeNonPowerProgressionPowers == false || record.IsPowerProgressionPower);
        }

        public Power AssignPower(PrototypeId powerProtoRef, in PowerIndexProperties indexProps, PrototypeId triggeringPowerRef = PrototypeId.Invalid, bool sendPowerAssignmentToClients = true)
        {
            PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
            if (!Verify.IsNotNull(powerProto)) return null;

            if (Power.IsComboEffect(powerProto) == false)
            {
                if (!Verify.IsTrue(_owner != null && _owner.IsInWorld, $"PowerCollection now only supports Assign() of powers while the owner is in world!\nEntity: [{_owner}]\nPower: [{powerProtoRef.GetName()}]"))
                    return null;
            }

            return AssignPowerInternal(powerProtoRef, indexProps, triggeringPowerRef, sendPowerAssignmentToClients);
        }

        public bool UnassignPower(PrototypeId powerProtoRef, bool sendPowerUnassignToClients = true)
        {
            if (!Verify.IsTrue(_owner != null && _owner.IsInWorld, $"PowerCollection now only supports Unassign() of powers while the owner is in world!\nEntity: [{_owner}]\nPower: [{powerProtoRef.GetName()}]"))
                return false;

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

            if (!Verify.IsNotNull(_owner)) return false;

            // Missile powers are assigned in parallel by the client when MissileCreationContext is applied in OnEnteredWorld().
            if (_owner is Missile)
                return true;

            // Make sure the provided player is actually interested in our owner
            AreaOfInterest aoi = player.AOI;
            if (!Verify.IsTrue(aoi.InterestedInEntity(_owner.Id, AOINetworkPolicyValues.AOIChannelProximity))) return false;

            var assignCollectionBuilder = NetMessageAssignPowerCollection.CreateBuilder();

            foreach (PowerCollectionRecord record in _powers.Values)
            {
                if (record.Power.GetPowerCategory() == PowerCategoryType.ComboEffect)
                    continue;

                for (int i = 0; i < record.PowerRefCount; i++)
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
            }

            player.SendMessage(assignCollectionBuilder.Build());

            return true;
        }

        public void OnOwnerEnteredWorld()
        {
            // Notify powers of the owner entering world
            foreach (PowerCollectionRecord record in _powers.Values)
                record.Power?.OnOwnerEnteredWorld();
        }

        public void OnOwnerExitedWorld()
        {
            // Notify powers of the owner exiting world
            foreach (PowerCollectionRecord record in _powers.Values)
                record.Power?.OnOwnerExitedWorld();

            // Copy to a temporary list to be able to remove entries while iterating
            using var recordsHandle = ListPool<KeyValuePair<PrototypeId, PowerCollectionRecord>>.Instance.Get(out var records);

            // This needs to be done in a loop to remove all copies of powers with RefCount higher than 0.
            while (_powers.Count > 0)
            {
                records.Set(_powers);

                bool unassignedAny = false;
                foreach (var kvp in records)
                {
                    Power power = kvp.Value.Power;

                    // This is needed for our iteration workaround because triggered powers may remain in the temp list after being unassigned from the main collection.
                    if (_powers.ContainsKey(kvp.Key) == false)
                        continue;

                    // Simply remove records that have no valid powers
                    if (!Verify.IsNotNull(power))
                    {
                        _powers.Remove(kvp.Key);
                        continue;
                    }

                    // Combo effects are unassigned separately
                    if (power.IsComboEffect())
                        continue;

                    // Unassign power
                    UnassignPower(kvp.Value.PowerPrototypeRef, false);
                    unassignedAny = true;
                }

                // Combo powers that are used to enter/exit a transform mode are not unassigned along with their triggering power.
                // Because of this, there may still be powers left in the collection when a transformed owner avatar exits world.
                // This appears to be not a bug, but rather a questionable design decision made by Gazillion.
                if (unassignedAny == false && _powers.Count > 0)
                {
                    Verify.IsTrue(_owner is Avatar avatar && avatar.CurrentTransformMode != PrototypeId.Invalid);
                    break;
                }
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
                    foreach (PowerCollectionRecord record in _powers.Values)
                    {
                        if (record.Power == null || record.Power.HasKeyword(keywordProto) == false)
                            continue;

                        record.Power.OnOwnerCastSpeedChange();
                    }

                    return;
                }
            }

            foreach (PowerCollectionRecord record in _powers.Values)
                record.Power?.OnOwnerCastSpeedChange();
        }

        public void OnOwnerLevelChange()
        {
            foreach (PowerCollectionRecord record in _powers.Values)
                record.Power?.OnOwnerLevelChange();
        }

        public void OnOwnerDeallocate()
        {
            foreach (var kvp in _powers)
                kvp.Value.Power.OnDeallocate();

            DeleteCondemnedPowers();
        }

        private PowerCollectionRecord GetPowerRecordByRef(PrototypeId powerProtoRef)
        {
            if (_powers.TryGetValue(powerProtoRef, out PowerCollectionRecord record) == false)
                return null;

            return record;
        }

        private Power AssignPowerInternal(PrototypeId powerProtoRef, in PowerIndexProperties indexProps, PrototypeId triggeringPowerRef, bool sendPowerAssignmentToClients)
        {
            // Do pre-assignment validation, this check combines and inlines PowerCollection::preAssignPowerInternal() and PowerCollection::validatePowerData()
            if (!Verify.IsTrue(GameDatabase.DataDirectory.PrototypeIsApproved(powerProtoRef), $"Power is not yet ready for game or review.\n[{powerProtoRef.GetName()}]"))
                return null;

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
                else if (Verify.IsNotNull(_owner))
                {
                    if (_owner is Agent agentOwner)
                    {
                        isPowerProgressionPower = agentOwner.HasPowerInPowerProgression(powerProtoRef);
                        if (isPowerProgressionPower == false)
                        {
                            Avatar avatarOwner = _owner.GetMostResponsiblePowerUser<Avatar>();
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

                powerRecord = CreatePowerRecord(powerProtoRef, indexProps, triggeringPowerRef, isPowerProgressionPower, isTeamUpPassiveWhileAway);
                if (!Verify.IsNotNull(powerRecord)) return null;
            }
            else
            {
                if (!Verify.IsNotNull(powerRecord.Power)) return null;
                if (!Verify.IsTrue(powerRecord.PowerPrototypeRef == powerProtoRef)) return null;

                // Only proc and combo effects can be assigned multiple times
                bool isProcEffect = powerRecord.Power.IsProcEffect();
                bool isComboEffect = powerRecord.Power.IsComboEffect() && powerRecord.Power.GetActivationType() != PowerActivationType.Passive;
                if (!Verify.IsTrue(isProcEffect || isComboEffect, $"The following power being assigned multiple times to a PowerCollection is not a Combo or Proc effect power, which is not allowed!\nOwner: [{_owner}]\nPower: [{powerRecord.Power}]"))
                    return null;

                // Increment power ref count
                powerRecord.PowerRefCount++;
            }

            return powerRecord.Power;
        }

        private PowerCollectionRecord CreatePowerRecord(PrototypeId powerProtoRef, in PowerIndexProperties indexProps, PrototypeId triggeringPowerRef,
            bool isAvatarPowerProgressionPower, bool isTeamUpPassivePowerWhileAway)
        {
            Power power = CreatePower(powerProtoRef, indexProps, triggeringPowerRef, isTeamUpPassivePowerWhileAway);
            if (!Verify.IsNotNull(power)) return null;

            // Here we have a custom Initialize() method not present in the client to clean up record initialization
            PowerCollectionRecord record = new();
            record.Initialize(power, powerProtoRef, indexProps, 1, isAvatarPowerProgressionPower, isTeamUpPassivePowerWhileAway);
            _powers.Add(record.PowerPrototypeRef, record);   // PowerCollection::addPowerRecord()

            FinishAssignPower(power);
            return record;
        }

        private Power CreatePower(PrototypeId powerProtoRef, in PowerIndexProperties indexProps, PrototypeId triggeringPowerRef, bool isTeamUpPassivePowerWhileAway)
        {
            if (!Verify.IsTrue(powerProtoRef != PrototypeId.Invalid)) return null;
            if (!Verify.IsNotNull(_owner)) return null;

            Game game = _owner.Game;
            if (!Verify.IsNotNull(game)) return null;

            Power retPower = game.AllocatePower(powerProtoRef);
            if (!Verify.IsNotNull(retPower)) return null;

            // Assemble property values passed as arguments into a collection
            using PropertyCollection initializeProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            initializeProperties[PropertyEnum.PowerRank] = indexProps.PowerRank;
            initializeProperties[PropertyEnum.CharacterLevel] = indexProps.CharacterLevel;
            initializeProperties[PropertyEnum.CombatLevel] = indexProps.CombatLevel;
            initializeProperties[PropertyEnum.ItemLevel] = indexProps.ItemLevel;

            if (triggeringPowerRef != PrototypeId.Invalid)
                initializeProperties[PropertyEnum.TriggeringPowerRef, powerProtoRef] = triggeringPowerRef;

            retPower.Initialize(_owner, isTeamUpPassivePowerWhileAway, initializeProperties);

            return retPower;
        }

        private void FinishAssignPower(Power power)
        {
            if (power.GetPowerCategory() == PowerCategoryType.ThrowablePower)
            {
                Verify.IsTrue(ThrowablePower == null, $"Trying to assign a throwable power while this entity already has a throwable power in its power collection. \nEntity: [{_owner}]");
                ThrowablePower = power;
            }
            else if (power.GetPowerCategory() == PowerCategoryType.ThrowableCancelPower)
            {
                Verify.IsTrue(ThrowableCancelPower == null, $"Trying to assign a throwable cancel power when this entity already has a throwable cancel power in its power collection. \nEntity: [{_owner}]");
                ThrowableCancelPower = power;
            }

            AssignTriggeredPowers(power);

            // NOTE: The client calls OnPowerAssigned before OnAssign, but then auto-activated powers do not get their keywords mask. If this a bug?
            // It has to be in this order though to initialize PowerChargesMax before applying PowerChargesMaxBonus.
            _owner.OnPowerAssigned(power);
            power.OnAssign();
        }

        private bool AssignTriggeredPowers(Power power)
        {
            if (!Verify.IsNotNull(_owner)) return false;

            PowerPrototype powerProto = power.Prototype;
            if (!Verify.IsNotNull(powerProto)) return false;

            PrototypeId powerProtoRef = power.PrototypeDataRef;
            if (!Verify.IsTrue(powerProtoRef != PrototypeId.Invalid)) return false;

            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return true;

            if (!Verify.IsTrue(_owner is Agent || _owner.Prototype is HotspotPrototype, $"The following entity can't cast combo powers, but is assigning a power that specifies PowerEventActions, which are only supported for agents/hotspots:\n[{_owner}]\n[{powerProto}]"))
                return false;

            PowerIndexProperties indexProps = new(power.Properties[PropertyEnum.PowerRank], power.Properties[PropertyEnum.CharacterLevel],
                power.Properties[PropertyEnum.CombatLevel], power.Properties[PropertyEnum.ItemLevel]);

            int assignedPowers = 0;

            // NOTE: We reuse the same list for all iterations
            using var triggeredPowerRefListHandle = ListPool<PrototypeId>.Instance.Get(out List<PrototypeId> triggeredPowerRefList);

            foreach (PowerEventActionPrototype triggeredPowerEventProto in powerProto.ActionsTriggeredOnPowerEvent)
            {
                PowerEventType powerEventType = triggeredPowerEventProto.PowerEvent;
                if (!Verify.IsTrue(powerEventType != PowerEventType.None, $"This power contains a triggered power event with a null type\nPower: [{powerProto}]"))
                    continue;

                // Check if this power event has triggered powers that need assignment
                switch (triggeredPowerEventProto.EventAction)
                {
                    case PowerEventActionType.UsePower:
                    case PowerEventActionType.ScheduleActivationAtPercent:
                    case PowerEventActionType.ScheduleActivationInSeconds:
                        if (!Verify.IsTrue(triggeredPowerEventProto.Power != PrototypeId.Invalid, $"Power [{power}] for agent [{_owner}] has a triggered power event with no power specified"))
                            continue;

                        triggeredPowerRefList.Add(triggeredPowerEventProto.Power);
                        break;

                    case PowerEventActionType.TransformModeStart:
                        PowerEventContextTransformModePrototype contextProto = triggeredPowerEventProto.PowerEventContext as PowerEventContextTransformModePrototype;
                        if (!Verify.IsNotNull(contextProto))
                            continue;

                        TransformModePrototype transformModeProto = contextProto.TransformMode.As<TransformModePrototype>();
                        if (!Verify.IsNotNull(transformModeProto))
                            continue;

                        if (!Verify.IsTrue(transformModeProto.EnterTransformModePower != PrototypeId.Invalid, $"Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no EnterTransformMode power specified"))
                            continue;

                        if (!Verify.IsTrue(transformModeProto.ExitTransformModePower != PrototypeId.Invalid, $"Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no ExitTransformModePower power specified"))
                            continue;

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
                if (powerEventType == PowerEventType.OnTargetKill ||
                    (powerEventType == PowerEventType.OnPowerEnd && triggeredPowerEventProto.HasEvalEventTriggerChance == false))
                {
                    if (!Verify.IsTrue(MathHelper.BitTest(assignedPowers, 1 << (int)powerEventType) == false,
                        $"This power contains multiple powers of type OnTargetKill or OnPowerEnded\nPower: [{powerProto}]"))
                    {
                        triggeredPowerRefList.Clear();
                        continue;
                    }
                }

                MathHelper.BitSet(ref assignedPowers, 1 << (int)powerEventType);

                // Assign triggered powers we determined we need to assign
                foreach (PrototypeId triggeredPowerRef in triggeredPowerRefList)
                {
                    if (!Verify.IsTrue(triggeredPowerRef != PrototypeId.Invalid))
                        continue;

                    // Some powers apparently can trigger themselves, no need to assign them again
                    if (triggeredPowerRef == powerProtoRef)
                        continue;

                    if (!Verify.IsNotNull(AssignPower(triggeredPowerRef, indexProps, powerProtoRef, false)))
                        return false;
                }

                triggeredPowerRefList.Clear();
            }

            return true;
        }

        private bool UnassignPowerInternal(PrototypeId powerProtoRef, bool sendPowerUnassignToClients)
        {
            if (!Verify.IsNotNull(_owner)) return false;

            Game game = _owner.Game;
            if (!Verify.IsNotNull(game)) return false;

            // Find and validate the record for our powerProtoRef
            PowerCollectionRecord powerRecord = GetPowerRecordByRef(powerProtoRef);
            if (!Verify.IsNotNull(powerRecord, $"When unassigning, failed to find power record for {powerProtoRef.GetName()}\n  Owner:[{_owner}]\n  NumRecordsInCollection:{_powers.Count} NumCondemnedPowers:{_condemnedPowers.Count}"))
                return false;

            if (!Verify.IsNotNull(powerRecord.Power, $"When unassigning, the power record was found but had no power instance! Power: [{powerProtoRef.GetName()}], RefCount: [{powerRecord.PowerRefCount}], Owner: [{_owner}]"))
                return false;

            if (!Verify.IsTrue(powerRecord.PowerRefCount > 0, $"When unassigned, the power record had an invalid refcount! Power: [{powerProtoRef.GetName()}], RefCount: [{powerRecord.PowerRefCount}], Owner: [{_owner}]"))
                return false;

            // Start by subtracting from the PowerRefCount
            powerRecord.PowerRefCount--;

            // Remove the record when our PowerRefCount reaches 0
            if (powerRecord.PowerRefCount == 0)
            {
                FinishUnassignPower(powerRecord.Power);

                game.EntityManager.RegisterEntityForCondemnedPowerDeletion(_owner.Id);
                _condemnedPowers.Push(powerRecord.Power);
                powerRecord.ClearPower();

                Verify.IsTrue(DestroyPowerRecord(powerRecord.PowerPrototypeRef));
            }

            // Send power unassignment message to interested clients
            if (sendPowerUnassignToClients && _owner.IsInGame && _owner.IsInWorld)
            {
                var unassignPowerMessage = NetMessagePowerCollectionUnassignPower.CreateBuilder()
                    .SetEntityId(_owner.Id)
                    .SetPowerProtoId((ulong)powerProtoRef)
                    .Build();

                game.NetworkManager.SendMessageToInterested(unassignPowerMessage, _owner, AOINetworkPolicyValues.AOIChannelProximity);
            }

            return true;
        }

        private bool DestroyPowerRecord(PrototypeId powerProtoRef)
        {
            if (_powers.TryGetValue(powerProtoRef, out PowerCollectionRecord powerRecord) == false)
                return false;

            Verify.IsTrue(powerRecord.Power == null && powerRecord.PowerRefCount == 0, $"Power Record not empty during Destroy Power Record - Power Ref [{powerProtoRef.GetName()}]");

            _powers.Remove(powerProtoRef);
            return true;
        }

        private void FinishUnassignPower(Power power)
        {
            if (power.GetPowerCategory() == PowerCategoryType.ThrowablePower)
            {
                Verify.IsNotNull(ThrowablePower, $"Trying to unassign a throwable power when this entity does not have a throwable power in its power collection. \nEntity: [{_owner}]");
                Verify.IsTrue(ThrowablePower == power, $"Trying to unassign a throwable power that isn't the same as this power collection's throwable power. \nEntity: [{_owner}]");

                ThrowablePower = null;
            }
            else if (power.GetPowerCategory() == PowerCategoryType.ThrowableCancelPower)
            {
                Verify.IsNotNull(ThrowableCancelPower, $"Trying to unassign a throwable cancel power when this entity does not have a throwable cancel power in its power collection. \nEntity: [{_owner}]");
                Verify.IsTrue(ThrowableCancelPower == power, $"Trying to unassign a throwable cancel power that isn't the same as this power collection's throwable cancel power. \nEntity: [{_owner}]");

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
            if (!Verify.IsNotNull(_owner)) return false;

            PowerPrototype powerProto = power.Prototype;
            if (!Verify.IsNotNull(powerProto)) return false;

            PrototypeId powerProtoRef = power.PrototypeDataRef;
            if (!Verify.IsTrue(powerProtoRef != PrototypeId.Invalid)) return false;

            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return true;

            // NOTE: We reuse the same list for all iterations
            using var triggeredPowerRefListHandle = ListPool<PrototypeId>.Instance.Get(out List<PrototypeId> triggeredPowerRefList);

            foreach (PowerEventActionPrototype triggeredPowerEventProto in powerProto.ActionsTriggeredOnPowerEvent)
            {
                PowerEventType powerEventType = triggeredPowerEventProto.PowerEvent;
                if (!Verify.IsTrue(powerEventType != PowerEventType.None, $"This power contains a triggered power event with a null type\nPower: [{powerProto}]"))
                    continue;

                // Check if this power event has triggered powers that need assignment
                switch (triggeredPowerEventProto.EventAction)
                {
                    case PowerEventActionType.UsePower:
                    case PowerEventActionType.ScheduleActivationAtPercent:
                    case PowerEventActionType.ScheduleActivationInSeconds:
                        if (!Verify.IsTrue(triggeredPowerEventProto.Power != PrototypeId.Invalid, $"Power [{power}] for agent [{_owner}] has a triggered power event with no power specified"))
                            continue;

                        triggeredPowerRefList.Add(triggeredPowerEventProto.Power);
                        break;

                    case PowerEventActionType.TransformModeStart:
                        PowerEventContextTransformModePrototype contextProto = triggeredPowerEventProto.PowerEventContext as PowerEventContextTransformModePrototype;
                        if (!Verify.IsNotNull(contextProto))
                            continue;

                        TransformModePrototype transformModeProto = contextProto.TransformMode.As<TransformModePrototype>();
                        if (!Verify.IsNotNull(transformModeProto))
                            continue;

                        // NOTE: Assignment happens for any type of owner, but unassignment is for avatars only. Is this intended?
                        if (_owner is Avatar avatar)
                        {
                            PrototypeId currentTransformMode = avatar.CurrentTransformMode;

                            if (currentTransformMode == PrototypeId.Invalid || currentTransformMode != transformModeProto.DataRef)
                            {
                                if (!Verify.IsTrue(transformModeProto.EnterTransformModePower != PrototypeId.Invalid, $"Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no EnterTransformMode power specified"))
                                    continue;

                                if (!Verify.IsTrue(transformModeProto.ExitTransformModePower != PrototypeId.Invalid, $"Power [{power}] for agent [{_owner}] has a triggered TransformModeStart power event with no ExitTransformModePower power specified"))
                                    continue;

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
                    if (!Verify.IsTrue(triggeredPowerRef != PrototypeId.Invalid))
                        continue;

                    // Some powers apparently can trigger themselves, no need to unassign them
                    if (triggeredPowerRef == powerProtoRef)
                        continue;

                    UnassignPower(triggeredPowerRef, false);
                }

                triggeredPowerRefList.Clear();
            }

            return true;
        }
    }
}
