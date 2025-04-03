namespace MHServerEmu.Games.GameData.Prototypes
{
    public class FullscreenMoviePrototype : Prototype
    {
        public AssetId MovieName { get; protected set; }
        public bool Skippable { get; protected set; }
        public MovieType MovieType { get; protected set; }
        public bool ExitGameAfterPlay { get; protected set; }
        public LocaleStringId MovieTitle { get; protected set; }
        public AssetId Banter { get; protected set; }
        public LocaleStringId YouTubeVideoID { get; protected set; }
        public bool YouTubeControlsEnabled { get; protected set; }
        public LocaleStringId StreamingMovieNameHQ { get; protected set; }
        public LocaleStringId StreamingMovieNameLQ { get; protected set; }
        public LocaleStringId StreamingMovieNameMQ { get; protected set; }
    }

    public class LoadingScreenPrototype : Prototype
    {
        public AssetId LoadingScreenAsset { get; protected set; }
        public LocaleStringId Title { get; protected set; }
    }

    public class KismetSequencePrototype : Prototype
    {
        public AssetId KismetSeqName { get; protected set; }
        public bool KismetSeqBlocking { get; protected set; }
        public bool AudioListenerAtCamera { get; protected set; }
        public bool HideAvatarsDuringPlayback { get; protected set; }
    }
}
