using MHServerEmu.Core.Memory;

namespace MHServerEmu.Games.Powers.Conditions
{
    public sealed class ConditionPool : ObjectPool<Condition>
    {
        // TODO: transfer ownership of condition instances from PowerResults as soon as they're applied and remove IsInPool field

        public ConditionPool() : base(ObjectPoolFlags.None) { }

        protected override Condition Allocate()
        {
            return new();
        }

        protected override void OnGet(Condition instance)
        {
            instance.IsInPool = false;
        }

        protected override void OnReturn(Condition instance)
        {
            instance.Clear();
            instance.IsInPool = true;
        }
    }
}
