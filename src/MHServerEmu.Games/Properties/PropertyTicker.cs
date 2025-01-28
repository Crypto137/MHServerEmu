using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData.Prototypes;

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

        private ulong _creatorId;
        private ulong _ultimateCreatorId;
        private ulong _conditionId;
        private PowerPrototype _powerProto;
        private readonly List<KeyValuePair<PropertyId, PropertyValue>> _propertyList = new();

        private TimeSpan _tickingStartTime = TimeSpan.Zero;
        private int _remainingTicks = InfiniteTicks;

        public ulong Id { get; private set; }

        public PropertyTicker()
        {
            _game = Game.Current;
        }

        public override string ToString()
        {
            return $"tickerId={Id}, powerProto={_powerProto}, targetId={_targetId}";
        }

        public bool Initialize(ulong id, PropertyCollection properties, ulong targetId, ulong creatorId, ulong ultimateCreatorId,
            TimeSpan updateInterval, ulong conditionId, PowerPrototype powerProto, bool targetsUltimateCreator)
        {
            // Set data
            Id = id;

            _targetId = targetId;
            _updateInterval = updateInterval;

            _creatorId = creatorId;
            _ultimateCreatorId = ultimateCreatorId;
            _conditionId = conditionId;
            _powerProto = powerProto;

            // Override target and ultimate creator if needed
            WorldEntity ultimateCreator = null;
            if (_ultimateCreatorId != Entity.InvalidId)
            {
                EntityManager entityManager = _game.EntityManager;
                ultimateCreator = entityManager.GetEntity<WorldEntity>(_ultimateCreatorId);
                while (ultimateCreator != null && ultimateCreator.HasPowerUserOverride)
                {
                    _ultimateCreatorId = ultimateCreator.PowerUserOverrideId;
                    ultimateCreator = entityManager.GetEntity<WorldEntity>(_ultimateCreatorId);
                }
            }

            if (ultimateCreator != null && targetsUltimateCreator)
                _targetId = _ultimateCreatorId;

            // Store over time properties in a list since it's more lightweight than a full property collection
            foreach (var kvp in properties)
            {
                if (Property.OverTimeProperties.Contains(kvp.Key.Enum) == false)
                    continue;

                _propertyList.Add(kvp);
            }

            return true;
        }

        public bool Start(TimeSpan duration, bool isPaused)
        {
            if (IsTicking())
                return Logger.WarnReturn(false, $"Start(): Ticker [{this}] is already ticking");

            Logger.Debug($"Start(): [{this}]");

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

            Logger.Debug($"Stop(): [{this}]");

            _tickingStartTime = TimeSpan.Zero;
            CancelScheduledTick();

            if (tickOnStop && _remainingTicks > 0)
                Tick(true);

            return true;
        }

        public void Update(TimeSpan duration, bool isPaused)
        {
            Logger.Debug($"Update(): [{this}]");

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
            // TODO
            return false;
        }

        private void Tick(bool finishTicking)
        {
            //Logger.Debug($"Tick(): [{this}]");

            WorldEntity target = _game.EntityManager.GetEntity<WorldEntity>(_targetId);
            if (target == null)
                return;

            if (_remainingTicks != InfiniteTicks)
                _remainingTicks--;

            // TODO: apply over time properties to target

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

        private class TickEvent : CallMethodEventParam1<PropertyTicker, bool>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.Tick(p1);
        }
    }
}
