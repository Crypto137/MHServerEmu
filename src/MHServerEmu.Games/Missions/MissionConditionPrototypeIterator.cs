using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;
using System.Collections;

namespace MHServerEmu.Games.Missions
{
    public class MissionConditionPrototypeIterator : IEnumerable<MissionConditionPrototype>
    {
        private readonly MissionConditionPrototype[] _conditions;
        private readonly Type _conditionType;
        private int _currentIndex;
        private MissionConditionPrototypeIterator _sublistIterator;

        public MissionConditionPrototypeIterator(MissionConditionListPrototype list, Type conditionType = null)
        {
            _conditions = list?.Conditions;
            _conditionType = conditionType;
            if (_conditions.HasValue())
            {
                _currentIndex = 0;
                if (IsValid() == false) Advance();
            }
        }

        public IEnumerator<MissionConditionPrototype> GetEnumerator()
        {
            while (End() == false)
            {
                yield return Current;
                MoveNext();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool End() => _conditions == null || (_sublistIterator == null && _currentIndex >= _conditions.Length);

        public MissionConditionPrototype Current => GetMissionConditionPrototype();

        private MissionConditionPrototype GetMissionConditionPrototype()
        {
            if (_sublistIterator != null)
                return _sublistIterator.Current;
            else if (_currentIndex >= 0 && _currentIndex < _conditions.Length)
                return _conditions[_currentIndex];
            else
                return null;
        }

        public void MoveNext()
        {
            if (_conditions.HasValue()) Advance();
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
                        _sublistIterator = new(conditionList, _conditionType);
                    }
                    ++_currentIndex;
                }

                if (_sublistIterator != null && _sublistIterator.End())
                    _sublistIterator = null;
            }
            while (IsValid() == false);
        }

        private bool IsValid()
        {
            if (End()) return false;
            var condition = GetMissionConditionPrototype();
            if (condition == null) return false;
            if (_conditionType != null && condition.GetType() != _conditionType) return false;
            else if (condition is MissionConditionListPrototype) return false;
            return true;
        }
    }

}
