using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities.PowerCollections
{
    public class PowerCollection : IEnumerable<KeyValuePair<PrototypeId, PowerCollectionRecord>>
    {
        // Relevant protobufs: NetMessagePowerCollectionAssignPower, NetMessageAssignPowerCollection,
        // NetMessagePowerCollectionUnassignPower, NetMessageUpdatePowerIndexProps

        private static readonly Logger Logger = LogManager.CreateLogger();

        private SortedDictionary<PrototypeId, PowerCollectionRecord> _powerDict = new();
 
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
                _powerDict.Add(record.PowerRef, record);
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

        public void TEMP_AddRecord(PowerCollectionRecord record)
        {
            // Temp method for compatibility with existing hacks
            _powerDict.Add(record.PowerRef, record);
        }

        // IEnumerable implementation
        public IEnumerator<KeyValuePair<PrototypeId, PowerCollectionRecord>> GetEnumerator() => _powerDict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
