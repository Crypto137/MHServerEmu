using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public partial class Power
    {
        #region Event Handlers

        // Please keep these sorted by PowerEventType enum value

        public void HandleTriggerPowerEventOnContactTime()              // 1
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnContactTime, ref settings);
        }

        public void HandleTriggerPowerEventOnCriticalHit(PowerResults powerResults)              // 2
        {
            PowerActivationSettings settings = powerResults.ActivationSettings;
            settings.TargetEntityId = powerResults.TargetId;
            settings.PowerResults = powerResults;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnCriticalHit, ref settings);
        }

        public void HandleTriggerPowerEventOnHitKeyword(PowerResults powerResults)               // 3
        {
            PowerActivationSettings settings = powerResults.ActivationSettings;
            settings.TargetEntityId = powerResults.TargetId;
            settings.PowerResults = powerResults;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnHitKeyword, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerApply(ref PowerActivationSettings payloadSettings)  // 4
        {
            PowerActivationSettings settings = payloadSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnPowerApply, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerEnd()                 // 5
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnPowerEnd, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerHit(PowerResults powerResults, ref PowerActivationSettings payloadSettings)    // 6
        {
            PowerActivationSettings settings = payloadSettings;
            settings.TargetEntityId = powerResults.TargetId;
            settings.PowerResults = powerResults;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner | PowerActivationSettingsFlags.ServerCombo;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(powerResults.TargetId);
            if (target != null)
                settings.TargetPosition = target.RegionLocation.Position;

            HandleTriggerPowerEvent(PowerEventType.OnPowerHit, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerStart()               // 7
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnPowerStart, ref settings);
        }

        public void HandleTriggerPowerEventOnProjectileHit(PowerResults powerResults)            // 8
        {
            PowerActivationSettings settings = powerResults.ActivationSettings;
            settings.PowerResults = powerResults;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnProjectileHit, ref settings);
        }

        public void HandleTriggerPowerEventOnStackCount(WorldEntity target, int stackCount)    // 9
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = target.Id;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnStackCount, ref settings, stackCount, MathComparisonType.Equals);
        }

        public void HandleTriggerPowerEventOnTargetKill(PowerResults powerResults)               // 10
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.PowerResults = powerResults;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnTargetKill, ref settings);
        }

        public void HandleTriggerPowerEventOnSummonEntity(ulong summonEntityId)             // 11
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = summonEntityId;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnSummonEntity, ref settings);
        }

        public void HandleTriggerPowerEventOnHoldBegin()                // 12
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnHoldBegin, ref settings);
        }

        public void HandleTriggerPowerEventOnMissileHit(WorldEntity target) // 13
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = target != null ? target.Id : Entity.InvalidId;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnMissileHit, ref settings);
        }

        public void HandleTriggerPowerEventOnMissileKilled(WorldEntity killer, Vector3 position)    // 14
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = killer != null ? killer.Id : Entity.InvalidId;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.TargetPosition = position;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnMissileKilled, ref settings);
        }

        public void HandleTriggerPowerEventOnHotspotNegated(Hotspot hotspot)           // 15
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = hotspot.Id;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.TargetPosition = hotspot.RegionLocation.Position;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnHotspotNegated, ref settings);
        }

        public void HandleTriggerPowerEventOnHotspotNegatedByOther(Hotspot hotspot)    // 16
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = hotspot.Id;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.TargetPosition = hotspot.RegionLocation.Position;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnHotspotNegatedByOther, ref settings);
        }

        public void HandleTriggerPowerEventOnHotspotOverlapBegin(ulong targetId)      // 17
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = targetId;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnHotspotOverlapBegin, ref settings);
        }

        public void HandleTriggerPowerEventOnHotspotOverlapEnd(ulong targetId)        // 18
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = targetId;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnHotspotOverlapEnd, ref settings);
        }

        public void HandleTriggerPowerEventOnRemoveCondition(PowerResults powerResults, int numRemoved) // 19
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = powerResults.TargetId;
            settings.PowerResults = powerResults;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            for (int i = 0; i < numRemoved; i++)
                HandleTriggerPowerEvent(PowerEventType.OnRemoveCondition, ref settings);
        }

        public void HandleTriggerPowerEventOnRemoveNegStatusEffect(PowerResults powerResults)    // 20
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TargetEntityId = powerResults.TargetId;
            settings.PowerResults = powerResults;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            HandleTriggerPowerEvent(PowerEventType.OnRemoveNegStatusEffect, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerPivot()               // 21
        {
            // Client-only?
            Logger.Debug("HandleTriggerPowerEventOnPowerPivot()");

            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ClientCombo;

            HandleTriggerPowerEvent(PowerEventType.OnPowerPivot, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerToggleOn()            // 22
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnPowerToggleOn, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerToggleOff()           // 23
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnPowerToggleOff, ref settings);
        }

        public bool HandleTriggerPowerEventOnPowerStopped(EndPowerFlags flags)             // 24
        {
            // This event's handling does its own thing
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "HandleTriggerPowerEventOnPowerStopped(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "HandleTriggerPowerEventOnPowerStopped(): Owner == null");
            if (Game == null) return Logger.WarnReturn(false, "HandleTriggerPowerEventOnPowerStopped(): Game == null");

            // Nothing to trigger
            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return true;

            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            foreach (PowerEventActionPrototype triggeredPowerEvent in powerProto.ActionsTriggeredOnPowerEvent)
            {
                // Check event type / action combination
                if (triggeredPowerEvent.PowerEvent == PowerEventType.None)
                {
                    Logger.Warn($"HandleTriggerPowerEventOnPowerStopped(): This power contains a triggered power event action with a null event type \n[{this}]");
                    continue;
                }

                PowerEventActionType actionType = triggeredPowerEvent.EventAction;

                if (actionType == PowerEventActionType.None)
                {
                    Logger.Warn($"HandleTriggerPowerEventOnPowerStopped(): This power contains a triggered power event action with a null action type\n[{this}]");
                    continue;
                }

                if (triggeredPowerEvent.PowerEvent != PowerEventType.OnPowerStopped)
                    continue;

                switch (actionType)
                {
                    case PowerEventActionType.CancelScheduledActivationOnTriggeredPower:    DoPowerEventActionCancelScheduledActivation(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.EndPower:                                     DoPowerEventActionEndPower(triggeredPowerEvent.Power, flags); break;
                    case PowerEventActionType.CooldownStart:                                DoPowerEventActionCooldownStart(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.CooldownEnd:                                  DoPowerEventActionCooldownEnd(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.CooldownModifySecs:                           DoPowerEventActionCooldownModifySecs(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.CooldownModifyPct:                            DoPowerEventActionCooldownModifyPct(triggeredPowerEvent, ref settings); break;

                    default: Logger.Warn($"HandleTriggerPowerEventOnPowerStopped(): Power [{this}] contains a triggered event with an unsupported action"); break;
                }
            }

            return true;
        }

        public void HandleTriggerPowerEventOnExtraActivationCooldown()  // 25
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnExtraActivationCooldown, ref settings);
        }

        public void HandleTriggerPowerEventOnPowerLoopEnd()             // 26
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnPowerLoopEnd, ref settings);
        }

        public void HandleTriggerPowerEventOnSpecializationPowerAssigned()      // 27
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnSpecializationPowerAssigned, ref settings);
        }

        public void HandleTriggerPowerEventOnSpecializationPowerUnassigned()    // 28
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnSpecializationPowerUnassigned, ref settings);
        }

        public void HandleTriggerPowerEventOnEntityControlled()                 // 29
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnEntityControlled, ref settings);
        }

        public void HandleTriggerPowerEventOnOutOfRangeActivateMovementPower()  // 30
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            HandleTriggerPowerEvent(PowerEventType.OnOutOfRangeActivateMovementPower, ref settings);
        }

        #endregion

        private bool HandleTriggerPowerEvent(PowerEventType eventType, ref PowerActivationSettings initialSettings,
            int comparisonParam = 0, MathComparisonType comparisonType = MathComparisonType.Invalid)
        {
            if (CanTriggerPowerEventType(eventType, ref initialSettings) == false)
                return false;

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "HandleTriggerPowerEvent(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "HandleTriggerPowerEvent(): Owner == null");
            if (Game == null) return Logger.WarnReturn(false, "HandleTriggerPowerEvent(): Game == null");

            // Early return for powers that don't have any triggered actions
            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return true;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(initialSettings.TargetEntityId);
            GRandom random = new((int)initialSettings.PowerRandomSeed);

            // Check all actions defined for this event type
            foreach (PowerEventActionPrototype triggeredPowerEvent in powerProto.ActionsTriggeredOnPowerEvent)
            {
                // Check event type / action combination
                if (triggeredPowerEvent.PowerEvent == PowerEventType.None)
                {
                    Logger.Warn($"HandleTriggerPowerEvent(): This power contains a triggered power event action with a null event type \n[{this}]");
                    continue;
                }

                PowerEventActionType actionType = triggeredPowerEvent.EventAction;

                if (actionType == PowerEventActionType.None)
                {
                    Logger.Warn($"HandleTriggerPowerEvent(): This power contains a triggered power event action with a null action type\n[{this}]");
                    continue;
                }

                if (eventType != triggeredPowerEvent.PowerEvent)
                    continue;

                if (CanTriggerPowerEventAction(eventType, actionType) == false)
                    continue;

                // Copy settings and generate a new seed
                PowerActivationSettings newSettings = initialSettings;
                newSettings.PowerRandomSeed = random.Next(1, 10000);

                // Run trigger chance check
                float eventTriggerChance = triggeredPowerEvent.GetEventTriggerChance(Properties, Owner, target);
                if (random.NextFloat() >= eventTriggerChance)
                    continue;

                // Run param comparison if needed
                if (comparisonType != MathComparisonType.Invalid)
                {
                    float eventParam = triggeredPowerEvent.GetEventParam(Properties, Owner);
                    switch (comparisonType)
                    {
                        case MathComparisonType.Equals:
                            if (comparisonParam != eventParam)
                                continue;
                            break;

                        case MathComparisonType.GreaterThan:
                            if (comparisonParam <= eventParam)
                                continue;
                            break;

                        case MathComparisonType.LessThan:
                            if (comparisonParam >= eventParam)
                                continue;
                            break;
                    }
                }

                // Do the action for this event
                switch (actionType)
                {
                    case PowerEventActionType.BodySlide:                                    DoPowerEventActionBodyslide(); break;
                    case PowerEventActionType.CancelScheduledActivation:
                    case PowerEventActionType.CancelScheduledActivationOnTriggeredPower:    DoPowerEventActionCancelScheduledActivation(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.ContextCallback:                              DoPowerEventActionContextCallback(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.DespawnTarget:                                DoPowerEventActionDespawnTarget(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.ChargesIncrement:                             DoPowerEventActionChargesIncrement(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.InteractFinish:                               DoPowerEventActionInteractFinish(); break;
                    case PowerEventActionType.RestoreThrowable:                             DoPowerEventActionRestoreThrowable(ref newSettings); break;
                    case PowerEventActionType.RescheduleActivationInSeconds:
                    case PowerEventActionType.ScheduleActivationAtPercent:
                    case PowerEventActionType.ScheduleActivationInSeconds:                  DoPowerEventActionScheduleActivation(triggeredPowerEvent, ref newSettings, actionType); break;
                    case PowerEventActionType.ShowBannerMessage:                            DoPowerEventActionShowBannerMessage(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.SpawnLootTable:                               DoPowerEventActionSpawnLootTable(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.SwitchAvatar:                                 DoPowerEventActionSwitchAvatar(); break;
                    case PowerEventActionType.ToggleOnPower:
                    case PowerEventActionType.ToggleOffPower:                               DoPowerEventActionTogglePower(triggeredPowerEvent, ref newSettings, actionType); break;
                    case PowerEventActionType.TransformModeChange:                          DoPowerEventActionTransformModeChange(triggeredPowerEvent); break;
                    case PowerEventActionType.TransformModeStart:                           DoPowerEventActionTransformModeStart(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.UsePower:                                     DoPowerEventActionUsePower(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.TeleportToPartyMember:                        DoPowerEventActionTeleportToPartyMember(); break;
                    case PowerEventActionType.ControlAgentAI:                               DoPowerEventActionControlAgentAI(newSettings.TargetEntityId); break;
                    case PowerEventActionType.RemoveAndKillControlledAgentsFromInv:         DoPowerEventActionRemoveAndKillControlledAgentsFromInv(); break;
                    case PowerEventActionType.EndPower:                                     DoPowerEventActionEndPower(triggeredPowerEvent.Power, EndPowerFlags.ExplicitCancel | EndPowerFlags.PowerEventAction); break;
                    case PowerEventActionType.CooldownStart:                                DoPowerEventActionCooldownStart(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.CooldownEnd:                                  DoPowerEventActionCooldownEnd(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.CooldownModifySecs:                           DoPowerEventActionCooldownModifySecs(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.CooldownModifyPct:                            DoPowerEventActionCooldownModifyPct(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.TeamUpAgentSummon:                            DoPowerEventActionTeamUpAgentSummon(triggeredPowerEvent); break;
                    case PowerEventActionType.TeleportToRegion:                             DoPowerEventActionTeleportRegion(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.StealPower:                                   DoPowerEventActionStealPower(newSettings.TargetEntityId); break;
                    case PowerEventActionType.PetItemDonate:                                DoPowerEventActionPetItemDonate(triggeredPowerEvent); break;
                    case PowerEventActionType.MapPowers:                                    DoPowerEventActionMapPowers(triggeredPowerEvent); break;
                    case PowerEventActionType.UnassignMappedPowers:                         DoPowerEventActionUnassignMappedPowers(triggeredPowerEvent); break;
                    case PowerEventActionType.RemoveSummonedAgentsWithKeywords:             DoPowerEventActionRemoveSummonedAgentsWithKeywords(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.SpawnControlledAgentWithSummonDuration:       DoPowerEventActionSummonControlledAgentWithDuration(); break;
                    case PowerEventActionType.LocalCoopEnd:                                 DoPowerEventActionLocalCoopEnd(); break;

                    default: Logger.Warn($"HandleTriggerPowerEvent(): Power [{this}] contains a triggered event with an unsupported action"); break;
                }
            }

            return true;
        }

        private bool CanTriggerPowerEventType(PowerEventType eventType, ref PowerActivationSettings settings)
        {
            if (settings.PowerResults != null && settings.PowerResults.TargetId != Entity.InvalidId)
            {
                WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.PowerResults.TargetId);
                if (target != null && target.Properties[PropertyEnum.DontTriggerOtherPowerEvents, (int)eventType])
                    return false;
            }

            return true;
        }

        private bool CanTriggerPowerEventAction(PowerEventType eventType, PowerEventActionType actionType)
        {
            if (actionType == PowerEventActionType.EndPower)
            {
                if (eventType != PowerEventType.OnPowerEnd && eventType != PowerEventType.OnPowerLoopEnd)
                {
                    return Logger.WarnReturn(false,
                        $"CanTriggerPowerEventAction(): Power [{this}] contains an unsupported triggered event/action combination: event=[{eventType}] action=[{actionType}]");
                }
            }

            return true;
        }

        public virtual void OnPayloadInit(PowerPayload payload) { }

        private bool DoActivateComboPower(Power triggeredPower, PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings initialSettings)
        {
            // Activate combo power - a power triggered by a power event action
            if (Owner == null) return Logger.WarnReturn(false, "DoActivateComboPower(): Owner == null");

            if (Owner.IsSimulated == false)
            {
                return Logger.WarnReturn(false,
                    $"DoActivateComboPower(): Trying to activate a combo power, but the power user is not simulated!\nParent power: {this}\nCombo power: {triggeredPower}\nUser: {Owner}");
            }

            if (triggeredPower.GetPowerCategory() != PowerCategoryType.ComboEffect)
            {
                return Logger.WarnReturn(false,
                    $"DoActivateComboPower(): Power [{this}] specified a combo power that is not marked as a combo effect:\n[{triggeredPower}]");
            }

            // Copy index properties to the combo power
            triggeredPower.RestampIndexProperties(GetIndexProperties());

            // Copy settings
            PowerActivationSettings settings = initialSettings;

            // Select random target if needed
            DoRandomTargetSelection(triggeredPower, ref settings);

            // Check target
            bool needsTarget = triggeredPower.NeedsTarget();

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);

            if (target != null && target.IsInWorld && triggeredPowerEvent.UseTriggeringPowerTargetVerbatim == false)
                settings.TargetPosition = target.RegionLocation.Position;    // Update target position if we have a valid target
            else if (needsTarget)
                return false;     // We need a target and we don't have a valid one, so we can't activate

            // Clear target if we don't actually need one
            if (needsTarget == false && triggeredPowerEvent.UseTriggeringPowerTargetVerbatim == false)
                settings.TargetEntityId = Entity.InvalidId;

            // Check if the target meets keyword requirements if there are any
            if (target != null && triggeredPowerEvent.PowerEvent == PowerEventType.OnHitKeyword && triggeredPowerEvent.Keywords.HasValue())
            {
                if (target.HasConditionWithAnyKeyword(triggeredPowerEvent.Keywords) == false)
                    return false;
            }

            if (Owner is Agent agentOwner)
            {
                // Agents have more things going on when they trigger combos

                // Update user position
                if (agentOwner.IsInWorld)
                    settings.UserPosition = agentOwner.RegionLocation.Position;
                
                // Calculate AoE offset if needed
                if (needsTarget == false && triggeredPowerEvent.PowerEventContext != null &&
                    triggeredPowerEvent.PowerEventContext is PowerEventContextOffsetActivationAOEPrototype offsetActivationAoe)
                {
                    // Calculate direction of the offset
                    Vector3 direction = Vector3.SafeNormalize2D(settings.TargetPosition - settings.UserPosition, agentOwner.Forward);
                    Transform3 transform = Transform3.BuildTransform(Vector3.Zero, new(MathHelper.ToRadians(offsetActivationAoe.RotationOffsetDegrees), 0f, 0f));
                    direction = transform * direction;

                    // Apply offset
                    settings.TargetPosition += direction * offsetActivationAoe.PositionOffsetMagnitude;
                    if (offsetActivationAoe.UseIncomingTargetPosAsUserPos)
                        settings.UserPosition = settings.TargetPosition;

                    // Do a sweep
                    RegionLocation sweepLocation = new(agentOwner.RegionLocation);
                    sweepLocation.SetPosition(settings.UserPosition);

                    Vector3? sweepPosition = settings.TargetPosition;
                    PowerPositionSweep(sweepLocation, settings.TargetPosition, settings.TargetEntityId, ref sweepPosition);
                    settings.TargetPosition = sweepPosition.Value;
                }

                // Update target position if needed
                if (triggeredPowerEvent.UseTriggeringPowerTargetVerbatim == false)
                {
                    Vector3 originalTargetPosition = settings.TargetEntityId == Entity.InvalidId && triggeredPowerEvent.UseTriggerPowerOriginalTargetPos
                        ? settings.OriginalTargetPosition
                        : settings.TargetPosition;

                    triggeredPower.GenerateActualTargetPosition(settings.TargetEntityId, originalTargetPosition, out settings.TargetPosition, ref settings);
                }

                // Refresh FX random seed if needed
                if (triggeredPowerEvent.ResetFXRandomSeed)
                {
                    if (Game == null) return Logger.WarnReturn(false, "DoActivateComboPower(): Game == null");
                    settings.FXRandomSeed = Game.Random.Next(1, 10000);
                }

                // Try activating the combo
                if (agentOwner.CanActivatePower(triggeredPower, settings.TargetEntityId, settings.TargetPosition) != PowerUseResult.Success)
                    return false;

                // Server should not activate client combos
                if (settings.Flags.HasFlag(PowerActivationSettingsFlags.ClientCombo))
                    return false;

                return Owner.ActivatePower(triggeredPower.PrototypeDataRef, ref settings) == PowerUseResult.Success;
            }
            else if (Owner is Hotspot)
            {
                // Just activate if our owner is a hotspot
                return triggeredPower.Activate(ref settings) == PowerUseResult.Success;
            }

            return false;
        }

        private bool GetPowersToOperateOnForPowerEvent(WorldEntity owner, PowerEventActionPrototype triggeredPowerEvent,
            ref PowerActivationSettings settings, List<Power> outputList)
        {
            WorldEntity cooldownPowerOwner = owner;

            PowerEventContextPrototype contextProto = triggeredPowerEvent.PowerEventContext;
            if (contextProto is PowerEventContextCooldownChangePrototype cooldownChangeContextProto &&
                cooldownChangeContextProto.TargetsOwner == false)
            {
                cooldownPowerOwner = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);

                if (cooldownPowerOwner == null)
                {
                    return Logger.WarnReturn(false,
                        $"GetPowersToOperateOnForPowerEvent(): Cooldown power event action has TargetsOwner=false but no target found. " +
                        $"targetEntityId=[{settings.TargetEntityId}] triggeringPower=[{settings.TriggeringPowerRef.GetName()}]");
                }
            }

            PowerCollection powerCollection = cooldownPowerOwner.PowerCollection;
            if (powerCollection == null) return Logger.WarnReturn(false, $"GetPowersToOperateOnForPowerEvent(): powerCollection == null");

            if (triggeredPowerEvent.Power != PrototypeId.Invalid)
            {
                Power power = powerCollection.GetPower(triggeredPowerEvent.Power);
                if (power != null)
                    outputList.Add(power);
            }

            if (triggeredPowerEvent.Keywords.HasValue())
                powerCollection.GetPowersMatchingAnyKeyword(outputList, triggeredPowerEvent.Keywords);

            return outputList.Count > 0;
        }

        #region Event Actions

        // Please keep these ordered by PowerEventActionType enum value

        // 1
        private bool DoPowerEventActionBodyslide()
        {
            Avatar avatar = Owner as Avatar;
            if (avatar == null) return Logger.WarnReturn(false, "DoPowerEventActionBodyslide(): avatar == null");

            avatar.ScheduleBodyslideTeleport();
            return true;
        }

        // 2, 3
        private bool DoPowerEventActionCancelScheduledActivation(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (triggeredPowerEvent.Power == PrototypeId.Invalid)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionCancelScheduledActivation(): Encountered a triggered power event with an invalid power ref:\n{triggeredPowerEvent}\n{this}");
            }

            PowerCollection powerCollection = Owner?.PowerCollection;
            if (powerCollection == null) return Logger.WarnReturn(false, "DoPowerEventActionCancelScheduledActivation(): powerCollection == null");

            Power triggeredPower = powerCollection.GetPower(triggeredPowerEvent.Power);
            if (triggeredPower == null)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionCancelScheduledActivation(): Power [{triggeredPower}] specifies a nextPower, but that power could not be found in the power collection.");
            }

            if (settings.TriggeringPowerRef == PrototypeId.Invalid)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionCancelScheduledActivation(): Encountered a triggered power event with an invalid triggering power ref:\n{triggeredPowerEvent}\n{this}");
            }

            Power sourcePower = null;

            switch (triggeredPowerEvent.EventAction)
            {
                case PowerEventActionType.CancelScheduledActivation:
                    if (settings.TriggeringPowerRef == PrototypeId.Invalid)
                    {
                        return Logger.WarnReturn(false,
                            $"DoPowerEventActionCancelScheduledActivation(): Encountered a triggered power event with an invalid triggering power ref:\n{triggeredPowerEvent}\n{this}");
                    }

                    sourcePower = powerCollection.GetPower(settings.TriggeringPowerRef);
                    
                    if (sourcePower == null)
                    {
                        return Logger.WarnReturn(false,
                            "DoPowerEventActionCancelScheduledActivation(): Couldn't find the triggering power for a triggered power event in the power collection. " +
                            $"Power: {this}\nTriggering power ref hash ID: {settings.TriggeringPowerRef}");
                    }

                    break;

                case PowerEventActionType.CancelScheduledActivationOnTriggeredPower:
                    sourcePower = triggeredPower;
                    break;

                default:
                    return Logger.WarnReturn(false,
                        $"DoPowerEventActionCancelScheduledActivation(): Encountered a triggered power event with an unsupported cancel scheduled action type:\n{triggeredPowerEvent}\n{this}");
            }

            return sourcePower?.CancelScheduledActivation(triggeredPower.PrototypeDataRef) == true;
        }

        // 4
        private bool DoPowerEventActionContextCallback(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {            
            PowerEventContextPrototype contextProto = triggeredPowerEvent.PowerEventContext;
            if (contextProto == null) return Logger.WarnReturn(false, "DoPowerEventActionContextCallback(): contextProto == null");

            if (contextProto is not PowerEventContextCallbackPrototype contextCallbackProto)
                return true;

            EntityManager entityManager = Game.EntityManager;

            WorldEntity target = entityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
            Vector3 targetPosition = settings.TargetPosition;

            if (contextCallbackProto.SetContextOnOwnerSummonEntities)
            {
                Inventory summonedInventory = Owner.SummonedInventory;
                if (summonedInventory == null) return Logger.WarnReturn(false, "DoPowerEventActionContextCallback(): summonedInventory == null");

                if (contextCallbackProto.SummonedEntitiesUsePowerTarget == false)
                    target = Owner;

                foreach (var entry in summonedInventory)
                {
                    WorldEntity summon = entityManager.GetEntity<WorldEntity>(entry.Id);
                    contextCallbackProto.HandlePowerEvent(summon, target, targetPosition);
                }
            }
            else if (contextCallbackProto.SetContextOnOwnerAgent)
            {
                WorldEntity owner = entityManager.GetEntity<WorldEntity>(Owner.PowerUserOverrideId);
                if (owner != null)
                    contextCallbackProto.HandlePowerEvent(owner, target, targetPosition);
            }
            else if (contextCallbackProto.SetContextOnTargetEntity)
            {
                contextCallbackProto.HandlePowerEvent(target, null, targetPosition);
            }
            else
            {
                contextCallbackProto.HandlePowerEvent(Owner, target, targetPosition);
            }

            return true;
        }

        // 5
        private bool DoPowerEventActionDespawnTarget(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
            if (target == null || target.IsInWorld == false)
                return true;

            if (target is Avatar || target.IsTeamUpAgent)
                return Logger.WarnReturn(false, "DoPowerEventActionDespawnTarget(): target is Avatar || target.IsTeamUpAgent");

            if (target == Owner)
            {
                // Delay owner destruction
                float delay = triggeredPowerEvent.GetEventParam(Properties, Owner);
                if (delay < 0f)
                {
                    Logger.Warn("DoPowerEventActionDespawnTarget(): delay < 0f");
                    delay = 0f;
                }

                target.ScheduleDestroyEvent(TimeSpan.FromSeconds(delay));
            }
            else
            {
                // Destroy other targets instantly
                target.Destroy();
            }

            return true;
        }

        // 6
        private bool DoPowerEventActionChargesIncrement(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            WorldEntity ultimateOwner = GetUltimateOwner();
            if (ultimateOwner == null) return Logger.WarnReturn(false, "DoPowerEventActionChargesIncrement(): ultimateOwner == null");

            // Team-ups should not be able to increment charges
            if (Owner.IsTeamUpAgent || ultimateOwner.IsTeamUpAgent || ultimateOwner is not Avatar)
                return false;

            int delta = (int)triggeredPowerEvent.GetEventParam(Properties, ultimateOwner);
            if (delta == 0)
                return false;

            List<Power> powersToOperateOnList = ListPool<Power>.Instance.Get();
            if (GetPowersToOperateOnForPowerEvent(ultimateOwner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                foreach (Power power in powersToOperateOnList)
                {
                    PrototypeId powerProtoRef = power.PrototypeDataRef;
                    PropertyCollection properties = power.Owner.Properties;

                    int chargesAvailable = properties[PropertyEnum.PowerChargesAvailable, powerProtoRef];
                    int chargesMax = properties[PropertyEnum.PowerChargesMax, powerProtoRef];

                    if (chargesAvailable >= chargesMax)
                        continue;

                    chargesAvailable += delta;

                    if (chargesAvailable >= chargesMax)
                    {
                        properties[PropertyEnum.PowerChargesAvailable, powerProtoRef] = chargesMax;
                        power.EndCooldown();
                    }
                    else if (chargesAvailable <= 0)
                    {
                        properties[PropertyEnum.PowerChargesAvailable, powerProtoRef] = 0;
                    }
                    else
                    {
                        properties.AdjustProperty(delta, new(PropertyEnum.PowerChargesAvailable, powerProtoRef));
                    }
                }
            }

            ListPool<Power>.Instance.Return(powersToOperateOnList);
            return true;
        }

        // 7
        private bool DoPowerEventActionInteractFinish()
        {
            if (Owner is not Avatar avatar)
                return Logger.WarnReturn(false, $"DoPowerEventActionInteractFinish(): Owner not Avatar");

            return avatar.PreInteractPowerEnd();
        }

        // 9
        private bool DoPowerEventActionRestoreThrowable(ref PowerActivationSettings settings)
        {
            if (Owner is not Agent agentOwner)
                return Logger.WarnReturn(false, $"DoPowerEventActionRestoreThrowable(): Owner cannot throw");

            return agentOwner.TryRestoreThrowable();
        }

        // 8, 10, 11
        private bool DoPowerEventActionScheduleActivation(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings, PowerEventActionType actionType)
        {
            if (triggeredPowerEvent.Power == PrototypeId.Invalid && actionType != PowerEventActionType.RescheduleActivationInSeconds)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionScheduleActivation(): Encountered a triggered power event with an invalid power ref that is not a RescheduleActivationInSeconds event type :\n{triggeredPowerEvent}\n{this}");
            }

            if (Game == null) return Logger.WarnReturn(false, "DoPowerEventActionScheduleActivation(): Game == null");
            if (Owner == null) return Logger.WarnReturn(false, "DoPowerEventActionScheduleActivation(): Owner == null");

            PowerCollection powerCollection = Owner.PowerCollection;
            if (powerCollection == null) return Logger.WarnReturn(false, "DoPowerEventActionScheduleActivation(): powerCollection == null");

            Power triggeredPower = powerCollection.GetPower(triggeredPowerEvent.Power);
            if (triggeredPower == null && actionType != PowerEventActionType.RescheduleActivationInSeconds)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionScheduleActivation(): Power [{this}] specifies a nextPower, but that power could not be found in the power collection.");
            }

            Power triggeringPower = powerCollection.GetPower(settings.TriggeringPowerRef);
            if (triggeringPower == null)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionScheduleActivation(): Triggering power for power [{this}] could not be found in the power collection that does not set to RescheduleActivationInSeconds event type.");
            }

            TimeSpan delay = TimeSpan.Zero;
            float eventParam;

            switch (actionType)
            {
                case PowerEventActionType.ScheduleActivationInSeconds:
                case PowerEventActionType.RescheduleActivationInSeconds:
                    eventParam = triggeredPowerEvent.GetEventParam(Properties, Owner);
                    if (eventParam <= 0f)
                    {
                        return Logger.WarnReturn(false,
                            $"DoPowerEventActionScheduleActivation(): Encountered a triggered power event with an invalid schedule time. EventParam must be greater than zero.\n{triggeredPowerEvent}\n{this}");
                    }

                    delay = TimeSpan.FromSeconds(eventParam);
                    break;

                case PowerEventActionType.ScheduleActivationAtPercent:
                    eventParam = triggeredPowerEvent.GetEventParamNoEval();

                    if (eventParam < 0f || eventParam > 1f)
                    {
                        return Logger.WarnReturn(false,
                            $"DoPowerEventActionScheduleActivation(): Encountered a triggered power event with an invalid schedule percentage. " +
                            $"EventParam was [{eventParam}f] and must be greater than zero and less than or equal to one.\n{triggeredPowerEvent}\n{this}");
                    }

                    delay = triggeringPower.GetFullExecutionTime() * eventParam;
                    break;

                default:
                    Logger.Warn($"DoPowerEventActionScheduleActivation(): Encountered a triggered power event with an unsupported schedule action type:\n{triggeredPowerEvent}\n{this}");
                    break;
            }

            Power powerToSchedule = actionType == PowerEventActionType.RescheduleActivationInSeconds ? triggeringPower : triggeredPower;

            if (powerToSchedule == null)
                return Logger.WarnReturn(false, $"DoPowerEventActionScheduleActivation(): Power to schedule power for activation from power [{this}] could not be found.");

            return triggeringPower.ScheduleScheduledActivation(delay, powerToSchedule, triggeredPowerEvent, ref settings);
        }

        // 12
        private bool DoPowerEventActionShowBannerMessage(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextShowBannerMessagePrototype showBannerContext)
                return Logger.WarnReturn(false, "DoPowerEventActionShowBannerMessage(): triggeredPowerEvent.PowerEventContext is not PowerEventContextShowBannerMessagePrototype showBannerContext");

            BannerMessagePrototype bannerMessageProto = showBannerContext.BannerMessage.As<BannerMessagePrototype>();
            if (bannerMessageProto == null) return Logger.WarnReturn(false, "DoPowerEventActionShowBannerMessage(): bannerMessageProto == null");

            NetMessageBannerMessage bannerMessage = NetMessageBannerMessage.CreateBuilder()
                .SetBannerText((ulong)bannerMessageProto.BannerText)
                .SetTextStyle((ulong)bannerMessageProto.TextStyle)
                .SetTimeToLiveMS((uint)bannerMessageProto.TimeToLiveMS)
                .SetMessageStyle((uint)bannerMessageProto.MessageStyle)
                .SetDoNotQueue(bannerMessageProto.DoNotQueue)
                .SetShowImmediately(bannerMessageProto.ShowImmediately)
                .Build();

            if (showBannerContext.SendToPrimaryTarget)
            {
                Avatar target = Game.EntityManager.GetEntity<Avatar>(settings.TargetEntityId);
                if (target != null)
                    Game.NetworkManager.SendMessageToInterested(bannerMessage, target, AOINetworkPolicyValues.AOIChannelOwner);
            }
            else
            {
                Game.NetworkManager.SendMessageToInterested(bannerMessage, Owner, AOINetworkPolicyValues.AOIChannelOwner);
            }

            return true;
        }

        // 13
        private bool DoPowerEventActionSpawnLootTable(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextLootTablePrototype lootTableContext)
                return Logger.WarnReturn(false, "DoPowerEventActionSpawnLootTable(): triggeredPowerEvent.PowerEventContext is not PowerEventContextLootTablePrototype lootTableContext");

            if (Owner.IsInWorld == false) return Logger.WarnReturn(false, "DoPowerEventActionSpawnLootTable(): Owner.IsInWorld == false");

            Avatar avatar = Owner as Avatar;

            HashSet<Player> recipientPlayers = HashSetPool<Player>.Instance.Get();
            List<(PrototypeId, LootActionType)> tables = ListPool<(PrototypeId, LootActionType)>.Instance.Get();

            try
            {
                if (lootTableContext.IncludeNearbyAvatars)
                {
                    List<Player> nearbyPlayerList = ListPool<Player>.Instance.Get();

                    bool requireCombatActive = Owner.WorldEntityPrototype.RequireCombatActiveForKillCredit;
                    ComputeNearbyPlayers(Owner.Region, Owner.RegionLocation.Position, 0, requireCombatActive, nearbyPlayerList);

                    foreach (Player player in nearbyPlayerList)
                        recipientPlayers.Add(player);

                    ListPool<Player>.Instance.Return(nearbyPlayerList);
                }
                else if (avatar == null)
                {
                    return Logger.WarnReturn(false, "DoPowerEventActionSpawnLootTable(): avatar == null");
                }

                if (avatar != null)
                {
                    Player player = avatar.GetOwnerOfType<Player>();
                    if (player == null) return Logger.WarnReturn(false, "DoPowerEventActionSpawnLootTable(): player == null");

                    recipientPlayers.Add(player);
                }

                int level = lootTableContext.UseItemLevelForLootRoll ? Properties[PropertyEnum.ItemLevel] : Owner.CharacterLevel;

                tables.Add((lootTableContext.LootTable, lootTableContext.PlaceLootInGeneralInventory ? LootActionType.Give : LootActionType.Spawn));

                int recipientId = 1;
                foreach (Player recipient in recipientPlayers)
                {
                    using LootInputSettings lootSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                    lootSettings.Initialize(LootContext.Drop, recipient, Owner, level);
                    Game.LootManager.AwardLootFromTables(tables, lootSettings, recipientId++);
                }
            }
            finally
            {
                HashSetPool<Player>.Instance.Return(recipientPlayers);
                ListPool<(PrototypeId, LootActionType)>.Instance.Return(tables);
            }

            return true;
        }

        // 14
        private bool DoPowerEventActionSwitchAvatar()
        {
            //Logger.Debug($"DoPowerEventActionSwitchAvatar()");

            Player player = Owner.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoPowerEventActionSwitchAvatar(): player == null");
            player.ScheduleSwitchAvatarEvent();
            return true;
        }

        // 15, 16
        private bool DoPowerEventActionTogglePower(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings, PowerEventActionType actionType)
        {
            Power triggeredPower = Owner?.GetPower(triggeredPowerEvent.Power);
            if (triggeredPower == null) return Logger.WarnReturn(false, "DoPowerEventActionTogglePower(): triggeredPower == null");

            // This is for toggled powers only
            if (triggeredPower.IsToggled() == false)
                return false;

            if (Owner is not Agent agent)
                return false;

            if ((triggeredPower.IsToggledOn() == false && actionType == PowerEventActionType.ToggleOnPower) ||
                (triggeredPower.IsToggledOn() && actionType == PowerEventActionType.ToggleOffPower))
            {
                if (agent.CanActivatePower(triggeredPower, settings.TargetEntityId, settings.TargetPosition) != PowerUseResult.Success)
                    return false;

                return agent.ActivatePower(triggeredPower.PrototypeDataRef, ref settings) == PowerUseResult.Success;
            }

            return false;
        }

        // 17
        private bool DoPowerEventActionTransformModeChange(PowerEventActionPrototype triggeredPowerEvent)
        {
            if (Owner is not Avatar ownerAvatar) return Logger.WarnReturn(false, "DoPowerEventActionTransformModeChange(): Owner is not Avatar ownerAvatar");

            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextTransformModePrototype contextProto)
                return Logger.WarnReturn(false, "DoPowerEventActionTransformModeChange(): Incompatible power event context type");

            PrototypeId transformModeRef = contextProto.TransformMode;

            TransformModePrototype transformModeProto = transformModeRef.As<TransformModePrototype>();
            if (transformModeProto == null) return Logger.WarnReturn(false, "DoPowerEventActionTransformModeChange(): transformModeProto == null");

            PrototypeId currentTransformMode = ownerAvatar.CurrentTransformMode;
            if (currentTransformMode != PrototypeId.Invalid && currentTransformMode != transformModeRef)
                return Logger.WarnReturn(false, $"DoPowerEventActionTransformModeChange(): Unexpected transform mode {currentTransformMode.GetName()} for avatar [{ownerAvatar}]");

            // If already in this transform mode, toggle it off
            if (currentTransformMode == transformModeRef)
                transformModeRef = PrototypeId.Invalid;

            ownerAvatar.ScheduleTransformModeChange(transformModeRef, currentTransformMode);
            return true;
        }

        // 18
        private bool DoPowerEventActionTransformModeStart(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (Owner is not Avatar ownerAvatar) return Logger.WarnReturn(false, "DoPowerEventActionTransformModeStart(): Owner is not Avatar ownerAvatar");

            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextTransformModePrototype contextProto)
                return Logger.WarnReturn(false, "DoPowerEventActionTransformModeStart(): Incompatible power event context type");

            PrototypeId transformModeRef = contextProto.TransformMode;

            TransformModePrototype transformModeProto = transformModeRef.As<TransformModePrototype>();
            if (transformModeProto == null) return Logger.WarnReturn(false, "DoPowerEventActionTransformModeStart(): transformModeProto == null");

            PrototypeId currentTransformMode = ownerAvatar.CurrentTransformMode;
            if (currentTransformMode != PrototypeId.Invalid && currentTransformMode != transformModeRef)
                return Logger.WarnReturn(false, $"DoPowerEventActionTransformModeStart(): Unexpected transform mode {currentTransformMode.GetName()} for avatar [{ownerAvatar}]");

            PrototypeId transformComboPowerRef = currentTransformMode == PrototypeId.Invalid
                ? transformModeProto.EnterTransformModePower
                : transformModeProto.ExitTransformModePower;

            if (transformComboPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "DoPowerEventActionTransformModeStart(): transformComboRef == PrototypeId.Invalid");

            Power transformComboPower = ownerAvatar.PowerCollection.GetPower(transformComboPowerRef);
            if (transformComboPower == null) return Logger.WarnReturn(false, "DoPowerEventActionTransformModeStart(): transformComboPower == null");

            PowerActivationSettings newSettings = settings;
            newSettings.TriggeringPowerRef = PrototypeDataRef;
            newSettings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            DoActivateComboPower(transformComboPower, triggeredPowerEvent, ref newSettings);
            return true;
        }

        // 19
        private bool DoPowerEventActionUsePower(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            // Validate

            if (Owner == null) return Logger.WarnReturn(false, "DoPowerEventActionUsePower(): Owner == null");

            PowerCollection powerCollection = Owner.PowerCollection;
            if (powerCollection == null) return Logger.WarnReturn(false, "DoPowerEventActionUsePower(): powerCollection == null");

            if (triggeredPowerEvent.Power == PrototypeId.Invalid)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionUsePower(): Encountered a triggered power event with an invalid power ref:\n{triggeredPowerEvent}\n{this}");
            }

            if (triggeredPowerEvent.EventAction != PowerEventActionType.ScheduleActivationAtPercent &&
                triggeredPowerEvent.EventAction != PowerEventActionType.ScheduleActivationInSeconds &&
                triggeredPowerEvent.Power == PrototypeDataRef)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionUsePower(): PowerEventAction.Power with same PrototypeDataRef as containing Power. This will cause an infinite loop. Power Aborted:\n {this}\n {triggeredPowerEvent}");
            }

            Power triggeredPower = powerCollection.GetPower(triggeredPowerEvent.Power);
            if (triggeredPower == null)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionUsePower(): Power [{this}] specifies a combo power triggered action, but that power could not be found in the power collection.");
            }

            // Activate
            return DoActivateComboPower(triggeredPower, triggeredPowerEvent, ref settings);            
        }

        // 20
        private bool DoPowerEventActionTeleportToPartyMember()
        {
            Player player = Owner?.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoPowerEventActionTeleportToPartyMember(): player == null");

            // This power should have been activated by the player, so the player entity should already have the id of the target player.
            player.ScheduleTeleportToPartyMember();
            return true;
        }

        // 21
        private bool DoPowerEventActionControlAgentAI(ulong targetId)
        {
            var manager = Game.EntityManager;
            var target = manager.GetEntity<WorldEntity>(targetId);
            if (target == null || target.IsControlledEntity) return false;

            var conditionCollection = target.ConditionCollection;
            if (conditionCollection == null) return false;

            var keywordGlobals = GameDatabase.KeywordGlobalsPrototype;
            if (keywordGlobals == null || keywordGlobals.ControlPowerKeywordPrototype == PrototypeId.Invalid) return false;

            Avatar masterAvatar = null;
            Power masterControlPower = null;
            TimeSpan maxTime = TimeSpan.Zero;

            List<Power> controlPowerEndList = ListPool<Power>.Instance.Get();

            foreach (var condition in conditionCollection)
            {
                if (condition == null) continue;
                if (condition.HasKeyword(keywordGlobals.ControlPowerKeywordPrototype) == false) continue;
                
                var controller = manager.GetEntity<WorldEntity>(condition.UltimateCreatorId);
                if (controller is not Avatar avatar) continue;

                var elapsedTime = condition.ElapsedTime;                    
                if (elapsedTime >= maxTime)
                {
                    masterAvatar = avatar;
                    maxTime = elapsedTime;
                }

                var powerRef = condition.CreatorPowerPrototypeRef;
                if (powerRef != PrototypeId.Invalid && condition.Duration == TimeSpan.Zero)
                {
                    var controlPower = avatar.GetPower(powerRef);
                    if (controlPower != null)
                    {
                        if (masterAvatar == avatar)
                            masterControlPower = controlPower;

                        controlPowerEndList.Add(controlPower);
                    }
                }
            }

            if (masterAvatar != null)
            {
                if (target is Agent targetAgent && masterAvatar.SetControlledAgent(targetAgent) == false)
                {
                    ListPool<Power>.Instance.Return(controlPowerEndList);
                    return Logger.WarnReturn(false,
                        $"DoPowerEventActionControlAgentAI(): Failed SetControlledAgent {targetAgent}");
                }

                masterControlPower?.HandleTriggerPowerEventOnEntityControlled();
            }

            foreach (var controlPower in controlPowerEndList)
                controlPower?.SchedulePowerEnd(TimeSpan.Zero, EndPowerFlags.ExplicitCancel);

            ListPool<Power>.Instance.Return(controlPowerEndList);

            return true;
        }

        // 22
        private void DoPowerEventActionRemoveAndKillControlledAgentsFromInv()
        {
            if (Owner is Avatar avatar) 
                avatar.RemoveAndKillControlledAgent();
        }

        // 23
        private bool DoPowerEventActionEndPower(PrototypeId powerProtoRef, EndPowerFlags flags)
        {
            if (powerProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"DoPowerEventActionEndPower(): Encountered a triggered power event with an invalid power ref!\n{this}");

            if (powerProtoRef == PrototypeDataRef)
            {
                return Logger.WarnReturn(false,
                    $"DoPowerEventActionEndPower(): Encountered a triggered power event action EndPower that is trying to end itself! Not performing the action.\n{this}");
            }

            PowerCollection powerCollection = Owner?.PowerCollection;
            if (powerCollection == null) return Logger.WarnReturn(false, "DoPowerEventActionEndPower(): powerCollection == null");

            Power triggeredPower = powerCollection.GetPower(powerProtoRef);
            return triggeredPower != null && triggeredPower.EndPower(flags | EndPowerFlags.PowerEventAction);
        }

        // 24
        private void DoPowerEventActionCooldownStart(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (settings.Flags.HasFlag(PowerActivationSettingsFlags.AutoActivate))
                return;

            List<Power> powersToOperateOnList = ListPool<Power>.Instance.Get();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                TimeSpan cooldownDuration = TimeSpan.FromSeconds(triggeredPowerEvent.GetEventParam(Properties, Owner));
                foreach (Power power in powersToOperateOnList)
                {
                    if (power == null)
                    {
                        Logger.Warn("DoPowerEventActionCooldownStart(): power == null");
                        continue;
                    }

                    power.StartCooldown(cooldownDuration);
                }
            }

            ListPool<Power>.Instance.Return(powersToOperateOnList);
        }

        // 25
        private void DoPowerEventActionCooldownEnd(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            List<Power> powersToOperateOnList = ListPool<Power>.Instance.Get();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                foreach (Power power in powersToOperateOnList)
                {
                    if (power == null)
                    {
                        Logger.Warn("DoPowerEventActionCooldownEnd(): power == null");
                        continue;
                    }

                    power.EndCooldown();
                }
            }

            ListPool<Power>.Instance.Return(powersToOperateOnList);
        }

        // 26
        private void DoPowerEventActionCooldownModifySecs(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            List<Power> powersToOperateOnList = ListPool<Power>.Instance.Get();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                TimeSpan offset = TimeSpan.FromSeconds(triggeredPowerEvent.GetEventParam(Properties, Owner));

                foreach (Power power in powersToOperateOnList)
                {
                    if (power == null)
                    {
                        Logger.Warn("DoPowerEventActionCooldownModifySecs(): power == null");
                        continue;
                    }

                    power.ModifyCooldown(offset);
                }
            }

            ListPool<Power>.Instance.Return(powersToOperateOnList);
        }

        // 27
        private void DoPowerEventActionCooldownModifyPct(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            List<Power> powersToOperateOnList = ListPool<Power>.Instance.Get();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                float eventParam = triggeredPowerEvent.GetEventParam(Properties, Owner);

                foreach (Power power in powersToOperateOnList)
                {
                    if (power == null)
                    {
                        Logger.Warn("DoPowerEventActionCooldownModifyPct(): power == null");
                        continue;
                    }

                    power.ModifyCooldownByPercentage(eventParam);
                }
            }

            ListPool<Power>.Instance.Return(powersToOperateOnList);
        }

        // 28
        private bool DoPowerEventActionTeamUpAgentSummon(PowerEventActionPrototype triggeredPowerEvent)
        {
            if (Owner is not Avatar avatar)
                return Logger.WarnReturn(false, $"DoPowerEventActionTeamUpAgentSummon(): A non-avatar entity {Owner} is trying to summon a team-up agent");

            float eventParam = triggeredPowerEvent.GetEventParam(Properties, Owner);
            if (eventParam < 0.0f)
                return Logger.WarnReturn(false, $"DoPowerEventActionTeamUpAgentSummon(): eventParam {eventParam} < 0.0f");

            avatar.SummonTeamUpAgent(TimeSpan.FromSeconds(eventParam));
            return true;
        }

        // 29
        private bool DoPowerEventActionTeleportRegion(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextTeleportRegionPrototype regionTeleportContext)
                return Logger.WarnReturn(false, "DoPowerEventActionTeleportRegion(): Incompatible power event context type");

            PrototypeId targetProtoRef = regionTeleportContext.Destination;
            if (targetProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "DoPowerEventActionTeleportRegion(): targetProtoRef == PrototypeId.Invalid");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(settings.TargetEntityId);
            if (avatar == null) return Logger.WarnReturn(false, "DoPowerEventActionTeleportRegion(): avatar == null");

            avatar.SchedulePowerTeleport(targetProtoRef, TimeSpan.Zero);
            return true;
        }

        // 30
        private bool DoPowerEventActionStealPower(ulong targetId)
        {
            if (Owner is not Avatar avatar) return Logger.WarnReturn(false, "DoPowerEventActionStealPower(): Owner is not Avatar avatar");

            Player player = avatar.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoPowerEventActionStealPower(): player == null");

            // Non-agent targets don't have stealable powers
            Agent target = Game.EntityManager.GetEntity<Agent>(targetId);
            if (target == null)
                return true;

            AgentPrototype agentProto = target.AgentPrototype;
            if (agentProto == null) return Logger.WarnReturn(false, "DoPowerEventActionStealPower(): agentProto == null");

            // Check if there is a power to steal
            StealablePowerInfoPrototype stealablePowerInfoProto = agentProto.StealablePower.As<StealablePowerInfoPrototype>();
            if (stealablePowerInfoProto == null)
                return true;

            PrototypeId stolenPowerRef = stealablePowerInfoProto.Power;
            if (stolenPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "DoPowerEventActionStealPower(): stolenPowerRef == PrototypeId.Invalid");

            BannerMessagePrototype bannerMessageProto = null;
            if (avatar.IsStolenPowerAvailable(stealablePowerInfoProto.Power) == false)
            {
                avatar.Properties[PropertyEnum.StolenPowerAvailable, stolenPowerRef] = true;
                bannerMessageProto = GameDatabase.UIGlobalsPrototype.MessageStolenPowerAvailable.As<BannerMessagePrototype>();
            }
            else
            {
                bannerMessageProto = GameDatabase.UIGlobalsPrototype.MessageStolenPowerDuplicate.As<BannerMessagePrototype>();
            }

            if (bannerMessageProto == null) return Logger.WarnReturn(false, "DoPowerEventActionStealPower(): bannerMessageProto == null");
            player.SendBannerMessage(bannerMessageProto);

            return true;
        }

        // 31
        private bool DoPowerEventActionPetItemDonate(PowerEventActionPrototype triggeredPowerEvent)
        {
            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextPetDonateItemPrototype itemDonateContext)
                return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): Incompatible power event context type");

            Avatar avatar = Owner as Avatar;
            if (avatar == null) return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): avatar == null");

            Player player = avatar.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): player == null");

            Region region = avatar.Region;
            if (region == null) return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): region == null");

            Inventory petItemInv = avatar.GetInventory(InventoryConvenienceLabel.PetItem);
            if (petItemInv == null) return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): petItemInv == null");

            Item petTechItem = Game.EntityManager.GetEntity<Item>(petItemInv.GetEntityInSlot(0));

            // Find items to vacuum
            Sphere vacuumVolume = new(avatar.RegionLocation.Position, itemDonateContext.Radius);
            DataDirectory dataDirectory = DataDirectory.Instance;
            BlueprintId donationBlueprint = dataDirectory.GetPrototypeBlueprintDataRef(GameDatabase.AdvancementGlobalsPrototype.PetTechDonationItemPrototype);
            RarityPrototype rarityThresholdProto = itemDonateContext.RarityThreshold.As<RarityPrototype>();

            List<Item> vacuumedItems = ListPool<Item>.Instance.Get();
            foreach (WorldEntity worldEntity in region.IterateEntitiesInVolume(vacuumVolume, new()))
            {
                // Skip non-item world entities
                if (worldEntity is not Item item)
                    continue;

                // Check if this is an item restricted to a player (instanced loot)
                // This check needs to happen asap to avoid wasting time on loot piles of other players
                ulong restrictedToPlayerGuid = item.Properties[PropertyEnum.RestrictedToPlayerGuid];
                if (restrictedToPlayerGuid != 0 && restrictedToPlayerGuid != player.DatabaseUniqueId)
                    continue;

                // Skip non-vacuumable items
                if (dataDirectory.PrototypeIsChildOfBlueprint(item.PrototypeDataRef, donationBlueprint) == false)
                    continue;

                // Check rarity
                RarityPrototype itemRarityProto = item.ItemSpec.RarityProtoRef.As<RarityPrototype>();
                if (itemRarityProto.Tier > rarityThresholdProto.Tier)
                    continue;

                // Add the item to the vacuum list
                vacuumedItems.Add(item);
            }

            // Acquire vacuumed items
            foreach (Item item in vacuumedItems)
            {
                if (item.Properties.HasProperty(PropertyEnum.RestrictedToPlayerGuid))
                {
                    PrototypeId rarityProtoRef = item.Properties[PropertyEnum.ItemRarity];
                    player.OnScoringEvent(new(ScoringEventType.ItemCollected, item.Prototype, rarityProtoRef.As<Prototype>()));
                }

                // Try to donate to PetTech. Fall back to credits if it's not possible.
                if (petTechItem == null || ItemPrototype.DonateItemToPetTech(player, petTechItem, item.ItemSpec, item) == false)
                {
                    int sellPrice = Math.Max(MathHelper.RoundUpToInt(item.GetSellPrice(player) * (float)avatar.Properties[PropertyEnum.PetTechDonationMultiplier]), 1);
                    player.AcquireCredits(sellPrice);
                    item.Destroy();
                }
            }

            ListPool<Item>.Instance.Return(vacuumedItems);
            return true;
        }

        // 32
        private bool DoPowerEventActionMapPowers(PowerEventActionPrototype triggeredPowerEvent)
        {
            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextMapPowersPrototype mapPowersContext)
                return Logger.WarnReturn(false, "DoPowerEventActionMapPowers(): Incompatible power event context type");

            if (Owner is not Avatar avatar) return Logger.WarnReturn(false, "DoPowerEventActionMapPowers(): Owner is not Avatar avatar");

            if (mapPowersContext.MappedPowers.IsNullOrEmpty())
                return true;

            PrototypeId continuousPowerRef = avatar.ContinuousPowerDataRef;

            foreach (MapPowerPrototype mapPowerProto in mapPowersContext.MappedPowers)
            {
                if (mapPowerProto.OriginalPower == continuousPowerRef)
                    avatar.ClearContinuousPower();

                avatar.MapPower(mapPowerProto.OriginalPower, mapPowerProto.MappedPower);
            }

            return true;
        }

        // 33
        private bool DoPowerEventActionUnassignMappedPowers(PowerEventActionPrototype triggeredPowerEvent)
        {
            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextUnassignMappedPowersPrototype unassignMappedPowersContext)
                return Logger.WarnReturn(false, "DoPowerEventActionUnassignMappedPowers(): Incompatible power event context type");

            if (Owner is not Avatar avatar) return Logger.WarnReturn(false, "DoPowerEventActionUnassignMappedPowers(): Owner is not Avatar avatar");

            if (unassignMappedPowersContext.MappedPowersToUnassign.IsNullOrEmpty())
                return true;

            PrototypeId continuousPowerRef = avatar.ContinuousPowerDataRef;

            foreach (MapPowerPrototype mapPowerProto in unassignMappedPowersContext.MappedPowersToUnassign)
            {
                if (mapPowerProto.MappedPower == continuousPowerRef)
                    avatar.ClearContinuousPower();

                avatar.UnassignMappedPower(mapPowerProto.MappedPower);
            }

            return true;
        }

        // 34
        private bool DoPowerEventActionRemoveSummonedAgentsWithKeywords(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (Game == null || Owner is not Avatar avatar) return false;

            float count = triggeredPowerEvent.GetEventParam(Properties, avatar);
            if (count < 0) return Logger.WarnReturn(false, $"DoPowerEventActionRemoveSummonedAgentsWithKeywords(): eventParam {count} < 0");
            if (triggeredPowerEvent.Keywords.IsNullOrEmpty()) 
                return Logger.WarnReturn(false, $"DoPowerEventActionRemoveSummonedAgentsWithKeywords(): Keywords is null or empty");

            int removed = avatar.RemoveSummonedAgentsWithKeywords(count, triggeredPowerEvent.KeywordsMask);
            if (removed > 0 && triggeredPowerEvent.Power != PrototypeId.Invalid)
            {
                var powerCollection = avatar.PowerCollection;
                if (powerCollection == null) return false;
                var comboPower = powerCollection.GetPower(triggeredPowerEvent.Power);
                if (comboPower == null)
                    return Logger.WarnReturn(false, $"DoPowerEventActionRemoveSummonedAgentsWithKeywords(): Failed GetPower {triggeredPowerEvent.Power.GetNameFormatted()}");
                
                while (removed > 0)
                {
                    PowerActivationSettings localSettings = settings;
                    localSettings.TriggeringPowerRef = PrototypeDataRef;
                    localSettings.Flags |= PowerActivationSettingsFlags.ServerCombo;
                    DoActivateComboPower(comboPower, triggeredPowerEvent, ref localSettings);
                    removed--;
                }
            }

            return true;
        }

        // 35
        private void DoPowerEventActionSummonControlledAgentWithDuration()
        {
            if (Game == null || Owner is not Avatar avatar) return;
            if (avatar.ControlledAgent == null) return;
            avatar.SummonControlledAgentWithDuration();
        }

        // 36
        private void DoPowerEventActionLocalCoopEnd()
        {
            Logger.Warn($"DoPowerEventActionLocalCoopEnd(): Not implemented");
        }

        #endregion
    }
}
