using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    /// <summary>
    /// An abstract template for a <see cref="TargetedScheduledEvent{T}"/> that
    /// calls a method with 1 parameter on an instance of <typeparamref name="TTarget"/>.
    /// </summary>
    public abstract class CallMethodEventParam1<TTarget, TParam1> : TargetedScheduledEvent<TTarget>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected TParam1 _param1;

        protected delegate void CallbackDelegate(TTarget target, TParam1 param1);

        /// <summary>
        /// Initializes data for this <see cref="CallMethodEventParam1{TTarget, TParam1}"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the <see cref="CallbackDelegate"/> that is invoked when this <see cref="CallMethodEventParam1{TTarget, TParam1}"/> is triggered.
        /// </summary>
        protected abstract CallbackDelegate GetCallback();
    }
}
