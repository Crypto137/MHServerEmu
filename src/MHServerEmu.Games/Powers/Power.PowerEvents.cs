using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
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
using MHServerEmu.Games.Powers.Conditions;
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

        public void HandleTriggerPowerEventOnPowerStopped(EndPowerFlags flags)             // 24
        {
            // This event's handling does its own thing
            PowerPrototype powerProto = Prototype;
            if (!Verify.IsNotNull(powerProto)) return;
            if (!Verify.IsNotNull(Owner)) return;
            if (!Verify.IsNotNull(Game)) return;

            // Nothing to trigger
            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return;

            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;

            foreach (PowerEventActionPrototype triggeredPowerEvent in powerProto.ActionsTriggeredOnPowerEvent)
            {
                // Check event type / action combination
                if (!Verify.IsTrue(triggeredPowerEvent.PowerEvent != PowerEventType.None, $"This power contains a triggered power event action with a null event type \n[{this}]"))
                    continue;

                PowerEventActionType eventActionType = triggeredPowerEvent.EventAction;
                if (!Verify.IsTrue(eventActionType != PowerEventActionType.None, $"This power contains a triggered power event action with a null action type\n[{this}]"))
                    continue;

                if (triggeredPowerEvent.PowerEvent != PowerEventType.OnPowerStopped)
                    continue;

                switch (eventActionType)
                {
                    case PowerEventActionType.CancelScheduledActivationOnTriggeredPower:    DoPowerEventActionCancelScheduledActivation(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.EndPower:                                     DoPowerEventActionEndPower(triggeredPowerEvent.Power, flags); break;
                    case PowerEventActionType.CooldownStart:                                DoPowerEventActionCooldownStart(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.CooldownEnd:                                  DoPowerEventActionCooldownEnd(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.CooldownModifySecs:                           DoPowerEventActionCooldownModifySecs(triggeredPowerEvent, ref settings); break;
                    case PowerEventActionType.CooldownModifyPct:                            DoPowerEventActionCooldownModifyPct(triggeredPowerEvent, ref settings); break;

                    default:
                        Verify.IsTrue(false, $"Power [{this}] contains a triggered event with an unsupported action");
                        break;
                }
            }
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

        private void HandleTriggerPowerEvent(PowerEventType eventType, ref PowerActivationSettings initialSettings,
            int comparisonParam = 0, MathComparisonType comparisonType = MathComparisonType.Invalid)
        {
            if (CanTriggerPowerEventType(eventType, ref initialSettings) == false)
                return;

            PowerPrototype powerProto = Prototype;
            if (!Verify.IsNotNull(powerProto)) return;
            if (!Verify.IsNotNull(Owner)) return;
            if (!Verify.IsNotNull(Game)) return;

            // Early return for powers that don't have any triggered actions
            if (powerProto.ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(initialSettings.TargetEntityId);
            GRandom random = new(initialSettings.PowerRandomSeed);

            // Check all actions defined for this event type
            foreach (PowerEventActionPrototype triggeredPowerEvent in powerProto.ActionsTriggeredOnPowerEvent)
            {
                // Check event type / action combination
                if (!Verify.IsTrue(triggeredPowerEvent.PowerEvent != PowerEventType.None, $"This power contains a triggered power event action with a null event type \n[{this}]"))
                    continue;

                PowerEventActionType eventActionType = triggeredPowerEvent.EventAction;
                if (!Verify.IsTrue(eventActionType != PowerEventActionType.None, $"This power contains a triggered power event action with a null action type\n[{this}]"))
                    continue;

                if (eventType != triggeredPowerEvent.PowerEvent)
                    continue;

                if (CanTriggerPowerEventAction(eventType, eventActionType) == false)
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
                switch (eventActionType)
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
                    case PowerEventActionType.ScheduleActivationInSeconds:                  DoPowerEventActionScheduleActivation(triggeredPowerEvent, ref newSettings, eventActionType); break;
                    case PowerEventActionType.ShowBannerMessage:                            DoPowerEventActionShowBannerMessage(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.SpawnLootTable:                               DoPowerEventActionSpawnLootTable(triggeredPowerEvent, ref newSettings); break;
                    case PowerEventActionType.SwitchAvatar:                                 DoPowerEventActionSwitchAvatar(); break;
                    case PowerEventActionType.ToggleOnPower:
                    case PowerEventActionType.ToggleOffPower:                               DoPowerEventActionTogglePower(triggeredPowerEvent, ref newSettings, eventActionType); break;
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

                    default:
                        Verify.IsTrue(false, $"Power [{this}] contains a triggered event with an unsupported action");
                        break;
                }
            }
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

        private bool CanTriggerPowerEventAction(PowerEventType eventType, PowerEventActionType eventActionType)
        {
            if (eventActionType == PowerEventActionType.EndPower)
            {
                if (!Verify.IsTrue(eventType == PowerEventType.OnPowerEnd || eventType == PowerEventType.OnPowerLoopEnd,
                    $"Power [{this}] contains an unsupported triggered event/action combination: event=[{eventType}] action=[{eventActionType}]"))
                    return false;
            }

            return true;
        }

        private void DoActivateComboPower(Power triggeredPower, PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings initialSettings)
        {
            // Activate combo power - a power triggered by a power event action
            if (!Verify.IsNotNull(Owner)) return;

            if (!Verify.IsTrue(Owner.IsSimulated, $"Trying to activate a combo power, but the power user is not simulated!\nParent power: {this}\nCombo power: {triggeredPower}\nUser: {Owner}"))
                return;

            if (!Verify.IsTrue(triggeredPower.GetPowerCategory() == PowerCategoryType.ComboEffect, $"Power [{this}] specified a combo power that is not marked as a combo effect:\n[{triggeredPower}]"))
                return;

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
                return;     // We need a target and we don't have a valid one, so we can't activate

            // Clear target if we don't actually need one
            if (needsTarget == false && triggeredPowerEvent.UseTriggeringPowerTargetVerbatim == false)
                settings.TargetEntityId = Entity.InvalidId;

            // Check if the target meets keyword requirements if there are any
            if (target != null && triggeredPowerEvent.PowerEvent == PowerEventType.OnHitKeyword && triggeredPowerEvent.Keywords.HasValue())
            {
                if (target.HasConditionWithAnyKeyword(triggeredPowerEvent.Keywords) == false)
                    return;
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
                    RegionLocation sweepLocation = agentOwner.RegionLocation;   // copy
                    sweepLocation.SetPosition(settings.UserPosition);

                    Vector3? sweepPosition = settings.TargetPosition;
                    PowerPositionSweep(ref sweepLocation, settings.TargetPosition, settings.TargetEntityId, ref sweepPosition);
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
                    if (!Verify.IsNotNull(Game)) return;
                    settings.FXRandomSeed = Game.Random.Next(1, 10000);
                }

                // Try activating the combo
                if (agentOwner.CanActivatePower(triggeredPower, settings.TargetEntityId, settings.TargetPosition) != PowerUseResult.Success)
                    return;

                // Server should not activate client combos
                if (!Verify.IsTrue(settings.Flags.HasFlag(PowerActivationSettingsFlags.ClientCombo) == false))
                    return;

                Owner.ActivatePower(triggeredPower.PrototypeDataRef, ref settings);
            }
            else if (Owner is Hotspot)
            {
                // Just activate if our owner is a hotspot
                triggeredPower.Activate(ref settings);
            }
        }

        private bool GetPowersToOperateOnForPowerEvent(WorldEntity owner, PowerEventActionPrototype triggeredPowerEvent,
            ref PowerActivationSettings settings, List<Power> outputList)
        {
            WorldEntity powerOwner = owner;

            PowerEventContextPrototype contextProto = triggeredPowerEvent.PowerEventContext;
            if (contextProto is PowerEventContextCooldownChangePrototype cooldownChangeContextProto &&
                cooldownChangeContextProto.TargetsOwner == false)
            {
                powerOwner = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
                if (!Verify.IsNotNull(powerOwner, $"Cooldown power event action has TargetsOwner=false but no target found. targetEntityId=[{settings.TargetEntityId}] triggeringPower=[{settings.TriggeringPowerRef.GetName()}]"))
                    return false;
            }

            PowerCollection powerCollection = powerOwner.PowerCollection;
            if (!Verify.IsNotNull(powerCollection)) return false;

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

        private void DoPowerEventActionBodyslide()
        {
            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            avatar.ScheduleBodyslideTeleport();
        }

        private void DoPowerEventActionCancelScheduledActivation(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsTrue(triggeredPowerEvent.Power != PrototypeId.Invalid, $"Encountered a triggered power event with an invalid power ref:\n{triggeredPowerEvent}\n{this}"))
                return;

            PowerCollection powerCollection = Owner?.PowerCollection;
            if (!Verify.IsNotNull(powerCollection)) return;

            Power triggeredPower = powerCollection.GetPower(triggeredPowerEvent.Power);
            if (!Verify.IsNotNull(triggeredPower, $"Power [{triggeredPower}] specifies a nextPower, but that power could not be found in the power collection."))
                return;

            if (!Verify.IsTrue(settings.TriggeringPowerRef != PrototypeId.Invalid, $"Encountered a triggered power event with an invalid triggering power ref:\n{triggeredPowerEvent}\n{this}"))
                return;

            Power sourcePower;

            switch (triggeredPowerEvent.EventAction)
            {
                case PowerEventActionType.CancelScheduledActivation:
                    // Skipping the second settings.TriggeringPowerRef != PrototypeId.Invalid verify here that the client has because it's meaningless.

                    sourcePower = powerCollection.GetPower(settings.TriggeringPowerRef);
                    if (!Verify.IsNotNull(sourcePower, $"Couldn't find the triggering power for a triggered power event in the power collection. Power: {this}\nTriggering power ref hash ID: {settings.TriggeringPowerRef}"))
                        return;

                    break;

                case PowerEventActionType.CancelScheduledActivationOnTriggeredPower:
                    sourcePower = triggeredPower;
                    break;

                default:
                    Verify.IsTrue(false, $"Encountered a triggered power event with an unsupported cancel scheduled action type:\n{triggeredPowerEvent}\n{this}");
                    return;
            }

            sourcePower.CancelScheduledActivation(triggeredPower.PrototypeDataRef);
        }

        private void DoPowerEventActionContextCallback(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {            
            PowerEventContextPrototype contextProto = triggeredPowerEvent.PowerEventContext;
            if (!Verify.IsNotNull(contextProto)) return;

            if (contextProto is not PowerEventContextCallbackPrototype contextCallbackProto)
                return;

            EntityManager entityManager = Game.EntityManager;

            WorldEntity target = entityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
            Vector3 targetPosition = settings.TargetPosition;

            if (contextCallbackProto.SetContextOnOwnerSummonEntities)
            {
                Inventory summonedInventory = Owner.SummonedInventory;
                if (!Verify.IsNotNull(summonedInventory)) return;

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
        }

        private void DoPowerEventActionDespawnTarget(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
            if (target == null || target.IsInWorld == false)
                return;

            if (!Verify.IsTrue(target is not Avatar && target.IsTeamUpAgent == false)) return;

            if (target == Owner)
            {
                // Delay owner destruction
                float delay = triggeredPowerEvent.GetEventParam(Properties, Owner);
                if (!Verify.IsTrue(delay >= 0f))
                    delay = 0f;

                target.ScheduleDestroyEvent(TimeSpan.FromSeconds(delay));
            }
            else
            {
                // Destroy other targets instantly
                target.Destroy();
            }
        }

        private void DoPowerEventActionChargesIncrement(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            WorldEntity ultimateOwner = GetUltimateOwner();
            if (!Verify.IsNotNull(ultimateOwner)) return;

            // Team-ups should not be able to increment charges
            if (!Verify.IsTrue(Owner.IsTeamUpAgent == false && ultimateOwner.IsTeamUpAgent == false && ultimateOwner is Avatar)) return;

            int delta = (int)triggeredPowerEvent.GetEventParam(Properties, ultimateOwner);
            if (!Verify.IsTrue(delta != 0)) return;

            using var powersToOperateOnListHandle = ListPool<Power>.Instance.Get(out List<Power> powersToOperateOnList);
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
        }

        private void DoPowerEventActionInteractFinish()
        {
            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            avatar.PreInteractPowerEnd();
        }

        private void DoPowerEventActionRestoreThrowable(ref PowerActivationSettings settings)
        {
            Agent agentOwner = Owner as Agent;
            if (!Verify.IsNotNull(agentOwner)) return;

            agentOwner.TryRestoreThrowable();
        }

        private void DoPowerEventActionScheduleActivation(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings, PowerEventActionType actionType)
        {
            if (!Verify.IsTrue(triggeredPowerEvent.Power != PrototypeId.Invalid || actionType == PowerEventActionType.RescheduleActivationInSeconds,
                $"Encountered a triggered power event with an invalid power ref that is not a RescheduleActivationInSeconds event type :\n{triggeredPowerEvent}\n{this}"))
                return;

            if (!Verify.IsNotNull(Game)) return;
            if (!Verify.IsNotNull(Owner)) return;

            PowerCollection powerCollection = Owner.PowerCollection;
            if (!Verify.IsNotNull(powerCollection)) return;

            Power triggeredPower = powerCollection.GetPower(triggeredPowerEvent.Power);
            if (!Verify.IsTrue(triggeredPower != null || actionType == PowerEventActionType.RescheduleActivationInSeconds,
                $"Power [{this}] specifies a nextPower, but that power could not be found in the power collection."))
                return;

            Power triggeringPower = powerCollection.GetPower(settings.TriggeringPowerRef);
            if (!Verify.IsNotNull(triggeringPower, $"Triggering power for power [{this}] could not be found in the power collection that does not set to RescheduleActivationInSeconds event type."))
                return;

            TimeSpan delay = TimeSpan.Zero;
            float eventParam;

            switch (actionType)
            {
                case PowerEventActionType.ScheduleActivationInSeconds:
                case PowerEventActionType.RescheduleActivationInSeconds:
                    eventParam = triggeredPowerEvent.GetEventParam(Properties, Owner);
                    if (!Verify.IsTrue(eventParam > 0f, $"Encountered a triggered power event with an invalid schedule time. EventParam must be greater than zero.\n{triggeredPowerEvent}\n{this}"))
                        return;

                    delay = TimeSpan.FromSeconds(eventParam);
                    break;

                case PowerEventActionType.ScheduleActivationAtPercent:
                    eventParam = triggeredPowerEvent.GetEventParamNoEval();
                    if (!Verify.IsTrue(eventParam > 0f && eventParam <= 1f,
                        $"Encountered a triggered power event with an invalid schedule percentage. EventParam was [{eventParam}f] and must be greater than zero and less than or equal to one.\n{triggeredPowerEvent}\n{this}"))
                        return;

                    delay = triggeringPower.GetFullExecutionTime() * eventParam;
                    break;

                default:
                    Verify.IsTrue(false, $"Encountered a triggered power event with an unsupported schedule action type:\n{triggeredPowerEvent}\n{this}");
                    return;
            }

            Power powerToSchedule = actionType == PowerEventActionType.RescheduleActivationInSeconds ? triggeringPower : triggeredPower;
            if (!Verify.IsNotNull(powerToSchedule, $"Power to schedule power for activation from power [{this}] could not be found."))
                return;

            triggeringPower.ScheduleScheduledActivation(delay, powerToSchedule, triggeredPowerEvent, ref settings);
        }

        private void DoPowerEventActionShowBannerMessage(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            PowerEventContextShowBannerMessagePrototype showBannerContext = triggeredPowerEvent.PowerEventContext as PowerEventContextShowBannerMessagePrototype;
            if (!Verify.IsNotNull(showBannerContext)) return;

            BannerMessagePrototype bannerMessageProto = showBannerContext.BannerMessage.As<BannerMessagePrototype>();
            if (!Verify.IsNotNull(bannerMessageProto)) return;

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
        }

        private void DoPowerEventActionSpawnLootTable(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            PowerEventContextLootTablePrototype lootTableContext = triggeredPowerEvent.PowerEventContext as PowerEventContextLootTablePrototype;
            if (!Verify.IsNotNull(lootTableContext)) return;

            if (!Verify.IsTrue(Owner.IsInWorld)) return;

            Avatar avatar = Owner as Avatar;

            using var recipientPlayersHandle = HashSetPool<Player>.Instance.Get(out HashSet<Player> recipientPlayers);
            using var tablesHandle = ListPool<(PrototypeId, LootActionType)>.Instance.Get(out List<(PrototypeId, LootActionType)> tables);

            if (lootTableContext.IncludeNearbyAvatars)
            {
                using var nearbyPlayerListHandle = ListPool<Player>.Instance.Get(out List<Player> nearbyPlayerList);

                bool requireCombatActive = Owner.WorldEntityPrototype.RequireCombatActiveForKillCredit;
                ComputeNearbyPlayers(Owner.Region, Owner.RegionLocation.Position, 0, requireCombatActive, nearbyPlayerList);

                foreach (Player player in nearbyPlayerList)
                    recipientPlayers.Add(player);
            }
            else if (!Verify.IsNotNull(avatar))
            {
                return;
            }

            if (avatar != null)
            {
                Player player = avatar.GetOwnerOfType<Player>();
                if (!Verify.IsNotNull(player)) return;

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

        private void DoPowerEventActionSwitchAvatar()
        {
            Player player = Owner.GetOwnerOfType<Player>();
            if (!Verify.IsNotNull(player)) return;

            player.ScheduleSwitchAvatarEvent();
        }

        private void DoPowerEventActionTogglePower(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings, PowerEventActionType actionType)
        {
            if (!Verify.IsNotNull(Owner)) return;

            Agent agent = Owner as Agent;
            if (!Verify.IsNotNull(agent)) return;

            Power triggeredPower = agent.GetPower(triggeredPowerEvent.Power);
            if (!Verify.IsNotNull(triggeredPower)) return;

            if (!Verify.IsTrue(triggeredPower.IsToggled())) return;

            if ((triggeredPower.IsToggledOn() == false && actionType == PowerEventActionType.ToggleOnPower) ||
                (triggeredPower.IsToggledOn() && actionType == PowerEventActionType.ToggleOffPower))
            {
                if (agent.CanActivatePower(triggeredPower, settings.TargetEntityId, settings.TargetPosition) == PowerUseResult.Success)
                    agent.ActivatePower(triggeredPower.PrototypeDataRef, ref settings);
            }
        }

        private void DoPowerEventActionTransformModeChange(PowerEventActionPrototype triggeredPowerEvent)
        {
            if (!Verify.IsNotNull(Owner)) return;

            Avatar ownerAvatar = Owner as Avatar;
            if (!Verify.IsNotNull(ownerAvatar)) return;

            PowerEventContextTransformModePrototype contextProto = triggeredPowerEvent.PowerEventContext as PowerEventContextTransformModePrototype;
            if (!Verify.IsNotNull(contextProto)) return;

            PrototypeId transformModeRef = contextProto.TransformMode;
            TransformModePrototype transformModeProto = transformModeRef.As<TransformModePrototype>();
            if (!Verify.IsNotNull(transformModeProto)) return;

            PrototypeId currentTransformMode = ownerAvatar.CurrentTransformMode;
            if (!Verify.IsTrue(currentTransformMode == PrototypeId.Invalid || currentTransformMode == contextProto.TransformMode)) return;

            // If already in this transform mode, toggle it off
            if (currentTransformMode == transformModeRef)
                transformModeRef = PrototypeId.Invalid;

            ownerAvatar.ScheduleTransformModeChange(transformModeRef, currentTransformMode);
        }

        private void DoPowerEventActionTransformModeStart(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsNotNull(Owner)) return;

            Avatar ownerAvatar = Owner as Avatar;
            if (!Verify.IsNotNull(ownerAvatar)) return;

            PowerEventContextTransformModePrototype contextProto = triggeredPowerEvent.PowerEventContext as PowerEventContextTransformModePrototype;
            if (!Verify.IsNotNull(contextProto)) return;

            PrototypeId transformModeRef = contextProto.TransformMode;
            TransformModePrototype transformModeProto = transformModeRef.As<TransformModePrototype>();
            if (!Verify.IsNotNull(transformModeProto)) return;

            PrototypeId currentTransformMode = ownerAvatar.CurrentTransformMode;
            if (!Verify.IsTrue(currentTransformMode == PrototypeId.Invalid || currentTransformMode == contextProto.TransformMode)) return;

            PrototypeId transformComboPowerRef = currentTransformMode == PrototypeId.Invalid
                ? transformModeProto.EnterTransformModePower
                : transformModeProto.ExitTransformModePower;

            if (!Verify.IsTrue(transformComboPowerRef != PrototypeId.Invalid)) return;

            Power transformComboPower = ownerAvatar.PowerCollection.GetPower(transformComboPowerRef);
            if (!Verify.IsNotNull(transformComboPower)) return;

            PowerActivationSettings newSettings = settings;
            newSettings.TriggeringPowerRef = PrototypeDataRef;
            newSettings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            DoActivateComboPower(transformComboPower, triggeredPowerEvent, ref newSettings);
        }

        private void DoPowerEventActionUsePower(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsNotNull(Owner)) return;

            PowerCollection powerCollection = Owner.PowerCollection;
            if (!Verify.IsNotNull(powerCollection)) return;

            if (!Verify.IsTrue(triggeredPowerEvent.Power != PrototypeId.Invalid, $"Encountered a triggered power event with an invalid power ref:\n{triggeredPowerEvent}\n{this}"))
                return;

            if (!Verify.IsTrue(triggeredPowerEvent.EventAction == PowerEventActionType.ScheduleActivationAtPercent || triggeredPowerEvent.EventAction == PowerEventActionType.ScheduleActivationInSeconds || triggeredPowerEvent.Power != PrototypeDataRef,
                $"PowerEventAction.Power with same PrototypeDataRef as containing Power. This will cause an infinite loop. Power Aborted:\n {this}\n {triggeredPowerEvent}"))
                return;

            Power triggeredPower = powerCollection.GetPower(triggeredPowerEvent.Power);
            if (!Verify.IsNotNull(triggeredPower, $"Power [{this}] specifies a combo power triggered action, but that power could not be found in the power collection."))
                return;

            // Activate
            DoActivateComboPower(triggeredPower, triggeredPowerEvent, ref settings);            
        }

        private void DoPowerEventActionTeleportToPartyMember()
        {
            if (!Verify.IsNotNull(Owner)) return;

            Player player = Owner.GetOwnerOfType<Player>();
            if (!Verify.IsNotNull(player)) return;

            // This power should have been activated by the player, so the player entity should already have the id of the target player.
            player.ScheduleTeleportToPartyMember();
        }

        private void DoPowerEventActionControlAgentAI(ulong targetId)
        {
            EntityManager entityManager = Game.EntityManager;

            WorldEntity target = entityManager.GetEntity<WorldEntity>(targetId);
            if (!Verify.IsNotNull(target)) return;

            if (target.IsControlledEntity)
                return;

            ConditionCollection conditionCollection = target.ConditionCollection;
            if (conditionCollection == null)
                return;

            KeywordGlobalsPrototype keywordGlobals = GameDatabase.KeywordGlobalsPrototype;
            if (!Verify.IsNotNull(keywordGlobals)) return;

            KeywordPrototype controlPowerKeywordProto = keywordGlobals.ControlPowerKeywordPrototype.As<KeywordPrototype>();
            if (!Verify.IsNotNull(controlPowerKeywordProto)) return;

            Avatar masterAvatar = null;
            Power masterControlPower = null;
            TimeSpan maxTime = TimeSpan.Zero;

            using var controlPowerEndListHandle = ListPool<Power>.Instance.Get(out List<Power> controlPowerEndList);

            foreach (Condition condition in conditionCollection)
            {
                if (!Verify.IsNotNull(condition))
                    continue;

                if (condition.HasKeyword(controlPowerKeywordProto) == false)
                    continue;
                
                WorldEntity controller = entityManager.GetEntity<WorldEntity>(condition.UltimateCreatorId);
                if (controller is not Avatar avatar)
                    continue;

                TimeSpan elapsedTime = condition.ElapsedTime;                    
                if (elapsedTime >= maxTime)
                {
                    masterAvatar = avatar;
                    maxTime = elapsedTime;
                }

                PrototypeId powerRef = condition.CreatorPowerPrototypeRef;
                if (powerRef != PrototypeId.Invalid && condition.Duration == TimeSpan.Zero)
                {
                    Power controlPower = avatar.GetPower(powerRef);
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
                Agent targetAgent = target as Agent;
                if (!Verify.IsNotNull(targetAgent)) return;

                if (!Verify.IsTrue(masterAvatar.SetControlledAgent(targetAgent), $"SetControlledAgent failed for target=[{targetAgent}], owner=[{Owner}]"))
                    return;

                masterControlPower?.HandleTriggerPowerEventOnEntityControlled();
            }

            foreach (Power controlPower in controlPowerEndList)
                controlPower?.SchedulePowerEnd(TimeSpan.Zero, EndPowerFlags.ExplicitCancel);
        }

        private void DoPowerEventActionRemoveAndKillControlledAgentsFromInv()
        {
            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            avatar.RemoveAndKillControlledAgent();
        }

        private void DoPowerEventActionEndPower(PrototypeId powerRef, EndPowerFlags flags)
        {
            if (!Verify.IsTrue(powerRef != PrototypeId.Invalid, $"Encountered a triggered power event with an invalid power ref!\n{this}"))
                return;

            if (!Verify.IsTrue(powerRef != PrototypeDataRef, $"Encountered a triggered power event action EndPower that is trying to end itself! Not performing the action.\n{this}"))
                return;

            PowerCollection powerCollection = Owner?.PowerCollection;
            if (!Verify.IsNotNull(powerCollection)) return;

            Power triggeredPower = powerCollection.GetPower(powerRef);
            triggeredPower?.EndPower(flags | EndPowerFlags.PowerEventAction);
        }

        private void DoPowerEventActionCooldownStart(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsNotNull(Owner)) return;

            if (settings.Flags.HasFlag(PowerActivationSettingsFlags.AutoActivate))
                return;

            using var powersToOperateOnListHandle = ListPool<Power>.Instance.Get(out List<Power> powersToOperateOnList);
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                TimeSpan cooldownDuration = TimeSpan.FromSeconds(triggeredPowerEvent.GetEventParam(Properties, Owner));
                foreach (Power power in powersToOperateOnList)
                {
                    if (!Verify.IsNotNull(power))
                        continue;

                    power.StartCooldown(cooldownDuration);
                }
            }
        }

        private void DoPowerEventActionCooldownEnd(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsNotNull(Owner)) return;

            using var powersToOperateOnListHandle = ListPool<Power>.Instance.Get(out List<Power> powersToOperateOnList);
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                foreach (Power power in powersToOperateOnList)
                {
                    if (!Verify.IsNotNull(power))
                        continue;

                    power.EndCooldown();
                }
            }
        }

        private void DoPowerEventActionCooldownModifySecs(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsNotNull(Owner)) return;
            
            using var powersToOperateOnListHandle = ListPool<Power>.Instance.Get(out List<Power> powersToOperateOnList);
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                TimeSpan offset = TimeSpan.FromSeconds(triggeredPowerEvent.GetEventParam(Properties, Owner));

                foreach (Power power in powersToOperateOnList)
                {
                    if (!Verify.IsNotNull(power))
                        continue;

                    power.ModifyCooldown(offset);
                }
            }
        }

        private void DoPowerEventActionCooldownModifyPct(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsNotNull(Owner)) return;

            using var powersToOperateOnListHandle = ListPool<Power>.Instance.Get(out List<Power> powersToOperateOnList);
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, ref settings, powersToOperateOnList))
            {
                float eventParam = triggeredPowerEvent.GetEventParam(Properties, Owner);

                foreach (Power power in powersToOperateOnList)
                {
                    if (!Verify.IsNotNull(power))
                        continue;

                    power.ModifyCooldownByPercentage(eventParam);
                }
            }
        }

        private void DoPowerEventActionTeamUpAgentSummon(PowerEventActionPrototype triggeredPowerEvent)
        {
            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            float eventParam = triggeredPowerEvent.GetEventParam(Properties, Owner);
            if (!Verify.IsTrue(eventParam >= 0f)) return;

            avatar.SummonTeamUpAgent(TimeSpan.FromSeconds(eventParam));
        }

        private void DoPowerEventActionTeleportRegion(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            PowerEventContextTeleportRegionPrototype teleportRegionContext = triggeredPowerEvent.PowerEventContext as PowerEventContextTeleportRegionPrototype;
            if (!Verify.IsNotNull(teleportRegionContext)) return;

            PrototypeId targetProtoRef = teleportRegionContext.Destination;
            if (!Verify.IsTrue(targetProtoRef != PrototypeId.Invalid)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(settings.TargetEntityId);
            if (!Verify.IsNotNull(avatar)) return;

            avatar.SchedulePowerTeleport(targetProtoRef, TimeSpan.Zero);
        }

        private void DoPowerEventActionStealPower(ulong targetId)
        {
            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            Player player = avatar.GetOwnerOfType<Player>();
            if (!Verify.IsNotNull(player)) return;

            // Non-agent targets don't have stealable powers
            Agent target = Game.EntityManager.GetEntity<Agent>(targetId);
            if (target == null)
                return;

            AgentPrototype agentProto = target.AgentPrototype;
            if (!Verify.IsNotNull(agentProto)) return;

            // Check if there is a power to steal
            StealablePowerInfoPrototype stealablePowerInfoProto = agentProto.StealablePower.As<StealablePowerInfoPrototype>();
            if (stealablePowerInfoProto == null)
                return;

            PrototypeId stolenPowerRef = stealablePowerInfoProto.Power;
            if (!Verify.IsTrue(stolenPowerRef != PrototypeId.Invalid)) return;

            BannerMessagePrototype bannerMessageProto;

            if (avatar.IsStolenPowerAvailable(stealablePowerInfoProto.Power) == false)
            {
                avatar.Properties[PropertyEnum.StolenPowerAvailable, stolenPowerRef] = true;
                bannerMessageProto = GameDatabase.UIGlobalsPrototype.MessageStolenPowerAvailable.As<BannerMessagePrototype>();
            }
            else
            {
                bannerMessageProto = GameDatabase.UIGlobalsPrototype.MessageStolenPowerDuplicate.As<BannerMessagePrototype>();
            }

            if (!Verify.IsNotNull(bannerMessageProto)) return;

            player.SendBannerMessage(bannerMessageProto);
        }

        private void DoPowerEventActionPetItemDonate(PowerEventActionPrototype triggeredPowerEvent)
        {
            PowerEventContextPetDonateItemPrototype itemDonateContext = triggeredPowerEvent.PowerEventContext as PowerEventContextPetDonateItemPrototype;
            if (!Verify.IsNotNull(itemDonateContext)) return;

            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            Player player = avatar.GetOwnerOfType<Player>();
            if (!Verify.IsNotNull(player)) return;

            Region region = avatar.Region;
            if (!Verify.IsNotNull(region)) return;

            Inventory petItemInv = avatar.GetInventory(InventoryConvenienceLabel.PetItem);
            if (!Verify.IsNotNull(petItemInv)) return;

            Item petTechItem = Game.EntityManager.GetEntity<Item>(petItemInv.GetEntityInSlot(0));

            // Find items to vacuum
            Sphere vacuumVolume = new(avatar.RegionLocation.Position, itemDonateContext.Radius);
            DataDirectory dataDirectory = DataDirectory.Instance;
            BlueprintId donationBlueprint = dataDirectory.GetPrototypeBlueprintDataRef(GameDatabase.AdvancementGlobalsPrototype.PetTechDonationItemPrototype);
            RarityPrototype rarityThresholdProto = itemDonateContext.RarityThreshold.As<RarityPrototype>();

            using var vacuumedItemsHandle = ListPool<Item>.Instance.Get(out List<Item> vacuumedItems);
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
        }

        private void DoPowerEventActionMapPowers(PowerEventActionPrototype triggeredPowerEvent)
        {
            PowerEventContextMapPowersPrototype mapPowersContext = triggeredPowerEvent.PowerEventContext as PowerEventContextMapPowersPrototype;
            if (!Verify.IsNotNull(mapPowersContext)) return;

            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            if (mapPowersContext.MappedPowers.IsNullOrEmpty())
                return;

            PrototypeId continuousPowerRef = avatar.ContinuousPowerDataRef;

            foreach (MapPowerPrototype mapPowerProto in mapPowersContext.MappedPowers)
            {
                if (mapPowerProto.OriginalPower == continuousPowerRef)
                    avatar.ClearContinuousPower();

                avatar.MapPower(mapPowerProto.OriginalPower, mapPowerProto.MappedPower);
            }
        }

        private void DoPowerEventActionUnassignMappedPowers(PowerEventActionPrototype triggeredPowerEvent)
        {
            PowerEventContextUnassignMappedPowersPrototype unassignMappedPowersContext = triggeredPowerEvent.PowerEventContext as PowerEventContextUnassignMappedPowersPrototype;
            if (!Verify.IsNotNull(unassignMappedPowersContext)) return;

            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            if (unassignMappedPowersContext.MappedPowersToUnassign.IsNullOrEmpty())
                return;

            PrototypeId continuousPowerRef = avatar.ContinuousPowerDataRef;

            foreach (MapPowerPrototype mapPowerProto in unassignMappedPowersContext.MappedPowersToUnassign)
            {
                if (mapPowerProto.MappedPower == continuousPowerRef)
                    avatar.ClearContinuousPower();

                avatar.UnassignMappedPower(mapPowerProto.MappedPower);
            }
        }

        private void DoPowerEventActionRemoveSummonedAgentsWithKeywords(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (!Verify.IsNotNull(Game)) return;

            Avatar avatar = Owner as Avatar;
            if (!Verify.IsNotNull(avatar)) return;

            float count = triggeredPowerEvent.GetEventParam(Properties, avatar);
            if (!Verify.IsTrue(count >= 0)) return;

            if (!Verify.IsTrue(triggeredPowerEvent.Keywords.HasValue())) return;
            int removed = avatar.RemoveSummonedAgentsWithKeywords(count, triggeredPowerEvent.KeywordsMask);

            if (removed > 0 && triggeredPowerEvent.Power != PrototypeId.Invalid)
            {
                PowerCollection powerCollection = avatar.PowerCollection;
                if (!Verify.IsNotNull(powerCollection)) return;

                Power comboPower = powerCollection.GetPower(triggeredPowerEvent.Power);
                if (!Verify.IsNotNull(comboPower)) return;
                
                while (removed > 0)
                {
                    PowerActivationSettings localSettings = settings;
                    localSettings.TriggeringPowerRef = PrototypeDataRef;
                    localSettings.Flags |= PowerActivationSettingsFlags.ServerCombo;
                    DoActivateComboPower(comboPower, triggeredPowerEvent, ref localSettings);
                    removed--;
                }
            }
        }

        private void DoPowerEventActionSummonControlledAgentWithDuration()
        {
            if (Game == null || Owner is not Avatar avatar) return;
            if (avatar.ControlledAgent == null) return;
            avatar.SummonControlledAgentWithDuration();
        }

        private void DoPowerEventActionLocalCoopEnd()
        {
            Verify.IsTrue(false);
        }

        #endregion
    }
}
