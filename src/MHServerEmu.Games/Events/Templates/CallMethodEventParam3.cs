using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    /// <summary>
    /// An abstract template for a <see cref="TargetedScheduledEvent{T}"/> that
    /// calls a method with 3 parameters on an instance of <typeparamref name="TTarget"/>.
    /// </summary>
    public abstract class CallMethodEventParam3<TTarget, TParam1, TParam2, TParam3> : TargetedScheduledEvent<TTarget>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected TParam1 _param1;
        protected TParam2 _param2;
        protected TParam3 _param3;

        protected delegate void CallbackDelegate(TTarget target, TParam1 param1, TParam2 param2, TParam3 param3);

        /// <summary>
        /// Initializes data for this <see cref="CallMethodEventParam3{TTarget, TParam1, TParam2, TParam3}"/>.
        /// </summary>
        public void Initialize(TTarget target, TParam1 param1, TParam2 param2, TParam3 param3)
        {
            _eventTarget = target;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
        }

        public override bool OnTriggered()
        {
            if (_eventTarget == null) return Logger.WarnReturn(false, "OnTriggered(): _eventTarget == null");
            GetCallback().Invoke(_eventTarget, _param1, _param2, _param3);
            return true;
        }

        public override void Clear()
        {
            base.Clear();
            _param1 = default;
            _param2 = default;
            _param3 = default;
        }

        /// <summary>
        /// Returns the <see cref="CallbackDelegate"/> that is invoked when this <see cref="CallMethodEventParam3{TTarget, TParam1, TParam2, TParam3}"/> is triggered.
        /// </summary>
        protected abstract CallbackDelegate GetCallback();
    }
}
