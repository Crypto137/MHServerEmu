using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Dialog
{
    public class VendorOption : InteractionOption
    {
        public VendorOption()
        {
            MethodEnum = InteractionMethod.Converse;
            IndicatorType = HUDEntityOverheadIcon.None;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                var interactee = interacteeDesc.GetEntity<WorldEntity>(interactor.Game);
                isAvailable = interactee != null 
                    && interactee.IsVendor 
                    && interactee.IsDead == false
                    && interactor.IsDead == false;
            }
            return isAvailable;
        }

        public override void FillOutputData(bool isAvailable, ref InteractionMethod outInteractions, ref InteractData outInteractData, WorldEntity localInteractee, WorldEntity interactor)
        {
            base.FillOutputData(isAvailable, ref outInteractions, ref outInteractData, localInteractee, interactor);

            if (localInteractee == null || outInteractData == null) return;
            var vendorTypeProtoRef = localInteractee.Properties[PropertyEnum.VendorType];
            if (vendorTypeProtoRef == PrototypeId.Invalid) 
            {
                Logger.Debug($"Entity has vendor interaction option but doesn't have VendorType set!\nEntity: {localInteractee}");
                return; 
            }

            var vendorTypeProto = GameDatabase.GetPrototype<VendorTypePrototype>(vendorTypeProtoRef);
            if (vendorTypeProto == null) return;
            PrototypeId? none = null;
            InteractionManager.TrySetIndicatorTypeAndMapOverrideWithPriority(localInteractee, ref outInteractData.IndicatorType, ref none, vendorTypeProto.InteractIndicator);
            if (outInteractData.DialogDataCollection != null)
            {
                if (localInteractee.Prototype is not WorldEntityPrototype interactEntityProto) return;
                var game = localInteractee.Game;
                if (game == null) return;
                var vendorDialogData = new VendorDialogData
                {
                    DialogText = LocaleStringId.Blank,
                    DialogStyle = interactEntityProto.DialogStyle,
                    VoCategory = VOCategory.Default,
                    AllowActionDonate = vendorTypeProto.AllowActionDonate,
                    AllowActionRefresh = vendorTypeProto.AllowActionRefresh,
                    IsCrafter = vendorTypeProto.IsCrafter,
                    IsRaidVendor = vendorTypeProto.IsRaidVendor,
                    IsGlobalEvent = vendorTypeProto.GlobalEvent != PrototypeId.Invalid,
                    InteractorId = interactor.Id
                };
                outInteractData.DialogDataCollection.Add(vendorDialogData);
            }            
        }
    }
}

