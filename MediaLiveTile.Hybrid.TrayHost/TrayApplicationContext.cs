using MediaLiveTile.Hybrid.TrayHost.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.ApplicationModel;
using Windows.System;

namespace MediaLiveTile.Hybrid.TrayHost
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _openMainAppMenuItem;
        private readonly ToolStripMenuItem _refreshNowMenuItem;
        private readonly ToolStripMenuItem _monitoringMenuItem;
        private readonly ToolStripMenuItem _startupMenuItem;
        private readonly ToolStripMenuItem _exitMenuItem;

        private readonly TrayMediaWatcherService _mediaWatcherService = new TrayMediaWatcherService();
        private readonly TrayTileUpdateService _tileUpdateService = new TrayTileUpdateService();
        private readonly TrayStartupTaskService _startupTaskService = new TrayStartupTaskService();
        private readonly TrayStateSnapshotService _stateSnapshotService = new TrayStateSnapshotService();
        private readonly TrayRefreshRequestService _refreshRequestService = new TrayRefreshRequestService();
        private readonly TrayLogService _logService = new TrayLogService();

        private readonly System.Windows.Forms.Timer _syncTimer;

        private bool _disposed;
        private bool _isUpdatingTiles;
        private bool _pendingTileUpdate;
        private bool _isWritingSnapshot;
        private bool _pendingSnapshotWrite;

        private StartupTaskState _startupTaskState = StartupTaskState.Disabled;
        private long _lastRefreshRequestStamp;

        private bool? _lastLoggedMonitoringPaused;
        private string _lastLoggedPrimarySignature = string.Empty;

        public TrayApplicationContext()
        {
            _menu = new ContextMenuStrip();
            _menu.Opening += Menu_Opening;

            _openMainAppMenuItem = new ToolStripMenuItem("打开主应用");
            _refreshNowMenuItem = new ToolStripMenuItem("立即刷新");
            _monitoringMenuItem = new ToolStripMenuItem("暂停监测");
            _startupMenuItem = new ToolStripMenuItem("开机自启：读取中...");
            _exitMenuItem = new ToolStripMenuItem("退出");

            _openMainAppMenuItem.Click += OpenMainAppMenuItem_Click;
            _refreshNowMenuItem.Click += RefreshNowMenuItem_Click;
            _monitoringMenuItem.Click += MonitoringMenuItem_Click;
            _startupMenuItem.Click += StartupMenuItem_Click;
            _exitMenuItem.Click += ExitMenuItem_Click;

            _menu.Items.Add(_openMainAppMenuItem);
            _menu.Items.Add(_refreshNowMenuItem);
            _menu.Items.Add(_monitoringMenuItem);
            _menu.Items.Add(_startupMenuItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_exitMenuItem);

            _notifyIcon = new NotifyIcon
            {
                Text = "MediaLiveTile.Hybrid",
                Icon = LoadTrayIcon(),
                Visible = true,
                ContextMenuStrip = _menu
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            _mediaWatcherService.StateChanged += MediaWatcherService_StateChanged;

            _syncTimer = new System.Windows.Forms.Timer();
            _syncTimer.Interval = 2000;
            _syncTimer.Tick += SyncTimer_Tick;
            _syncTimer.Start();

            _ = InitializeAsync();
        }

        private Icon LoadTrayIcon()
        {
            try
            {
                var assembly = typeof(TrayApplicationContext).Assembly;
                string resourceName = null;

                foreach (var name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith(".Assets.tray.ico", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = name;
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(resourceName))
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            return new Icon(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadTrayIcon failed: " + ex);
            }

            return SystemIcons.Application;
        }

        private async Task InitializeAsync()
        {
            try
            {
                _lastRefreshRequestStamp = _refreshRequestService.GetStamp();

                await _logService.InfoAsync("TrayHost 启动");
                await _mediaWatcherService.InitializeAsync();
                await RefreshMonitoringMenuStateAsync();
                await RefreshStartupMenuStateAsync();
                await UpdateTilesFromLatestAsync();
                await WriteStateSnapshotAsync();
                await LogStateChangesAsync(true);

                ShowCurrentSummaryBalloon("托盘宿主已启动");
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync("TrayHost 初始化失败", ex);
                ShowBalloonTip("MediaLiveTile.Hybrid", "初始化失败：" + ex.Message);
            }
        }

        private async void SyncTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                await _mediaWatcherService.SyncMonitoringStateFromSettingsAsync();
                await RefreshMonitoringMenuStateAsync();

                long currentRefreshRequestStamp = _refreshRequestService.GetStamp();
                if (currentRefreshRequestStamp != _lastRefreshRequestStamp)
                {
                    _lastRefreshRequestStamp = currentRefreshRequestStamp;
                    await _logService.InfoAsync("收到外部立即刷新请求");
                    await RefreshNowAsync();
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync("同步定时器执行失败", ex);
            }
        }

        private async void Menu_Opening(object? sender, CancelEventArgs e)
        {
            await RefreshMonitoringMenuStateAsync();
            await RefreshStartupMenuStateAsync();
        }

        private void MediaWatcherService_StateChanged(object? sender, EventArgs e)
        {
            UpdateNotifyIconText();
            _ = RefreshMonitoringMenuStateAsync();
            _ = WriteStateSnapshotAsync();
            _ = LogStateChangesAsync(false);

            if (!_mediaWatcherService.IsRefreshing && !_mediaWatcherService.IsMonitoringPaused)
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

        private async void RefreshNowMenuItem_Click(object? sender, EventArgs e)
        {
            await RefreshNowAsync();
        }

        private async void MonitoringMenuItem_Click(object? sender, EventArgs e)
        {
            await ToggleMonitoringAsync();
        }

        private async void StartupMenuItem_Click(object? sender, EventArgs e)
        {
            await ToggleStartupAsync();
        }

        private async void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            await _logService.InfoAsync("TrayHost 退出");
            ExitThread();
        }

        private async Task OpenMainAppAsync()
        {
            try
            {
                bool launched = await Launcher.LaunchUriAsync(new Uri("medialivetilehybrid:"));

                await _logService.InfoAsync(launched
                    ? "请求打开 UWP 主应用"
                    : "请求打开 UWP 主应用失败");

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
                await _logService.ErrorAsync("打开主应用失败", ex);

                MessageBox.Show(
                    "打开主应用失败：" + ex.Message,
                    "MediaLiveTile.Hybrid",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task RefreshNowAsync()
        {
            SetBusyState(true);

            try
            {
                await _logService.InfoAsync("开始手动刷新");
                await _mediaWatcherService.RefreshNowAsync(true);
                await UpdateTilesFromLatestAsync();
                await WriteStateSnapshotAsync();
                await _logService.InfoAsync("手动刷新完成");

                ShowCurrentSummaryBalloon("媒体状态已刷新");
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync("手动刷新失败", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task ToggleMonitoringAsync()
        {
            try
            {
                SetBusyState(true);

                bool newPaused = !_mediaWatcherService.IsMonitoringPaused;
                await _mediaWatcherService.SetMonitoringPausedAsync(newPaused);
                await RefreshMonitoringMenuStateAsync();
                await WriteStateSnapshotAsync();

                if (newPaused)
                {
                    await _logService.InfoAsync("监测已暂停");
                    ShowBalloonTip("MediaLiveTile.Hybrid", "已暂停监测");
                }
                else
                {
                    await UpdateTilesFromLatestAsync();
                    await WriteStateSnapshotAsync();
                    await _logService.InfoAsync("监测已恢复");
                    ShowBalloonTip("MediaLiveTile.Hybrid", "已恢复监测");
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync("切换监测状态失败", ex);
                ShowBalloonTip("MediaLiveTile.Hybrid", "设置监测状态失败：" + ex.Message);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task ToggleStartupAsync()
        {
            try
            {
                SetBusyState(true);

                await RefreshStartupMenuStateAsync();

                if (TrayStartupTaskService.IsEnabled(_startupTaskState))
                {
                    _startupTaskState = await _startupTaskService.DisableAsync();
                    TrayStartupTaskService.BumpSyncStamp();

                    await RefreshStartupMenuStateAsync();
                    await _logService.InfoAsync("开机自启已关闭");
                    ShowBalloonTip("MediaLiveTile.Hybrid", "已关闭开机自启");
                }
                else
                {
                    _startupTaskState = await _startupTaskService.EnableAsync();
                    TrayStartupTaskService.BumpSyncStamp();

                    await RefreshStartupMenuStateAsync();

                    if (TrayStartupTaskService.IsEnabled(_startupTaskState))
                    {
                        await _logService.InfoAsync("开机自启已开启");
                        ShowBalloonTip("MediaLiveTile.Hybrid", "已开启开机自启");
                    }
                    else
                    {
                        string stateText = TrayStartupTaskService.GetStateText(_startupTaskState);
                        await _logService.WarnAsync("开机自启未能开启：" + stateText);
                        ShowBalloonTip("MediaLiveTile.Hybrid", "开机自启未开启：" + stateText);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync("切换开机自启失败", ex);
                ShowBalloonTip("MediaLiveTile.Hybrid", "设置开机自启失败：" + ex.Message);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private Task RefreshMonitoringMenuStateAsync()
        {
            _monitoringMenuItem.Checked = !_mediaWatcherService.IsMonitoringPaused;
            _monitoringMenuItem.Text = _mediaWatcherService.IsMonitoringPaused ? "恢复监测" : "暂停监测";

            return Task.CompletedTask;
        }

        private async Task RefreshStartupMenuStateAsync()
        {
            try
            {
                _startupTaskState = await _startupTaskService.GetStateAsync();
            }
            catch
            {
                _startupTaskState = StartupTaskState.Disabled;
            }

            _startupMenuItem.Checked = TrayStartupTaskService.IsEnabled(_startupTaskState);
            _startupMenuItem.Text = "开机自启：" + TrayStartupTaskService.GetStateText(_startupTaskState);
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
                await _logService.ErrorAsync("磁贴更新失败", ex);
                Debug.WriteLine("Tile update failed: " + ex);
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

        private async Task WriteStateSnapshotAsync()
        {
            if (_isWritingSnapshot)
            {
                _pendingSnapshotWrite = true;
                return;
            }

            _isWritingSnapshot = true;

            try
            {
                await _stateSnapshotService.WriteAsync(
                    _mediaWatcherService.LatestResult,
                    _mediaWatcherService.StatusText,
                    _mediaWatcherService.IsMonitoringPaused,
                    _mediaWatcherService.LastRefreshTime);
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync("状态快照写入失败", ex);
                Debug.WriteLine("State snapshot write failed: " + ex);
            }
            finally
            {
                _isWritingSnapshot = false;

                if (_pendingSnapshotWrite)
                {
                    _pendingSnapshotWrite = false;
                    _ = WriteStateSnapshotAsync();
                }
            }
        }

        private async Task LogStateChangesAsync(bool force)
        {
            try
            {
                if (_lastLoggedMonitoringPaused == null ||
                    _lastLoggedMonitoringPaused.Value != _mediaWatcherService.IsMonitoringPaused ||
                    force)
                {
                    _lastLoggedMonitoringPaused = _mediaWatcherService.IsMonitoringPaused;

                    await _logService.InfoAsync(
                        _mediaWatcherService.IsMonitoringPaused
                            ? "当前状态：监测已暂停"
                            : "当前状态：监测中");
                }

                if (_mediaWatcherService.IsRefreshing)
                    return;

                var primary = _mediaWatcherService.LatestResult != null
                    ? _mediaWatcherService.LatestResult.PrimaryMedia
                    : null;

                string signature = primary == null
                    ? "<none>"
                    : primary.SourceDisplayName + "|" + primary.Title + "|" + primary.PlaybackStatus;

                if (force || signature != _lastLoggedPrimarySignature)
                {
                    _lastLoggedPrimarySignature = signature;

                    if (primary == null)
                    {
                        await _logService.InfoAsync("当前无主媒体");
                    }
                    else
                    {
                        await _logService.InfoAsync(
                            "主媒体更新：" +
                            primary.SourceDisplayName + " - " +
                            primary.Title +
                            (string.IsNullOrWhiteSpace(primary.Artist) ? string.Empty : " / " + primary.Artist) +
                            " / " + primary.PlaybackStatus);
                    }
                }
            }
            catch
            {
            }
        }

        private void SetBusyState(bool isBusy)
        {
            _openMainAppMenuItem.Enabled = !isBusy;
            _refreshNowMenuItem.Enabled = !isBusy;
            _monitoringMenuItem.Enabled = !isBusy;
            _startupMenuItem.Enabled = !isBusy;
        }

        private void UpdateNotifyIconText()
        {
            string text;

            if (_mediaWatcherService.IsMonitoringPaused)
            {
                text = "MediaLiveTile.Hybrid - 监测已暂停";
            }
            else
            {
                var primary = _mediaWatcherService.LatestResult != null
                    ? _mediaWatcherService.LatestResult.PrimaryMedia
                    : null;

                if (primary == null)
                {
                    text = "MediaLiveTile.Hybrid";
                }
                else
                {
                    text = "MediaLiveTile - " + primary.SourceDisplayName + ": " + primary.Title;
                }
            }

            _notifyIcon.Text = TrimNotifyIconText(text);
        }

        private void ShowCurrentSummaryBalloon(string title)
        {
            if (_mediaWatcherService.IsMonitoringPaused)
            {
                ShowBalloonTip(title, "监测已暂停");
                return;
            }

            var primary = _mediaWatcherService.LatestResult != null
                ? _mediaWatcherService.LatestResult.PrimaryMedia
                : null;

            if (primary == null)
            {
                ShowBalloonTip(title, _mediaWatcherService.StatusText);
                return;
            }

            string subtitle = string.IsNullOrWhiteSpace(primary.Artist) ||
                              string.Equals(primary.Artist, "无艺人", StringComparison.OrdinalIgnoreCase)
                ? primary.SourceDisplayName
                : primary.Artist;

            ShowBalloonTip(title, primary.Title + "\n" + subtitle);
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

                _syncTimer.Stop();
                _syncTimer.Dispose();

                _mediaWatcherService.StateChanged -= MediaWatcherService_StateChanged;

                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _menu.Dispose();
            }

            base.ExitThreadCore();
        }
    }
}