using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class SelectEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        internal static WorldEntity DoSelectEntity(ref SelectEntityContext selectionContext)
        {
            throw new NotImplementedException();
        }

        internal static bool RegisterSelectedEntity(AIController ownerController, WorldEntity selectedEntity, SelectEntityType selectEntityType)
        {
            throw new NotImplementedException();
        }

        public struct SelectEntityContext
        {
            public AIController OwnerController;
            public AIEntityAttributePrototype[] AttributeList;
            public float MaxDistanceThreshold;
            public float MinDistanceThreshold;
            public SelectEntityPoolType PoolType;
            public SelectEntityMethodType SelectionMethod;
            public SelectEntityType SelectEntityType;
            public bool LockEntityOnceSelected;
            public float CellOrRegionAABBScale;
            public PrototypeId AlliancePriority;
            public PropertyEnum ComparisonEnum;
            public bool StaticEntities;
            public PropertyDataType ComparisonDataType;

            public SelectEntityContext(AIController ownerController, SelectEntityContextPrototype proto)
            {
                OwnerController = ownerController;
                SelectionMethod = proto.SelectionMethod;
                PoolType = proto.PoolType;
                AttributeList = proto.AttributeList;
                MinDistanceThreshold = proto.MinDistanceThreshold;                
                MaxDistanceThreshold = proto.MaxDistanceThreshold;
                LockEntityOnceSelected = proto.LockEntityOnceSelected;
                SelectEntityType = proto.SelectEntityType;
                CellOrRegionAABBScale = proto.CellOrRegionAABBScale;
                AlliancePriority = proto.AlliancePriority;
                ComparisonEnum = 0;
                StaticEntities = false;
                ComparisonDataType = PropertyDataType.Invalid;
                if (FindPropertyInfoForPropertyComparison(ref ComparisonEnum, ref ComparisonDataType, proto.EntitiesPropertyForComparison) == false)
                    Logger.Warn("SelectEntityInfo()::Could not find property info for targets property for comparison");
            }

        }

        public static bool FindPropertyInfoForPropertyComparison(ref PropertyEnum property, ref PropertyDataType dataType, PrototypeId propertyForComparison)
        {
            if (propertyForComparison != PrototypeId.Invalid)
            {
                PropertyInfoTable infoTable = GameDatabase.PropertyInfoTable;
                property = infoTable.GetPropertyEnumFromPrototype(propertyForComparison);
                if (property == PropertyEnum.Invalid) return false;

                PropertyInfo propertyInfo = infoTable.LookupPropertyInfo(property);
                dataType = propertyInfo.DataType;
                if (propertyInfo.ParamCount != 0)
                {
                    Logger.Warn("Found an ActionSelectEntity that has the EntitiesPropertyForComparison option referencing a property with parameters, which isn't supported!");
                    return false;
                }
            }
            return true;
        }
    }
}
