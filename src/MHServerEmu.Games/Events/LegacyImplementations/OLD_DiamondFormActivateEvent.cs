using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_DiamondFormActivateEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; set; }

        public override bool OnTriggered()
        {
            Avatar avatar = PlayerConnection.Player.CurrentAvatar;

            Condition diamondFormCondition = avatar.ConditionCollection.GetCondition(111);
            if (diamondFormCondition != null) return false;

            Logger.Trace($"Event Start EmmaDiamondForm");

            // Get the asset id for the current costume to set the correct owner asset id override
            PrototypeId emmaCostume = avatar.Properties[PropertyEnum.CostumeCurrent];
            // Invalid prototype id is the same as the default costume
            if (emmaCostume == PrototypeId.Invalid)
                emmaCostume = GameDatabase.GetPrototypeRefByName("Entity/Items/Costumes/Prototypes/EmmaFrost/Modern.prototype");    // MarvelPlayer_EmmaFrost_Modern

            AssetId costumeAsset = emmaCostume.As<CostumePrototype>().CostumeUnrealClass;

            // Create and add a condition for the diamond form
            diamondFormCondition = avatar.ConditionCollection.AllocateCondition();
            diamondFormCondition.InitializeFromPowerMixinPrototype(111, (PrototypeId)OLD_PowerPrototypes.EmmaFrost.DiamondFormCondition, 0, TimeSpan.Zero, true, costumeAsset);
            avatar.ConditionCollection.AddCondition(diamondFormCondition);

            return true;
        }
    }
}
