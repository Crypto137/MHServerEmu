namespace MHServerEmu.Games.Dialog
{
    public class CircleOption : InteractionOption
    {
        public CircleOption()
        {
            Priority = 50;
        }
    }

    public class FriendOption : CircleOption
    {
        public FriendOption()
        {
            MethodEnum = InteractionMethod.Friend;
        }
    }

    public class IgnoreOption : CircleOption
    {
        public IgnoreOption()
        {
            MethodEnum = InteractionMethod.Ignore;
        }
    }

    public class UnfriendOption : CircleOption
    {
        public UnfriendOption()
        {
            MethodEnum = InteractionMethod.Unfriend;
        }
    }

    public class UnignoreOption : CircleOption
    {
        public UnignoreOption()
        {
            MethodEnum = InteractionMethod.Unignore;
        }
    }

}