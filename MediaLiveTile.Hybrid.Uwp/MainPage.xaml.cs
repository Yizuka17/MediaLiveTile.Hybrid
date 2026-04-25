using MediaLiveTile.Hybrid.Shared;
using MediaLiveTile.Hybrid.Shared.Models;
using MediaLiveTile.Hybrid.Uwp.Models;
using MediaLiveTile.Hybrid.Uwp.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace MediaLiveTile.Hybrid.Uwp
{
    public sealed partial class MainPage : Page
    {
        private const int FixedTargetSlotCount = 6;
        private const string GitHubUrl = "https://github.com/Yizuka17/MediaLiveTile.Hybrid"; 

        private readonly UwpStartupTaskService _startupTaskService = new UwpStartupTaskService();
        private readonly UwpMonitoringStateService _monitoringStateService = new UwpMonitoringStateService();
        private readonly UwpStateSnapshotService _stateSnapshotService = new UwpStateSnapshotService();
        private readonly UwpTileSettingsService _tileSettingsService = new UwpTileSettingsService();
        private readonly UwpRefreshRequestService _refreshRequestService = new UwpRefreshRequestService();
        private readonly UwpSecondaryTileService _secondaryTileService = new UwpSecondaryTileService();

        private readonly DispatcherTimer _uiSyncTimer = new DispatcherTimer();
        private readonly BitmapImage _defaultPreviewImage = new BitmapImage();

        private bool _isUpdatingStartupToggle;
        private bool _isUpdatingMonitoringToggle;
        private bool _isApplyingTargetSelections;
        private int _actionStatusVersion;

        private long _lastStartupSyncStamp;
        private long _lastStateSyncStamp;
        private bool _lastMonitoringPaused;

        private int _smallTileTargetIndex;
        private int _mediumTileTargetIndex;
        private int _wideTileTargetIndex;
        private int _largeTileTargetIndex;

        private SharedStateSnapshot? _latestSnapshot;

        public ObservableCollection<SharedStateSessionItem> Sessions { get; } =
            new ObservableCollection<SharedStateSessionItem>();

        public ObservableCollection<TileTargetOption> TargetOptions { get; } =
            new ObservableCollection<TileTargetOption>();

        public MainPage()
        {
            this.InitializeComponent();

            _defaultPreviewImage.UriSource = new Uri("ms-appx:///Assets/Square150x150Logo.png");

            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;

            _uiSyncTimer.Interval = TimeSpan.FromSeconds(2);
            _uiSyncTimer.Tick += UiSyncTimer_Tick;

            InitializeFixedTargetOptions();
        }

        private void UpdatePinButtonTexts()
        {
            UpdatePinButtonText(PinSmallTileButton, PinnedTileKind.Small, _smallTileTargetIndex);
            UpdatePinButtonText(PinMediumTileButton, PinnedTileKind.Medium, _mediumTileTargetIndex);
            UpdatePinButtonText(PinWideTileButton, PinnedTileKind.Wide, _wideTileTargetIndex);
            UpdatePinButtonText(PinLargeTileButton, PinnedTileKind.Large, _largeTileTargetIndex);
        }

        private void UpdatePinButtonText(Button button, PinnedTileKind kind, int targetIndex)
        {
            bool exists = _secondaryTileService.Exists(kind, targetIndex);
            button.Content = exists ? "已固定" : "固定磁贴";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string launchInfo = e.Parameter as string ?? "无启动参数";
            TrayStatusTextBlock.Text = launchInfo;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTileSettings();
            ApplyTargetSelectionsToUi();
            UpdatePinButtonTexts();

            _lastStartupSyncStamp = UwpStartupTaskService.GetSyncStamp();
            _lastStateSyncStamp = UwpStateSnapshotService.GetSyncStamp();
            _lastMonitoringPaused = _monitoringStateService.IsPaused();

            _uiSyncTimer.Start();

            await RefreshStartupUiAsync();
            ApplyMonitoringState(_lastMonitoringPaused);
            await RefreshStateSnapshotUiAsync();
            ApplyTrayHostLaunchFailureMessage();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _uiSyncTimer.Stop();
        }

        private async void UiSyncTimer_Tick(object sender, object e)
        {
            long currentStartupStamp = UwpStartupTaskService.GetSyncStamp();
            if (currentStartupStamp != _lastStartupSyncStamp)
            {
                _lastStartupSyncStamp = currentStartupStamp;
                await RefreshStartupUiAsync();
            }

            bool currentMonitoringPaused = _monitoringStateService.IsPaused();
            if (currentMonitoringPaused != _lastMonitoringPaused)
            {
                _lastMonitoringPaused = currentMonitoringPaused;
                ApplyMonitoringState(currentMonitoringPaused);
            }

            long currentStateStamp = UwpStateSnapshotService.GetSyncStamp();
            if (currentStateStamp != _lastStateSyncStamp)
            {
                _lastStateSyncStamp = currentStateStamp;
                await RefreshStateSnapshotUiAsync();
                ApplyTrayHostLaunchFailureMessage();
            }

            UpdatePinButtonTexts();
        }

        private void InitializeFixedTargetOptions()
        {
            TargetOptions.Clear();

            for (int i = 0; i < FixedTargetSlotCount; i++)
            {
                TargetOptions.Add(new TileTargetOption
                {
                    SessionIndex = i,
                    DisplayText = GetTargetOptionText(i)
                });
            }
        }

        private string GetTargetOptionText(int index)
        {
            if (index == 0)
                return "主媒体";

            return "次媒体#" + index;
        }

        private void LoadTileSettings()
        {
            _smallTileTargetIndex = NormalizeTargetIndex(_tileSettingsService.GetSmallTileTargetIndex());
            _mediumTileTargetIndex = NormalizeTargetIndex(_tileSettingsService.GetMediumTileTargetIndex());
            _wideTileTargetIndex = NormalizeTargetIndex(_tileSettingsService.GetWideTileTargetIndex());
            _largeTileTargetIndex = NormalizeTargetIndex(_tileSettingsService.GetLargeTileTargetIndex());
        }

        private int NormalizeTargetIndex(int index)
        {
            if (index < 0)
                return 0;

            if (index >= FixedTargetSlotCount)
                return FixedTargetSlotCount - 1;

            return index;
        }

        private void ApplyTargetSelectionsToUi()
        {
            _isApplyingTargetSelections = true;

            try
            {
                SmallTargetComboBox.SelectedIndex = _smallTileTargetIndex;
                MediumTargetComboBox.SelectedIndex = _mediumTileTargetIndex;
                WideTargetComboBox.SelectedIndex = _wideTileTargetIndex;
                LargeTargetComboBox.SelectedIndex = _largeTileTargetIndex;
            }
            finally
            {
                _isApplyingTargetSelections = false;
            }
        }

        private async void SmallTargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingTargetSelections)
                return;

            _smallTileTargetIndex = SmallTargetComboBox.SelectedIndex >= 0 ? SmallTargetComboBox.SelectedIndex : 0;
            _tileSettingsService.SetSmallTileTargetIndex(_smallTileTargetIndex);

            UpdatePreviewsFromCurrentSelection();
            UpdatePinButtonTexts();
            await RequestTrayRefreshAsync();
        }

        private async void MediumTargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingTargetSelections)
                return;

            _mediumTileTargetIndex = MediumTargetComboBox.SelectedIndex >= 0 ? MediumTargetComboBox.SelectedIndex : 0;
            _tileSettingsService.SetMediumTileTargetIndex(_mediumTileTargetIndex);

            UpdatePreviewsFromCurrentSelection();
            UpdatePinButtonTexts();
            await RequestTrayRefreshAsync();
        }

        private async void WideTargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingTargetSelections)
                return;

            _wideTileTargetIndex = WideTargetComboBox.SelectedIndex >= 0 ? WideTargetComboBox.SelectedIndex : 0;
            _tileSettingsService.SetWideTileTargetIndex(_wideTileTargetIndex);

            UpdatePreviewsFromCurrentSelection();
            UpdatePinButtonTexts();
            await RequestTrayRefreshAsync();
        }

        private async void LargeTargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingTargetSelections)
                return;

            _largeTileTargetIndex = LargeTargetComboBox.SelectedIndex >= 0 ? LargeTargetComboBox.SelectedIndex : 0;
            _tileSettingsService.SetLargeTileTargetIndex(_largeTileTargetIndex);

            UpdatePreviewsFromCurrentSelection();
            UpdatePinButtonTexts();
            await RequestTrayRefreshAsync();
        }

        private async void RefreshNowButton_Click(object sender, RoutedEventArgs e)
        {
            await RequestTrayRefreshAsync();
            await ShowTemporaryActionStatusAsync("已请求托盘立即刷新");
        }

        private async void PinSmallTileButton_Click(object sender, RoutedEventArgs e)
        {
            await PinPreviewTileAsync(PinnedTileKind.Small, _smallTileTargetIndex);
        }

        private async void PinMediumTileButton_Click(object sender, RoutedEventArgs e)
        {
            await PinPreviewTileAsync(PinnedTileKind.Medium, _mediumTileTargetIndex);
        }

        private async void PinWideTileButton_Click(object sender, RoutedEventArgs e)
        {
            await PinPreviewTileAsync(PinnedTileKind.Wide, _wideTileTargetIndex);
        }

        private async void PinLargeTileButton_Click(object sender, RoutedEventArgs e)
        {
            await PinPreviewTileAsync(PinnedTileKind.Large, _largeTileTargetIndex);
        }

        private async void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    SharedConstants.Logging.LogFileName,
                    CreationCollisionOption.OpenIfExists);

                var props = await file.GetBasicPropertiesAsync();
                if (props.Size == 0)
                {
                    await FileIO.AppendLinesAsync(file, new[]
                    {
                        "Media Live Tile Hybrid Log",
                        "==========================",
                        ""
                    });
                }

                bool launched = await Launcher.LaunchFileAsync(file);

                if (launched)
                {
                    await ShowTemporaryActionStatusAsync("已打开日志文件");
                }
                else
                {
                    SetActionStatus("无法打开日志文件");
                }
            }
            catch (Exception ex)
            {
                SetActionStatus("打开日志失败：" + ex.Message);
            }
        }

        private async void AboutAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GitHubUrl))
            {
                await ShowTemporaryActionStatusAsync("GitHub 链接尚未设置");
                return;
            }

            try
            {
                bool launched = await Launcher.LaunchUriAsync(new Uri(GitHubUrl));
                if (!launched)
                {
                    SetActionStatus("无法打开 GitHub 链接");
                }
            }
            catch (Exception ex)
            {
                SetActionStatus("打开链接失败：" + ex.Message);
            }
        }

        private async void RefreshStartupButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStartupUiAsync();
        }

        private async void StartupToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingStartupToggle)
                return;

            try
            {
                StartupTaskState state;

                if (StartupToggleSwitch.IsOn)
                {
                    state = await _startupTaskService.EnableAsync();
                }
                else
                {
                    state = await _startupTaskService.DisableAsync();
                }

                _lastStartupSyncStamp = UwpStartupTaskService.BumpSyncStamp();
                ApplyStartupState(state);
            }
            catch (Exception ex)
            {
                StartupStateTextBlock.Text = "状态：操作失败 - " + ex.Message;
            }
        }

        private void MonitoringToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingMonitoringToggle)
                return;

            bool paused = !MonitoringToggleSwitch.IsOn;
            _monitoringStateService.SetPaused(paused);
            _lastMonitoringPaused = paused;
            ApplyMonitoringState(paused);
        }

        private async Task RefreshStartupUiAsync()
        {
            try
            {
                var state = await _startupTaskService.GetStateAsync();
                ApplyStartupState(state);
            }
            catch (Exception ex)
            {
                StartupStateTextBlock.Text = "状态：读取失败 - " + ex.Message;
                StartupHintTextBlock.Text = string.Empty;
            }
        }

        private async Task RefreshStateSnapshotUiAsync()
        {
            try
            {
                var snapshot = await _stateSnapshotService.ReadAsync();
                if (snapshot == null && _latestSnapshot != null)
                {
                    TrayStatusTextBlock.Text = "状态：快照读取失败，保留上次成功状态";
                    return;
                }

                ApplyStateSnapshot(snapshot);
            }
            catch (Exception ex)
            {
                if (_latestSnapshot != null)
                {
                    TrayStatusTextBlock.Text = "状态：读取失败，保留上次成功状态 - " + ex.Message;
                    return;
                }

                TrayStatusTextBlock.Text = "状态：读取失败 - " + ex.Message;
                TrayLastRefreshTextBlock.Text = "上次刷新：--";
                PrimaryMediaTextBlock.Text = "主媒体：无";
                SecondaryMediaTextBlock.Text = "次媒体：无";
                Sessions.Clear();

                _latestSnapshot = null;
                UpdatePreviewsFromCurrentSelection();
            }
        }

        private void ApplyTrayHostLaunchFailureMessage()
        {
            object? raw = ApplicationData.Current.LocalSettings.Values[
                SharedConstants.LocalSettingsKeys.TrayHostLaunchFailureMessage];

            if (raw is string message && !string.IsNullOrWhiteSpace(message))
            {
                TrayStatusTextBlock.Text = message;
            }
        }

        private void ApplyStartupState(StartupTaskState state)
        {
            _isUpdatingStartupToggle = true;

            try
            {
                StartupToggleSwitch.IsOn = UwpStartupTaskService.IsEnabled(state);

                StartupToggleSwitch.IsEnabled =
                    state != StartupTaskState.DisabledByPolicy &&
                    state != StartupTaskState.EnabledByPolicy;

                StartupStateTextBlock.Text = "状态：" + UwpStartupTaskService.GetStateText(state);
                StartupHintTextBlock.Text = UwpStartupTaskService.GetHintText(state);
            }
            finally
            {
                _isUpdatingStartupToggle = false;
            }
        }

        private void ApplyMonitoringState(bool isPaused)
        {
            _isUpdatingMonitoringToggle = true;

            try
            {
                MonitoringToggleSwitch.IsOn = !isPaused;
                MonitoringStateTextBlock.Text = isPaused ? "状态：已暂停" : "状态：已运行";
                MonitoringHintTextBlock.Text = isPaused
                    ? "已暂停自动媒体监听与自动磁贴刷新，但仍可通过“立即刷新”手动更新。"
                    : "正在监听系统媒体变化，并由托盘后台自动刷新主磁贴与次级磁贴。";
            }
            finally
            {
                _isUpdatingMonitoringToggle = false;
            }
        }

        private void ApplyStateSnapshot(SharedStateSnapshot? snapshot)
        {
            if (snapshot == null)
            {
                if (_latestSnapshot != null)
                {
                    TrayStatusTextBlock.Text = "状态：暂无新状态快照，保留上次成功状态";
                    return;
                }

                TrayStatusTextBlock.Text = "状态：暂无状态快照";
                TrayLastRefreshTextBlock.Text = "上次刷新：--";
                PrimaryMediaTextBlock.Text = "主媒体：无";
                SecondaryMediaTextBlock.Text = "次媒体：无";
                Sessions.Clear();
                UpdatePreviewsFromCurrentSelection();
                return;
            }

            _latestSnapshot = snapshot;
            TrayStatusTextBlock.Text = "状态：" + (snapshot.StatusText ?? string.Empty);

            if (snapshot.HasLastRefreshTime)
            {
                var time = DateTimeOffset.FromUnixTimeMilliseconds(snapshot.LastRefreshUnixTimeMilliseconds);
                TrayLastRefreshTextBlock.Text = "上次刷新：" + time.ToString("HH:mm:ss");
            }
            else
            {
                TrayLastRefreshTextBlock.Text = "上次刷新：--";
            }

            PrimaryMediaTextBlock.Text = BuildMediaSummary("主媒体", snapshot.PrimaryMedia);
            SecondaryMediaTextBlock.Text = BuildMediaSummary("次媒体", snapshot.SecondaryMedia);

            Sessions.Clear();

            if (snapshot.Sessions != null)
            {
                foreach (var item in snapshot.Sessions)
                {
                    Sessions.Add(item);
                }
            }

            UpdatePreviewsFromCurrentSelection();
        }

        private string BuildMediaSummary(string label, SharedStateSessionItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Title))
                return label + "：无";

            return label + "：" + item.SourceDisplayName + " - " + item.Title;
        }

        private SharedStateSessionItem? ResolveMediaByIndex(int index)
        {
            if (_latestSnapshot == null || _latestSnapshot.Sessions == null)
                return null;

            if (index < 0 || index >= _latestSnapshot.Sessions.Count)
                return null;

            return _latestSnapshot.Sessions[index];
        }

        private void UpdatePreviewsFromCurrentSelection()
        {
            UpdateSmallPreview(ResolveMediaByIndex(_smallTileTargetIndex));
            UpdateMediumPreview(ResolveMediaByIndex(_mediumTileTargetIndex));
            UpdateWidePreview(ResolveMediaByIndex(_wideTileTargetIndex));
            UpdateLargePreview(ResolveMediaByIndex(_largeTileTargetIndex));
        }

        private void UpdateSmallPreview(SharedStateSessionItem? media)
        {
            SetPreviewImage(SmallPreviewImage, media != null ? media.ImageUri : null);
        }

        private void UpdateMediumPreview(SharedStateSessionItem? media)
        {
            SetPreviewImage(MediumCoverPreviewImage, media != null ? media.ImageUri : null);
            ApplyInfoPreview(media, MediumInfoTitleText, MediumInfoArtistText, MediumInfoSourceText);
        }

        private void UpdateWidePreview(SharedStateSessionItem? media)
        {
            SetPreviewImage(WidePreviewImage, media != null ? media.ImageUri : null);
            ApplyInfoPreview(media, WideTitleText, WideArtistText, WideSourceText);
        }

        private void UpdateLargePreview(SharedStateSessionItem? media)
        {
            SetPreviewImage(LargeCoverPreviewImage, media != null ? media.ImageUri : null);
            ApplyInfoPreview(media, LargeInfoTitleText, LargeInfoArtistText, LargeInfoSourceText);
        }

        private void SetPreviewImage(Image image, string? imageUri)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(imageUri))
                {
                    image.Source = new BitmapImage(new Uri(imageUri));
                }
                else
                {
                    image.Source = _defaultPreviewImage;
                }
            }
            catch
            {
                image.Source = _defaultPreviewImage;
            }
        }

        private void ApplyInfoPreview(
            SharedStateSessionItem? media,
            TextBlock titleText,
            TextBlock artistText,
            TextBlock sourceText)
        {
            if (media == null)
            {
                titleText.Text = "当前无媒体";
                artistText.Text = string.Empty;
                sourceText.Text = string.Empty;
                return;
            }

            titleText.Text = GetDisplayTitle(media);
            artistText.Text = GetArtistText(media);
            sourceText.Text = GetSourceText(media);
        }

        private string GetDisplayTitle(SharedStateSessionItem? media)
        {
            if (media != null && !string.IsNullOrWhiteSpace(media.Title))
                return media.Title;

            return "当前无媒体";
        }

        private string GetArtistText(SharedStateSessionItem? media)
        {
            if (media == null || string.IsNullOrWhiteSpace(media.Artist) || media.Artist == "无艺人")
                return string.Empty;

            return media.Artist;
        }

        private string GetSourceText(SharedStateSessionItem? media)
        {
            return media == null ? string.Empty : (media.SourceDisplayName ?? string.Empty);
        }

        private async Task PinPreviewTileAsync(PinnedTileKind kind, int targetIndex)
        {
            try
            {
                if (_secondaryTileService.Exists(kind, targetIndex))
                {
                    UpdatePinButtonTexts();
                    await ShowTemporaryActionStatusAsync("该磁贴已固定");
                    return;
                }

                string targetText = GetTargetOptionText(targetIndex);
                bool created = await _secondaryTileService.RequestCreateAsync(kind, targetIndex, targetText);

                if (!created)
                {
                    SetActionStatus("已取消固定磁贴");
                    return;
                }

                UpdatePinButtonTexts();
                await RequestTrayRefreshAsync();
                await ShowTemporaryActionStatusAsync("已固定磁贴（默认显示为中磁贴，可在开始菜单手动调整大小）");
            }
            catch (Exception ex)
            {
                SetActionStatus("固定磁贴失败：" + ex.Message);
            }
        }

        private async Task RequestTrayRefreshAsync()
        {
            _refreshRequestService.RequestRefresh();
            await Task.CompletedTask;
        }

        private void SetActionStatus(string text)
        {
            _actionStatusVersion++;
            ActionStatusTextBlock.Text = text;
        }

        private async Task ShowTemporaryActionStatusAsync(string text, int milliseconds = 1800)
        {
            _actionStatusVersion++;
            int currentVersion = _actionStatusVersion;

            ActionStatusTextBlock.Text = text;

            await Task.Delay(milliseconds);

            if (currentVersion == _actionStatusVersion)
            {
                ActionStatusTextBlock.Text = string.Empty;
            }
        }
    }
}