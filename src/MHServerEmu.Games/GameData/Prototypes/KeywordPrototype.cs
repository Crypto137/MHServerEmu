using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    /// <summary>
    /// Indicates that an object can be flagged with one or more <see cref="KeywordPrototype"/> instances.
    /// </summary>
    public interface IKeyworded
    {
        public bool HasKeyword(KeywordPrototype keywordProto);
    }

    public class KeywordsMask : GBitArray, IKeyworded
    {
        public static readonly KeywordsMask Empty = new();

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return KeywordPrototype.TestKeywordBit(this, keywordProto);
        }
    }

    public class KeywordPrototype : Prototype
    {
        public PrototypeId IsAKeyword { get; protected set; }

        private int _bitIndex = -1;
        private KeywordsMask _bitMask = new();

        public static KeywordsMask GetBitMaskForKeywordList(PrototypeId[] keywordsList)
        {
            KeywordsMask result = new();

            if (keywordsList.HasValue())
            {
                foreach (PrototypeId keywordRef in keywordsList)
                {
                    KeywordPrototype keywordProto = GameDatabase.GetPrototype<KeywordPrototype>(keywordRef);
                    keywordProto?.GetBitMask(ref result);
                }
            }
            return result;
        }

        public static KeywordsMask GetBitMaskForKeywordList(IEnumerable<PrototypeId> keywordsList)
        {
            KeywordsMask result = new();

            foreach (PrototypeId keywordRef in keywordsList)
            {
                KeywordPrototype keywordProto = GameDatabase.GetPrototype<KeywordPrototype>(keywordRef);
                keywordProto?.GetBitMask(ref result);
            }

            return result;
        }

        public void GetBitMask(ref KeywordsMask keywordMask)
        {
            if (_bitIndex == -1)
            {
                CacheBitMaskInfo();
                if (_bitMask.Any() == false) return;
                
            }
            keywordMask = GBitArray.Or(keywordMask, _bitMask);
        }

        public int GetBitIndex()
        {
            if (_bitIndex == -1)
            {
                CacheBitMaskInfo();
                if (_bitIndex == -1) return 0;

            }
            return _bitIndex;
        }

        private void CacheBitMaskInfo()
        {
            BlueprintId keywordBlueprintRef = GameDatabase.DataDirectory.KeywordBlueprint;
            if (keywordBlueprintRef != BlueprintId.Invalid)
            {
                _bitIndex = GameDatabase.DataDirectory.GetPrototypeEnumValue(DataRef, keywordBlueprintRef);
                if (_bitIndex >= 0)
                {
                    _bitMask.Clear();
                    _bitMask.Set(_bitIndex);

                    if (IsAKeyword != PrototypeId.Invalid)
                    {
                        KeywordPrototype isAKeywordProto = GameDatabase.GetPrototype<KeywordPrototype>(IsAKeyword);
                        isAKeywordProto?.GetBitMask(ref _bitMask);
                    }
                }
            }
        }

        public static bool TestKeywordBit(KeywordsMask keywordsMask, KeywordPrototype keywordProto)
        {
            return keywordsMask[keywordProto.GetBitIndex()];
        }
    }

    public class EntityKeywordPrototype : KeywordPrototype
    {
        public LocaleStringId DisplayName { get; protected set; }
    }

    public class MobKeywordPrototype : EntityKeywordPrototype
    {
    }

    public class AvatarKeywordPrototype : EntityKeywordPrototype
    {
    }

    public class MissionKeywordPrototype : KeywordPrototype
    {
    }

    public class PowerKeywordPrototype : KeywordPrototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public bool DisplayInPowerKeywordsList { get; protected set; }
    }

    public class RankKeywordPrototype : KeywordPrototype
    {
    }

    public class RegionKeywordPrototype : KeywordPrototype
    {
    }

    public class AffixCategoryPrototype : KeywordPrototype
    {
    }

    public class FulfillablePrototype : Prototype
    {
    }

}
