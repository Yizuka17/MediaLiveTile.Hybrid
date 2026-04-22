using Windows.Storage;

namespace MediaLiveTile.Hybrid.Uwp.Services
{
    internal sealed class UwpTileSettingsService
    {
        private const string SmallTileTargetIndexKey = "SmallTileTargetIndex";
        private const string MediumTileTargetIndexKey = "MediumTileTargetIndex";
        private const string WideTileTargetIndexKey = "WideTileTargetIndex";
        private const string LargeTileTargetIndexKey = "LargeTileTargetIndex";

        public int GetSmallTileTargetIndex() => GetInt(SmallTileTargetIndexKey, 0);
        public int GetMediumTileTargetIndex() => GetInt(MediumTileTargetIndexKey, 0);
        public int GetWideTileTargetIndex() => GetInt(WideTileTargetIndexKey, 0);
        public int GetLargeTileTargetIndex() => GetInt(LargeTileTargetIndexKey, 0);

        public void SetSmallTileTargetIndex(int value) => SetInt(SmallTileTargetIndexKey, value);
        public void SetMediumTileTargetIndex(int value) => SetInt(MediumTileTargetIndexKey, value);
        public void SetWideTileTargetIndex(int value) => SetInt(WideTileTargetIndexKey, value);
        public void SetLargeTileTargetIndex(int value) => SetInt(LargeTileTargetIndexKey, value);

        private int GetInt(string key, int defaultValue)
        {
            object raw = ApplicationData.Current.LocalSettings.Values[key];

            if (raw is int intValue)
                return intValue;

            if (raw is string text && int.TryParse(text, out int parsed))
                return parsed;

            return defaultValue;
        }

        private void SetInt(string key, int value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }
    }
}