using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Populations
{
    public class PropTable
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public List<AssetId> PropSetRefs = new();
        public Dictionary<PrototypeId, PropGroupList> Map = new();

        public PropTable() { }

        public bool AppendPropSet(AssetId propSetRef)
        {
            if (propSetRef == 0) return false;

            PropSetRefs.Add(propSetRef);
            PropSetPrototype propSetProto = GetPropSetPrototypeFromRef(propSetRef);
            if (propSetProto == null) return false;

            if (propSetProto.PropShapeLists.HasValue())
            {
                foreach (PropSetTypeListPrototype propList in propSetProto.PropShapeLists)
                {
                    if (propList == null)
                    {
                        Logger.Warn($"propList PropSetRef: {GameDatabase.GetAssetName(propSetRef)}");
                        continue;
                    }

                    PrototypeGuid propTypeGuid = propList.PropType;
                    if (propTypeGuid == 0) continue;

                    PrototypeId propTypeDataRef = GameDatabase.GetDataRefByPrototypeGuid(propTypeGuid);
                    if (propTypeDataRef == 0)
                    {
                        Logger.Warn($"propTypeDataRef PropSetRef: {GameDatabase.GetAssetName(propSetRef)}");
                        continue;
                    }

                    if (propList.PropShapeEntries.HasValue())
                    {
                        if (!Map.TryGetValue(propTypeDataRef, out PropGroupList groupList))
                        {
                            groupList = Map[propTypeDataRef] = new();
                        }

                        foreach (PropSetTypeEntryPrototype shapeEntry in propList.PropShapeEntries)
                        {
                            if (shapeEntry == null || shapeEntry.NameId == "" || shapeEntry.ResourcePackage == "") continue;

                            ProceduralPropGroupPrototype propGroup = GetProceduralPropGroupFromPackageAndId(shapeEntry.ResourcePackage, shapeEntry.NameId);
                            PropGroupListEntry entry = new(propGroup, propSetRef);
                            groupList.Add(entry);
                        }
                    }
                }
            }

            return true;
        }

        private static PropSetPrototype GetPropSetPrototypeFromRef(AssetId propSetRef)
        {
            if (propSetRef == 0)
            {
                Console.WriteLine("Area contains an empty PropSet List entry.");
                return null;
            }

            PrototypeId proto = GameDatabase.GetDataRefByAsset(propSetRef);
            if (proto == 0)
            {
                Console.WriteLine("Area contains a PropSet Asset that does not match any files in the resource folder.");
                return null;
            }

            return GameDatabase.GetPrototype<PropSetPrototype>(proto);
        }

        private static ProceduralPropGroupPrototype GetProceduralPropGroupFromPackageAndId(string packageName, string nameId)
        {
            if (packageName == "" || string.IsNullOrEmpty(nameId))
            {
                Console.WriteLine("Invalid package name or nameId.");
                return null;
            }

            PrototypeId packageRef = GameDatabase.GetPrototypeRefByName(packageName);
            PropPackagePrototype packageProto = GameDatabase.GetPrototype<PropPackagePrototype>(packageRef);
            if (packageProto == null)
            {
                Console.WriteLine($"Unable to find Prop Package with Resource Guid {packageName}");
                return null;
            }

            ProceduralPropGroupPrototype propGroupProto = packageProto.GetPropGroupFromName(nameId);
            if (propGroupProto == null)
            {
                Console.WriteLine($"Unable to find Prop in Package {packageName} of Name {nameId}");
            }

            return propGroupProto;
        }

        public bool GetRandomPropMarkerOfType(GRandom random, PrototypeId propMarkerRef, out PropGroupListEntry propGroup)
        {
            propGroup = new(null, AssetId.Invalid);

            if (Map.ContainsKey(propMarkerRef) && Map[propMarkerRef] != null)
            {
                Picker<PropGroupListEntry> picker = new(random);
                foreach (var entry in Map[propMarkerRef])
                    picker.Add(entry);

                if (picker.Empty() == false && picker.Pick(out var prop))
                {
                    propGroup = prop;
                    return true;
                }
            }
            return false;
        }

        public class PropGroupList : List<PropGroupListEntry> { }

        public class PropGroupListEntry
        {
            public ProceduralPropGroupPrototype PropGroup;
            public AssetId PropSetRef;

            public PropGroupListEntry(ProceduralPropGroupPrototype propGroup, AssetId propSetRef)
            {
                PropGroup = propGroup;
                PropSetRef = propSetRef;
            }
        }

        public static void GetPropRandomOffsetAndRotation(out Vector3 randomOffset, out float randomRotation, int randomSeed, ProceduralPropGroupPrototype propGroup)
        {
            randomOffset = Vector3.Zero;
            randomRotation = 0;

            if (propGroup.RandomPosition > 0 || propGroup.RandomRotationDegrees > 0)
            {
                GRandom random = new(randomSeed);

                if (propGroup.RandomPosition > 0)
                    randomOffset = Vector3.RandomUnitVector2D(random);

                if (propGroup.RandomRotationDegrees > 0)
                    randomRotation = MathHelper.ToRadians(propGroup.RandomRotationDegrees * 2) * random.NextFloat() - MathHelper.ToRadians(propGroup.RandomRotationDegrees);
            }
        }

    }

}
