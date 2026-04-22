using Windows.Storage;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal static class TrayTileSettingsService
    {
        private const string SmallTileTargetIndexKey = "SmallTileTargetIndex";
        private const string MediumTileTargetIndexKey = "MediumTileTargetIndex";
        private const string WideTileTargetIndexKey = "WideTileTargetIndex";
        private const string LargeTileTargetIndexKey = "LargeTileTargetIndex";

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