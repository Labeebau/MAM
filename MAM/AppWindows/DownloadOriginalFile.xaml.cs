using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews;
using MAM.Views.ProcessesViews;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PdfSharp.Snippets.Drawing;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadOriginalFile : Window
    {
        private static DownloadOriginalFile _instance;
        public DownloadOriginal ViewModel { get; set; }
        private DataAccess dataAccess { get; } = new DataAccess();
        public MediaPlayerItem Asset { get; set; }

        public DownloadOriginalFile(MediaPlayerItem media)
        {
            this.InitializeComponent();
            var titleBar = AppWindow.TitleBar;
            // Set the background colors for active and inactive states
            titleBar.BackgroundColor = Colors.Black;
            titleBar.InactiveBackgroundColor = Colors.DarkGray;
            // Set the foreground colors (text/icons) for active and inactive states
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveForegroundColor = Colors.Gray;
            GlobalClass.Instance.DisableMaximizeButton(this);
            GlobalClass.Instance.SetWindowSizeAndPosition(600, 400, this);
            Asset = media;
            ViewModel = new DownloadOriginal(media);
            ViewModel.DownloadItems.Add(ViewModel);
            MainGrid.DataContext = ViewModel;
            UpdateRecentFilesMenu();
            DownloadOriginalDropDown.Content = ViewModel.DownloadPath;
        }

        public static void ShowWindow(MediaPlayerItem media)
        {
            if (_instance == null)
            {
                _instance = new DownloadOriginalFile(media);
                // When the window closes, clear the instance so it can be opened again
                _instance.Closed += (s, e) => _instance = null;
            }
            _instance.Activate();
        }
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _instance = null;
        }
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadButton.IsEnabled = false;
            if (Asset.IsArchived)
                await CopyFileWithProgressAsync(Asset.ArchivePath);
            else
                await CopyFileWithProgressAsync(Asset.MediaSource.LocalPath);

            DownloadButton.IsEnabled = true;
            this.Close();

        }

        private async Task CopyFileWithProgressAsync(string sourcePath)
        {
            var DownloadOriginal = await ProcessManager.CreateProcessAsync(Asset.MediaId, sourcePath, ProcessType.DownloadOriginalFile, "Downloading File");

            try
            {
               // string sourcePath = Asset.MediaSource.LocalPath;
                string destinationPath = Path.Combine(ViewModel.DownloadPath, Path.GetFileName(Asset.Title));
                if (File.Exists(destinationPath))
                {
                    ContentDialogResult result = await GlobalClass.Instance.ShowDialogAsync("The file already exists. Do you want to overwrite it?",this.Content.XamlRoot, "Yes", "No", "File Exists");
                    if (result != ContentDialogResult.Primary) return;
                }

                App.UIDispatcherQueue.TryEnqueue(async () =>
                {
                    Debug.WriteLine(DownloadOriginal.Status);
                    TransactionHistoryPage.TransactionHistoryStatic.InitializeWithParameter("DownloadHistory");
                });
                await CopyFileAsync(sourcePath, destinationPath, DownloadOriginal);
            }
            catch (Exception ex)
            {
                await ProcessManager.FailProcessAsync(DownloadOriginal, "Downloading Failed");
            }
        }

        private async Task CopyFileAsync(string sourcePath, string destinationPath, Process downloadOriginal)
        {
            if (downloadOriginal != null)
            {
                FileInfo fileInfo = new(sourcePath);
                long totalBytes = fileInfo.Length;
                long copiedBytes = 0;
                byte[] buffer = new byte[81920];

                try
                {
                    using FileStream sourceStream = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using FileStream destinationStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

                    int bytesRead;
                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await destinationStream.WriteAsync(buffer, 0, bytesRead);
                        copiedBytes += bytesRead;

                        App.UIDispatcherQueue.TryEnqueue(() =>
                        {
                            downloadOriginal.Progress = (int)(copiedBytes / (double)totalBytes * 100);
                            downloadOriginal.Status = "Downloading...";

                        });
                    }
                    App.UIDispatcherQueue.TryEnqueue(async () =>
                    {
                        var process = ProcessManager.AllProcesses.FirstOrDefault(p => p.ProcessId == downloadOriginal.ProcessId);
                        if (process != null)
                        {
                            Debug.WriteLine(process.Result);
                            ProcessStatusPage.Instance?.FilterData();
                            await ProcessManager.CompleteProcessAsync(process,"Downloading finished");
                        }
                    });
                }
                catch (Exception ex)
                {
                    await ProcessManager.FailProcessAsync(downloadOriginal, "Downloading Failed");
                }
            }
        }
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderExplorer();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ChbActive_Checked(object sender, RoutedEventArgs e)
        {

        }
        private async void OpenFolderExplorer()
        {
            var picker = new FolderPicker();

            // Initialize the picker with the current window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            // Set file types and options
            foreach (string ext in GlobalClass.Instance.SupportedFiles)
            {
                picker.FileTypeFilter.Add(ext); // Allow all files
            }
            // Allow selecting multiple files
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Show picker and get selected files
            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                ViewModel.DownloadPath = folder.Path;
                ViewModel.SelectedFilePath = folder.Path;
                DownloadOriginalDropDown.Content = ViewModel.SelectedFilePath;
                if(!GlobalClass.Instance.RecentFiles.Contains(folder.Path))
                    GlobalClass.Instance.RecentFiles.Add(folder.Path);
                UpdateRecentFilesMenu();
            }
        }
        private void UpdateRecentFilesMenu()
        {
            RecentFilesFlyout.Items.Clear();

            if (GlobalClass.Instance.RecentFiles.Count == 0)
            {
                var noFilesItem = new MenuFlyoutItem { Text = "No recent files", IsEnabled = false };
                RecentFilesFlyout.Items.Add(noFilesItem);
            }
            else
            {
                foreach (var filePath in GlobalClass.Instance.RecentFiles)
                {
                    var menuItem = new MenuFlyoutItem { Text = filePath };
                    menuItem.Click += RecentFile_Click;
                    RecentFilesFlyout.Items.Add(menuItem);
                }
            }
        }
        private void RecentFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                ViewModel.SelectedFilePath = item.Text;
            }
        }
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                ViewModel.DownloadPath = menuItem.Text;
            }
        }
    }
    public class DownloadOriginal : ObservableObject
    {
        private MediaPlayerItem media;
        private string selectedFilePath;
        private string downloadPath = GlobalClass.Instance.DownloadFolder;

        public MediaPlayerItem Media
        {
            get => media;
            set => SetProperty(ref media, value);
        }
        public string SelectedFilePath
        {
            get => selectedFilePath;
            set => SetProperty(ref selectedFilePath, value);
        }
        public string DownloadPath
        {
            get => downloadPath;
            set => SetProperty(ref downloadPath, value);
        }
        public DownloadOriginal(MediaPlayerItem mediaPlayerItem) 
        {
            Media = mediaPlayerItem;
        }
        public ObservableCollection<DownloadOriginal> DownloadItems { get; set; } = new();

    }
}
