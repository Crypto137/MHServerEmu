using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Games.Powers.Conditions
{
    public sealed class ConditionPool
    {
        // Allocate conditions in chunks of 256 instances, which should more than enough for one player.
        // Cap the maximum number of chunks at 128, which should be enough for how many players a single game can handle.
        private const int ChunkSize = 256;
        private const int MaxChunkCount = 128;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Stack<Condition> _conditionStack = new();
        private readonly HashSet<Condition> _activeConditions = new();  // Track all active conditions to prevent returning multiple times

        private int _chunkCount = 0;
        private int _allocatedCount = 0;

        public ConditionPool() { }

        public override string ToString()
        {
            return $"chunks={_chunkCount}/{MaxChunkCount}, active={_activeConditions.Count}/{_allocatedCount}";
        }

        public Condition Get()
        {
            Condition condition;

            if (_conditionStack.Count == 0 && AllocateChunk() == false)
            {
                Logger.Warn($"Get(): Exceeded maximum capacity ({this})");
                condition = new();
            }
            else
            {
                condition = _conditionStack.Pop();
            }

            condition.IsInPool = false;
            _activeConditions.Add(condition);
            //Logger.Debug($"Get(): {this}");
            return condition;
        }

        public bool Return(Condition condition)
        {
            if (_activeConditions.Remove(condition) == false)
                return Logger.WarnReturn(false, $"Return(): Condition [{condition}] is not an active condition tracked by this pool");  

            if (_conditionStack.Count >= _allocatedCount)
                return false;

            condition.Clear();
            condition.IsInPool = true;
            _conditionStack.Push(condition);
            return true;
        }

        public string GetConditionList()
        {
            StringBuilder sb = new();

            sb.AppendLine("StartTime\tCondition\tIsInCollection");

            foreach (Condition condition in _activeConditions.OrderBy(condition => condition.StartTime))
                sb.AppendLine($"{Clock.GameTimeToDateTime(condition.StartTime):yyyy.MM.dd HH:mm:ss.fff}\t{condition}\t{condition.IsInCollection}");

            return sb.ToString();
        }

        private bool AllocateChunk()
        {
            if (_chunkCount >= MaxChunkCount)
                return false;

            _chunkCount++;
            _allocatedCount += ChunkSize;

            _conditionStack.EnsureCapacity(_allocatedCount);
            _activeConditions.EnsureCapacity(_allocatedCount);

            for (int i = 0; i < ChunkSize; i++)
                _conditionStack.Push(new() { IsInPool = true });

            Logger.Trace($"AllocateChunk(): {this}");
            return true;
        }
    }
}
