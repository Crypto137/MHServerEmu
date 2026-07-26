using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

#if GAME_VERSION_1_53
    [AssetEnum((int)None)]
    [Flags]
    public enum CinematicType
    {
        None            = 0,
        Kismet          = 1 << 0,
        FullscreenMovie = 1 << 1,
        Loading         = 1 << 2,
        TransitionFade  = 1 << 3,
    }
#endif

#if GAME_VERSION_1_53
    [AssetEnum((int)None)]
    public enum TransitionFade
    {
        None,
        FadeToBlack,
        FadeFromBlack,
    }
#endif

    #endregion

#if GAME_VERSION_1_53
    public class CinematicPrototype : Prototype
    {
        public CinematicType CinematicType { get; protected set; }
    }
#endif

#if GAME_VERSION_1_53
    public class FullscreenMoviePrototype : CinematicPrototype
#else
    public class FullscreenMoviePrototype : Prototype
#endif
    {
        public AssetId MovieName { get; protected set; }
        public bool Skippable { get; protected set; }
#if !GAME_VERSION_1_53
        public MovieType MovieType { get; protected set; }
#endif
        public bool ExitGameAfterPlay { get; protected set; }
        public LocaleStringId MovieTitle { get; protected set; }
        public AssetId Banter { get; protected set; }
        public LocaleStringId YouTubeVideoID { get; protected set; }
        public bool YouTubeControlsEnabled { get; protected set; }
        public LocaleStringId StreamingMovieNameHQ { get; protected set; }
        public LocaleStringId StreamingMovieNameLQ { get; protected set; }
        public LocaleStringId StreamingMovieNameMQ { get; protected set; }
    }

#if GAME_VERSION_1_53
    public class LoadingScreenPrototype : CinematicPrototype
#else
    public class LoadingScreenPrototype : Prototype
#endif
    {
        public AssetId LoadingScreenAsset { get; protected set; }
        public LocaleStringId Title { get; protected set; }
    }

#if GAME_VERSION_1_53
    public class KismetSequencePrototype : CinematicPrototype
#else
    public class KismetSequencePrototype : Prototype
#endif
    {
        public AssetId KismetSeqName { get; protected set; }
        public bool KismetSeqBlocking { get; protected set; }
        public bool AudioListenerAtCamera { get; protected set; }
        public bool HideAvatarsDuringPlayback { get; protected set; }
    }

#if GAME_VERSION_1_53
    public class TransitionFadePrototype : CinematicPrototype
    {
        public TransitionFade TransitionFade { get; protected set; }
    }
#endif
}
