using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Windows.Graphics;
using MAM.Utilities;
using MAM.Data;
using MAM.Views.MediaBinViews;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.ComponentModel;
using MAM.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class DownloadProxy :Window 
    {
        private static DownloadProxy _instance;
        public DownloadHistory ViewModel { get; set; }
        private DataAccess dataAccess { get; } = new DataAccess();
        public MediaPlayerItem Asset { get; set; }
        public ObservableCollection<DownloadHistory> AssetList { get; set; } = new ObservableCollection<DownloadHistory>();

        public DownloadProxy(MediaPlayerItem media)
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
            AssetList.Add(new DownloadHistory { Media = media });
            // downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
            var newDownload = new DownloadHistory
            { 
                Media=media,
                Extension = Path.GetExtension(media.MediaSource.LocalPath),
                Status = "Pending",
                Progress = 0,
                StartTime = DateTime.Now,
            };
            ViewModel = newDownload;
            MainGrid.DataContext = this;
           // DownloadProxyDropDown.Content = ViewModel.DownloadPath;
            UpdateRecentFilesMenu();
        }
      
        public static void ShowWindow(MediaPlayerItem media)
        {
            if (_instance == null)
            {
                _instance = new DownloadProxy(media);
                _instance.Activate(); // Show the window
            }
            else 
            {
                try
                {
                    _instance.Activate(); // Bring the existing window to the front
                }
                catch(Exception ex)
                {
                    _instance=new DownloadProxy(media);
                    _instance.Activate();
                }
            }
        }
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _instance = null;
        }
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadButton.IsEnabled = false;
            await CopyFileWithProgressAsync();

            if (DownloadHistoryPage.DownloadHistoryStatic != null)
            {
                foreach (var ast in AssetList)
                {
                    var newDownload = new DownloadHistory
                    {   Media=ast.Media,
                        DownloadPath = ViewModel.DownloadPath,
                        Extension = Path.GetExtension(ast.Media.MediaSource.LocalPath),
                        Status = ViewModel.Status,
                        Progress = ViewModel.Progress,
                        StartTime = ViewModel.StartTime,
                        CompletionTime = ViewModel.CompletionTime
                    };
                    DownloadHistoryPage.DownloadHistoryStatic.DownloadHistories.Add(newDownload);
                }
            }
            DownloadButton.IsEnabled = true;
            this.Close();

        }

        private async Task CopyFileWithProgressAsync()
        {
            try
            {
                ////ViewModel = new DownloadProxyViewModel();
                string sourcePath = Asset.ProxyPath.LocalPath;
                string destinationPath = Path.Combine(ViewModel.DownloadPath, Path.GetFileName(Asset.Title));
                DateTime startTime = DateTime.Now;
                App.UIDispatcherQueue.TryEnqueue(() =>
                {
                    ViewModel.StartTime = startTime;
                    ViewModel.Status = "Copying started...";
                    ViewModel.Progress = 0;
                    });

                if (File.Exists(destinationPath))
                {
                    var dialog = new ContentDialog
                    {
                        Title = "File Exists",
                        Content = "The file already exists. Do you want to overwrite it?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.Content.XamlRoot
                    };

                    if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
                }

                await CopyFileAsync(sourcePath, destinationPath);

                ViewModel.CompletionTime = DateTime.Now;
                ViewModel.Status = $"Copy completed in {(ViewModel.CompletionTime - startTime).TotalSeconds:F2} seconds";
            }
            catch (Exception ex)
            {
                ViewModel.Status = "Error: " + ex.Message;
            }
        }

        private async Task CopyFileAsync(string sourcePath, string destinationPath)
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
                        ViewModel.Progress = (int)(copiedBytes / (double)totalBytes * 100);
                    });
                }
            }
            catch (Exception ex)
            {
                ViewModel.Status = "Error: " + ex.Message;
            }
        }



        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderExplorer();
            
                
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedFilePath = string.Empty;
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

            if (folder != null )
            {
               ////ViewModel.DownloadPath=folder.Path;
               //// DownloadProxyDropDown.Content = ViewModel.DownloadPath;

                ViewModel.SelectedFilePath = folder.Path;
                ViewModel.AddRecentFile(folder.Path);
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
                ViewModel.DownloadPath= menuItem.Text;
            }
        }
    }
    
}
