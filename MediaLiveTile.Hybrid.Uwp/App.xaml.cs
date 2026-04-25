using MediaLiveTile.Hybrid.Shared;
using MediaLiveTile.Hybrid.Uwp.Services;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MediaLiveTile.Hybrid.Uwp
{
    sealed partial class App : Application
    {
        private bool _trayLaunchRequested;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = EnsureRootFrame();

            bool trayHostLaunched = await EnsureTrayHostLaunchedAsync();
            string launchInfo = trayHostLaunched
                ? "普通启动"
                : "托盘宿主未启动，部分功能不可用";

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), launchInfo);
            }

            Window.Current.Activate();
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            var rootFrame = EnsureRootFrame();

            bool trayHostLaunched = await EnsureTrayHostLaunchedAsync();
            string trayStatus = trayHostLaunched
                ? string.Empty
                : "；托盘宿主未启动，部分功能不可用";

            if (args is ProtocolActivatedEventArgs protocolArgs)
            {
                rootFrame.Navigate(typeof(MainPage), $"协议启动：{protocolArgs.Uri}{trayStatus}");
            }
            else
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), $"激活类型：{args.Kind}{trayStatus}");
                }
            }

            Window.Current.Activate();
        }

        private Frame EnsureRootFrame()
        {
            if (Window.Current.Content is Frame existingFrame)
            {
                return existingFrame;
            }

            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            Window.Current.Content = rootFrame;

            return rootFrame;
        }

        private async Task<bool> EnsureTrayHostLaunchedAsync()
        {
            // 避免一次启动流程里重复请求
            if (_trayLaunchRequested)
                return !ApplicationData.Current.LocalSettings.Values.ContainsKey(
                    SharedConstants.LocalSettingsKeys.TrayHostLaunchFailureMessage);

            _trayLaunchRequested = true;

            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                ApplicationData.Current.LocalSettings.Values.Remove(
                    SharedConstants.LocalSettingsKeys.TrayHostLaunchFailureMessage);
                return true;
            }
            catch (Exception ex)
            {
                string message = "托盘宿主未启动，部分功能不可用";
                ApplicationData.Current.LocalSettings.Values[
                    SharedConstants.LocalSettingsKeys.TrayHostLaunchFailureMessage] = message + "：" + ex.Message;
                UwpStateSnapshotService.BumpSyncStamp();
                return false;
            }
        }

        private void OnNavigationFailed(object sender, Windows.UI.Xaml.Navigation.NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}