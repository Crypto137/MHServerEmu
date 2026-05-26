using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Avatars
{
    // Enum names from version 1.0.4932.0, may be outdated
    // Also see CAvatar::triggerPowerStartInternal() for reference
    public enum PendingActionState
    {
        None,
        Targeting,
        MovingToRange,
        WaitingForPrevPower,
        AfterPowerMove,
        AfterPowerInteract,
        AfterCCMove,
        State7,                 // Set PowerUseResult.RestrictiveCondition
        MovingToInteract,
        VariableActivation,
        FindingLandingSpot,
        WaitingForThrowable,
        WaitingForAvatarSwitch,
        AvatarSwitchInProgress,
        DelayedPowerActivate,
        State15                 // Something to do with avatar switching
    }

    // NOTE: While pending actions are used extensively client-side to queue player input,
    // server side we really need them only for WaitingForPrevPower, DelayedPowerActivate and FindingLandingSpot.
    public class PendingAction : PendingPowerData
    {
        public PendingActionState PendingActionState { get; private set; }
        public InteractionMethod InteractionMethod { get; private set; }
        public InteractionFlags InteractionFlags { get; private set; }

        public override bool SetData(PrototypeId powerProtoRef, ulong targetId, Vector3 targetPosition, ulong sourceItemId)
        {
            // Breaking SOLID! Oh no! Anyway
            Verify.IsTrue(false, "You cannot use this version to set data on a PendingAction");
            return false;
        }

        public bool SetData(PendingActionState pendingState, PrototypeId powerProtoRef, ulong targetId, Vector3 targetPosition, ulong sourceItemId)
        {
            if (!Verify.IsTrue(pendingState != PendingActionState.None, "Please user Clear to set no pending action!"))
                return false;

            if (pendingState == PendingActionState.FindingLandingSpot)
                return false;

            if (pendingState == PendingActionState.VariableActivation)
                return false;

            if (base.SetData(powerProtoRef, targetId, targetPosition, sourceItemId) == false)
                return false;

            PendingActionState = pendingState;
            InteractionMethod = InteractionMethod.None;
            InteractionFlags = InteractionFlags.None;

            return true;
        }

        public bool SetData(PendingActionState pendingState, PrototypeId powerProtoRef, ulong targetId, Vector3 targetPosition,
            InteractionMethod interactionMethod, InteractionFlags interactionFlags)
        {
            if (!Verify.IsTrue(pendingState != PendingActionState.None, "Please user Clear to set no pending action!"))
                return false;

            if (pendingState == PendingActionState.FindingLandingSpot)
                return false;

            if (pendingState == PendingActionState.VariableActivation)
                return false;

            if (base.SetData(powerProtoRef, targetId, targetPosition, Entity.InvalidId) == false)
                return false;

            PendingActionState = pendingState;
            InteractionMethod = interactionMethod;
            InteractionFlags = interactionFlags;

            return true;
        }

        public override void Clear()
        {
            if (!Verify.IsTrue(PendingActionState != PendingActionState.FindingLandingSpot, "Avatar trying to clear the pending action state while moving to land - this will cause a failure to land and getting temporarily stuck flying."))
                return;

            base.Clear();

            PendingActionState = PendingActionState.None;
            InteractionMethod = InteractionMethod.None;
            InteractionFlags = InteractionFlags.None;
        }

        public void CancelFindLandingSpot()
        {
            if (PendingActionState == PendingActionState.FindingLandingSpot)
            {
                // PendingActionState needs to be explicitly set here to avoid triggering verify in Clear()
                PendingActionState = PendingActionState.None;
                Clear();
            }
        }
    }
}
