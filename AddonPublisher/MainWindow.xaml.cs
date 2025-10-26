using AddonPublisher.Models.Enums;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace AddonPublisher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _selectedFolder = "";
        private string _lastZipPath = "";
        private readonly double _windowBaseHeight;

        public MainWindow()
        {
            InitializeComponent();
            _windowBaseHeight = this.Height;
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _selectedFolder = dialog.SelectedPath;
                SelectedFolderText.Text = _selectedFolder;
                PopulateTocFileList();
                SuggestZipName();
            }
        }

        private void TocFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SuggestZipName();
        }

        private void BumpVersionInToc(string tocFile)
        {
            var lines = File.ReadAllLines(tocFile);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("## Version:", StringComparison.OrdinalIgnoreCase))
                {
                    string version = lines[i].Substring("## Version:".Length).Trim();
                    string[] parts = version.Split('.');
                    if (parts.Length > 0 && int.TryParse(parts[^1], out int patch))
                    {
                        parts[^1] = (patch + 1).ToString();
                        string newVersion = string.Join(".", parts);
                        lines[i] = $"## Version: {newVersion}";
                    }
                    break;
                }
            }
            File.WriteAllLines(tocFile, lines);
        }

        private void BumpVersionCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            BumpAllCheckbox.IsEnabled = true;
            SuggestZipName();
        }

        private void BumpVersionCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            BumpAllCheckbox.IsChecked = false;
            BumpAllCheckbox.IsEnabled = false;
            SuggestZipName();
        }

        private void CreateZip_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder))
            {
                ShowToast("No folder selected.", ToastType.Error);
                return;
            }

            if (BumpVersionCheckbox.IsChecked == true)
            {
                if (BumpAllCheckbox.IsChecked == true)
                {
                    foreach (string tocFile in Directory.GetFiles(_selectedFolder, "*.toc"))
                        BumpVersionInToc(tocFile);
                }
                else if (TocFileList.SelectedItem is string selectedToc)
                {
                    BumpVersionInToc(selectedToc);
                }
            }

            var addonInfo = GetAddonInfoFromToc();
            if (addonInfo == null)
            {
                ShowToast("No .toc file selected or found.", ToastType.Error);
                return;
            }

            string zipName = ZipNameBox.Text.Trim();
            if (string.IsNullOrEmpty(zipName)) zipName = "output.zip";
            if (!zipName.EndsWith(".zip")) zipName += ".zip";

            string zipPath = Path.Combine(Path.GetDirectoryName(_selectedFolder)!, zipName);

            // Exclusion rules
            string[] excludedFilenames = { ".gitattributes" };
            string[] excludedFolders = { ".git" };

            if (File.Exists(zipPath)) File.Delete(zipPath);

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

            foreach (string file in Directory.GetFiles(_selectedFolder, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(_selectedFolder, file);
                string fileName = Path.GetFileName(file);
                string folderName = Path.GetDirectoryName(relativePath)?.Split(Path.DirectorySeparatorChar).FirstOrDefault();

                if (excludedFilenames.Contains(fileName) || excludedFolders.Contains(folderName))
                    continue;

                archive.CreateEntryFromFile(file, relativePath);
            }

            ShowToast("Zip file created successfully!", ToastType.Success);

            _lastZipPath = zipPath;
            OpenFolderButton.IsEnabled = true;
        }

        private void PopulateTocFileList()
        {
            TocFileList.Items.Clear();
            var tocFiles = Directory.GetFiles(_selectedFolder, "*.toc");
            if (tocFiles.Length == 0) return;

            if (tocFiles.Length == 1)
            {
                TocFileList.Items.Add(tocFiles[0]);
                TocFileList.SelectedIndex = 0;
                TocFileList.Visibility = Visibility.Collapsed;
            }
            else
            {
                foreach (var file in tocFiles)
                    TocFileList.Items.Add(file);

                TocFileList.SelectedIndex = 0;
                TocFileList.Visibility = Visibility.Visible;
            }

            AdjustWindowHeight();
        }

        private void AdjustWindowHeight()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                double maxExtraHeight = 400;
                double listHeight = Math.Min(TocFileList.ActualHeight, maxExtraHeight);
                this.Height = _windowBaseHeight + listHeight;
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            string message = "Addon Packager v1.0\nCreated by DarkruneDK\n\nThis tool helps you zip World of Warcraft addons, bump version numbers, and exclude git-related files.";
            System.Windows.MessageBox.Show(message, "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SuggestZipName()
        {
            var info = GetAddonInfoFromToc();
            if (info != null)
            {
                string suggestedName = $"{info.Value.addonName}-{info.Value.version}.zip";
                ZipNameBox.Text = suggestedName;

                ZipNameHint.Text = BumpVersionCheckbox.IsChecked == true
                ? "Suggested filename is based on the selected .toc file and the bumped version number."
                : "Suggested filename is based on the selected .toc file and its current version number.";
            }
        }

        private (string addonName, string version)? GetAddonInfoFromToc()
        {
            if (TocFileList.SelectedItem is not string tocFile) return null;

            string addonName = Path.GetFileNameWithoutExtension(tocFile);
            string version = "unknown";

            foreach (var line in File.ReadLines(tocFile))
            {
                if (line.StartsWith("## Version:", StringComparison.OrdinalIgnoreCase))
                {
                    version = line.Substring("## Version:".Length).Trim();

                    // If bumping is enabled, simulate the bump
                    if (BumpVersionCheckbox.IsChecked == true)
                    {
                        string[] parts = version.Split('.');
                        if (parts.Length > 0 && int.TryParse(parts[^1], out int patch))
                        {
                            parts[^1] = (patch + 1).ToString();
                            version = string.Join(".", parts);
                        }
                    }
                    break;
                }
            }

            return (addonName, version);
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastZipPath) && File.Exists(_lastZipPath))
            {
                string folder = Path.GetDirectoryName(_lastZipPath)!;
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }

        private async void ShowToast(string message, ToastType type = ToastType.Success, int durationMs = 2000)
        {
            ToastText.Text = message;

            switch (type)
            {
                case ToastType.Error:
                    ToastPanel.Background = new SolidColorBrush(Color.FromRgb(200, 50, 50)); // red
                    ToastText.Foreground = Brushes.White;
                    ToastText.Text = "⚠ " + message;
                    break;

                case ToastType.Warning:
                    ToastPanel.Background = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // orange
                    ToastText.Foreground = Brushes.Black;
                    ToastText.Text = "⚠ " + message;
                    break;

                case ToastType.Info:
                    ToastPanel.Background = new SolidColorBrush(Color.FromRgb(70, 130, 180)); // steel blue
                    ToastText.Foreground = Brushes.White;
                    ToastText.Text = "ℹ " + message;
                    break;

                case ToastType.Success:
                default:
                    ToastPanel.Background = new SolidColorBrush(Color.FromRgb(50, 150, 50)); // green
                    ToastText.Foreground = Brushes.White;
                    ToastText.Text = "✔ " + message;
                    break;
            }

            ToastPanel.Visibility = Visibility.Visible;

            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            ToastPanel.BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(durationMs);

            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => ToastPanel.Visibility = Visibility.Collapsed;
            ToastPanel.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}