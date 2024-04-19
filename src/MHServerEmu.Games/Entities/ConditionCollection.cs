using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Entities
{
    public class ConditionCollection : IEnumerable<KeyValuePair<ulong, Condition>>
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
            if (InsertCondition(condition) == false)
                return false;

            OnInsertCondition();
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

        private bool InsertCondition(Condition condition)
        {
            // TODO: ConditionCollection::Handle
            return _currentConditionDict.TryAdd(condition.Id, condition);
        }

        private void OnInsertCondition()
        {
            // NYI
        }
    }
}
