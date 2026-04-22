using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MediaLiveTile.Hybrid.Uwp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string launchInfo = e.Parameter as string ?? "无启动参数";
            StatusTextBlock.Text = launchInfo;
        }

        private async void TestProtocolButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool launched = await Launcher.LaunchUriAsync(new Uri("medialivetilehybrid:"));
                StatusTextBlock.Text = launched
                    ? "已发起协议唤起请求"
                    : "协议唤起失败";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"协议测试失败：{ex.Message}";
            }
        }
    }
}