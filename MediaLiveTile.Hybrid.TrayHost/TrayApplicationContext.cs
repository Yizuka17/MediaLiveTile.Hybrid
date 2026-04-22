using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MediaLiveTile.Hybrid.TrayHost.Services;
using Windows.System;

namespace MediaLiveTile.Hybrid.TrayHost
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _openMainAppMenuItem;
        private readonly ToolStripMenuItem _refreshNowMenuItem;
        private readonly ToolStripMenuItem _exitMenuItem;

        private readonly TrayMediaWatcherService _mediaWatcherService = new TrayMediaWatcherService();
        private readonly TrayTileUpdateService _tileUpdateService = new TrayTileUpdateService();

        private bool _disposed;
        private bool _isUpdatingTiles;
        private bool _pendingTileUpdate;

        public TrayApplicationContext()
        {
            _menu = new ContextMenuStrip();

            _openMainAppMenuItem = new ToolStripMenuItem("打开主应用");
            _refreshNowMenuItem = new ToolStripMenuItem("立即刷新");
            _exitMenuItem = new ToolStripMenuItem("退出");

            _openMainAppMenuItem.Click += OpenMainAppMenuItem_Click;
            _refreshNowMenuItem.Click += RefreshNowMenuItem_Click;
            _exitMenuItem.Click += ExitMenuItem_Click;

            _menu.Items.Add(_openMainAppMenuItem);
            _menu.Items.Add(_refreshNowMenuItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_exitMenuItem);

            _notifyIcon = new NotifyIcon
            {
                Text = "MediaLiveTile.Hybrid",
                Icon = SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = _menu
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            _mediaWatcherService.StateChanged += MediaWatcherService_StateChanged;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _mediaWatcherService.InitializeAsync();
                await UpdateTilesFromLatestAsync();
                ShowCurrentSummaryBalloon("托盘宿主已启动");
            }
            catch (Exception ex)
            {
                ShowBalloonTip("MediaLiveTile.Hybrid", $"初始化失败：{ex.Message}");
            }
        }

        private void MediaWatcherService_StateChanged(object? sender, EventArgs e)
        {
            UpdateNotifyIconText();

            if (!_mediaWatcherService.IsRefreshing)
            {
                _ = UpdateTilesFromLatestAsync();
            }
        }

        private async void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            await OpenMainAppAsync();
        }

        private async void OpenMainAppMenuItem_Click(object? sender, EventArgs e)
        {
            await OpenMainAppAsync();
        }

        private async Task OpenMainAppAsync()
        {
            try
            {
                bool launched = await Launcher.LaunchUriAsync(new Uri("medialivetilehybrid:"));

                if (!launched)
                {
                    MessageBox.Show(
                        "无法打开主应用。\n\n请确认 UWP 壳已正确安装并注册协议。",
                        "MediaLiveTile.Hybrid",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"打开主应用失败：{ex.Message}",
                    "MediaLiveTile.Hybrid",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void RefreshNowMenuItem_Click(object? sender, EventArgs e)
        {
            await RefreshNowAsync();
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            ExitThread();
        }

        

        private static bool TryLaunchByProtocol(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task RefreshNowAsync()
        {
            SetBusyState(true);

            try
            {
                await _mediaWatcherService.RefreshNowAsync();
                await UpdateTilesFromLatestAsync();
                ShowCurrentSummaryBalloon("媒体状态已刷新");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task UpdateTilesFromLatestAsync()
        {
            if (_isUpdatingTiles)
            {
                _pendingTileUpdate = true;
                return;
            }

            _isUpdatingTiles = true;

            try
            {
                await _tileUpdateService.UpdateMainTileAsync(_mediaWatcherService.LatestResult);
                await _tileUpdateService.UpdateAllSecondaryTilesAsync(_mediaWatcherService.LatestResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tile update failed: {ex}");
            }
            finally
            {
                _isUpdatingTiles = false;

                if (_pendingTileUpdate)
                {
                    _pendingTileUpdate = false;
                    _ = UpdateTilesFromLatestAsync();
                }
            }
        }

        private void SetBusyState(bool isBusy)
        {
            _openMainAppMenuItem.Enabled = !isBusy;
            _refreshNowMenuItem.Enabled = !isBusy;
        }

        private void UpdateNotifyIconText()
        {
            string text;

            var primary = _mediaWatcherService.LatestResult?.PrimaryMedia;
            if (primary == null)
            {
                text = "MediaLiveTile.Hybrid";
            }
            else
            {
                text = $"MediaLiveTile - {primary.SourceDisplayName}: {primary.Title}";
            }

            _notifyIcon.Text = TrimNotifyIconText(text);
        }

        private void ShowCurrentSummaryBalloon(string title)
        {
            var primary = _mediaWatcherService.LatestResult?.PrimaryMedia;

            if (primary == null)
            {
                ShowBalloonTip(title, _mediaWatcherService.StatusText);
                return;
            }

            string subtitle = string.IsNullOrWhiteSpace(primary.Artist) ||
                              string.Equals(primary.Artist, "无艺人", StringComparison.OrdinalIgnoreCase)
                ? primary.SourceDisplayName
                : primary.Artist;

            ShowBalloonTip(title, $"{primary.Title}\n{subtitle}");
        }

        private void ShowBalloonTip(string title, string text)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(1500);
        }

        private string TrimNotifyIconText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "MediaLiveTile.Hybrid";

            text = text.Replace('\r', ' ').Replace('\n', ' ');

            return text.Length <= 63
                ? text
                : text.Substring(0, 63);
        }

        protected override void ExitThreadCore()
        {
            if (!_disposed)
            {
                _disposed = true;

                _mediaWatcherService.StateChanged -= MediaWatcherService_StateChanged;

                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _menu.Dispose();
            }

            base.ExitThreadCore();
        }
    }
}