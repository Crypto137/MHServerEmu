using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    public abstract class CallMethodEventParam1<TTarget, TParam1> : TargetedScheduledEvent<TTarget>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected TParam1 _param1;

        protected delegate void CallbackDelegate(TTarget target, TParam1 param1);

        protected abstract CallbackDelegate GetCallback();

        public void Initialize(TTarget target, TParam1 param1)
        {
            _eventTarget = target;
            _param1 = param1;
        }

        public override bool OnTriggered()
        {
            if (_eventTarget == null) return Logger.WarnReturn(false, "OnTriggered(): _eventTarget == null");
            GetCallback().Invoke(_eventTarget, _param1);
            return true;
        }

        public override void Clear()
        {
            base.Clear();
            _param1 = default;
        }
    }
}
