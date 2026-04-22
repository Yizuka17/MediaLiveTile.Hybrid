using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), "普通启动");
            }

            Window.Current.Activate();

            await EnsureTrayHostLaunchedAsync();
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            var rootFrame = EnsureRootFrame();

            if (args is ProtocolActivatedEventArgs protocolArgs)
            {
                rootFrame.Navigate(typeof(MainPage), $"协议启动：{protocolArgs.Uri}");
            }
            else
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), $"激活类型：{args.Kind}");
                }
            }

            Window.Current.Activate();

            await EnsureTrayHostLaunchedAsync();
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

        private async Task EnsureTrayHostLaunchedAsync()
        {
            // 避免一次启动流程里重复请求
            if (_trayLaunchRequested)
                return;

            _trayLaunchRequested = true;

            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            catch
            {
                // 常见情况：
                // 1. 当前不是通过 Package 启动
                // 2. TrayHost 已经启动
                // 3. manifest / fullTrust 配置未正确生效
                // 当前先静默，后面需要时可接日志
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