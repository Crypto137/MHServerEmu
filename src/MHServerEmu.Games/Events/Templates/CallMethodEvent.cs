using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    public abstract class CallMethodEvent<TTarget> : TargetedScheduledEvent<TTarget>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected delegate void CallbackDelegate(TTarget target);

        protected abstract CallbackDelegate GetCallback();

        public void Initialize(TTarget target)
        {
            _eventTarget = target;
        }

        public override bool OnTriggered()
        {
            if (_eventTarget == null) return Logger.WarnReturn(false, "OnTriggered(): _eventTarget == null");
            GetCallback().Invoke(_eventTarget);
            return true;
        }
    }
}
