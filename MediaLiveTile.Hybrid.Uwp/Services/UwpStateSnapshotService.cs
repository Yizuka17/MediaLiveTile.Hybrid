using MediaLiveTile.Hybrid.Shared;
using MediaLiveTile.Hybrid.Shared.Models;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MediaLiveTile.Hybrid.Uwp.Services
{
    internal sealed class UwpStateSnapshotService
    {
        private static readonly string SnapshotFolderName = SharedConstants.StateSnapshot.FolderName;
        private static readonly string SnapshotFileName = SharedConstants.StateSnapshot.FileName;
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

        public async Task<SharedStateSnapshot?> ReadAsync()
        {
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    SnapshotFolderName,
                    CreationCollisionOption.OpenIfExists);

                var file = await folder.GetFileAsync(SnapshotFileName);
                var serializer = new DataContractJsonSerializer(typeof(SharedStateSnapshot));

                using (IRandomAccessStream randomAccessStream = await file.OpenAsync(FileAccessMode.Read))
                using (var stream = randomAccessStream.AsStreamForRead())
                {
                    return serializer.ReadObject(stream) as SharedStateSnapshot;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}