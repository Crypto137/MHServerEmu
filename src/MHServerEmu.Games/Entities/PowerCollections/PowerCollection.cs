using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
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
        // Relevant protobufs: NetMessagePowerCollectionAssignPower, NetMessageAssignPowerCollection,
        // NetMessagePowerCollectionUnassignPower, NetMessageUpdatePowerIndexProps

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly WorldEntity _owner;

        private SortedDictionary<PrototypeId, PowerCollectionRecord> _powerDict = new();
 
        public Power ThrowablePower { get; private set; }
        public Power ThrowableCancelPower { get; private set; }

        public PowerCollection(WorldEntity owner)
        {
            _owner = owner;
        }

        public static bool SerializeRecordCount(Archive archive, PowerCollection powerCollection, ref uint recordCount)
        {
            throw new NotImplementedException();
        }

        public static bool SerializeTo(Archive archive, PowerCollection powerCollection, uint recordCount)
        {
            throw new NotImplementedException();
        }

        public static bool SerializeFrom(Archive archive, PowerCollection powerCollection, uint recordCount)
        {
            throw new NotImplementedException();
        }

        public void Decode(CodedInputStream stream, AOINetworkPolicyValues replicationPolicy)
        {
            if (replicationPolicy.HasFlag(AOINetworkPolicyValues.AOIChannelProximity) == false) return;

            uint recordCount = stream.ReadRawVarint32();
            if (recordCount == 0) return;

            // The first record is standalone. Records that follow it will omit data that matches the previous one.
            PowerCollectionRecord previousRecord = null;
            for (uint i = 0; i < recordCount; i++)
            {
                PowerCollectionRecord record = new();
                record.Decode(stream, previousRecord);
                _powerDict.Add(record.PowerPrototypeRef, record);
                previousRecord = record;
            }
        }

        public void Encode(CodedOutputStream stream, AOINetworkPolicyValues replicationPolicy)
        {
            if (replicationPolicy.HasFlag(AOINetworkPolicyValues.AOIChannelProximity) == false) return;

            stream.WriteRawVarint32((uint)_powerDict.Count);

            PowerCollectionRecord previousRecord = null;
            foreach (PowerCollectionRecord record in _powerDict.Values)
            {
                record.Encode(stream, previousRecord);
                previousRecord = record;
            }
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

            // TODO: Uncomment IsInWorld check when we have world entities properly entering and exiting world
            if (Power.IsComboEffect(powerProto) == false && (_owner == null /* || _owner.IsInWorld == false */))
                return Logger.WarnReturn<Power>(null, "AssignPower(): PowerCollection only supports Assign() of powers while the owner is in world!");

            return AssignPowerInternal(powerProtoRef, indexProps, triggeringPowerRef, sendPowerAssignmentToClients);
        }

        public bool UnassignPower(PrototypeId powerProtoRef, bool sendPowerUnassignToClients = true)
        {
            // TODO: Uncomment IsInWorld check when we have world entities properly entering and exiting world
            if (_owner == null /* || _owner.IsInWorld == false */)
                return Logger.WarnReturn(false, "UnassignPower(): PowerCollection only supports Unassign() of powers while the owner is in world!");

            return UnassignPowerInternal(powerProtoRef, sendPowerUnassignToClients);
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
                                // TODO: Uncomment this once all avatars have a game
                                //var avatarOwner = _owner.GetMostResponsiblePowerUser<Avatar>();
                                Avatar avatarOwner = _owner.Game != null ? _owner.GetMostResponsiblePowerUser<Avatar>() : null;
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
                if (powerRecord == null) return Logger.WarnReturn<Power>(null, "AssignPowerInternal(): powerRecord == null");
            }
            else
            {
                Logger.ErrorReturn<Power>(null, "AssignPowerInternal(): Assigning a power multiple times is not yet implemented");
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
            if (powerProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn<Power>(null, "CreatePower(): powerProtoRef == PrototypeId.Invalid");

            // TODO: owner null check
            // TODO: owner game null check

            Power power = new(null, powerProtoRef);

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
            if (power.PowerCategory == PowerCategoryType.ThrowablePower)
            {
                if (ThrowablePower != null)
                    Logger.Warn("FinishAssignPower(): Trying to assign a throwable power when this entity already has a throwable power in its power collection");

                ThrowablePower = power;
            }
            else if (power.PowerCategory == PowerCategoryType.ThrowableCancelPower)
            {
                if (ThrowableCancelPower != null)
                    Logger.Warn("FinishAssignPower(): Trying to assign a throwable cancel power when this entity already has a throwable cancel power in its power collection");

                ThrowableCancelPower = power;
            }

            // TODO: PowerCollection::assignTriggeredPowers()
            // TODO: _owner.OnPowerAssigned()
            power.OnAssign();
        }

        private bool UnassignPowerInternal(PrototypeId powerProtoRef, bool sendPowerUnassignToClients)
        {
            if (_owner == null) return Logger.WarnReturn(false, "UnassignPowerInternal(): _owner == null");
            // TODO: Uncomment this later
            //if (_owner.Game == null) return Logger.WarnReturn(false, "UnAssignPowerInternal(): _owner.Game == null");

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

                // TODO: EntityManager::RegisterEntityForCondemnedPowerDeletion()

                DestroyPowerRecord(powerRecord.PowerPrototypeRef);
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
            if (power.PowerCategory == PowerCategoryType.ThrowablePower)
            {
                if (ThrowablePower == null)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable power when this entity does not have a throwable power in its power collection");

                if (ThrowablePower != power)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable power that isn't the same as this power collection's throwable power");

                ThrowablePower = null;
            }
            else if (power.PowerCategory == PowerCategoryType.ThrowableCancelPower)
            {
                if (ThrowableCancelPower == null)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable cancel power when this entity does not have a throwable cancel power in its power collection");

                if (ThrowableCancelPower != power)
                    Logger.Warn("FinishUnassignPower(): Trying to unassign a throwable cancel power that isn't the same as this power collection's throwable cancel power");

                ThrowableCancelPower = null;
            }

            if (_owner.IsDestroyed() == false)
            {
                // TODO: _owner.OnPowerUnAssigned()
            }

            // TODO: PowerCollection::unassignTriggeredPowers()
        }
    }
}
