using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_StartMagikUltimate : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;

        public void Initialize(PlayerConnection playerConnection)
        {
            _playerConnection = playerConnection;
        }

        public override bool OnTriggered()
        {
            Avatar avatar = _playerConnection.Player.CurrentAvatar;

            Condition magikUltimateCondition = avatar.ConditionCollection.GetCondition(777);
            if (magikUltimateCondition != null) return Logger.WarnReturn(false, "OnStartMagikUltimate(): magikUltimateCondition != null");

            Logger.Trace($"EventStart Magik Ultimate");

            // Create and add a condition for the ultimate
            magikUltimateCondition = avatar.ConditionCollection.AllocateCondition();
            magikUltimateCondition.InitializeFromPowerMixinPrototype(777, (PrototypeId)PowerPrototypes.Magik.Ultimate, 0, TimeSpan.FromMilliseconds(20000));
            avatar.ConditionCollection.AddCondition(magikUltimateCondition);

            return true;
        }
    }
}
