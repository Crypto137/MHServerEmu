using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.Generators.Population
{
    public class PropTable
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public List<ulong> PropSetRefs = new();
        public Dictionary<ulong, PropGroupList> Map = new();

        public PropTable() { }

        public bool AppendPropSet(ulong propSetRef)
        {
            if (propSetRef == 0) return false;

            PropSetRefs.Add(propSetRef);
            PropSetPrototype propSetProto = GetPropSetPrototypeFromRef(propSetRef);
            if (propSetProto == null) return false;

            if (propSetProto.PropShapeLists != null && propSetProto.PropShapeLists.Length > 0)
            {
                foreach (PropSetTypeListPrototype propList in propSetProto.PropShapeLists)
                {
                    if (propList == null)
                    {
                        Logger.Warn($"propList PropSetRef: {GameDatabase.GetAssetName(propSetRef)}");
                        continue;
                    }

                    ulong propTypeGuid = propList.PropType;
                    if (propTypeGuid == 0) continue;

                    ulong propTypeDataRef = GameDatabase.GetDataRefByPrototypeGuid(propTypeGuid);
                    if (propTypeDataRef == 0)
                    {
                        Logger.Warn($"propTypeDataRef PropSetRef: {GameDatabase.GetAssetName(propSetRef)}");
                        continue;
                    }

                    if (propList.PropShapeEntries != null && propList.PropShapeEntries.Length > 0)
                    {
                        if (!Map.TryGetValue(propTypeDataRef, out PropGroupList groupList))
                        {
                            groupList = Map[propTypeDataRef] = new ();
                        }

                        foreach (PropSetTypeEntryPrototype shapeEntry in propList.PropShapeEntries)
                        {
                            if (shapeEntry == null || shapeEntry.NameId == "" || shapeEntry.ResourcePackage == "") continue;

                            ProceduralPropGroupPrototype propGroup = GetProceduralPropGroupFromPackageAndId(shapeEntry.ResourcePackage, shapeEntry.NameId);
                            PropGroupListEntry entry = new (propGroup, propSetRef);
                            groupList.Add(entry);
                        }
                    }
                }
            }

            return true;
        }

        private static PropSetPrototype GetPropSetPrototypeFromRef(ulong propSetRef)
        {
            if (propSetRef == 0)
            {
                Console.WriteLine("Area contains an empty PropSet List entry.");
                return null;
            }

            ulong proto = GameDatabase.GetDataRefByAsset(propSetRef);
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

            ulong packageRef = GameDatabase.GetPrototypeRefByName(packageName);
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

        public class PropGroupList : List<PropGroupListEntry> { }

        public class PropGroupListEntry
        {
            private ProceduralPropGroupPrototype _propGroup;
            private ulong _propSetRef;

            public PropGroupListEntry(ProceduralPropGroupPrototype propGroup, ulong propSetRef)
            {
                _propGroup = propGroup;
                _propSetRef = propSetRef;
            }
        }
    }
}
