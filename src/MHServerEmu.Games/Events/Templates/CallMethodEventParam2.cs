using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    public abstract class CallMethodEventParam2<TTarget, TParam1, TParam2> : TargetedScheduledEvent<TTarget>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected TParam1 _param1;
        protected TParam2 _param2;

        protected delegate void CallbackDelegate(TTarget target, TParam1 param1, TParam2 param2);

        protected abstract CallbackDelegate GetCallback();

        public void Initialize(TTarget target, TParam1 param1, TParam2 param2)
        {
            _eventTarget = target;
            _param1 = param1;
            _param2 = param2;
        }

        public override bool OnTriggered()
        {
            if (_eventTarget == null) return Logger.WarnReturn(false, "OnTriggered(): _eventTarget == null");
            GetCallback().Invoke(_eventTarget, _param1, _param2);
            return true;
        }
    }
}
