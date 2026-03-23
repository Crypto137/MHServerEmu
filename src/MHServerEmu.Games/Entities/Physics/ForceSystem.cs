using MHServerEmu.Core.Collections;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Entities.Physics
{
    public class ForceSystem
    {
        public Vector3 Epicenter { get; set; }
        public ForceSystemMemberList Members { get; private set; }

        public ForceSystem(Vector3 epicenter) 
        {
            Epicenter = epicenter;
            Members = new ();
        }
    }

    public sealed class ForceSystemMemberList : InvasiveList<ForceSystemMember>
    {
        public ForceSystemMemberList(int maxIterators = 1) : base(maxIterators) { }
        public override ref InvasiveListNode<ForceSystemMember> GetInvasiveListNode(ForceSystemMember element, int listId) => ref element.InvasiveListNode;
    }

    public class ForceSystemMember
    {
        private InvasiveListNode<ForceSystemMember> _invasiveListNode;

        public ulong EntityId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public float Time { get; set; }
        public float Speed { get; set; }
        public float Acceleration { get; set; }
        public ref InvasiveListNode<ForceSystemMember> InvasiveListNode { get => ref _invasiveListNode; }

        public ForceSystemMember() { }
    }
}
