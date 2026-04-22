using System.Threading;
using System.Windows.Forms;

namespace MediaLiveTile.Hybrid.TrayHost
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            using var singleInstanceMutex = new Mutex(
                true,
                @"Local\MediaLiveTile.Hybrid.TrayHost",
                out bool createdNew);

            if (!createdNew)
            {
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext());
        }
    }
}