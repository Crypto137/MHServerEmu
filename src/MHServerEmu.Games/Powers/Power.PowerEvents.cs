namespace MHServerEmu.Games.Powers
{
    public partial class Power
    {
        #region Event Handlers

        // Please keep these sorted by PowerEventType enum value

        public void HandleTriggerPowerEventOnContactTime()              // 1
        {

        }

        public void HandleTriggerPowerEventOnCriticalHit()              // 2
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnHitKeyword()               // 3
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnPowerApply()               // 4
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnPowerEnd()                 // 5
        {

        }

        public void HandleTriggerPowerEventOnPowerHit()                 // 6
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnPowerStart()               // 7
        {
            PowerActivationSettings settings = LastActivationSettings;
            settings.TriggeringPowerPrototypeRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnPowerStart, in settings);
        }

        public void HandleTriggerPowerEventOnProjectileHit()            // 8
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnStackCount()               // 9
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnTargetKill()               // 10
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnSummonEntity()             // 11
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnHoldBegin()                // 12
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnMissileHit()               // 13
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnMissileKilled()            // 14
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnHotspotNegated()           // 15
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnHotspotNegatedByOther()    // 16
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnHotspotOverlapBegin()      // 17
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnHotspotOverlapEnd()        // 18
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnRemoveCondition()          // 19
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnRemoveNegStatusEffect()    // 20
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnPowerPivot()               // 21
        {
            // client-only?
        }

        public void HandleTriggerPowerEventOnPowerToggleOn()            // 22
        {

        }

        public void HandleTriggerPowerEventOnPowerToggleOff()           // 23
        {

        }

        public void HandleTriggerPowerEventOnPowerStopped()             // 24
        {

        }

        public void HandleTriggerPowerEventOnExtraActivationCooldown()  // 25
        {

        }

        public void HandleTriggerPowerEventOnPowerLoopEnd()             // 26
        {

        }

        public void HandleTriggerPowerEventOnSpecializationPowerAssigned()      // 27
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnSpecializationPowerUnassigned()    // 28
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnEntityControlled()                 // 29
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnOutOfRangeActivateMovementPower()  // 30
        {

        }

        private void HandleTriggerPowerEvent(PowerEventType eventType, in PowerActivationSettings settings,
            int param = 0, MathComparisonType comparisonType = MathComparisonType.Invalid)
        {

        }

        #endregion

        #region Event Actions

        // Please keep these ordered by PowerEventActionType enum value

        private void DoPowerEventActionBodyslide()                  // 1
        {
            Logger.Warn($"DoPowerEventActionBodyslide(): Not implemented");
        }

        private void DoPowerEventActionCancelScheduledActivation()  // 2, 3
        {
            Logger.Warn($"DoPowerEventActionCancelScheduledActivation(): Not implemented");
        }

        private void DoPowerEventActionContextCallback()            // 4
        {
            Logger.Warn($"DoPowerEventActionContextCallback(): Not implemented");
        }

        private void DoPowerEventActionDespawnTarget()              // 5
        {
            Logger.Warn($"DoPowerEventActionDespawnTarget(): Not implemented");
        }

        private void DoPowerEventActionChargesIncrement()           // 6
        {
            Logger.Warn($"DoPowerEventActionChargesIncrement(): Not implemented");
        }

        private void DoPowerEventActionInteractFinish()             // 7
        {
            Logger.Warn($"DoPowerEventActionInteractFinish(): Not implemented");
        }

        private void DoPowerEventActionRestoreThrowable()           // 9
        {
            Logger.Warn($"DoPowerEventActionRestoreThrowable(): Not implemented");
        }

        private void DoPowerEventActionScheduleActivation()         // 8, 10, 11
        {
            Logger.Warn($"DoPowerEventActionScheduleActivation(): Not implemented");
        }

        private void DoPowerEventActionShowBannerMessage()          // 12
        {
            Logger.Warn($"DoPowerEventActionShowBannerMessage(): Not implemented");
        }

        private void DoPowerEventActionSpawnLootTable()             // 13
        {
            Logger.Warn($"DoPowerEventActionSpawnLootTable(): Not implemented");
        }

        private void DoPowerEventActionSwitchAvatar()               // 14
        {
            Logger.Warn($"DoPowerEventActionSwitchAvatar(): Not implemented");
        }

        private void DoPowerEventActionTogglePower()                // 15, 16
        {
            Logger.Warn($"DoPowerEventActionTogglePower(): Not implemented");
        }

        private void DoPowerEventActionTransformModeChange()        // 17
        {
            Logger.Warn($"DoPowerEventActionTransformModeChange(): Not implemented");
        }

        private void DoPowerEventActionTransformModeStart()         // 18
        {
            Logger.Warn($"DoPowerEventActionTransformModeStart(): Not implemented");
        }

        private void DoPowerEventActionUsePower()                   // 19
        {
            Logger.Warn($"DoPowerEventActionUsePower(): Not implemented");
        }

        private void DoPowerEventActionTeleportToPartyMember()      // 20
        {
            Logger.Warn($"DoPowerEventActionTeleportToPartyMember(): Not implemented");
        }

        private void DoPowerEventActionControlAgentAI()             // 21
        {
            Logger.Warn($"DoPowerEventActionControlAgentAI(): Not implemented");
        }

        private void DoPowerEventActionRemoveAndKillControlledAgentsFromInv()   // 22
        {
            Logger.Warn($"DoPowerEventActionRemoveAndKillControlledAgentsFromInv(): Not implemented");
        }

        private void DoPowerEventActionEndPower()                   // 23
        {
            Logger.Warn($"DoPowerEventActionEndPower(): Not implemented");
        }

        private void DoPowerEventActionCooldownStart()              // 24
        {
            Logger.Warn($"DoPowerEventActionCooldownStart(): Not implemented");
        }

        private void DoPowerEventActionCooldownEnd()                // 25
        {
            Logger.Warn($"DoPowerEventActionCooldownEnd(): Not implemented");
        }

        private void DoPowerEventActionCooldownModifySecs()         // 26
        {
            Logger.Warn($"DoPowerEventActionCooldownModifySecs(): Not implemented");
        }

        private void DoPowerEventActionCooldownModifyPct()          // 27
        {
            Logger.Warn($"DoPowerEventActionCooldownModifyPct(): Not implemented");
        }

        private void DoPowerEventActionTeamUpAgentSummon()          // 28
        {
            Logger.Warn($"DoPowerEventActionTeamUpAgentSummon(): Not implemented");
        }

        private void DoPowerEventActionTeleportRegion()             // 29
        {
            Logger.Warn($"DoPowerEventActionTeleportRegion(): Not implemented");
        }

        private void DoPowerEventActionStealPower()                 // 30
        {
            Logger.Warn($"DoPowerEventActionStealPower(): Not implemented");
        }

        private void DoPowerEventActionPetItemDonate()              // 31
        {
            Logger.Warn($"DoPowerEventActionPetItemDonate(): Not implemented");
        }

        private void DoPowerEventActionMapPowers()                  // 32
        {
            Logger.Warn($"DoPowerEventActionMapPowers(): Not implemented");
        }

        private void DoPowerEventActionUnassignMappedPowers()       // 33
        {
            Logger.Warn($"DoPowerEventActionUnassignMappedPowers(): Not implemented");
        }

        private void DoPowerEventActionRemoveSummonedAgentsWithKeywords()   // 34
        {
            Logger.Warn($"DoPowerEventActionRemoveSummonedAgentsWithKeywords(): Not implemented");
        }

        private void DoPowerEventActionSummonControlledAgentWithDuration()  // 35
        {
            Logger.Warn($"DoPowerEventActionSummonControlledAgentWithDuration(): Not implemented");
        }

        private void DoPowerEventActionLocalCoopEnd()               // 36
        {
            Logger.Warn($"DoPowerEventActionLocalCoopEnd(): Not implemented");
        }

        #endregion
    }
}
