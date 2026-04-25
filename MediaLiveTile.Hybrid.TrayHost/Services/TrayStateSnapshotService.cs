using MediaLiveTile.Hybrid.Shared;
using MediaLiveTile.Hybrid.Shared.Models;
using MediaLiveTile.Hybrid.TrayHost.Models;
using System;
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
        private static readonly string SnapshotFolderName = SharedConstants.StateSnapshot.FolderName;
        private static readonly string SnapshotFileName = SharedConstants.StateSnapshot.FileName;
        private static readonly string SnapshotTempFileName = SharedConstants.StateSnapshot.TempFileName;
        private static readonly string StateSnapshotSyncStampKey = SharedConstants.LocalSettingsKeys.StateSnapshotSyncStamp;

        public static long GetSyncStamp()
        {
            object? raw = ApplicationData.Current.LocalSettings.Values[StateSnapshotSyncStampKey];

            if (raw is long longValue)
                return longValue;

            if (raw is string text && long.TryParse(text, out long parsed))
                return parsed;

            return 0L;
        }

        public static long BumpSyncStamp()
        {
            long stamp = GetSyncStamp() + 1;
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

            var tempFile = await folder.CreateFileAsync(
                SnapshotTempFileName,
                CreationCollisionOption.ReplaceExisting);

            var serializer = new DataContractJsonSerializer(typeof(SharedStateSnapshot));

            using (IRandomAccessStream randomAccessStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
            using (var stream = randomAccessStream.AsStreamForWrite())
            {
                stream.SetLength(0);
                serializer.WriteObject(stream, snapshot);
                await stream.FlushAsync();
                await randomAccessStream.FlushAsync();
            }

            try
            {
                var existingFile = await folder.GetFileAsync(SnapshotFileName);
                await tempFile.MoveAndReplaceAsync(existingFile);
            }
            catch (System.IO.FileNotFoundException)
            {
                await tempFile.RenameAsync(SnapshotFileName, NameCollisionOption.ReplaceExisting);
            }
            catch
            {
                var existingFile = await TryGetFileAsync(folder, SnapshotFileName);
                if (existingFile != null)
                {
                    await tempFile.MoveAndReplaceAsync(existingFile);
                }
                else
                {
                    await tempFile.RenameAsync(SnapshotFileName, NameCollisionOption.ReplaceExisting);
                }
            }

            BumpSyncStamp();
        }

        private static async Task<StorageFile?> TryGetFileAsync(StorageFolder folder, string fileName)
        {
            try
            {
                return await folder.GetFileAsync(fileName);
            }
            catch
            {
                return null;
            }
        }

        private SharedStateSnapshot BuildSnapshot(
            TrayMediaSelectionResult latestResult,
            string statusText,
            bool isMonitoringPaused,
            DateTimeOffset? lastRefreshTime)
        {
            var snapshot = new SharedStateSnapshot
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
                    var session = ConvertSession(item);
                    if (session != null)
                    {
                        snapshot.Sessions.Add(session);
                    }
                }
            }

            return snapshot;
        }

        private SharedStateSessionItem? ConvertSession(TrayMediaSessionInfo? item)
        {
            if (item == null)
                return null;

            return new SharedStateSessionItem
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