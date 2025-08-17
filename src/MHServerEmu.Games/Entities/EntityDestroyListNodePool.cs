using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Entities
{
    internal class EntityDestroyListNodePool
    {
        private const int ChunkSize = 256;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly uint _gameThreadId;
        private readonly Stack<LinkedListNode<ulong>> _nodeStack = new();
        private int _numChunks = 0;

        [ThreadStatic]
        internal static EntityDestroyListNodePool Instance;

        public EntityDestroyListNodePool(uint gameThreadId)
        {
            _gameThreadId = gameThreadId;
        }

        public LinkedListNode<ulong> Get(ulong entityId)
        {
            if (_nodeStack.Count == 0)
            {
                Logger.Trace($"Get(): Allocating chunk {++_numChunks} for game thread {_gameThreadId}");
                for (int i = 0; i < ChunkSize; i++)
                    _nodeStack.Push(new(0));
            }

            LinkedListNode<ulong> node = _nodeStack.Pop();
            node.Value = entityId;
            return node;
        }

        public void Return(LinkedListNode<ulong> node)
        {
            if (node.List != null)
                throw new Exception("Attempted to return a destroy list node that is currently in a list.");

            _nodeStack.Push(node);
        }
    }
}
