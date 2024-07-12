using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
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
            HandleTriggerPowerEvent(PowerEventType.OnContactTime, in settings);
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
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnPowerApply, in settings);
        }

        public void HandleTriggerPowerEventOnPowerEnd()                 // 5
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnPowerEnd, in settings);
        }

        public void HandleTriggerPowerEventOnPowerHit()                 // 6
        {
            // not present in the client
        }

        public void HandleTriggerPowerEventOnPowerStart()               // 7
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
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
            // Client-only?
            Logger.Debug("HandleTriggerPowerEventOnPowerPivot()");
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            settings.Flags |= PowerActivationSettingsFlags.ClientCombo;
            HandleTriggerPowerEvent(PowerEventType.OnPowerPivot, in settings);
        }

        public void HandleTriggerPowerEventOnPowerToggleOn()            // 22
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnPowerToggleOn, in settings);
        }

        public void HandleTriggerPowerEventOnPowerToggleOff()           // 23
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnPowerToggleOff, in settings);
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
                    case PowerEventActionType.CancelScheduledActivationOnTriggeredPower:    DoPowerEventActionCancelScheduledActivation(triggeredPowerEvent, in settings); break;
                    case PowerEventActionType.EndPower:                                     DoPowerEventActionEndPower(triggeredPowerEvent.Power, flags); break;
                    case PowerEventActionType.CooldownStart:                                DoPowerEventActionCooldownStart(triggeredPowerEvent, in settings); break;
                    case PowerEventActionType.CooldownEnd:                                  DoPowerEventActionCooldownEnd(triggeredPowerEvent, in settings); break;
                    case PowerEventActionType.CooldownModifySecs:                           DoPowerEventActionCooldownModifySecs(triggeredPowerEvent, in settings); break;
                    case PowerEventActionType.CooldownModifyPct:                            DoPowerEventActionCooldownModifyPct(triggeredPowerEvent, in settings); break;

                    default: Logger.Warn($"HandleTriggerPowerEventOnPowerStopped(): Power [{this}] contains a triggered event with an unsupported action"); break;
                }
            }

            return true;
        }

        public void HandleTriggerPowerEventOnExtraActivationCooldown()  // 25
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnExtraActivationCooldown, in settings);
        }

        public void HandleTriggerPowerEventOnPowerLoopEnd()             // 26
        {
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnPowerLoopEnd, in settings);
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
            PowerActivationSettings settings = _lastActivationSettings;
            settings.TriggeringPowerRef = PrototypeDataRef;
            HandleTriggerPowerEvent(PowerEventType.OnOutOfRangeActivateMovementPower, in settings);
        }

        #endregion

        private bool HandleTriggerPowerEvent(PowerEventType eventType, in PowerActivationSettings initialSettings,
            int comparisonParam = 0, MathComparisonType comparisonType = MathComparisonType.Invalid)
        {
            if (CanTriggerPowerEventType(eventType, in initialSettings) == false)
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
                newSettings.PowerRandomSeed = (uint)random.Next(1, 10000);

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
                    case PowerEventActionType.CancelScheduledActivationOnTriggeredPower:    DoPowerEventActionCancelScheduledActivation(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.ContextCallback:                              DoPowerEventActionContextCallback(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.DespawnTarget:                                DoPowerEventActionDespawnTarget(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.ChargesIncrement:                             DoPowerEventActionChargesIncrement(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.InteractFinish:                               DoPowerEventActionInteractFinish(); break;
                    case PowerEventActionType.RestoreThrowable:                             DoPowerEventActionRestoreThrowable(in newSettings); break;
                    case PowerEventActionType.RescheduleActivationInSeconds:
                    case PowerEventActionType.ScheduleActivationAtPercent:
                    case PowerEventActionType.ScheduleActivationInSeconds:                  DoPowerEventActionScheduleActivation(triggeredPowerEvent, in newSettings, actionType); break;
                    case PowerEventActionType.ShowBannerMessage:                            DoPowerEventActionShowBannerMessage(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.SpawnLootTable:                               DoPowerEventActionSpawnLootTable(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.SwitchAvatar:                                 DoPowerEventActionSwitchAvatar(); break;
                    case PowerEventActionType.ToggleOnPower:
                    case PowerEventActionType.ToggleOffPower:                               DoPowerEventActionTogglePower(triggeredPowerEvent, ref newSettings, actionType); break;
                    case PowerEventActionType.TransformModeChange:                          DoPowerEventActionTransformModeChange(triggeredPowerEvent); break;
                    case PowerEventActionType.TransformModeStart:                           DoPowerEventActionTransformModeStart(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.UsePower:                                     DoPowerEventActionUsePower(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.TeleportToPartyMember:                        DoPowerEventActionTeleportToPartyMember(); break;
                    case PowerEventActionType.ControlAgentAI:                               DoPowerEventActionControlAgentAI(newSettings.TargetEntityId); break;
                    case PowerEventActionType.RemoveAndKillControlledAgentsFromInv:         DoPowerEventActionRemoveAndKillControlledAgentsFromInv(); break;
                    case PowerEventActionType.EndPower:                                     DoPowerEventActionEndPower(triggeredPowerEvent.Power, EndPowerFlags.ExplicitCancel | EndPowerFlags.PowerEventAction); break;
                    case PowerEventActionType.CooldownStart:                                DoPowerEventActionCooldownStart(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.CooldownEnd:                                  DoPowerEventActionCooldownEnd(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.CooldownModifySecs:                           DoPowerEventActionCooldownModifySecs(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.CooldownModifyPct:                            DoPowerEventActionCooldownModifyPct(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.TeamUpAgentSummon:                            DoPowerEventActionTeamUpAgentSummon(triggeredPowerEvent); break;
                    case PowerEventActionType.TeleportToRegion:                             DoPowerEventActionTeleportRegion(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.StealPower:                                   DoPowerEventActionStealPower(newSettings.TargetEntityId); break;
                    case PowerEventActionType.PetItemDonate:                                DoPowerEventActionPetItemDonate(triggeredPowerEvent); break;
                    case PowerEventActionType.MapPowers:                                    DoPowerEventActionMapPowers(triggeredPowerEvent); break;
                    case PowerEventActionType.UnassignMappedPowers:                         DoPowerEventActionUnassignMappedPowers(triggeredPowerEvent); break;
                    case PowerEventActionType.RemoveSummonedAgentsWithKeywords:             DoPowerEventActionRemoveSummonedAgentsWithKeywords(triggeredPowerEvent, in newSettings); break;
                    case PowerEventActionType.SpawnControlledAgentWithSummonDuration:       DoPowerEventActionSummonControlledAgentWithDuration(); break;
                    case PowerEventActionType.LocalCoopEnd:                                 DoPowerEventActionLocalCoopEnd(); break;

                    default: Logger.Warn($"HandleTriggerPowerEvent(): Power [{this}] contains a triggered event with an unsupported action"); break;
                }
            }

            return true;
        }

        private bool CanTriggerPowerEventType(PowerEventType eventType, in PowerActivationSettings settings)
        {
            // TODO: Recheck this when we have a proper PowerEffectsPacket / PowerResults implementation
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

        private bool DoActivateComboPower(Power triggeredPower, PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings initialSettings)
        {
            // Activate combo power - a power triggered by a power event action
            Logger.Debug($"DoActivateComboPower(): {triggeredPower.Prototype}");

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
                    settings.TargetPosition = direction * offsetActivationAoe.PositionOffsetMagnitude;
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

                    triggeredPower.GenerateActualTargetPosition(settings.TargetEntityId, originalTargetPosition, out settings.TargetPosition, in settings);
                }

                // Refresh FX random seed if needed
                if (triggeredPowerEvent.ResetFXRandomSeed)
                {
                    if (Game == null) return Logger.WarnReturn(false, "DoActivateComboPower(): Game == null");
                    settings.FXRandomSeed = (uint)Game.Random.Next(1, 10000);
                }

                // Try activating the combo
                if (agentOwner.CanActivatePower(triggeredPower, settings.TargetEntityId, settings.TargetPosition) != PowerUseResult.Success)
                    return false;

                // Server should not activate client combos
                if (settings.Flags.HasFlag(PowerActivationSettingsFlags.ClientCombo))
                    return false;

                return triggeredPower.Activate(ref settings) == PowerUseResult.Success;
            }
            else if (Owner is Hotspot)
            {
                // Just activate if our owner is a hotspot
                return triggeredPower.Activate(ref settings) == PowerUseResult.Success;
            }

            return false;
        }

        private bool GetPowersToOperateOnForPowerEvent(WorldEntity owner, PowerEventActionPrototype triggeredPowerEvent,
            in PowerActivationSettings settings, List<Power> outputList)
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
                outputList.AddRange(powerCollection.GetPowersMatchingAnyKeyword(triggeredPowerEvent.Keywords));

            return outputList.Count > 0;
        }

        #region Event Actions

        // Please keep these ordered by PowerEventActionType enum value

        // 1
        private bool DoPowerEventActionBodyslide()
        {
            Logger.Trace($"DoPowerEventActionBodyslide(): Owner={Owner}");

            Player player = Owner.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, $"DoPowerEventActionBodyslide(): player == null");

            Game.MovePlayerToRegion(player.PlayerConnection, (PrototypeId)RegionPrototypeId.NPEAvengersTowerHUBRegion,
                (PrototypeId)WaypointPrototypeId.NPEAvengersTowerHub);
            return true;
        }

        // 2, 3
        private bool DoPowerEventActionCancelScheduledActivation(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Debug($"DoPowerEventActionCancelScheduledActivation(): {triggeredPowerEvent.Power.GetName()}");

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
        private void DoPowerEventActionContextCallback(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionContextCallback(): Not implemented");
        }

        // 5
        private void DoPowerEventActionDespawnTarget(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionDespawnTarget(): Not implemented");
        }

        // 6
        private void DoPowerEventActionChargesIncrement(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionChargesIncrement(): Not implemented");
        }

        // 7
        private void DoPowerEventActionInteractFinish()             
        {
            Logger.Warn($"DoPowerEventActionInteractFinish(): Not implemented");
        }

        // 9
        private bool DoPowerEventActionRestoreThrowable(in PowerActivationSettings settings)
        {
            Logger.Trace($"DoPowerEventActionRestoreThrowable()");

            if (Owner is not Agent agentOwner)
                return Logger.WarnReturn(false, $"DoPowerEventActionRestoreThrowable(): Owner cannot throw");

            return agentOwner.TryRestoreThrowable();
        }

        // 8, 10, 11
        private bool DoPowerEventActionScheduleActivation(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings, PowerEventActionType actionType)
        {
            Logger.Debug($"DoPowerEventActionScheduleActivation(): {triggeredPowerEvent.Power.GetName()}");

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

            return triggeringPower.ScheduleScheduledActivation(delay, powerToSchedule, triggeredPowerEvent, in settings);
        }

        // 12
        private void DoPowerEventActionShowBannerMessage(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionShowBannerMessage(): Not implemented");
        }

        // 13
        private void DoPowerEventActionSpawnLootTable(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionSpawnLootTable(): Not implemented");
        }

        // 14
        private bool DoPowerEventActionSwitchAvatar()
        {
            Logger.Debug($"DoPowerEventActionSwitchAvatar()");

            Player player = Owner.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoPowerEventActionSwitchAvatar(): player == null");
            player.SwitchAvatar();
            return true;
        }

        // 15, 16
        private bool DoPowerEventActionTogglePower(PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings, PowerEventActionType actionType)
        {
            Logger.Debug($"DoPowerEventActionTogglePower(): {triggeredPowerEvent.Power.GetName()} - {actionType}");

            Power triggeredPower = Owner?.GetPower(triggeredPowerEvent.Power);
            if (triggeredPower == null) return Logger.WarnReturn(false, "DoPowerEventActionTogglePower(): triggeredPower == null");

            // This is for toggled powers only
            if (triggeredPower.IsToggled() == false) return false;

            if ((triggeredPower.IsToggledOn() == false && actionType == PowerEventActionType.ToggleOnPower) ||
                (triggeredPower.IsToggledOn() && actionType == PowerEventActionType.ToggleOffPower))
            {
                Owner.ActivatePower(triggeredPower.PrototypeDataRef, ref settings);
                return true;
            }

            return false;
        }

        // 17
        private void DoPowerEventActionTransformModeChange(PowerEventActionPrototype triggeredPowerEvent)
        {
            Logger.Warn($"DoPowerEventActionTransformModeChange(): Not implemented");
        }

        // 18
        private void DoPowerEventActionTransformModeStart(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionTransformModeStart(): Not implemented");
        }

        // 19
        private bool DoPowerEventActionUsePower(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Debug($"DoPowerEventActionUsePower(): {triggeredPowerEvent.Power.GetName()}");

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
            return DoActivateComboPower(triggeredPower, triggeredPowerEvent, in settings);            
        }

        // 20
        private void DoPowerEventActionTeleportToPartyMember()
        {
            Logger.Warn($"DoPowerEventActionTeleportToPartyMember(): Not implemented");
        }

        // 21
        private void DoPowerEventActionControlAgentAI(ulong targetId)
        {
            Logger.Warn($"DoPowerEventActionControlAgentAI(): Not implemented");
        }

        // 22
        private void DoPowerEventActionRemoveAndKillControlledAgentsFromInv()
        {
            Logger.Warn($"DoPowerEventActionRemoveAndKillControlledAgentsFromInv(): Not implemented");
        }

        // 23
        private bool DoPowerEventActionEndPower(PrototypeId powerProtoRef, EndPowerFlags flags)
        {
            Logger.Debug($"DoPowerEventActionEndPower(): powerProtoRef={powerProtoRef.GetName()}, flags={flags}");
            
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
        private void DoPowerEventActionCooldownStart(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            //Logger.Debug($"DoPowerEventActionCooldownStart()");

            if (settings.Flags.HasFlag(PowerActivationSettingsFlags.AutoActivate))
                return;

            List<Power> powersToOperateOnList = new();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, in settings, powersToOperateOnList))
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
        }

        // 25
        private void DoPowerEventActionCooldownEnd(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            //Logger.Debug($"DoPowerEventActionCooldownEnd()");

            List<Power> powersToOperateOnList = new();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, in settings, powersToOperateOnList))
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
        }

        // 26
        private void DoPowerEventActionCooldownModifySecs(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Debug($"DoPowerEventActionCooldownModifySecs()");

            List<Power> powersToOperateOnList = new();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, in settings, powersToOperateOnList))
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
        }

        // 27
        private void DoPowerEventActionCooldownModifyPct(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Debug($"DoPowerEventActionCooldownModifyPct()");

            List<Power> powersToOperateOnList = new();
            if (GetPowersToOperateOnForPowerEvent(Owner, triggeredPowerEvent, in settings, powersToOperateOnList))
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
        }

        // 28
        private bool DoPowerEventActionTeamUpAgentSummon(PowerEventActionPrototype triggeredPowerEvent)
        {
            Logger.Debug($"DoPowerEventActionTeamUpAgentSummon()");

            if (Owner is not Avatar avatar)
                return Logger.WarnReturn(false, $"DoPowerEventActionTeamUpAgentSummon(): A non-avatar entity {Owner} is trying to summon a team-up agent");

            avatar.SummonTeamUpAgent();
            return true;
        }

        // 29
        private void DoPowerEventActionTeleportRegion(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionTeleportRegion(): Not implemented");
        }

        // 30
        private void DoPowerEventActionStealPower(ulong targetId)
        {
            Logger.Warn($"DoPowerEventActionStealPower(): Not implemented");
        }

        // 31
        private bool DoPowerEventActionPetItemDonate(PowerEventActionPrototype triggeredPowerEvent)
        {
            Logger.Trace($"DoPowerEventActionPetItemDonate()");

            // We need the right context
            if (triggeredPowerEvent.PowerEventContext is not PowerEventContextPetDonateItemPrototype itemDonateContext)
                return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): Incompatible power event context type");

            // We need a player to give credits to
            Player player = Owner.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): player == null");

            // Region to search for items to vacuum
            Region region = Owner.Region;
            if (region == null) return Logger.WarnReturn(false, "DoPowerEventActionPetItemDonate(): region == null");

            // Find items to vacuum
            Sphere vacuumVolume = new(Owner.RegionLocation.Position, itemDonateContext.Radius);
            Stack<Item> vacuumStack = new();

            foreach (WorldEntity worldEntity in region.IterateEntitiesInVolume(vacuumVolume, new(EntityRegionSPContextFlags.ActivePartition)))
            {
                if (worldEntity is not Item item)
                    continue;

                // Check if this is an item restricted to a player (instanced loot)
                ulong restrictedToPlayerGuid = item.Properties[PropertyEnum.RestrictedToPlayerGuid];
                if (restrictedToPlayerGuid != 0 && restrictedToPlayerGuid != player.DatabaseUniqueId)
                    continue;

                // Push the item to the stack
                vacuumStack.Push(item);
            }

            // TODO: Proper donation

            // Destroy vacuumed items
            PrototypeId creditsProtoRef = GameDatabase.CurrencyGlobalsPrototype.Credits;
            uint creditsToAdd = 0;

            while (vacuumStack.Count > 0)
            {
                Item item = vacuumStack.Pop();
                creditsToAdd += item.GetSellPrice(player);
                item.Destroy();
            }

            // Add credits for all vacuumed items
            if (creditsToAdd > 0)
                player.Properties[PropertyEnum.Currency, creditsProtoRef] += creditsToAdd;

            return true;
        }

        // 32
        private void DoPowerEventActionMapPowers(PowerEventActionPrototype triggeredPowerEvent)
        {
            Logger.Warn($"DoPowerEventActionMapPowers(): Not implemented");
        }

        // 33
        private void DoPowerEventActionUnassignMappedPowers(PowerEventActionPrototype triggeredPowerEvent)
        {
            Logger.Warn($"DoPowerEventActionUnassignMappedPowers(): Not implemented");
        }

        // 34
        private void DoPowerEventActionRemoveSummonedAgentsWithKeywords(PowerEventActionPrototype triggeredPowerEvent, in PowerActivationSettings settings)
        {
            Logger.Warn($"DoPowerEventActionRemoveSummonedAgentsWithKeywords(): Not implemented");
        }

        // 35
        private void DoPowerEventActionSummonControlledAgentWithDuration()
        {
            Logger.Warn($"DoPowerEventActionSummonControlledAgentWithDuration(): Not implemented");
        }

        // 36
        private void DoPowerEventActionLocalCoopEnd()
        {
            Logger.Warn($"DoPowerEventActionLocalCoopEnd(): Not implemented");
        }

        #endregion
    }
}
