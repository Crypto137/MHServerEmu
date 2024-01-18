using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using System.Reflection;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PrototypeFile
    {
        public CalligraphyHeader Header { get; }
        public Prototype Prototype { get; }

        public PrototypeFile(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadCalligraphyHeader();
                Prototype = new(reader);
            }
        }
    }

    public class Prototype
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        private ulong _dataRef;

        public byte Flags { get; }
        public ulong ParentId { get; private set; }  // 0 for .defaults
        public PrototypeEntry[] Entries { get; }

        public virtual void PostProcess() { }

        public Prototype() { } // for Resource Prototype

        public ulong GetDataRef() 
        {
            return _dataRef; 
        }

        public void SetDataRef(ulong prototypeId)
        {
            _dataRef = prototypeId;
        }

        public Prototype(Prototype proto) { }

        private object ConvertValue(object Value, Type fieldType, CalligraphyValueType valueType)
        {
            object convertedValue = null;

            if (Value == null) 
                return Convert.ChangeType(null, fieldType);
            
            switch (valueType)
            {
                case CalligraphyValueType.B: // bool
                case CalligraphyValueType.D: // float
                case CalligraphyValueType.L: // int short
                    convertedValue = Convert.ChangeType(Value, fieldType);
                    break;

                case CalligraphyValueType.R: // PrototypeRef 
                    Prototype proto = (Prototype)Value;
                    if (proto.ParentId != 0)
                    {
                        string className = GameDatabase.DataDirectory.GetPrototypeBlueprint(proto).RuntimeBinding;
                        // Logger.Info($"Init Prototype {className}");
                        if (className == "PropertyPrototype")
                        {
                            /*   Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(proto);
                               Prototype defaultData = GameDatabase.DataDirectory.GetBlueprintDefaultPrototype(blueprint);
                               convertedValue = new PropertyPrototype(defaultData);*/
                            return null;
                        }
                        Type protoType = Type.GetType("MHServerEmu.Games.GameData.Prototypes." + className);
                        if (protoType == null)
                        {
                            Logger.Warn($"PrototypeClass {className} not exist");
                            return null;
                        }
                        convertedValue = Activator.CreateInstance(protoType, new object[] { proto });
                    }
                    else 
                        convertedValue = null;
                    break;

                case CalligraphyValueType.P: // PrototypeId
                case CalligraphyValueType.S: // StringId
                case CalligraphyValueType.C: // CurveId
                    convertedValue = Value; // ulong
                    break;

                case CalligraphyValueType.A: // AssetName String                    

                    if (fieldType.IsEnum)
                    {
                        ulong assetId = (ulong)Value;
                        string assetName = GameDatabase.GetAssetName(assetId);
                        if (Enum.IsDefined(fieldType, assetName))
                        {
                            convertedValue = Enum.Parse(fieldType, assetName);
                        }
                    }
                    else convertedValue = (ulong)Value;

                    break;
                
                default:
                    convertedValue = Convert.ChangeType(Value, fieldType);
                    break;
            }
            return convertedValue;
        }

        private void SetValue(object Value, FieldInfo fieldInfo, CalligraphyValueType valueType)
        {
          //  Logger.Info($"Try Convert {valueType} type for {fieldInfo.Name}");
            object convertedValue = ConvertValue(Value, fieldInfo.FieldType, valueType);            
            fieldInfo.SetValue(this, convertedValue);
        }

        private void SetValues(object[] Values, FieldInfo fieldInfo, CalligraphyValueType valueType)
        {
          //  Logger.Info($"Try Convert {valueType} type for {fieldInfo.Name}");
            Type elementType = fieldInfo.FieldType.GetElementType(); 
            if (elementType != null)
            {
                Array array = Array.CreateInstance(elementType, Values.Length); 

                for (int i = 0; i < Values.Length; i++)
                {
                    object convertedValue = ConvertValue(Values[i], elementType, valueType);
                    array.SetValue(convertedValue, i);
                }

                fieldInfo.SetValue(this, array); 
            }
        }

        private void FillFieldsFromData(Prototype data, Blueprint blueprint, Type protoType)
        {
          //  Logger.Info($"FillFields for {blueprint.RuntimeBinding}");
            foreach (var member in blueprint.Members)
            {
                var fieldName = member.FieldName;
                var fieldType = protoType.GetField(fieldName);
                bool nextMember = true;
                if (fieldType != null)
                {
                    if (data.Entries != null)
                    {
                        foreach (var entry in data.Entries)
                        {
                            if (entry.Id == blueprint.DefaultPrototypeId)
                            {
                                if (fieldType.FieldType.IsArray)
                                {
                                    foreach (var element in entry.ListElements)
                                    {
                                        if (element.Id == member.FieldId)
                                        {
                                            SetValues(element.Values, fieldType, element.Type);
                                            nextMember = false;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var element in entry.Elements)
                                    {
                                        if (element.Id == member.FieldId)
                                        {
                                            SetValue(element.Value, fieldType, element.Type);
                                            nextMember = false;
                                            break;
                                        }
                                    }
                                }
                                if (nextMember == false) break;
                            }
                        }
                    }
                }

            }
        }

        public void LoadDefault(Type protoType, Blueprint blueprint) // TODO make Dictonary with defaults
        {
            // ulong defProto = blueprint.DefaultPrototypeId;
           // Logger.Info($"Default{GameDatabase.GetPrototypeName(defProto)}");
            Prototype defaultData = GameDatabase.DataDirectory.GetBlueprintDefaultPrototype(blueprint);
            //Type protoType = Type.GetType("MHServerEmu.Games.Generators.Prototypes." + blueprint.RuntimeBinding); 
            FillFieldsFromData(defaultData, blueprint, protoType);
        }

        public void FillPrototype(Type protoType, Prototype proto)
        {
            // copy data from old proto
            _dataRef = proto._dataRef; 
            ParentId = proto.ParentId;

            Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(proto);

            LoadDefault(protoType, blueprint);

            ulong parent = proto.ParentId;
            List<Prototype> parents = new();
            while (parent != blueprint.DefaultPrototypeId)
            {
                if (parent == 0) break;
                //Logger.Info($"{GameDatabase.GetPrototypeName(parent)}");
                Prototype parentProto = parent.GetPrototype();
                parents.Add(parentProto);
                parent = parentProto.ParentId;                
            }

            if (parents.Count > 0)
            {
                parents.Reverse();
                foreach (Prototype parentProto in parents)
                    FillFieldsFromData(parentProto, blueprint, protoType);
            }

            foreach (var parentRef in blueprint.Parents)
            {
                Prototype parentProto = parentRef.Id.GetPrototype();
                Blueprint parentBlueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(parentProto);
                FillFieldsFromData(proto, parentBlueprint, protoType);
            }

            FillFieldsFromData(proto, blueprint, protoType);            
        }

        public Prototype(BinaryReader reader)
        {
            Flags = reader.ReadByte();

            if ((Flags & 0x01) > 0)      // flag0 == contains parent id
            {
                ParentId = reader.ReadUInt64();

                if ((Flags & 0x02) > 0)  // flag1 == contains data
                {
                    Entries = new PrototypeEntry[reader.ReadUInt16()];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(reader);
                }
            }

            // flag2 == ??
        }

        public PrototypeEntry GetEntry(ulong blueprintId)
        {
            if (Entries == null) return null;
            return Entries.FirstOrDefault(entry => entry.Id == blueprintId);
        }
        public PrototypeEntry GetEntry(BlueprintId blueprintId) => GetEntry((ulong)blueprintId);
    }

    public class PrototypeEntry
    {
        public ulong Id { get; }
        public byte ByteField { get; }
        public PrototypeEntryElement[] Elements { get; }
        public PrototypeEntryListElement[] ListElements { get; }

        public PrototypeEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            ByteField = reader.ReadByte();

            Elements = new PrototypeEntryElement[reader.ReadUInt16()];
            for (int i = 0; i < Elements.Length; i++)
                Elements[i] = new(reader);

            ListElements = new PrototypeEntryListElement[reader.ReadUInt16()];
            for (int i = 0; i < ListElements.Length; i++)
                ListElements[i] = new(reader);
        }

        public PrototypeEntryElement GetField(ulong fieldId)
        {
            if (Elements == null) return null;
            return Elements.FirstOrDefault(field => field.Id == fieldId);
        }
        public PrototypeEntryElement GetField(FieldId fieldId) => GetField((ulong)fieldId);

        public ulong GetFieldDef(FieldId fieldId)
        {
            PrototypeEntryElement field = GetField((ulong)fieldId);
            if (field == null) return 0;
            return (ulong)field.Value;
        }

        public PrototypeEntryListElement GetListField(ulong fieldId)
        {
            if (ListElements == null) return null;
            return ListElements.FirstOrDefault(field => field.Id == fieldId);
        }

        public PrototypeEntryListElement GetListField(FieldId fieldId) => GetListField((ulong)fieldId);
    }

    public class PrototypeEntryElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object Value { get; }
        public PrototypeEntryElement(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Type = (CalligraphyValueType)reader.ReadByte();

            switch (Type)
            {
                case CalligraphyValueType.B:
                    Value = Convert.ToBoolean(reader.ReadUInt64());
                    break;
                case CalligraphyValueType.D:
                    Value = reader.ReadDouble();
                    break;
                case CalligraphyValueType.L:
                    Value = reader.ReadInt64();
                    break;
                case CalligraphyValueType.R:
                    Value = new Prototype(reader);
                    break;
                default:
                    Value = reader.ReadUInt64();
                    break;
            }
        }
    }

    public class PrototypeEntryListElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object[] Values { get; }

        public PrototypeEntryListElement(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Type = (CalligraphyValueType)reader.ReadByte();

            Values = new object[reader.ReadUInt16()];
            for (int i = 0; i < Values.Length; i++)
            {
                switch (Type)
                {
                    case CalligraphyValueType.B:
                        Values[i] = Convert.ToBoolean(reader.ReadUInt64());
                        break;
                    case CalligraphyValueType.D:
                        Values[i] = reader.ReadDouble();
                        break;
                    case CalligraphyValueType.L:
                        Values[i] = reader.ReadInt64();
                        break;
                    case CalligraphyValueType.R:
                        Values[i] = new Prototype(reader);
                        break;
                    default:
                        Values[i] = reader.ReadUInt64();
                        break;
                }
            }
        }
    }
}
