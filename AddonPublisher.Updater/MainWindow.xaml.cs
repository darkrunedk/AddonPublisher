using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace AddonPublisher.Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _assetUrl;
        private readonly string _targetPath;

        public MainWindow(string assetUrl, string targetPath)
        {
            InitializeComponent();
            _assetUrl = assetUrl;
            _targetPath = targetPath;

            Loaded += async (_, __) => await StartUpdateAsync();
        }

        private async Task StartUpdateAsync()
        {
            StatusText.Text = "Downloading update...";
            ProgressBar.IsIndeterminate = true;

            // Run your update logic here
            try
            {
                await RunUpdate(_assetUrl, _targetPath);
                StatusText.Text = "Update complete. Restarting...";
                ProgressBar.IsIndeterminate = false;
                await Task.Delay(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Update failed: {ex.Message}";
                ProgressBar.IsIndeterminate = false;
            }

            await Task.Delay(1000);
            Environment.Exit(0);
        }

        private async Task RunUpdate(string assetUrl, string targetPath)
        {
            string tempZip = Path.Combine(Path.GetTempPath(), "AddonPublisher_Update.zip");
            string extractPath = Path.Combine(Path.GetTempPath(), "AddonPublisher_Extracted");

            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(assetUrl);
            await File.WriteAllBytesAsync(tempZip, data);

            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            ZipFile.ExtractToDirectory(tempZip, extractPath);

            foreach (var sourceFile in Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(extractPath, sourceFile);
                string destinationFile = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Copy(sourceFile, destinationFile, overwrite: true);
            }

            string exePath = Path.Combine(targetPath, "AddonPublisher.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Update cancelled.";
            Environment.Exit(0);
        }

    }
}