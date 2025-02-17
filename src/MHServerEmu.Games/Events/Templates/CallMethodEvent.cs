using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    /// <summary>
    /// An abstract template for a <see cref="TargetedScheduledEvent{T}"/> that
    /// calls a method with no parameters on an instance of <typeparamref name="TTarget"/>.
    /// </summary>
    public abstract class CallMethodEvent<TTarget> : TargetedScheduledEvent<TTarget>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected delegate void CallbackDelegate(TTarget target);

        /// <summary>
        /// Initializes data for this <see cref="CallMethodEvent{TTarget}"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the <see cref="CallbackDelegate"/> that is invoked when this <see cref="CallMethodEvent{TTarget}"/> is triggered.
        /// </summary>
        protected abstract CallbackDelegate GetCallback();
    }
}
