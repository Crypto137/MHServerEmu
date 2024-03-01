using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Prototypes;
using System.Collections;

namespace MHServerEmu.Games.Missions
{
    public class MissionConditionPrototypeIterator : IEnumerable<MissionConditionPrototype>
    {
        private readonly MissionConditionPrototype[] _conditions;
        private readonly Type _type;
        private readonly IEnumerator<MissionConditionPrototype> _enumerator;
        private MissionConditionPrototypeIterator _sublistIterator;

        public MissionConditionPrototypeIterator(MissionConditionListPrototype list, Type type = null)
        {
            _conditions = list?.Conditions;
            _type = type;
            if (_conditions.IsNullOrEmpty() == false)
            {
                _enumerator = (IEnumerator<MissionConditionPrototype>)_conditions.GetEnumerator();
                if (End() == false && IsValid() == false)
                    Advance();
            }
        }

        public IEnumerator<MissionConditionPrototype> GetEnumerator()
        {
            while (End() == false)
            {
                var current = Current;
                MoveNext();
                yield return current;                
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool End()
        {
            return _conditions.IsNullOrEmpty() || (_sublistIterator == null && _enumerator.MoveNext() == false);
        }

        public MissionConditionPrototype Current => GetMissionConditionPrototype();

        private MissionConditionPrototype GetMissionConditionPrototype()
        {
            if (_sublistIterator != null)
                return _sublistIterator.Current;
            else if (_enumerator != null)
                return _enumerator.Current;
            else
                return null;
        }

        public void MoveNext()
        {
            if (_conditions.IsNullOrEmpty() == false) Advance();
        }

        private void Advance()
        {
            do
            {
                if (_sublistIterator != null)
                    _sublistIterator.MoveNext();
                else
                {
                    var condition = GetMissionConditionPrototype();
                    if (condition == null) return;
                    if (condition is MissionConditionListPrototype conditionList)
                    {
                        if (_sublistIterator != null) return;
                        _sublistIterator = new (conditionList, _type);
                    }
                    _enumerator.MoveNext();
                }

                if (_sublistIterator != null && _sublistIterator.End())
                    _sublistIterator = null;
            }
            while (End() == false && IsValid() == false);
        }


        private bool IsValid()
        {
            if (End()) return false;

            var condition = GetMissionConditionPrototype();
            if (condition == null) return false;

            if (_type != null)
            {
                if (condition.GetType() != _type) return false;
            }
            else if (condition is MissionConditionListPrototype) return false; 

            return true;
        }
    }
}
