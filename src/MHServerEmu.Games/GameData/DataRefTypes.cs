namespace MHServerEmu.Games.GameData
{
    // Here we define strict types for data refs using enums

    // Regular ids can change between versions
    public enum StringId : ulong { Invalid = 0 }        // Currently unknown hash, used for Calligraphy blueprint member names
    public enum AssetId : ulong { Invalid = 0 }         // Currently unknown hash, used for asset values
                                                        // NOTE: AssetId is supposed to inherit from StringId, which is why AssetIds are managed by StringRefManager.
    public enum LocaleStringId : ulong { Invalid = 0, Blank = 0 }  // Currently unknown hash, used for localized strings
    public enum AssetTypeId : ulong { Invalid = 0 }     // Hashed Calligraphy path, see HashHelper.HashPath()
    public enum CurveId : ulong { Invalid = 0 }         // Hashed Calligraphy path
    public enum BlueprintId : ulong { Invalid = 0 }     // Hashed Calligraphy path, refers to both .blueprint and .defaults prototype files, hashed from the blueprint path
    public enum PrototypeId : ulong { Invalid = 0 }     // Hashed Calligraphy path

    // GUIDs stay the same between versions
    // Generation algorithm currently unknown
    public enum AssetGuid : ulong { Invalid = 0 }
    public enum AssetTypeGuid : ulong { Invalid = 0 }
    public enum CurveGuid : ulong { Invalid = 0}        // CurveGuid doesn't seem to be used anywhere at all
    public enum BlueprintGuid : ulong { Invalid = 0 }   // Unlike id, this is not the same as prototype GUID
    public enum PrototypeGuid : ulong { Invalid = 0 }
}
