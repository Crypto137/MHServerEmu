
namespace MHServerEmu.Games.Dialog
{
    public class InteractionOption
    {
        public int Priority { get; set; }
        public int MethodEnum { get; }

        public InteractionOption()
        {
            Priority = 50;
            MethodEnum = 0;
        }

        public int CompareTo(InteractionOption other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}
