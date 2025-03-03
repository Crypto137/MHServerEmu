using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior
{
    public class BehaviorBlackboard: IPropertyChangeWatcher
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Agent _owner; 
        public PropertyCollection PropertyCollection { get; private set; }
        public Vector3 SpawnPoint { get; set; }
        public Vector3 SpawnOffset { get; set; }
        public Vector3 UsePowerTargetPos { get; set; }
        public Queue<CustomPowerQueueEntry> CustomPowerQueue { get; private set; }
        public Vector3 LastFlankTargetEntityPos { get; set; }
        public Vector3 LastFlockPosition { get; set; }
        public Vector3 MoveToCurrentPathNodePos { get; set; }
        public Dictionary<ulong, long> DamageMap { get; private set; }
        public int AICustomThinkRateMS { get; private set; }

        public BehaviorBlackboard(Agent owner)
        {
            _owner = owner;
            PropertyCollection = new ();
            Attach(PropertyCollection);
            SpawnPoint = Vector3.Zero;
            SpawnOffset = Vector3.Zero;
            UsePowerTargetPos = Vector3.Zero;
        }

        public void Initialize(BehaviorProfilePrototype profile, SpawnSpec spec, PropertyCollection collection)
        {
            SpawnPoint = _owner.RegionLocation.Position;
            if (spec != null)
                SpawnOffset = spec.Transform.Translation;
       
            PropertyCollection[PropertyEnum.AIAggroDropRange] = profile.AggroDropDistance;
            PropertyCollection[PropertyEnum.AIAggroDropByLOSChance] = profile.AggroDropChanceLOS / 100.0f;
            PropertyCollection[PropertyEnum.AIAggroRangeHostile] = profile.AggroRangeHostile;
            PropertyCollection[PropertyEnum.AIAggroRangeAlly] = profile.AggroRangeAlly;

            if (profile.AlwaysAggroed)
            {
                PropertyCollection[PropertyEnum.AIAlwaysAggroed] = true;
                PropertyCollection[PropertyEnum.AIAggroState] = true;
                PropertyCollection[PropertyEnum.AIAggroTime] = (long)_owner.Game.CurrentTime.TotalMilliseconds;
            }

            if (profile.Properties != null)
                PropertyCollection.FlattenCopyFrom(profile.Properties, false);
            if (collection != null)
                PropertyCollection.FlattenCopyFrom(collection, false);
        }

        public void Attach(PropertyCollection propertyCollection)
        {
            if (propertyCollection != PropertyCollection)
            {
                Logger.Warn("Attach(): Entities can attach only to their own property collection");
                return;
            }
            PropertyCollection.AttachWatcher(this);
        }

        public void Detach(bool removeFromAttachedCollection)
        {
            if (removeFromAttachedCollection)
                PropertyCollection.DetachWatcher(this);
        }

        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            if (flags.HasFlag(SetPropertyFlags.Refresh) == false)
            {
                if (id.Enum == PropertyEnum.AICustomThinkRateMS)
                    AICustomThinkRateMS = newValue;
            }

             _owner.AIController?.Brain?.OnPropertyChange(id, newValue, oldValue, flags);
        }

        public void AddCustomPower(PrototypeId powerRef, Vector3 targetPos, ulong targetId)
        {
            CustomPowerQueue ??= new();
            CustomPowerQueue.Enqueue(new(powerRef, targetPos, targetId));
        }

        public void OnTrackIncomingDamage(ulong attackerId, long damage)
        {
            DamageMap ??= new();
            DamageMap.TryGetValue(attackerId, out long oldDamage);
            DamageMap[attackerId] = oldDamage + damage;
        }

        private GeneratedPath _cachedGenPath;
        public GeneratedPath CachedPath
        {
            get
            {
                _cachedGenPath ??= new ();
                return _cachedGenPath;
            }
        }

        private Dictionary<Type, ProceduralProfileRuntimeData> _proceduralProfileData = new ();

        public T GetProceduralProfileRuntimeData<T>() where T : ProceduralProfileRuntimeData
        {
            _proceduralProfileData.TryGetValue(typeof(T), out var runtimeData);
            return runtimeData as T;
        }

        public void SetProceduralProfileRuntimeData<T>(T runtimeData) where T : ProceduralProfileRuntimeData
        {
            _proceduralProfileData[typeof(T)] = runtimeData;
        }

        public bool ChangeBlackboardFact(PrototypeId propertyInfoRef, int value, BlackboardOperatorType operation, ulong targetId)
        {
            var infoTable = GameDatabase.PropertyInfoTable;
            var index = infoTable.GetPropertyEnumFromPrototype(propertyInfoRef);
            if (index == PropertyEnum.Invalid) return false;

            var dataType = infoTable.LookupPropertyInfo(index).DataType;

            if (operation == BlackboardOperatorType.SetTargetId)
            {
                if (dataType != PropertyDataType.EntityId) return false;
                if (_owner == null) return false;

                var controller = _owner.AIController;
                if (controller == null) return false;

                if (targetId != Entity.InvalidId)
                {
                    PropertyCollection.SetProperty(targetId, index);
                }
                else
                {
                    var targetEntity = controller.TargetEntity;
                    if (targetEntity != null)
                        PropertyCollection.SetProperty(targetEntity.Id, index);
                    else
                        return false;
                }
            }
            else if (operation == BlackboardOperatorType.ClearTargetId)
            {
                if (dataType != PropertyDataType.EntityId) return false;
                PropertyCollection.RemoveProperty(index);
            }
            else
            {
                if (dataType == PropertyDataType.EntityId) return false;

                if (dataType == PropertyDataType.Boolean)
                {
                    if (operation != BlackboardOperatorType.Set || (value < 0 || value > 1)) return false;
                    PropertyCollection.SetProperty(value == 1, index);
                }
                else
                {
                    int newValue = PropertyCollection.GetProperty(index);

                    switch (operation)
                    {
                        case BlackboardOperatorType.Add:
                            newValue += value;
                            break;
                        case BlackboardOperatorType.Div:
                            if (value == 0) return Logger.DebugReturn(false, "Attempted division by zero!");
                            newValue /= value;
                            break;
                        case BlackboardOperatorType.Mul:
                            newValue *= value;
                            break;
                        case BlackboardOperatorType.Set:
                            newValue = value;
                            break;
                        case BlackboardOperatorType.Sub:
                            newValue -= value;
                            break;
                    }

                    PropertyCollection.SetProperty(newValue, index);
                }
            }

            return true;
        }

    }

    public class ProceduralProfileRuntimeData { }

    public struct CustomPowerQueueEntry
    {
        public PrototypeId PowerRef;
        public Vector3 TargetPos;
        public ulong TargetId;

        public CustomPowerQueueEntry(PrototypeId powerRef, Vector3 targetPos, ulong targetId)
        {
            PowerRef = powerRef;
            TargetPos = targetPos;
            TargetId = targetId;
        }
    }
}
