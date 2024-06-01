using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Dialog
{
    public class OpenMTXStoreOption : InteractionOption
    {
        public OpenMTXStoreOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.OpenMTXStore;
            IndicatorType = HUDEntityOverheadIcon.Vendor;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            var interactee = interacteeDesc.GetEntity<WorldEntity>(interactor.Game);
            bool isAvailable = false;

            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                isAvailable = interactee != null 
                    && interactor.IsDead == false 
                    && interactee.Properties.HasProperty(PropertyEnum.OpenMTXStore);
            }
            return isAvailable;
        }

        public override void FillOutputData(bool isAvailable, ref InteractionMethod outInteractions, ref InteractData outInteractData, WorldEntity localInteractee, WorldEntity interactor)
        {
            base.FillOutputData(isAvailable, ref outInteractions, ref outInteractData, localInteractee, interactor);

            if (localInteractee == null) return;
            if (isAvailable && outInteractData?.DialogDataCollection != null)
            {
                if (localInteractee.Properties.HasProperty(PropertyEnum.OpenMTXStore))
                {
                    var game = localInteractee.Game;
                    if (game == null) return;
                    var storeDialogData = new MTXStoreDialogData
                    {
                        DialogText = LocaleStringId.Blank,
                        VoCategory = VOCategory.Default,
                        InteractorId = interactor.Id
                    };
                    var storeNameRef = localInteractee.Properties[PropertyEnum.OpenMTXStore];
                    storeDialogData.StoreName = GameDatabase.GetAssetName(storeNameRef);
                    storeDialogData.StoreId = 0;
                    outInteractData.DialogDataCollection.Add(storeDialogData);
                }
            }
        }

    }
}
