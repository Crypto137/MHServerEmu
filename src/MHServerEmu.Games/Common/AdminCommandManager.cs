
namespace MHServerEmu.Games.Common
{
    public class AdminCommandManager
    {
        private Game _game;
        private AdminFlags _flags;

        public AdminCommandManager(Game game) 
        { 
            _game = game;
            _flags = AdminFlags.LocomotionSync;
        }

        public bool TestAdminFlag(AdminFlags flag)
        {
            return _flags.HasFlag(flag);
        }
    }

    [Flags]
    public enum AdminFlags
    {
        LocomotionSync = 1 << 1,
    }
}
