using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    /// <summary>
    /// Provides shortcuts for access to game data.
    /// </summary>
    public static class GameDataExtensions
    {
        /// <summary>
        /// Returns the <see cref="AssetType"/> that this <see cref="PrototypeId"/> refers to.
        /// </summary>
        public static AssetType AsAssetType(this AssetTypeId assetTypeId)
        {
            return GameDatabase.GetAssetType(assetTypeId);
        }

        /// <summary>
        /// Returns the <see cref="Curve"/> that this <see cref="CurveId"/> refers to.
        /// </summary>
        public static Curve AsCurve(this CurveId curveId)
        {
            return GameDatabase.GetCurve(curveId);
        }

        /// <summary>
        /// Returns the <see cref="Blueprint"/> that this <see cref="BlueprintId"/> refers to.
        /// </summary>
        public static Blueprint AsBlueprint(this BlueprintId blueprintId)
        {
            return GameDatabase.GetBlueprint(blueprintId);
        }

        /// <summary>
        /// Returns the <typeparamref name="T"/> that this <see cref="PrototypeId"/> refers to.
        /// </summary>
        public static T As<T>(this PrototypeId prototypeId) where T: Prototype
        {
            return GameDatabase.GetPrototype<T>(prototypeId);
        }
    }
}
