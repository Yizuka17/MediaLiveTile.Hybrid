using MediaLiveTile.Hybrid.Shared;
using Windows.Storage;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal static class TrayTileSettingsService
    {
        private static readonly string SmallTileTargetIndexKey = SharedConstants.LocalSettingsKeys.SmallTileTargetIndex;
        private static readonly string MediumTileTargetIndexKey = SharedConstants.LocalSettingsKeys.MediumTileTargetIndex;
        private static readonly string WideTileTargetIndexKey = SharedConstants.LocalSettingsKeys.WideTileTargetIndex;
        private static readonly string LargeTileTargetIndexKey = SharedConstants.LocalSettingsKeys.LargeTileTargetIndex;

        public static int GetSmallTileTargetIndex() => GetInt(SmallTileTargetIndexKey, 0);
        public static int GetMediumTileTargetIndex() => GetInt(MediumTileTargetIndexKey, 0);
        public static int GetWideTileTargetIndex() => GetInt(WideTileTargetIndexKey, 0);
        public static int GetLargeTileTargetIndex() => GetInt(LargeTileTargetIndexKey, 0);

        private static int GetInt(string key, int defaultValue)
        {
            object? raw = ApplicationData.Current.LocalSettings.Values[key];

            if (raw is int intValue)
                return intValue;

            if (raw is string text && int.TryParse(text, out int parsed))
                return parsed;

            return defaultValue;
        }
    }
}