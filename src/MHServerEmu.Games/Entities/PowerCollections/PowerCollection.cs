using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
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

        private WorldEntity _owner;

        private SortedDictionary<PrototypeId, PowerCollectionRecord> _powerDict = new();
 
        public Power ThrowablePower { get; private set; }
        public Power ThrowableCancelPower { get; private set; }

        public PowerCollection() { }

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

        public Power AssignPower(PrototypeId powerProtoRef, PowerIndexProperties indexProps, PrototypeId triggeringPowerRef = PrototypeId.Invalid, bool sendPowerAssignmentToClients = true)
        {
            // TODO: More validation checks

            return AssignPowerInternal(powerProtoRef, indexProps, triggeringPowerRef, sendPowerAssignmentToClients);
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
                // Determine the source of this power
                bool isAvatarPowerProgressionPower = false;
                bool isTeamUpPassivePowerWhileAway = false;

                // Inherit the source from the triggering power if we have one
                PowerCollectionRecord triggeringPowerRecord = GetPowerRecordByRef(triggeringPowerRef);
                if (triggeringPowerRecord != null)
                {
                    isAvatarPowerProgressionPower = triggeringPowerRecord.IsAvatarPowerProgressionPower;
                    isTeamUpPassivePowerWhileAway = triggeringPowerRecord.IsTeamUpPassivePowerWhileAway;
                }
                else //if (_owner != null)
                {
                    // Figure out the source from the power collection's owner, skip this for now
                }

                powerRecord = CreatePowerRecord(powerProtoRef, indexProps, triggeringPowerRef, isAvatarPowerProgressionPower, isTeamUpPassivePowerWhileAway);
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
    }
}
