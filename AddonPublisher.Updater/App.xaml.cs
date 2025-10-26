using System.Windows;

namespace AddonPublisher.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length < 2)
            {
                MessageBox.Show("Missing parameters.");
                Shutdown();
                return;
            }

            string assetUrl = e.Args[0];
            string targetPath = e.Args[1];
            var mainWindow = new MainWindow(assetUrl, targetPath);
            mainWindow.Show();
        }
    }
}
