using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Dialog
{
    public class StashOption : InteractionOption
    {
        public StashOption()
        {
            Priority = 51;
            MethodEnum = InteractionMethod.Use;
            IndicatorType = HUDEntityOverheadIcon.None;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            var interactee = interacteeDesc.GetEntity<WorldEntity>(interactor.Game);
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                isAvailable = interactee != null
                    && interactor.IsDead == false
                    && interactee.Properties[PropertyEnum.OpenPlayerStash];
            }
            return isAvailable;
        }

        public override void FillOutputData(bool isAvailable, ref InteractionMethod outInteractions, ref InteractData outInteractData, WorldEntity localInteractee, WorldEntity interactor)
        {
            base.FillOutputData(isAvailable, ref outInteractions, ref outInteractData, localInteractee, interactor);

            if (localInteractee == null) return;
            if (isAvailable && outInteractData?.DialogDataCollection != null)
            {
                if (localInteractee.Prototype is not WorldEntityPrototype interactEntityProto) return;
                var game = localInteractee.Game;
                if (game == null) return;
                var stashDialogData = new StashDialogData
                {
                    DialogText = LocaleStringId.Blank,
                    DialogStyle = interactEntityProto.DialogStyle,
                    VoCategory = VOCategory.Default,
                    InteractorId = interactor.Id
                };
                outInteractData.DialogDataCollection.Add(stashDialogData);
            }
        }

    }
}
