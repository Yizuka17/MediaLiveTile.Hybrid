using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TrayLogService
    {
        private const string LogFileName = "MediaLiveTile.log";
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public Task InfoAsync(string message)
        {
            return WriteAsync("INFO", message);
        }

        public Task WarnAsync(string message)
        {
            return WriteAsync("WARN", message);
        }

        public Task ErrorAsync(string message, Exception ex = null)
        {
            if (ex == null)
            {
                return WriteAsync("ERROR", message);
            }

            return WriteAsync("ERROR", message + " | " + ex.GetType().Name + " | " + ex.Message);
        }

        private async Task WriteAsync(string level, string message)
        {
            await _gate.WaitAsync();

            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    LogFileName,
                    CreationCollisionOption.OpenIfExists);

                string line = string.Format(
                    "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}{3}",
                    DateTimeOffset.Now,
                    level,
                    message ?? string.Empty,
                    Environment.NewLine);

                // 简单重试，避免极偶发占用
                Exception lastError = null;

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        await FileIO.AppendTextAsync(file, line);
                        return;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        await Task.Delay(40);
                    }
                }

                if (lastError != null)
                {
                    System.Diagnostics.Debug.WriteLine("Log write failed: " + lastError);
                }
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}