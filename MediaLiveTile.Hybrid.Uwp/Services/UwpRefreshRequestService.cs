using MediaLiveTile.Hybrid.Shared;
using Windows.Storage;

namespace MediaLiveTile.Hybrid.Uwp.Services
{
    internal sealed class UwpRefreshRequestService
    {
        private static readonly string RefreshRequestStampKey = SharedConstants.LocalSettingsKeys.RefreshRequestStamp;

        public static long GetStamp()
        {
            object? raw = ApplicationData.Current.LocalSettings.Values[RefreshRequestStampKey];

            if (raw is long longValue)
                return longValue;

            if (raw is string text && long.TryParse(text, out long parsed))
                return parsed;

            return 0L;
        }

        public long RequestRefresh()
        {
            long stamp = GetStamp() + 1;
            ApplicationData.Current.LocalSettings.Values[RefreshRequestStampKey] = stamp;
            return stamp;
        }
    }
}