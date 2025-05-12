using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    /// <summary>
    /// An abstract template for a <see cref="TargetedScheduledEvent{T}"/> that
    /// calls a method with 2 parameters on an instance of <typeparamref name="TTarget"/>.
    /// </summary>
    public abstract class CallMethodEventParam2<TTarget, TParam1, TParam2> : TargetedScheduledEvent<TTarget>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected TParam1 _param1;
        protected TParam2 _param2;

        protected delegate void CallbackDelegate(TTarget target, TParam1 param1, TParam2 param2);

        /// <summary>
        /// Initializes data for this <see cref="CallMethodEventParam2{TTarget, TParam1, TParam2}"/>.
        /// </summary>
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

        public override void Clear()
        {
            base.Clear();
            _param1 = default;
            _param2 = default;
        }

        /// <summary>
        /// Returns the <see cref="CallbackDelegate"/> that is invoked when this <see cref="CallMethodEventParam2{TTarget, TParam1, TParam2}"/> is triggered.
        /// </summary>
        protected abstract CallbackDelegate GetCallback();
    }
}
