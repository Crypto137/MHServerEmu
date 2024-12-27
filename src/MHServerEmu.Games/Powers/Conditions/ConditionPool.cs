using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Powers.Conditions
{
    public sealed class ConditionPool
    {
        // Allocate conditions in chunks of 256 instances, which should more than enough for one player.
        // Cap the maximum number of chunks at 128, which should be enough for how many players a single game can handle.
        private const int ChunkSize = 256;
        private const int MaxChunkCount = 128;
        private const int MaxCapacity = ChunkSize * MaxChunkCount;

        private static readonly Logger Logger = LogManager.CreateLogger();

        [ThreadStatic]
        private static ConditionPool ThreadInstance;
        public static ConditionPool Instance { get { ThreadInstance ??= new(); return ThreadInstance; } }

        private readonly Stack<Condition> _conditionStack = new();

        private int _chunkCount = 0;

        private int _allocatedCount = 0;
        private int _activeCount = 0;

        private ConditionPool() { }

        public override string ToString()
        {
            return $"chunks={_chunkCount}/{MaxChunkCount}, active={_activeCount}/{_allocatedCount}";
        }

        public Condition Get()
        {
            _activeCount++;

            if (_conditionStack.Count == 0 && AllocateChunk() == false)
                return Logger.WarnReturn(new Condition(), $"Get(): Exceeded maximum capacity ({this})");

            //Logger.Debug($"Get(): {this}");
            return _conditionStack.Pop();
        }

        public void Return(Condition condition)
        {
            _activeCount--;

            if (_conditionStack.Count >= _allocatedCount)
                return;

            condition.Clear();
            _conditionStack.Push(condition);
        }

        private bool AllocateChunk()
        {
            if (_chunkCount >= MaxChunkCount)
                return false;

            _chunkCount++;
            _allocatedCount += ChunkSize;

            _conditionStack.EnsureCapacity(_allocatedCount);
            for (int i = 0; i < ChunkSize; i++)
                _conditionStack.Push(new());

            Logger.Trace($"AllocateChunk(): {this}");
            return true;
        }
    }
}
