using Windows.Storage;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TrayRefreshRequestService
    {
        private const string RefreshRequestStampKey = "RefreshRequestStamp";

        public long GetStamp()
        {
            object raw = ApplicationData.Current.LocalSettings.Values[RefreshRequestStampKey];

            if (raw is long longValue)
                return longValue;

            if (raw is string text && long.TryParse(text, out long parsed))
                return parsed;

            return 0L;
        }
    }
}