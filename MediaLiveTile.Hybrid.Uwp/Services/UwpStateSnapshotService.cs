using MediaLiveTile.Hybrid.Uwp.Models;
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

        public async Task<UwpStateSnapshot> ReadAsync()
        {
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    SnapshotFolderName,
                    CreationCollisionOption.OpenIfExists);

                var file = await folder.GetFileAsync(SnapshotFileName);
                var serializer = new DataContractJsonSerializer(typeof(UwpStateSnapshot));

                using (IRandomAccessStream randomAccessStream = await file.OpenAsync(FileAccessMode.Read))
                using (var stream = randomAccessStream.AsStreamForRead())
                {
                    var result = serializer.ReadObject(stream) as UwpStateSnapshot;
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}