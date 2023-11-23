namespace MHServerEmu.Games.GameData
{
    // Here we define strict types for data refs using enums

    // Regular ids can change between versions
    public enum StringId : ulong { Invalid = 0 }        // Used for assets and field names
    public enum LocaleStringId : ulong { Invalid = 0 }  // Used for localized strings
    public enum AssetTypeId : ulong { Invalid = 0 }
    public enum CurveId : ulong { Invalid = 0 }
    public enum BlueprintId : ulong { Invalid = 0 }     // Same as prototype id, refers both to .blueprint and .defaults files
    public enum PrototypeId : ulong { Invalid = 0 }     // Hashed file path, see HashHelper.HashPath() for more details

    // GUIDs stay the same between versions
    public enum AssetGuid : ulong { Invalid = 0 }
    public enum AssetTypeGuid : ulong { Invalid = 0 }
    public enum CurveGuid : ulong { Invalid = 0}        // CurveGuid doesn't seem to be used anywhere at all
    public enum BlueprintGuid : ulong { Invalid = 0 }   // Unlike id, this is not the same as prototype GUID
    public enum PrototypeGuid : ulong { Invalid = 0 }
}
