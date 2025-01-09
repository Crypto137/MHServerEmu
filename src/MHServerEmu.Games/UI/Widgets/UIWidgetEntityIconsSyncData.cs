using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetEntityIconsSyncData : UISyncData
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<FilterEntry> _filterList = new();

        public UIWidgetEntityIconsPrototype Prototype;

        public UIWidgetEntityIconsSyncData(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef)
        {
            Prototype = GameDatabase.GetPrototype<UIWidgetEntityIconsPrototype>(widgetRef);
            if (Prototype == null)
            {
                Logger.Warn($"UIWidgetEntityIconsSyncData(): widgetPrototype == null");
                return;
            }

            if (Prototype.Entities.IsNullOrEmpty()) return;

            var region = uiDataProvider.Region;
            if (region == null) return;

            int index = 0;
            foreach (var entryProto in Prototype.Entities)
            {
                if (entryProto.Filter == null)
                {
                    Logger.Warn("UIWidgetEntityIconsSyncData(): entryPrototype.Filter == null");
                    continue;
                }

                FilterEntry filterEntry = new();
                filterEntry.Index = index++;
                filterEntry.KnownEntityDict = new();

                foreach(var entity in region.EntityTracker.Iterate(widgetRef, Dialog.EntityTrackingFlag.HUD))
                    if (entryProto.Filter.Evaluate(entity, new()))
                    {
                        KnownEntityEntry entityEntry = new();
                        entityEntry.EntityId = entity.Id;
                        entityEntry.State = entity.IsDead ? UIWidgetEntityState.Dead : UIWidgetEntityState.Alive;
                        entityEntry.AttachWatcher(this, entity);
                        filterEntry.KnownEntityDict.Add(entityEntry.EntityId, entityEntry);
                        UpdateKnownEntityTrackedProperties(entity.Id, entityEntry, entryProto, PropertyId.Invalid);                       
                    }

                _filterList.Add(filterEntry);
            }
        }

        public override void Deallocate() => ClearData();

        private void ClearData()
        {
            foreach (var filterEntry in _filterList)
                if (filterEntry.KnownEntityDict != null)
                {
                    foreach (var knowEntity in filterEntry.KnownEntityDict.Values)
                        knowEntity?.Destroy();

                    filterEntry.KnownEntityDict.Clear();
                }

            _filterList.Clear();
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            int numFilterEntries = _filterList.Count;
            success &= Serializer.Transfer(archive, ref numFilterEntries);

            // The current implementation, although questionable, is client-accurate.
            // TODO: We should clean this up and implement ISerialize for FilterEntry and KnownEntityEntry.
            if (archive.IsPacking)
            {
                foreach (FilterEntry filterEntry in _filterList)
                {
                    int index = filterEntry.Index;
                    success &= Serializer.Transfer(archive, ref index);
                    int numEntityEntries = filterEntry.KnownEntityDict == null ? 0 : filterEntry.KnownEntityDict.Count;
                    success &= Serializer.Transfer(archive, ref numEntityEntries);

                    if (filterEntry.KnownEntityDict == null) continue;

                    foreach (var kvp in filterEntry.KnownEntityDict)
                    {
                        ulong entityId = kvp.Key;
                        var entityEntry = kvp.Value;
                        int state = (int)entityEntry.State;
                        int healthPercent = entityEntry.HealthPercent;
                        int iconIndexForHealthPercentEval = entityEntry.IconIndexForHealthPercentEval;
                        bool forceRefreshEntityHealthPercent = entityEntry.ForceRefreshEntityHealthPercent;
                        entityEntry.ForceRefreshEntityHealthPercent = false;
                        long enrageStartTime = (long)entityEntry.EnrageStartTime.TotalMilliseconds;
                        bool hasPropertyEntryEval = entityEntry.HasPropertyEntryEval;
                        int propertyEntryIndex = entityEntry.PropertyEntryTableIndex;

                        success &= Serializer.Transfer(archive, ref entityId);
                        success &= Serializer.Transfer(archive, ref state);
                        success &= Serializer.Transfer(archive, ref healthPercent);
                        success &= Serializer.Transfer(archive, ref iconIndexForHealthPercentEval);
                        success &= Serializer.Transfer(archive, ref forceRefreshEntityHealthPercent);
                        success &= Serializer.Transfer(archive, ref enrageStartTime);
                        success &= Serializer.Transfer(archive, ref hasPropertyEntryEval);
                        success &= Serializer.Transfer(archive, ref propertyEntryIndex);
                    }
                }
            }
            else
            {
                _filterList.Clear();

                for (int i = 0; i < numFilterEntries; i++)
                {
                    int index = 0;
                    success &= Serializer.Transfer(archive, ref index);
                    int numEntityEntries = 0;
                    success &= Serializer.Transfer(archive, ref numEntityEntries);

                    FilterEntry filterEntry = new();
                    _filterList.Add(filterEntry);

                    filterEntry.Index = index;
                    if (numEntityEntries == 0) continue;

                    filterEntry.KnownEntityDict = new();

                    for (int j = 0; j < numEntityEntries; j++)
                    {
                        ulong entityId = 0;
                        int state = 0;
                        int healthPercent = 0;
                        int iconIndexForHealthPercentEval = -1;
                        bool forceRefreshEntityHealthPercent = false;
                        long enrageStartTime = 0;
                        bool hasPropertyEntryEval = false;
                        int propertyEntryIndex = -1;

                        success &= Serializer.Transfer(archive, ref entityId);
                        success &= Serializer.Transfer(archive, ref state);
                        success &= Serializer.Transfer(archive, ref healthPercent);
                        success &= Serializer.Transfer(archive, ref iconIndexForHealthPercentEval);
                        success &= Serializer.Transfer(archive, ref forceRefreshEntityHealthPercent);
                        success &= Serializer.Transfer(archive, ref enrageStartTime);
                        success &= Serializer.Transfer(archive, ref hasPropertyEntryEval);
                        success &= Serializer.Transfer(archive, ref propertyEntryIndex);

                        KnownEntityEntry entityEntry = new();
                        entityEntry.EntityId = entityId;
                        entityEntry.State = (UIWidgetEntityState)state;
                        entityEntry.HealthPercent = healthPercent;
                        entityEntry.IconIndexForHealthPercentEval = iconIndexForHealthPercentEval;
                        entityEntry.ForceRefreshEntityHealthPercent = forceRefreshEntityHealthPercent;
                        entityEntry.EnrageStartTime = TimeSpan.FromMilliseconds(enrageStartTime);
                        entityEntry.HasPropertyEntryEval = hasPropertyEntryEval;
                        entityEntry.PropertyEntryTableIndex = propertyEntryIndex;
                        filterEntry.KnownEntityDict.Add(entityId, entityEntry);
                    }
                }

                UpdateUI();
            }

            return success;
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < _filterList.Count; i++)
                sb.AppendLine($"{nameof(_filterList)}[{i}]: {_filterList[i]}");
        }

        public override void OnEntityLifecycle(WorldEntity worldEntity)
        {
            bool update = false;
            foreach (var filter in _filterList)
            {
                var entryProto = GetEntryPrototypeByIndex(filter.Index);
                if (entryProto == null) continue;
                if (entryProto.Filter.Evaluate(worldEntity, new()))
                {
                    if (filter.KnownEntityDict.TryGetValue(worldEntity.Id, out var entityEntry) == false)
                    {
                        entityEntry = new();
                        filter.KnownEntityDict.Add(worldEntity.Id, entityEntry);
                    }
                    entityEntry.State = worldEntity.IsDead ? UIWidgetEntityState.Dead : UIWidgetEntityState.Alive;                    
                    update = true;
                }
            }

            if (update) UpdateUI();
        }

        public override void OnEntityTracked(WorldEntity worldEntity)
        {
            if (worldEntity == null) return;

            bool update = false;
            foreach (var filter in _filterList)
            {
                var entryProto = GetEntryPrototypeByIndex(filter.Index);
                if (entryProto == null) continue;
                if (entryProto.Filter.Evaluate(worldEntity, new()))
                {
                    if (filter.KnownEntityDict.TryGetValue(worldEntity.Id, out var entityEntry) == false)
                    {
                        entityEntry = new();
                        filter.KnownEntityDict.Add(worldEntity.Id, entityEntry);
                    }
                    entityEntry.State = worldEntity.IsDead ? UIWidgetEntityState.Dead : UIWidgetEntityState.Alive;
                    entityEntry.AttachWatcher(this, worldEntity);
                    update = true;
                }
            }

            if (update) UpdateUI();
        }

        public override void OnKnownEntityPropertyChanged(PropertyId id)
        {
            UpdateKnownEntitiesTrackedProperties(id);
        }

        private void UpdateKnownEntitiesTrackedProperties(PropertyId id)
        {
            bool update = false;
            foreach (var filter in _filterList)
            {
                var entryProto = GetEntryPrototypeByIndex(filter.Index);
                if (entryProto == null) continue;
                if (filter.KnownEntityDict != null)
                    foreach (var kvp in filter.KnownEntityDict)
                        if (UpdateKnownEntityTrackedProperties(kvp.Key, kvp.Value, entryProto, id))
                            update = true;                    
            }

            if (update) UpdateUI();
        }

        private UIWidgetEntityIconsEntryPrototype GetEntryPrototypeByIndex(int index)
        {
            var entries = Prototype.Entities;
            if (entries.IsNullOrEmpty() || index < 0 || index >= entries.Length) return null;
            return entries[index];
        }

        public bool UpdateKnownEntityTrackedProperties(ulong entityId, KnownEntityEntry knownEntityEntry, UIWidgetEntityIconsEntryPrototype entryPrototype, PropertyId propertyId)
        {
            if (entryPrototype == null) return false;
            var game = _uiDataProvider.Game;

            if (knownEntityEntry.State == UIWidgetEntityState.Alive)
            {
                var worldEntity = game.EntityManager.GetEntity<WorldEntity>(entityId);
                if (worldEntity != null)
                {
                    if (entryPrototype is UIWidgetHealthPercentEntryPrototype healthPercentProto)
                    {
                        long health = worldEntity.Properties[PropertyEnum.Health];
                        long healthMax = worldEntity.Properties[PropertyEnum.HealthMaxOther];
                        int healthPercent = 100;
                        if (health != healthMax && healthMax != 0) 
                            healthPercent = Math.Min((int)Math.Ceiling((double)health / healthMax * 100), 99);

                        if (healthPercent != knownEntityEntry.HealthPercent)
                        {
                            knownEntityEntry.HealthPercent = healthPercent;

                            if (healthPercentProto.HealthDisplayTable.IsNullOrEmpty()) return false;

                            int index = 0;
                            foreach (var healthPercentIcon in healthPercentProto.HealthDisplayTable)
                            {
                                if (healthPercentIcon == null) continue;
                                if (healthPercent >= healthPercentIcon.HealthPercent)
                                {
                                    if (knownEntityEntry.IconIndexForHealthPercentEval != index)
                                    {
                                        knownEntityEntry.IconIndexForHealthPercentEval = index;
                                        knownEntityEntry.ForceRefreshEntityHealthPercent = true;
                                    }
                                    break;
                                }
                                index++;
                            }
                            return true;
                        }
                        return false;
                    }

                    if (entryPrototype is UIWidgetEnrageEntryPrototype && worldEntity.Properties.HasProperty(PropertyEnum.EnrageStartTime))
                    {
                        TimeSpan enrageStartTime = worldEntity.Properties[PropertyEnum.EnrageStartTime];
                        if (enrageStartTime != knownEntityEntry.EnrageStartTime)
                        {
                            knownEntityEntry.EnrageStartTime = enrageStartTime;
                            return true;
                        }
                        return false;
                    }

                    if (entryPrototype is UIWidgetEntityPropertyEntryPrototype propertyProto)
                    {
                        if (propertyProto.PropertyEval != null)
                        {
                            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();                            
                            evalContext.Game = game;
                            evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, worldEntity.Properties);

                            if (worldEntity is Agent agent && agent.AIController != null)
                                evalContext.SetVar_PropertyCollectionPtr(EvalContext.EntityBehaviorBlackboard, agent.AIController.Blackboard.PropertyCollection);

                            List<PropertyId> propertyIds = new();
                            Eval.GetEvalPropertyIds(propertyProto.PropertyEval, propertyIds, GetEvalPropertyIdEnum.Input, null);

                            if (propertyId == PropertyId.Invalid || propertyIds.Contains(propertyId))
                            {
                                bool hasProperty = Eval.RunBool(propertyProto.PropertyEval, evalContext);
                                if (hasProperty != knownEntityEntry.HasPropertyEntryEval)
                                {
                                    knownEntityEntry.HasPropertyEntryEval = hasProperty;
                                    return true;
                                }
                            }                            
                        }

                        if (propertyProto.PropertyEntryTable.HasValue())
                        {
                            bool hasDescriptor = propertyProto.Descriptor != PrototypeId.Invalid;

                            if (propertyId == PropertyId.Invalid || propertyProto.PropertyIds.Contains(propertyId) || hasDescriptor)
                            {
                                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, worldEntity.Properties);

                                if (worldEntity is Agent agent && agent.AIController != null)
                                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.EntityBehaviorBlackboard, agent.AIController.Blackboard.PropertyCollection);

                                int tableIndex = -1; 
                                int index = 0;
                                foreach (var entry in propertyProto.PropertyEntryTable)
                                {
                                    if (Eval.RunBool(entry.PropertyEval, evalContext))
                                    {
                                        tableIndex = index;
                                        break;
                                    }
                                    index++;
                                }

                                if (knownEntityEntry.PropertyEntryTableIndex != tableIndex)
                                {
                                    knownEntityEntry.PropertyEntryTableIndex = tableIndex;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            else if (knownEntityEntry.State == UIWidgetEntityState.Dead)
            {
                if (entryPrototype is UIWidgetHealthPercentEntryPrototype && knownEntityEntry.HealthPercent != 0)
                {
                    knownEntityEntry.HealthPercent = 0;
                    return true;
                }
            }

            return false;
        }

    }

    public class FilterEntry
    {
        public int Index { get; set; }
        public Dictionary<ulong, KnownEntityEntry> KnownEntityDict { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Index: {Index}");
            foreach (var kvp in KnownEntityDict)
                sb.AppendLine($"{nameof(KnownEntityDict)}[{kvp.Key}]: {kvp.Value}");
            return sb.ToString();
        }
    }

    public class KnownEntityEntry : IPropertyChangeWatcher
    {
        private PropertyCollection _properties;
        private UISyncData _uiSyncData;
        public ulong EntityId { get; set; }
        public UIWidgetEntityState State { get; set; }
        public int HealthPercent { get; set; }
        public int IconIndexForHealthPercentEval { get; set; }
        public bool ForceRefreshEntityHealthPercent { get; set; }
        public TimeSpan EnrageStartTime { get; set; }
        public bool HasPropertyEntryEval { get; set; }
        public int PropertyEntryTableIndex { get; set; }

        public KnownEntityEntry()
        {
            // IconIndexForHealthPercentEval = -1; // Bonus* bug
            PropertyEntryTableIndex = -1;
            EnrageStartTime = TimeSpan.FromMilliseconds(-1);
        }

        public void Destroy()
        {
            if (_properties != null) Detach(true);
        }

        public void Attach(PropertyCollection propertyCollection)
        {
            if (_properties != null && _properties == propertyCollection) return;
            _properties = propertyCollection;
            _properties.AttachWatcher(this);
        }

        public void Detach(bool removeFromAttachedCollection)
        {
            if (removeFromAttachedCollection)
                _properties?.DetachWatcher(this);
        }

        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            _uiSyncData?.OnKnownEntityPropertyChanged(id);
        }

        public void AttachWatcher(UISyncData widget, WorldEntity entity)
        {
            _uiSyncData = widget;

            if (entity is Agent agent && agent.AIController != null) 
                Attach(agent.AIController.Blackboard.PropertyCollection);
            Attach(entity.Properties);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            //sb.AppendLine($"{nameof(EntityId)}: {EntityId}");
            sb.AppendLine($"{nameof(State)}: {State}");
            sb.AppendLine($"{nameof(HealthPercent)}: {HealthPercent}");
            sb.AppendLine($"{nameof(IconIndexForHealthPercentEval)}: {IconIndexForHealthPercentEval}");
            sb.AppendLine($"{nameof(ForceRefreshEntityHealthPercent)}: {ForceRefreshEntityHealthPercent}");
            sb.AppendLine($"{nameof(EnrageStartTime)}: {EnrageStartTime - Game.Current.CurrentTime}");
            sb.AppendLine($"{nameof(HasPropertyEntryEval)}: {HasPropertyEntryEval}");
            sb.AppendLine($"{nameof(PropertyEntryTableIndex)}: {PropertyEntryTableIndex}");
            return sb.ToString();
        }
    }
}
