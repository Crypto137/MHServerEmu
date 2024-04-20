using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Entities
{
    public class ConditionCollection : IEnumerable<KeyValuePair<ulong, Condition>>, ISerialize
    {
        public const int MaxConditions = 256;
        public const ulong InvalidConditionId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly WorldEntity _owner;

        private SortedDictionary<ulong, Condition> _currentConditionDict = new();   // m_currentConditions

        public int Count { get => _currentConditionDict.Count; }    // Temp property for compatibility with our existing hacks

        public ConditionCollection(WorldEntity owner = null)
        {
            _owner = owner;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // if (archive.IsTransient) -> This wasn't originally used for persistent serialization, same as individual conditions
            if (archive.IsPacking)
            {
                if (_currentConditionDict.Count >= MaxConditions)
                    return Logger.ErrorReturn(false, $"Serialize(): _currentConditionDict.Count >= MaxConditions");

                uint numConditions = (uint)_currentConditionDict.Count;
                success &= Serializer.Transfer(archive, ref numConditions);

                foreach (Condition condition in _currentConditionDict.Values)
                    success &= condition.Serialize(archive, _owner);
            }
            else
            {
                if (_currentConditionDict.Count != 0)
                    return Logger.ErrorReturn(false, $"Serialize(): _currrentConditionDict is not empty");

                uint numConditions = 0;
                success &= Serializer.Transfer(archive, ref numConditions);

                if (numConditions >= MaxConditions)
                    return Logger.ErrorReturn(false, $"Serialize(): numConditions >= MaxConditions");

                for (uint i = 0; i < numConditions; i++)
                {
                    Condition condition = AllocateCondition();
                    success &= condition.Serialize(archive, _owner);
                    InsertCondition(condition);
                }
            }

            return success;
        }

        public void Decode(CodedInputStream stream)
        {
            if (_currentConditionDict.Count > 0)
                Logger.Warn($"Decode(): _currentConditionDict is not empty");

            uint numConditions = stream.ReadRawVarint32();
            for (ulong i = 0; i < numConditions; i++)
            {
                Condition condition = new();
                condition.Decode(stream);
                InsertCondition(condition);
            }
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32((uint)_currentConditionDict.Count);
            foreach (Condition condition in _currentConditionDict.Values)
                condition.Encode(stream);
        }

        public bool AddCondition(Condition condition)
        {
            if (condition == null)
                return Logger.WarnReturn(false, "AddCondition(): condition == null");

            if (InsertCondition(condition) == false)
                return false;

            OnInsertCondition(condition);
            return true;
        }

        public bool HasANegativeStatusEffectCondition()
        {
            foreach (Condition condition in _currentConditionDict.Values)
                if (condition != null && condition.IsANegativeStatusEffect()) return true;
            return false;
        }

        // TODO: ConditionCollection::Iterator implementation
        public IEnumerator<KeyValuePair<ulong, Condition>> GetEnumerator() => _currentConditionDict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Condition AllocateCondition()
        {
            // ConditionCollection::AllocateCondition() in the client also checks to make sure this collection has a valid owner / game.
            // However, this is going to break parsing of individual messages where owners do not exist in a game.
            // Possible solution: create a dummy game for parsing purposes.
            // if (_owner == null || _owner.Game == null)
            //     return null;

            return new();
        }

        private bool InsertCondition(Condition condition)
        {
            // TODO: ConditionCollection::Handle
            return _currentConditionDict.TryAdd(condition.Id, condition);
        }

        private void OnInsertCondition(Condition condition)
        {
            // NYI
        }
    }
}
