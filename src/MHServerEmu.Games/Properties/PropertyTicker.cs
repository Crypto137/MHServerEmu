using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers.Conditions;

namespace MHServerEmu.Games.Properties
{
    public class PropertyTicker
    {
        private const int InfiniteTicks = -1;
        public const ulong InvalidId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;
        private readonly EventPointer<TickEvent> _tickEvent = new();

        private ulong _targetId;
        private TimeSpan _duration;
        private TimeSpan _updateInterval;

        private readonly TickData _tickData = new();

        private TimeSpan _tickingStartTime = TimeSpan.Zero;
        private TimeSpan _lastTickTime = TimeSpan.Zero;
        private int _remainingTicks = InfiniteTicks;

        public ulong Id { get; private set; }

        public PropertyTicker()
        {
            _game = Game.Current;
        }

        public override string ToString()
        {
            return $"tickerId={Id}, targetId={_targetId}, tickData=[{_tickData}]";
        }

        public bool Initialize(ulong id, PropertyCollection properties, ulong targetId, ulong creatorId, ulong ultimateCreatorId,
            TimeSpan updateInterval, ulong conditionId, PowerPrototype powerProto, bool targetsUltimateCreator)
        {
            // Set data
            Id = id;

            _targetId = targetId;
            _updateInterval = updateInterval;

            _tickData.CreatorId = creatorId;
            _tickData.UltimateCreatorId = ultimateCreatorId;
            _tickData.ConditionId = conditionId;
            _tickData.PowerProto = powerProto;

            // Override target and ultimate creator if needed
            WorldEntity ultimateCreator = null;
            if (_tickData.UltimateCreatorId != Entity.InvalidId)
            {
                EntityManager entityManager = _game.EntityManager;
                ultimateCreator = entityManager.GetEntity<WorldEntity>(_tickData.UltimateCreatorId);
                while (ultimateCreator != null && ultimateCreator.HasPowerUserOverride)
                {
                    _tickData.UltimateCreatorId = ultimateCreator.PowerUserOverrideId;
                    ultimateCreator = entityManager.GetEntity<WorldEntity>(_tickData.UltimateCreatorId);
                }
            }

            if (ultimateCreator != null && targetsUltimateCreator)
                _targetId = _tickData.UltimateCreatorId;

            // Store over time properties in a list since it's more lightweight than a full property collection
            foreach (var kvp in properties)
            {
                if (Property.OverTimeProperties.Contains(kvp.Key.Enum) == false)
                    continue;

                _tickData.PropertyList.Add(kvp);
            }

            return true;
        }

        public bool Start(TimeSpan duration, bool isPaused)
        {
            if (IsTicking())
                return Logger.WarnReturn(false, $"Start(): Ticker [{this}] is already ticking");

            // A condition's duration does not run out when it is paused, so we get infinite ticks
            _tickingStartTime = _game.CurrentTime;
            _duration = Clock.Max(duration, TimeSpan.Zero);
            _remainingTicks = isPaused ? InfiniteTicks : CalculateRemainingTicks(true);

            if (IsTickOnStart())
                Tick(false);
            else
                ScheduleTick(_updateInterval, false);

            return true;
        }

        public bool Stop(bool tickOnStop)
        {
            if (IsTicking() == false)
                return Logger.WarnReturn(false, $"Stop(): Ticker [{this}] is not ticking");

            _tickingStartTime = TimeSpan.Zero;
            CancelScheduledTick();

            if (tickOnStop && _remainingTicks > 0)
                Tick(true);

            return true;
        }

        public void Update(TimeSpan duration, bool isPaused)
        {
            // A condition's duration does not run out when it is paused, so we get infinite ticks
            _duration = duration;
            _remainingTicks = isPaused ? InfiniteTicks : CalculateRemainingTicks(true);

            if (_tickEvent.IsValid == false)
                ScheduleTick(_updateInterval, false);
        }

        private bool IsTicking()
        {
            return _tickingStartTime != TimeSpan.Zero;
        }

        private bool IsTickOnStart()
        {
            // Do not tick on start for non-condition tickers
            ulong conditionId = _tickData.ConditionId;
            if (conditionId == ConditionCollection.InvalidConditionId)
                return false;

            WorldEntity target = _game.EntityManager.GetEntity<WorldEntity>(_targetId);
            if (target == null) return Logger.WarnReturn(false, "IsTickOnStart(): target == null");

            // Apply on start only to condition ticker that target their owner
            Condition condition = target.ConditionCollection?.GetCondition(conditionId);
            if (condition == null)
                return false;

            return condition.ShouldApplyInitialTickImmediately();
        }

        private void Tick(bool finishTicking)
        {
            WorldEntity target = _game.EntityManager.GetEntity<WorldEntity>(_targetId);
            if (target == null)
                return;

            if (_remainingTicks != InfiniteTicks)
                _remainingTicks--;

            // Over time properties specify their effect per second, but tickers can tick at varying rates.
            // Adjust per second value depending on how long this tick took.
            float tickDurationSeconds = GetTickDurationSeconds(finishTicking);

            // If our time multiplier reduces effect to 0, don't bother applying
            if (tickDurationSeconds > 0f)
            {
                _tickData.TickDurationSeconds = tickDurationSeconds;

                if (finishTicking == false)
                {
                    // Copy current tick data and schedule it to be applied at the end of the current frame
                    TickData tickDataToApply = new(_tickData);
                    target.ScheduleTickEvent(tickDataToApply);
                }
                else
                {
                    // Apply right now if we are finishing ticking
                    target.ApplyPropertyTicker(_tickData);
                }
            }

            _lastTickTime = _game.CurrentTime;

            if (finishTicking == false && _remainingTicks != 0)
                ScheduleTick(_updateInterval, false);
        }

        private int CalculateRemainingTicks(bool isStarting)
        {
            if (_duration == TimeSpan.Zero || _updateInterval == TimeSpan.Zero)
                return InfiniteTicks;

            int ticks = (int)(_duration.TotalMilliseconds / _updateInterval.TotalMilliseconds);

            if (isStarting)
                ticks++;

            if (_tickEvent.IsValid)
                ticks++;

            return ticks;
        }

        private float GetTickDurationSeconds(bool finishTicking)
        {
            // Start with the expected tick duration
            float tickDurationSeconds = (float)_updateInterval.TotalMilliseconds / 1000f;

            // Adjust the duration if the tick took more or less time than expected
            TimeSpan lastTickTime = _lastTickTime;

            // If we are ending before the first tick was able to happen as scheduled,
            // take into account how much time has passed.
            if (lastTickTime == TimeSpan.Zero && finishTicking)
                lastTickTime = _tickingStartTime;

            // Adjust tick duration
            if (lastTickTime != TimeSpan.Zero)
            {
                TimeSpan actualDuration = _game.CurrentTime - lastTickTime;
                float durationMult = Math.Clamp((float)actualDuration.Ticks / _updateInterval.Ticks, 0f, 1f);
                tickDurationSeconds *= durationMult;
            }

            return tickDurationSeconds;
        }

        private bool ScheduleTick(TimeSpan delay, bool isLastTick)
        {
            if (IsTicking() == false) return Logger.WarnReturn(false, "ScheduleTick(): IsTicking() == false");
            if (_tickEvent.IsValid) return Logger.WarnReturn(false, "ScheduleTick(): _tickEvent.IsValid");

            _game.GameEventScheduler.ScheduleEvent(_tickEvent, delay);
            _tickEvent.Get().Initialize(this, isLastTick);

            return true;
        }

        private bool CancelScheduledTick()
        {
            _game.GameEventScheduler.CancelEvent(_tickEvent);
            return true;
        }

        /// <summary>
        /// Container class for all the data needed to apply a <see cref="PropertyTicker"/> to a <see cref="WorldEntity"/>.
        /// </summary>
        public class TickData
        {
            public ulong CreatorId { get; set; }
            public ulong UltimateCreatorId { get; set; }
            public ulong ConditionId { get; set; }
            public PowerPrototype PowerProto { get; set; }
            public float TickDurationSeconds { get; set; }

            // TODO: Custom fixed array style struct for storing properties to apply (similar to Navi.ContentFlagCounts) or just pool the whole TickData
            public List<KeyValuePair<PropertyId, PropertyValue>> PropertyList { get; } = new();

            public TickData()
            {
            }

            public TickData(TickData other)
            {
                CreatorId = other.CreatorId;
                UltimateCreatorId = other.UltimateCreatorId;
                ConditionId = other.ConditionId;
                PowerProto = other.PowerProto;
                TickDurationSeconds = other.TickDurationSeconds;

                foreach (var kvp in other.PropertyList)
                    PropertyList.Add(kvp);
            }

            public override string ToString()
            {
                return $"PowerProto={PowerProto}, TickDurationSeconds={TickDurationSeconds}, PropertyList={string.Join(',', PropertyList.Select(kvp => kvp.Key.ToString()))}";
            }
        }

        private class TickEvent : CallMethodEventParam1<PropertyTicker, bool>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.Tick(p1);
        }
    }
}
