using MediaLiveTile.Hybrid.TrayHost.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TrayStateSnapshotService
    {
        private const string SnapshotFolderName = "Shared";
        private const string SnapshotFileName = "CurrentState.json";
        private const string StateSnapshotSyncStampKey = "StateSnapshotSyncStamp";

        public static long GetSyncStamp()
        {
            object raw = ApplicationData.Current.LocalSettings.Values[StateSnapshotSyncStampKey];

            if (raw is long longValue)
                return longValue;

            if (raw is string text && long.TryParse(text, out long parsed))
                return parsed;

            return 0L;
        }

        public static long BumpSyncStamp()
        {
            long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ApplicationData.Current.LocalSettings.Values[StateSnapshotSyncStampKey] = stamp;
            return stamp;
        }

        public async Task WriteAsync(
            TrayMediaSelectionResult latestResult,
            string statusText,
            bool isMonitoringPaused,
            DateTimeOffset? lastRefreshTime)
        {
            var snapshot = BuildSnapshot(
                latestResult,
                statusText,
                isMonitoringPaused,
                lastRefreshTime);

            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                SnapshotFolderName,
                CreationCollisionOption.OpenIfExists);

            var file = await folder.CreateFileAsync(
                SnapshotFileName,
                CreationCollisionOption.ReplaceExisting);

            var serializer = new DataContractJsonSerializer(typeof(TrayStateSnapshot));

            using (IRandomAccessStream randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            using (var stream = randomAccessStream.AsStreamForWrite())
            {
                stream.SetLength(0);
                serializer.WriteObject(stream, snapshot);
                await stream.FlushAsync();
            }

            BumpSyncStamp();
        }

        private TrayStateSnapshot BuildSnapshot(
            TrayMediaSelectionResult latestResult,
            string statusText,
            bool isMonitoringPaused,
            DateTimeOffset? lastRefreshTime)
        {
            var snapshot = new TrayStateSnapshot
            {
                StatusText = statusText ?? string.Empty,
                IsMonitoringPaused = isMonitoringPaused,
                HasLastRefreshTime = lastRefreshTime.HasValue,
                LastRefreshUnixTimeMilliseconds = lastRefreshTime.HasValue
                    ? lastRefreshTime.Value.ToUnixTimeMilliseconds()
                    : 0L,
                PrimaryMedia = ConvertSession(latestResult?.PrimaryMedia),
                SecondaryMedia = ConvertSession(latestResult?.SecondaryMedia)
            };

            if (latestResult?.AllSessions != null)
            {
                foreach (var item in latestResult.AllSessions)
                {
                    snapshot.Sessions.Add(ConvertSession(item));
                }
            }

            return snapshot;
        }

        private TrayStateSessionItem ConvertSession(TrayMediaSessionInfo item)
        {
            if (item == null)
                return null;

            return new TrayStateSessionItem
            {
                Order = item.Order,
                Role = item.Role ?? string.Empty,
                SourceDisplayName = item.SourceDisplayName ?? string.Empty,
                SourceAppUserModelId = item.SourceAppUserModelId ?? string.Empty,
                Title = item.Title ?? string.Empty,
                Artist = item.Artist ?? string.Empty,
                AlbumTitle = item.AlbumTitle ?? string.Empty,
                PlaybackStatus = item.PlaybackStatus ?? string.Empty,
                ImageUri = item.EffectiveImageUri ?? string.Empty
            };
        }
    }
}