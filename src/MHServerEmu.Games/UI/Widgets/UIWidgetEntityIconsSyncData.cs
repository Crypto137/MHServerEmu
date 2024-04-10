using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetEntityIconsSyncData : UISyncData
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<FilterEntry> _filterList = new();

        public UIWidgetEntityIconsSyncData(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef)
        {
            var prototype = GameDatabase.GetPrototype<UIWidgetEntityIconsPrototype>(widgetRef);
            if (prototype == null)
            {
                Logger.Warn($"UIWidgetEntityIconsSyncData(): widgetPrototype == null");
                return;
            }

            if (prototype.Entities == null) return;

            for (int i = 0; i < prototype.Entities.Length; i++)
            {
                UIWidgetEntityIconsEntryPrototype entryPrototype = prototype.Entities[i];
                if (entryPrototype.Filter == null)
                {
                    Logger.Warn("UIWidgetEntityIconsSyncData(): entryPrototype.Filter == null");
                    continue;
                }

                FilterEntry filterEntry = new();
                filterEntry.Index = i;
                filterEntry.KnownEntityDict = new();

                // TODO: get entity data from the UIDataProvider's owner region, for now add dummy data for testing
                for (ulong j = 100; j < 101; j++)
                {
                    KnownEntityEntry entityEntry = new();
                    entityEntry.EntityId = j;
                    entityEntry.State = UIWidgetEntityState.Alive;
                    entityEntry.HealthPercent = 100;
                    entityEntry.EnrageStartTime = Clock.GameTime.Add(TimeSpan.FromMilliseconds(1000 * 60 * 60));  // 60 minutes
                    filterEntry.KnownEntityDict.Add(entityEntry.EntityId, entityEntry);
                }

                _filterList.Add(filterEntry);
            }
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
                        int state = (int)kvp.Value.State;
                        int healthPercent = kvp.Value.HealthPercent;
                        int iconIndexForHealthPercentEval = kvp.Value.IconIndexForHealthPercentEval;
                        bool forceRefreshEntityHealthPercent = kvp.Value.ForceRefreshEntityHealthPercent;
                        long enrageStartTime = (long)kvp.Value.EnrageStartTime.TotalMilliseconds;
                        bool hasPropertyEntryEval = kvp.Value.HasPropertyEntryEval;
                        int propertyEntryIndex = kvp.Value.PropertyEntryIndex;

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

                        filterEntry.KnownEntityDict.Add(entityId, entityEntry);
                    }
                }

                UpdateUI();
            }

            return success;
        }

        public override void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            base.Decode(stream, boolDecoder);

            _filterList.Clear();

            int numFilterEntries = stream.ReadRawInt32();
            for (int i = 0; i < numFilterEntries; i++)
            {
                FilterEntry filterEntry = new();
                filterEntry.Index = stream.ReadRawInt32();

                int numEntityEntries = stream.ReadRawInt32();
                if (numEntityEntries == 0) continue;

                filterEntry.KnownEntityDict = new();
                for (int j = 0; j < numEntityEntries; j++)
                {
                    KnownEntityEntry entityEntry = new();

                    entityEntry.EntityId = stream.ReadRawVarint64();
                    entityEntry.State = (UIWidgetEntityState)stream.ReadRawInt32();
                    entityEntry.HealthPercent = stream.ReadRawInt32();
                    entityEntry.IconIndexForHealthPercentEval = stream.ReadRawInt32();
                    entityEntry.ForceRefreshEntityHealthPercent = boolDecoder.ReadBool(stream);
                    entityEntry.EnrageStartTime = TimeSpan.FromMilliseconds(stream.ReadRawInt64());
                    entityEntry.HasPropertyEntryEval = boolDecoder.ReadBool(stream);
                    entityEntry.PropertyEntryIndex = stream.ReadRawInt32();

                    filterEntry.KnownEntityDict.Add(entityEntry.EntityId, entityEntry);
                }

                _filterList.Add(filterEntry);
            }

            UpdateUI();
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawInt32(_filterList.Count);
            foreach (FilterEntry filterEntry in _filterList)
            {
                stream.WriteRawInt32(filterEntry.Index);

                if (filterEntry.KnownEntityDict == null)
                {
                    stream.WriteRawInt32(0);
                    continue;
                }

                stream.WriteRawInt32(filterEntry.KnownEntityDict.Count);
                foreach (KnownEntityEntry entityEntry in filterEntry.KnownEntityDict.Values)
                {
                    stream.WriteRawVarint64(entityEntry.EntityId);
                    stream.WriteRawInt32((int)entityEntry.State);
                    stream.WriteRawInt32(entityEntry.HealthPercent);
                    stream.WriteRawInt32(entityEntry.IconIndexForHealthPercentEval);
                    boolEncoder.WriteBuffer(stream);    // ForceRefreshEntityHealthPercent
                    stream.WriteRawInt64((long)entityEntry.EnrageStartTime.TotalMilliseconds);
                    boolEncoder.WriteBuffer(stream);    // HasPropertyEntryEval
                    stream.WriteRawInt32(entityEntry.PropertyEntryIndex);
                }
            }
        }

        public override void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (FilterEntry filterEntry in _filterList)
            {
                if (filterEntry.KnownEntityDict == null) continue;
                foreach (KnownEntityEntry entityEntry in filterEntry.KnownEntityDict.Values)
                {
                    boolEncoder.EncodeBool(entityEntry.ForceRefreshEntityHealthPercent);
                    boolEncoder.EncodeBool(entityEntry.HasPropertyEntryEval);
                }
            }
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < _filterList.Count; i++)
                sb.AppendLine($"{nameof(_filterList)}[{i}]: {_filterList[i]}");
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

    public class KnownEntityEntry
    {
        public ulong EntityId { get; set; }
        public UIWidgetEntityState State { get; set; }
        public int HealthPercent { get; set; }
        public int IconIndexForHealthPercentEval { get; set; }
        public bool ForceRefreshEntityHealthPercent { get; set; }
        public TimeSpan EnrageStartTime { get; set; }
        public bool HasPropertyEntryEval { get; set; }
        public int PropertyEntryIndex { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new();
            //sb.AppendLine($"{nameof(EntityId)}: {EntityId}");
            sb.AppendLine($"{nameof(State)}: {State}");
            sb.AppendLine($"{nameof(HealthPercent)}: {HealthPercent}");
            sb.AppendLine($"{nameof(IconIndexForHealthPercentEval)}: {IconIndexForHealthPercentEval}");
            sb.AppendLine($"{nameof(ForceRefreshEntityHealthPercent)}: {ForceRefreshEntityHealthPercent}");
            sb.AppendLine($"{nameof(EnrageStartTime)}: {EnrageStartTime - Clock.GameTime}");
            sb.AppendLine($"{nameof(HasPropertyEntryEval)}: {HasPropertyEntryEval}");
            sb.AppendLine($"{nameof(PropertyEntryIndex)}: {PropertyEntryIndex}");
            return sb.ToString();
        }
    }
}
