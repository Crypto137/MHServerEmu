using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Entities.Physics
{
    public class ForceSystem
    {
        public Vector3 Epicenter { get; private set; }
        public ForceSystemMemberList Members { get; private set; }
        public ForceSystem() 
        {
            Epicenter = Vector3.Zero;
            Members = new ();
        }
    }

    public class ForceSystemMemberList : InvasiveList<ForceSystemMember>
    {
        public ForceSystemMemberList(int maxIterators = 1) : base(maxIterators) { }
        public override InvasiveListNode<ForceSystemMember> GetInvasiveListNode(ForceSystemMember element, int listId) => element.InvasiveListNode;
    }

    public class ForceSystemMember
    {
        public ulong EntityId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public float Time { get; set; }
        public float Speed { get; set; }
        public float Acceleration { get; set; }
        public InvasiveListNode<ForceSystemMember> InvasiveListNode { get; private set; }

        public ForceSystemMember()
        {
            EntityId = 0;
            Position = Vector3.Zero; 
            Direction = Vector3.Zero;
            Time = 0.0f;
            Speed = 0.0f;
            Acceleration = 0.0f;
            InvasiveListNode = new();
        }
    }
}
