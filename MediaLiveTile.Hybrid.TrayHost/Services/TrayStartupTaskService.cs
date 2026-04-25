using MediaLiveTile.Hybrid.Shared;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TrayStartupTaskService
    {
        public const string StartupTaskId = "TrayStartupTask";
        private static readonly string StartupSyncStampKey = SharedConstants.LocalSettingsKeys.StartupTaskSyncStamp;

        public async Task<StartupTaskState> GetStateAsync()
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            return startupTask.State;
        }

        public async Task<StartupTaskState> EnableAsync()
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            return await startupTask.RequestEnableAsync();
        }

        public async Task<StartupTaskState> DisableAsync()
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            startupTask.Disable();
            return startupTask.State;
        }

        public static bool IsEnabled(StartupTaskState state)
        {
            return state == StartupTaskState.Enabled
                || state == StartupTaskState.EnabledByPolicy;
        }

        public static string GetStateText(StartupTaskState state)
        {
            switch (state)
            {
                case StartupTaskState.Enabled:
                    return "已开启";

                case StartupTaskState.Disabled:
                    return "已关闭";

                case StartupTaskState.EnabledByPolicy:
                    return "已由策略启用";

                case StartupTaskState.DisabledByPolicy:
                    return "已由策略禁用";

                case StartupTaskState.DisabledByUser:
                    return "已被用户禁用";

                default:
                    return state.ToString();
            }
        }

        public static long GetSyncStamp()
        {
            object? raw = ApplicationData.Current.LocalSettings.Values[StartupSyncStampKey];

            if (raw is long longValue)
                return longValue;

            if (raw is string text && long.TryParse(text, out long parsed))
                return parsed;

            return 0L;
        }

        public static long BumpSyncStamp()
        {
            long stamp = GetSyncStamp() + 1;
            ApplicationData.Current.LocalSettings.Values[StartupSyncStampKey] = stamp;
            return stamp;
        }
    }
}