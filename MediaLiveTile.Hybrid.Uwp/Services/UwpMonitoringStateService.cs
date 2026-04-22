using Windows.Storage;

namespace MediaLiveTile.Hybrid.Uwp.Services
{
    internal sealed class UwpMonitoringStateService
    {
        private const string MonitoringPausedKey = "MonitoringPaused";

        public bool IsPaused()
        {
            object raw = ApplicationData.Current.LocalSettings.Values[MonitoringPausedKey];

            if (raw is bool boolValue)
                return boolValue;

            if (raw is string text && bool.TryParse(text, out bool parsed))
                return parsed;

            return false;
        }

        public void SetPaused(bool paused)
        {
            ApplicationData.Current.LocalSettings.Values[MonitoringPausedKey] = paused;
        }
    }
}