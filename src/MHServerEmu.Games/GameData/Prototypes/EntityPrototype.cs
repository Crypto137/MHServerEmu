using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum EntitySelectorAttributeActions
    {
        None,
        DisableInteractions,
        EnableInteractions,
    }

    [AssetEnum((int)None)]
    [Flags]
    public enum EntitySelectorActionEventType
    {
        None = 0,
        OnDetectedEnemy = 1 << 0,
        OnGotAttacked = 1 << 1,
        OnGotDamaged = 1 << 2,
        OnGotDefeated = 1 << 3,
        OnGotKilled = 1 << 4,
        OnAllyDetectedEnemy = 1 << 5,
        OnAllyGotAttacked = 1 << 6,
        OnAllyGotKilled = 1 << 7,
        OnMetLeashDistance = 1 << 8,
        OnEnteredCombat = 1 << 9,
        OnExitedCombat = 1 << 10,
        OnKilledOther = 1 << 11,
        OnDetectedFriend = 1 << 12,
        OnSimulated = 1 << 13,
        OnEnemyProximity = 1 << 14,
        OnDetectedPlayer = 1 << 15,
        OnDetectedNonPlayer = 1 << 16,
        OnAllyDetectedPlayer = 1 << 17,
        OnAllyDetectedNonPlayer = 1 << 18,
        OnClusterEnemiesCleared = 1 << 19,
        OnPlayerInteract = 1 << 20,
        OnPlayerProximity = 1 << 21,
        OnGotAttackedByPlayer = 1 << 22,
        OnAllyGotAttackedByPlayer = 1 << 23,
        OnMissionBroadcast = 1 << 24,
    }

    [AssetEnum((int)Invalid)]
    public enum ScriptRoleKeyEnum
    {
        Invalid = 0,
        FriendlyPassive01 = 1,
        FriendlyPassive02 = 2,
        FriendlyPassive03 = 3,
        FriendlyPassive04 = 4,
        FriendlyCombatant01 = 5,
        FriendlyCombatant02 = 6,
        FriendlyCombatant03 = 7,
        FriendlyCombatant04 = 8,
        HostileCombatant01 = 9,
        HostileCombatant02 = 10,
        HostileCombatant03 = 11,
        HostileCombatant04 = 12,
    }

    [AssetEnum((int)None)]
    public enum LocomotorMethod
    {
        None = 0,
        Ground = 1,
        Airborne = 2,
        TallGround = 3,
        Missile = 4,
        MissileSeeking = 5,
        HighFlying = 6,
        Default = 7
    }

    [AssetEnum((int)None)]
    public enum DialogStyle
    {
        None = 0,
        ComputerTerminal = 1,
        NPCDialog = 2,
        OverheadText = 3,
    }

    [AssetEnum((int)Transition)]
    public enum RegionTransitionType
    {
        Transition,
        TransitionDirect,
        Marker,
        Waypoint,
        TowerUp,
        TowerDown,
        PartyJoin,
        Checkpoint,
        TransitionDirectReturn,
        ReturnToLastTown,
        EndlessDown,
    }

    [AssetEnum((int)Invalid)]
    public enum EntityAppearanceEnum    // Entity/Types/AppearanceEnum.type
    {
        Invalid = -1,
        None = 0,
        Closed = 1,
        Destroyed = 2,
        Disabled = 3,
        Enabled = 4,
        Locked = 5,
        Open = 6,
        Dead = 7,
    }

    [AssetEnum((int)None)]
    public enum HotspotOverlapEventTriggerType
    {
        None = 0,
        Allies = 1,
        Enemies = 2,
        All = 3,
    }

    [AssetEnum((int)None)]
    public enum HotspotNegateByAllianceType
    {
        None = 0,
        Allies = 1,
        Enemies = 2,
        All = 3,
    }

    [AssetEnum((int)Never)]
    public enum SpawnerDefeatCriteria
    {
        Never = 0,
        MaxReachedAndNoHostileMobs = 1,
    }

    [AssetEnum((int)None)]
    public enum FormationFacing // Populations/Blueprints/FacingEnum.type
    {
        None = 0,
        FaceParent = 0,
        FaceParentInverse = 1,
        FaceOrigin = 2,
        FaceOriginInverse = 3,
    }

    [AssetEnum((int)Fail)]
    public enum SpawnFailBehavior
    {
        Fail = 0,
        RetryIgnoringBlackout = 1,
        RetryForce = 2,
    }

    [AssetEnum((int)Default)]
    public enum FactionColor
    {
        Default = 0,
        Red = 1,
        White = 2,
        Blue = 3,
    }

    [AssetEnum]
    public enum WaypointPOIType
    {
        HUB = 0,
        PCZ = 1,    // Public Combat Zone
        PI = 2,     // Private Instance
    }

    #endregion

    public class EntityPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId IconPath { get; protected set; }                                  // A Entity/Types/EntityIconPathType.type
        public PrototypePropertyCollection Properties { get; protected set; }             // Populated from mixins? Parsed from the game as ulong?
        public bool ReplicateToProximity { get; protected set; }
        public bool ReplicateToParty { get; protected set; }
        public bool ReplicateToOwner { get; protected set; }
        public bool ReplicateToDiscovered { get; protected set; }
        public EntityInventoryAssignmentPrototype[] Inventories { get; protected set; }
        public EvalPrototype[] EvalOnCreate { get; protected set; }
        public LocaleStringId DisplayNameInformal { get; protected set; }
        public LocaleStringId DisplayNameShort { get; protected set; }
        public bool ReplicateToTrader { get; protected set; }
        public int LifespanMS { get; protected set; }
        public AssetId IconPathTooltipHeader { get; protected set; }                     // A Entity/Types/EntityIconPathType.type
        public AssetId IconPathHiRes { get; protected set; }                             // A Entity/Types/EntityIconPathType.type

        [DoNotCopy]
        public AOINetworkPolicyValues RepNetwork { get; protected set; } = AOINetworkPolicyValues.AOIChannelNone;

        public override void PostProcess()
        {
            base.PostProcess();

            // Reconstruct AOI network policy (same thing as PropertyInfoPrototype::PostProcess())
            if (ReplicateToProximity)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelProximity;

            if (ReplicateToParty)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelParty;

            if (ReplicateToOwner)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelOwner;

            if (ReplicateToDiscovered)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelDiscovery;

            if (ReplicateToTrader)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelTrader;
        }
    }

    public class WorldEntityPrototype : EntityPrototype
    {
        public PrototypeId Alliance { get; protected set; }
        public BoundsPrototype Bounds { get; protected set; }
        public LocaleStringId DialogText { get; protected set; }
        public AssetId UnrealClass { get; protected set; }
        public CurveId XPGrantedCurve { get; protected set; }
        public bool HACKBuildMouseCollision { get; protected set; }
        public PrototypeId PreInteractPower { get; protected set; }
        public DialogStyle DialogStyle { get; protected set; }
        public WeightedTextEntryPrototype[] DialogTextList { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public PrototypeId Rank { get; protected set; }
        public LocomotorMethod NaviMethod { get; protected set; }
        public bool SnapToFloorOnSpawn { get; protected set; }
        public bool AffectNavigation { get; protected set; }
        public StateChangePrototype PostInteractState { get; protected set; }
        public StateChangePrototype PostKilledState { get; protected set; }
        public bool OrientToInteractor { get; protected set; }
        public PrototypeId TooltipInWorldTemplate { get; protected set; }
        public bool InteractIgnoreBoundsForDistance { get; protected set; }
        public float PopulationWeight { get; protected set; }
        public bool VisibleByDefault { get; protected set; }
        public int RemoveFromWorldTimerMS { get; protected set; }
        public bool RemoveNavInfluenceOnKilled { get; protected set; }
        public bool AlwaysSimulated { get; protected set; }
        public bool XPIsShared { get; protected set; }
        public PrototypeId TutorialTip { get; protected set; }
        public bool TrackingDisabled { get; protected set; }
        public PrototypeId[] ModifiersGuaranteed { get; protected set; }
        public float InteractRangeBonus { get; protected set; }
        public bool ShouldIgnoreMaxDeadBodies { get; protected set; }
        public bool ModifierSetEnable { get; protected set; }
        public bool LiveTuningDefaultEnabled { get; protected set; }
        public bool UpdateOrientationWithParent { get; protected set; }
        public bool MissionEntityDeathCredit { get; protected set; }
        public bool HACKDiscoverInRegion { get; protected set; }
        public bool CanCollideWithPowerUserItems { get; protected set; }
        public bool ForwardOnHitProcsToOwner { get; protected set; }
        public ObjectiveInfoPrototype ObjectiveInfo { get; protected set; }
        public WorldEntityIconsPrototype Icons { get; protected set; }
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; protected set; }
        public bool OverheadIndicator { get; protected set; }
        public bool RequireCombatActiveForKillCredit { get; protected set; }
        public bool ClonePerPlayer { get; protected set; }
        public bool PrefetchMarkedAssets { get; protected set; }
        public AssetId MarvelModelRenderClass { get; protected set; }
        public DesignWorkflowState DesignStatePS4 { get; protected set; }
        public DesignWorkflowState DesignStateXboxOne { get; protected set; }

        // ---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private KeywordsMask _keywordsMask;

        private object _interactionDataLock;
        private bool _interactionDataCached;

        [DoNotCopy]
        public KeywordsMask KeywordsMask { get => _keywordsMask; }

        [DoNotCopy]
        public bool IsVacuumable { get; protected set; }

        [DoNotCopy]
        public bool IsCurrency { get => Properties != null && Properties.HasProperty(PropertyEnum.ItemCurrency); }

        [DoNotCopy]
        public AlliancePrototype AlliancePrototype { get => Alliance.As<AlliancePrototype>(); }
        [DoNotCopy]
        public RankPrototype RankPrototype { get => Rank.As<RankPrototype>(); }
        [DoNotCopy]
        public InteractionData InteractionData { get; set; }
        [DoNotCopy]
        public List<InteractionData> KeywordsInteractionData { get; protected set; }

        [DoNotCopy]
        public int WorldEntityPrototypeEnumValue { get; private set; }
        [DoNotCopy]
        public virtual int LiveTuneEternitySplinterCost { get => (int)LiveTuningManager.GetLiveWorldEntityTuningVar(this, WorldEntityTuningVar.eWETV_EternitySplinterPrice); }

        [DoNotCopy]
        public bool DiscoverInRegion { get => ObjectiveInfo?.EdgeEnabled == true || HACKDiscoverInRegion; }

        [DoNotCopy]
        public virtual LocomotorPrototype Locomotor { get => null; }

        public override void PostProcess()
        {
            base.PostProcess();

            _keywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);

            var keywordVacuumable = GameDatabase.KeywordGlobalsPrototype.VacuumableKeyword.As<KeywordPrototype>();
            IsVacuumable = keywordVacuumable != null && HasKeyword(keywordVacuumable);

            // hack for Mutants CivilianFemaleMutantV01 CivilianMaleMutantV01 CivilianMaleMutantV02
            if (DataRef == (PrototypeId)428108881470233161 || DataRef == (PrototypeId)14971691258158061950 || DataRef == (PrototypeId)6207165219079199103)
                GameDatabase.GetPrototype<KeywordPrototype>((PrototypeId)5036792181542097410).GetBitMask(ref _keywordsMask); // Mutant

            // NOTE: This is a hack straight from the client, do not change
            if (DataRef != (PrototypeId)DataDirectory.Instance.GetBlueprintDataRefByGuid((BlueprintGuid)13337309842336122384))  // Entity/PowerAgnostic.blueprint
                WorldEntityPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetWorldEntityBlueprintDataRef());
        }

        public override bool ApprovedForUse()
        {
            // Add settings for using DesignStatePS4 or DesignStateXboxOne here if we end up supporting console clients
            return GameDatabase.DesignStateOk(DesignState);
        }

        public virtual bool IsLiveTuningEnabled()
        {
            int tuningVar = (int)Math.Floor(LiveTuningManager.GetLiveWorldEntityTuningVar(this, WorldEntityTuningVar.eWETV_Enabled));

            if (tuningVar == 0)
                return false;

            if (tuningVar == 1)
                return LiveTuningDefaultEnabled;

            return true;
        }

        public static bool IsLiveTuningEnabled(PrototypeId worldEntityProtoRef)
        {
            WorldEntityPrototype thisProto = worldEntityProtoRef.As<WorldEntityPrototype>();
            if (thisProto == null)
                return Logger.WarnReturn(false, $"IsLiveTuningEnabled(): Attempting to check LiveTuningDefaultEnabled on something that is not a WorldEntityPrototype!\n DataRef: {worldEntityProtoRef.GetName()}");

            return thisProto.IsLiveTuningEnabled();
        }

        public bool IsLiveTuningVendorEnabled()
        {
            if (IsLiveTuningEnabled() == false)
                return false;

            int tuningVar = (int)Math.Floor(LiveTuningManager.GetLiveWorldEntityTuningVar(this, WorldEntityTuningVar.eWETV_VendorEnabled));
            return tuningVar != 0;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && KeywordPrototype.TestKeywordBit(_keywordsMask, keywordProto);
        }

        public bool HasKeyword(PrototypeId keyword)
        {
            return HasKeyword(GameDatabase.GetPrototype<KeywordPrototype>(keyword));
        }

        public bool GetCurrency(out PrototypeId currencyRef, out int amount)
        {
            currencyRef = PrototypeId.Invalid;
            amount = 0;

            if (Properties == null)
                return Logger.WarnReturn(false, "GetCurrency(): Properties == null");

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.ItemCurrency))
            {
                Property.FromParam(kvp.Key, 0, out currencyRef);
                if (currencyRef == PrototypeId.Invalid)
                    return Logger.WarnReturn(false, "GetCurrency(): currencyRef == PrototypeId.Invalid");

                amount = kvp.Value;
                return true;
            }

            return Logger.WarnReturn(false, $"GetCurrency(): No currency property found for world entity {this}");
        }

        public bool GetXPAwarded(int level, out long xp, out long minXP, bool applyGlobalTuning)
        {
            xp = 0;
            minXP = 0;

            if (XPGrantedCurve == CurveId.Invalid)
                return Logger.WarnReturn(false, $"GetXPAwarded(): WorldEntity doesn't have XPGrantedCurve! WorldEntity: {this}");

            Curve xpCurve = CurveDirectory.Instance.GetCurve(XPGrantedCurve);
            if (xpCurve == null) return Logger.WarnReturn(false, "GetXPAwarded(): xpCurve == null");

            if (xpCurve.GetInt64At(level, out long baseXP) == false)
                Logger.Warn($"GetXPAwarded(): Invalid result returned from XP Curve! Level: {level} WorldEntity: {this}");

            float xpMinPct = Properties != null ? Properties[PropertyEnum.ExperienceAwardMinimumPct] : 0f;
            if (xpMinPct > 0f)  // If this entity has a minimum XP pct defined, always award at least 1 XP
                minXP = Math.Max(1, (long)(baseXP * xpMinPct));

            float multiplier = LiveTuningManager.GetLiveWorldEntityTuningVar(this, WorldEntityTuningVar.eWETV_MobXP);
            if (applyGlobalTuning || LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_RespectLevelForGlobalXP) == 0f)
            {
                multiplier *= LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_XPGain);
            }

            xp = (long)(Math.Max(baseXP, minXP) * multiplier);

            return xp > 0 || minXP > 0;
        }

        public virtual PrototypeId GetPortalTarget()
        {
            // Overriden in Item and Transition
            return PrototypeId.Invalid;
        }

        public InteractionData GetInteractionData()
        {
            if (_interactionDataCached == false)
            {
                _interactionDataLock = new();
                KeywordsInteractionData = new();
                BuildInteractionDataCache();
            }
            return InteractionData;
        }

        public List<InteractionData> GetKeywordsInteractionData()
        {
            if (_interactionDataCached == false) BuildInteractionDataCache();
            return KeywordsInteractionData;
        }

        private void BuildInteractionDataCache()
        {
            lock (_interactionDataLock)
            {
                if (_interactionDataCached == false)
                {
                    GameDatabase.InteractionManager.BuildEntityPrototypeCachedData(this);
                    _interactionDataCached = true;
                }
            }
        }
    }

    public class StateChangePrototype : Prototype
    {
    }

    public class StateTogglePrototype : StateChangePrototype
    {
        public PrototypeId StateA { get; protected set; }
        public PrototypeId StateB { get; protected set; }
    }

    public class StateSetPrototype : StateChangePrototype
    {
        public PrototypeId State { get; protected set; }
    }

    public class WorldEntityIconsPrototype : Prototype
    {
        public AssetId EdgeIcon { get; protected set; }
        public PrototypeId MapIcon { get; protected set; }
        public AssetId EdgeIconHiRes { get; protected set; }
    }

    #region EntityAction

    public class EntityActionBasePrototype : Prototype
    {
        public int Weight { get; protected set; }
    }

    public class EntityActionAIOverridePrototype : EntityActionBasePrototype
    {
        public PrototypeId Power { get; protected set; }
        public bool PowerRemove { get; protected set; }
        public PrototypeId Brain { get; protected set; }
        public bool BrainRemove { get; protected set; }
        public bool SelectorReferencedPowerRemove { get; protected set; }
        public int AIAggroRangeOverrideAlly { get; protected set; }
        public int AIAggroRangeOverrideEnemy { get; protected set; }
        public int AIProximityRangeOverride { get; protected set; }
        public int LifespanMS { get; protected set; }
        public PrototypeId LifespanEndPower { get; protected set; }
    }

    public class EntityActionOverheadTextPrototype : EntityActionBasePrototype
    {
        public LocaleStringId Text { get; protected set; }
        public int Duration { get; protected set; }
    }

    public class EntityActionEventBroadcastPrototype : EntityActionBasePrototype
    {
        public EntitySelectorActionEventType EventToBroadcast { get; protected set; }
        public int BroadcastRange { get; protected set; }
    }

    public class EntityActionSpawnerTriggerPrototype : EntityActionBasePrototype
    {
        public bool EnableClusterLocalSpawner { get; protected set; }
    }

    public class EntitySelectorActionBasePrototype : Prototype
    {
        public PrototypeId[] AIOverrides { get; protected set; }
        public EntityActionAIOverridePrototype[] AIOverridesList { get; protected set; }
        public PrototypeId[] OverheadTexts { get; protected set; }
        public EntityActionOverheadTextPrototype[] OverheadTextsList { get; protected set; }
        public PrototypeId[] Rewards { get; protected set; }
        public EntitySelectorAttributeActions[] AttributeActions { get; protected set; }
        public HUDEntitySettingsPrototype HUDEntitySettingOverride { get; protected set; }
    }

    public class EntitySelectorActionPrototype : EntitySelectorActionBasePrototype
    {
        public EntitySelectorActionEventType[] EventTypes { get; protected set; }
        public int ReactionTimeMS { get; protected set; }
        public EntitySelectorActionEventType[] CancelOnEventTypes { get; protected set; }
        public PrototypeId SpawnerTrigger { get; protected set; }
        public PrototypeId AllianceOverride { get; protected set; }
        public PrototypeId BroadcastEvent { get; protected set; }

        [DoNotCopy]
        public bool RequiresBrain { get; protected set; }

        public override void PostProcess()
        {
            PreCheck();

            if (EventTypes.HasValue())
            {
                var needBrainEventTypes =
                    EntitySelectorActionEventType.OnDetectedEnemy |
                    EntitySelectorActionEventType.OnDetectedFriend |
                    EntitySelectorActionEventType.OnEnemyProximity |
                    EntitySelectorActionEventType.OnDetectedPlayer |
                    EntitySelectorActionEventType.OnPlayerProximity;

                foreach (var eventType in EventTypes)
                    if (needBrainEventTypes.HasFlag(eventType))
                    {
                        RequiresBrain = true;
                        return;
                    }
            }
        }

        public EntityActionOverheadTextPrototype PickOverheadText(GRandom random)
        {
            if (OverheadTexts.IsNullOrEmpty() && OverheadTextsList.IsNullOrEmpty()) return null;

            Picker<EntityActionOverheadTextPrototype> picker = new(random);
            if (OverheadTexts.HasValue())
            {
                foreach (var overheadTextRef in OverheadTexts)
                {
                    var overheadText = overheadTextRef.As<EntityActionOverheadTextPrototype>();
                    picker.Add(overheadText, overheadText.Weight);
                }
            }
            else if (AIOverridesList.HasValue())
            {
                foreach (var overheadText in OverheadTextsList)
                    picker.Add(overheadText, overheadText.Weight);
            }
            return picker.Pick();
        }

        public EntityActionAIOverridePrototype PickAIOverride(GRandom random)
        {
            if (AIOverrides.IsNullOrEmpty() && AIOverridesList.IsNullOrEmpty()) return null;

            Picker<EntityActionAIOverridePrototype> picker = new(random);
            if (AIOverrides.HasValue())
            {
                foreach (var brainRef in AIOverrides) 
                {
                    var brain = brainRef.As<EntityActionAIOverridePrototype>();
                    picker.Add(brain, brain.Weight);
                }                   
            } 
            else if (AIOverridesList.HasValue())
            {
                foreach (var brain in AIOverridesList)
                    picker.Add(brain, brain.Weight);
            }
            return picker.Pick();
        }
    }

    public class EntitySelectorActionSetPrototype : Prototype
    {
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; protected set; }
    }

    public class EntitySelectorPrototype : Prototype
    {
        public PrototypeId[] Entities { get; protected set; }
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; protected set; }
        public PrototypeId EntitySelectorActionsTemplate { get; protected set; }
        public PrototypeId DefaultBrainOnSimulated { get; protected set; }
        public bool IgnoreMissionOwnerForTargeting { get; protected set; }
        public float DefaultAggroRangeAlly { get; protected set; }
        public float DefaultAggroRangeHostile { get; protected set; }
        public float DefaultProximityRangeHostile { get; protected set; }
        public EvalPrototype EvalSpawnProperties { get; protected set; }
        public bool SelectUniqueEntities { get; protected set; }

        public PrototypeId SelectEntity(GRandom random, Region region)
        {
            if (Entities.HasValue())
            {
                int index = random.Next(0, Entities.Length);
                if (SelectUniqueEntities)
                {
                    if (region == null) return PrototypeId.Invalid;
                    region.GetUnuqueSelectorIndex(ref index, Entities.Length, DataRef);
                }
                return Entities[index];
            }

            return PrototypeId.Invalid;
        }

        public void SetUniqueEntity(PrototypeId entityRef, Region region, bool set)
        {
            if (Entities.IsNullOrEmpty() || SelectUniqueEntities == false || region == null) return;
            int index = Array.IndexOf(Entities, entityRef);
            if (index != -1) region.SetUnuqueSelectorIndex(index, set, DataRef);
        }
    }

    public class EntityActionTimelineScriptActionPrototype : EntitySelectorActionBasePrototype
    {
        public ScriptRoleKeyEnum[] ScriptRoleKeys { get; protected set; }
        public PrototypeId SpawnerTrigger { get; protected set; }
    }

    public class EntityActionTimelineScriptEventPrototype : Prototype
    {
        public PrototypeId[] ActionsList { get; protected set; }
        public EntityActionTimelineScriptActionPrototype[] ActionsVector { get; protected set; }
        public int EventTime { get; protected set; }
        public EntitySelectorActionEventType[] InterruptOnEventTypes { get; protected set; }
    }

    public class EntityActionTimelineScriptPrototype : Prototype
    {
        public EntitySelectorActionEventType[] TriggerOnEventTypes { get; protected set; }
        public EntitySelectorActionEventType[] CancelOnEventTypes { get; protected set; }
        public EntityActionTimelineScriptEventPrototype[] ScriptEvents { get; protected set; }
        public bool RunOnceOnly { get; protected set; }
    }

    #endregion

    public class WeightedTextEntryPrototype : Prototype
    {
        public LocaleStringId Text { get; protected set; }
        public long Weight { get; protected set; }
    }

    public class TransitionPrototype : WorldEntityPrototype
    {
        public RegionTransitionType Type { get; protected set; }
        public int SpawnOffset { get; protected set; }
        public PrototypeId Waypoint { get; protected set; }
        public bool SupressBlackout { get; protected set; }
        public bool ShowIndicator { get; protected set; }
        public bool ShowConfirmationDialog { get; protected set; }
        public PrototypeId DirectTarget { get; protected set; }
        public PrototypeId[] RegionAffixesBySummonerRarity { get; protected set; }
        public LocaleStringId ShowConfirmationDialogOverride { get; protected set; }
        public PrototypeId ShowConfirmationDialogTemplate { get; protected set; }
        public PrototypeId ShowConfirmationDialogEnemy { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override PrototypeId GetPortalTarget()
        {
            if (DirectTarget == PrototypeId.Invalid)
                return PrototypeId.Invalid;

            RegionConnectionTargetPrototype connectionTargetProto = DirectTarget.As<RegionConnectionTargetPrototype>();
            if (connectionTargetProto == null) return Logger.WarnReturn(PrototypeId.Invalid, "GetPortalTarget(): connectionTargetProto == null");

            return connectionTargetProto.Region;
        }

        public Vector3 CalcSpawnOffset(in Orientation rotation)
        {
            return new(SpawnOffset * MathF.Cos(rotation.Yaw),
                       SpawnOffset * MathF.Sin(rotation.Yaw),
                       0f);
        }

        public static Vector3 CalcSpawnOffset(EntityMarkerPrototype entityMarkerProto)
        {
            var transitionProto = entityMarkerProto?.GetMarkedPrototype<TransitionPrototype>();
            if (transitionProto == null) return Vector3.Zero;

            return transitionProto.CalcSpawnOffset(entityMarkerProto.Rotation);
        }
    }

    public class EntityAppearancePrototype : Prototype
    {
        public EntityAppearanceEnum AppearanceEnum { get; protected set; }
    }

    public class EntityStatePrototype : Prototype
    {
        public PrototypeId Appearance { get; protected set; }
        public PrototypeId[] OnActivatePowers { get; protected set; }

        [DoNotCopy]
        public EntityAppearanceEnum AppearanceEnum { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            var appreanceProto = Appearance.As<EntityAppearancePrototype>();
            AppearanceEnum = (appreanceProto != null) ? appreanceProto.AppearanceEnum : EntityAppearanceEnum.None;
        }
    }

    public class DoorEntityStatePrototype : EntityStatePrototype
    {
        public bool IsOpen { get; protected set; }
    }

    public class InteractionSpecPrototype : Prototype
    {
        public virtual void GetPrototypeContextRefs(HashSet<PrototypeId> refs) { }
    }

    public class ConnectionTargetEnableSpecPrototype : InteractionSpecPrototype
    {
        public PrototypeId ConnectionTarget { get; protected set; }
        public bool Enabled { get; protected set; }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (ConnectionTarget != PrototypeId.Invalid) refs.Add(ConnectionTarget);
        }
    }

    public class EntityBaseSpecPrototype : InteractionSpecPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
            }
        }
    }

    public class EntityVisibilitySpecPrototype : EntityBaseSpecPrototype
    {
        public bool Visible { get; protected set; }
    }

    public class EntityAppearanceSpecPrototype : EntityBaseSpecPrototype
    {
        public PrototypeId Appearance { get; protected set; }
        public LocaleStringId FailureReasonText { get; protected set; }
        public TriBool InteractionEnabled { get; protected set; }
    }

    public class HotspotDirectApplyToMissilesDataPrototype : Prototype
    {
        public bool AffectsAllyMissiles { get; protected set; }
        public bool AffectsHostileMissiles { get; protected set; }
        public EvalPrototype EvalPropertiesToApply { get; protected set; }
        public bool AffectsReflectedMissilesOnly { get; protected set; }
        public bool IsPermanent { get; protected set; }
    }

    public class HotspotPrototype : WorldEntityPrototype
    {
        public PrototypeId[] AppliesPowers { get; protected set; }             // VectorPrototypeRefPtr PowerPrototype
        public PrototypeId[] AppliesIntervalPowers { get; protected set; }     // VectorPrototypeRefPtr PowerPrototype
        public int IntervalPowersTimeDelayMS { get; protected set; }
        public bool IntervalPowersRandomTarget { get; protected set; }
        public int IntervalPowersNumRandomTargets { get; protected set; }
        public UINotificationPrototype UINotificationOnEnter { get; protected set; }
        public int MaxSimultaneousTargets { get; protected set; }
        public bool KillCreatorWhenHotspotIsEmpty { get; protected set; }
        public PrototypeId KismetSeq { get; protected set; }
        public bool Negatable { get; protected set; }
        public bool KillSelfWhenPowerApplied { get; protected set; }
        public HotspotOverlapEventTriggerType OverlapEventsTriggerOn { get; protected set; }
        public int OverlapEventsMaxTargets { get; protected set; }
        public HotspotDirectApplyToMissilesDataPrototype DirectApplyToMissilesData { get; protected set; }
        public int ApplyEffectsDelayMS { get; protected set; }
        public PrototypeId CameraSettings { get; protected set; }
        public int MaxLifetimeTargets { get; protected set; }
    }

    public class OverheadTextPrototype : Prototype
    {
        public EntityFilterFilterListPrototype OverheadTextEntityFilter { get; protected set; }
        public LocaleStringId OverheadText { get; protected set; }
    }

    public class SpawnerSequenceEntryPrototype : PopulationRequiredObjectPrototype
    {
        public bool OnKilledDefeatSpawner { get; protected set; }
        public PrototypeId OnDefeatAIOverride { get; protected set; }
        public bool Unique { get; protected set; }
        public OverheadTextPrototype[] OnSpawnOverheadTexts { get; protected set; }
    }

    public class SpawnerPrototype : WorldEntityPrototype
    {
        public int SpawnLifetimeMax { get; protected set; }
        public int SpawnDistanceMin { get; protected set; }
        public int SpawnDistanceMax { get; protected set; }
        public int SpawnIntervalMS { get; protected set; }
        public int SpawnSimultaneousMax { get; protected set; }
        public PrototypeId SpawnedEntityInventory { get; protected set; }
        public SpawnerSequenceEntryPrototype[] SpawnSequence { get; protected set; }
        public bool SpawnsInheritMissionPrototype { get; protected set; }
        public bool StartEnabled { get; protected set; }
        public bool OnDestroyCleanupSpawnedEntities { get; protected set; }
        public int SpawnIntervalVarianceMS { get; protected set; }
        public PrototypeId HotspotTrigger { get; protected set; }
        public BannerMessagePrototype OnDefeatBannerMessage { get; protected set; }
        public bool OnDefeatDefeatGroup { get; protected set; }
        public SpawnerDefeatCriteria DefeatCriteria { get; protected set; }
        public EvalPrototype EvalSpawnProperties { get; protected set; }
        public FormationFacing SpawnFacing { get; protected set; }
        public SpawnFailBehavior SpawnFailBehavior { get; protected set; }
        public int DefeatTimeoutMS { get; protected set; }

        [DoNotCopy]
        public AlliancePrototype EntityAlliance { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();
            EntityAlliance = CheckForSingleEntityAlliance();
        }

        private AlliancePrototype CheckForSingleEntityAlliance()
        {
            AlliancePrototype resultProto = null;

            if (SpawnSequence.HasValue())
            {
                HashSet<PrototypeId> entities = new ();

                foreach (var sequenceProto in SpawnSequence)
                {
                    if (sequenceProto == null) continue;
                    PopulationObjectPrototype popObject = sequenceProto.GetPopObject();
                    popObject?.GetContainedEntities(entities);
                }

                foreach (var entityRef in entities)
                {
                    if (entityRef == PrototypeId.Invalid) continue;                    
                    var proto = GameDatabase.GetPrototype<Prototype>(entityRef);
                    if (proto is AgentPrototype agentProto && agentProto.Alliance != PrototypeId.Invalid)
                    {
                        var allianceProto = agentProto.Alliance.As<AlliancePrototype>();
                        if (resultProto == null || resultProto == allianceProto)
                            resultProto = allianceProto;
                        else
                            return null;
                    }
                    else
                    {
                        return null;
                    }
                   
                }
            }

            return resultProto;
        }

    }

    public class KismetSequenceEntityPrototype : WorldEntityPrototype
    {
        public PrototypeId KismetSequence { get; protected set; }
    }

    public class FactionPrototype : Prototype
    {
        public AssetId IconPath { get; protected set; }
        public PrototypeId TextStyle { get; protected set; }
        public FactionColor HealthColor { get; protected set; }
    }

    public class WaypointPrototype : Prototype
    {
        public PrototypeId Destination { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public bool SupressBannerMessage { get; protected set; }
        public bool IsCheckpoint { get; protected set; }
        public PrototypeId WaypointGraph { get; protected set; }
        public PrototypeId[] WaypointGraphList { get; protected set; }
        public PrototypeId RequiresItem { get; protected set; }
        public EvalPrototype EvalShouldDisplay { get; protected set; }
        public LocaleStringId Tooltip { get; protected set; }
        public bool IncludeWaypointPrefixInName { get; protected set; }
        public bool StartLocked { get; protected set; }
        public PrototypeId[] DestinationBossEntities { get; protected set; }
        public bool IsAccountWaypoint { get; protected set; }
        public int MigrationUnlockedByLevel { get; protected set; }
        public PrototypeId[] MigrationUnlockedByChapters { get; protected set; }
        public WaypointPOIType MapPOIType { get; protected set; }
        public PrototypeId[] MapConnectTo { get; protected set; }
        public LocaleStringId MapDescription { get; protected set; }
        public float MapPOIXCoord { get; protected set; }
        public float MapPOIYCoord { get; protected set; }
        public AssetId MapImage { get; protected set; }
        public PrototypeId OpenToWaypointGraph { get; protected set; }
        public AssetId MapImageConsole { get; protected set; }
        public AssetId LocationImageConsole { get; protected set; }
        public LocaleStringId ConsoleRegionDescription { get; protected set; }
        public LocaleStringId ConsoleLocationName { get; protected set; }
        public LocaleStringId ConsoleRegionType { get; protected set; }
        public LocaleStringId ConsoleLevelRange { get; protected set; }
        public LocalizedTextAndImagePrototype[] ConsoleRegionItems { get; protected set; }
        public PrototypeId[] ConsoleWaypointGraphList { get; protected set; }
    }

    public class WaypointChapterPrototype : Prototype
    {
        public PrototypeId Chapter { get; protected set; }
        public PrototypeId[] Waypoints { get; protected set; }
    }

    public class WaypointGraphPrototype : Prototype
    {
        public WaypointChapterPrototype[] Chapters { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public LocaleStringId MapDescription { get; protected set; }
        public AssetId MapImage { get; protected set; }
        public LocaleStringId Tooltip { get; protected set; }
    }

    public class CheckpointPrototype : Prototype
    {
        public PrototypeId Destination { get; protected set; }
    }
}
