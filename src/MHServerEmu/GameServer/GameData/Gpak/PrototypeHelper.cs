using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public enum BlueprintId : ulong
    {
        WorldEntity = 7901305308382563236,
        ThrowablePowerProp = 8706319841384272336,
        ThrowableRestorePowerProp = 1483936524176856276,
        Costume = 10774581141289766864,
    }
    public enum MemberId : ulong
    {
        UnrealClass = 9963296804083405606,
        CostumeUnrealClass = 3331018908052953682,
    }
    public static class PrototypeHelper // Temporary replacement for prototype classes
    {   
        public static Prototype Prototype(this ulong prototype)
        {
            return GameDatabase.Calligraphy.GetPrototype(prototype);
        }
       
        public static PrototypeDataEntry GetById(this PrototypeDataEntry[] entries, ulong id)
        {
            return entries.FirstOrDefault(e => e.Id == id);
        }
        public static PrototypeDataEntry Entry(this Prototype prototype, BlueprintId id)
        {
            return prototype.Data.Entries.GetById((ulong)id);
        }
        public static PrototypeDataEntryElement GetById(this PrototypeDataEntryElement[] elements, ulong id)
        {
            return elements.FirstOrDefault(e => e.Id == id);
        }
        public static PrototypeDataEntryElement Field(this PrototypeDataEntry entry, MemberId id)
        {
            return entry.Elements.GetById((ulong)id);
        }
    }
}
