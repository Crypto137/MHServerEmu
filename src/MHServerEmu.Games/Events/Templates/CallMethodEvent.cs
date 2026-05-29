using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events.Templates
{
    /// <summary>
    /// An abstract template for a <see cref="TargetedScheduledEvent{T}"/> that
    /// calls a method with no parameters on an instance of <typeparamref name="TTarget"/>.
    /// </summary>
    public abstract class CallMethodEvent<TTarget> : TargetedScheduledEvent<TTarget>
    {
        protected delegate void CallbackDelegate(TTarget target);

        /// <summary>
        /// Initializes data for this <see cref="CallMethodEvent{TTarget}"/>.
        /// </summary>
        public void Initialize(TTarget target)
        {
            _eventTarget = target;
        }

        public override void OnTriggered()
        {
            if (!Verify.IsTrue(_eventTarget != null)) return;
            GetCallback().Invoke(_eventTarget);
        }

        /// <summary>
        /// Returns the <see cref="CallbackDelegate"/> that is invoked when this <see cref="CallMethodEvent{TTarget}"/> is triggered.
        /// </summary>
        protected abstract CallbackDelegate GetCallback();
    }
}
