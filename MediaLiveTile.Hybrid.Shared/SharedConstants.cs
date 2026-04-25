namespace MediaLiveTile.Hybrid.Shared
{
    public static class SharedConstants
    {
        public static class LocalSettingsKeys
        {
            public const string MonitoringPaused = "MonitoringPaused";
            public const string RefreshRequestStamp = "RefreshRequestStamp";
            public const string StartupTaskSyncStamp = "StartupTaskSyncStamp";
            public const string StateSnapshotSyncStamp = "StateSnapshotSyncStamp";
            public const string TrayHostLaunchFailureMessage = "TrayHostLaunchFailureMessage";
            public const string SmallTileTargetIndex = "SmallTileTargetIndex";
            public const string MediumTileTargetIndex = "MediumTileTargetIndex";
            public const string WideTileTargetIndex = "WideTileTargetIndex";
            public const string LargeTileTargetIndex = "LargeTileTargetIndex";
        }

        public static class StateSnapshot
        {
            public const string FolderName = "Shared";
            public const string FileName = "CurrentState.json";
            public const string TempFileName = "CurrentState.tmp";
        }

        public static class CacheFolders
        {
            public const string TrayThumbCache = "TrayThumbCache";
            public const string TrayAppIconCache = "TrayAppIconCache";
        }

        public static class Logging
        {
            public const string LogFileName = "MediaLiveTile.log";
            public const string OldLogFileName = "MediaLiveTile.old";
            public const ulong MaxLogFileBytes = 5UL * 1024UL * 1024UL;
        }
    }
}
