using MediaLiveTile.Hybrid.Shared;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TrayLogService
    {
        private static readonly string LogFileName = SharedConstants.Logging.LogFileName;
                private static readonly string OldLogFileName = SharedConstants.Logging.OldLogFileName;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly string _logDirectory;

        public TrayLogService()
        {
            _logDirectory = ResolveLogDirectory();
        }

        public Task InfoAsync(string message)
        {
            return WriteAsync("INFO", message);
        }

        public Task WarnAsync(string message)
        {
            return WriteAsync("WARN", message);
        }

        public Task ErrorAsync(string message, Exception? ex = null)
        {
            if (ex == null)
            {
                return WriteAsync("ERROR", message);
            }

            return WriteAsync("ERROR", message + " | " + ex.GetType().Name + " | " + ex.Message);
        }

                private string ResolveLogDirectory()
        {
            try
            {
                string path = ApplicationData.Current.LocalFolder.Path;
                if (!string.IsNullOrWhiteSpace(path))
                    return path;
            }
            catch
            {
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MediaLiveTile.Hybrid");
        }

        private string GetWritableLogFilePath()
        {
            Directory.CreateDirectory(_logDirectory);

            string logFilePath = Path.Combine(_logDirectory, LogFileName);
            string oldLogFilePath = Path.Combine(_logDirectory, OldLogFileName);

            var fileInfo = new FileInfo(logFilePath);
            if (!fileInfo.Exists || (ulong)fileInfo.Length <= SharedConstants.Logging.MaxLogFileBytes)
                return logFilePath;

            try
            {
                if (File.Exists(oldLogFilePath))
                {
                    File.Delete(oldLogFilePath);
                }

                File.Move(logFilePath, oldLogFilePath, true);
            }
            catch
            {
            }

            return logFilePath;
        }

        private async Task WriteAsync(string level, string message)
        {
            await _gate.WaitAsync();

            try
            {
                string filePath = GetWritableLogFilePath();

                string line = string.Format(
                    "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}{3}",
                    DateTimeOffset.Now,
                    level,
                    message ?? string.Empty,
                    Environment.NewLine);

                // 简单重试，避免极偶发占用
                Exception? lastError = null;

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        await File.AppendAllTextAsync(filePath, line, Encoding.UTF8);
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