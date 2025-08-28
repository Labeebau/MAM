using MAM.Data;
using MAM.Utilities;
using MAM.Views.AdminPanelViews;
using MAM.Views.MediaBinViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Data;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SendToArchiveWindow : Window
    {
        private static SendToArchiveWindow _instance;

        private DataAccess dataAccess { get; } = new DataAccess();
        // public ObservableCollection< MediaPlayerItem> ArchiveList { get; set; }
        public ArchiveServer ArchiveServer { get; set; }
        public SendToArchiveViewModel ViewModel { get; }

        public SendToArchiveWindow(ObservableCollection<MediaPlayerItem> medias)
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
            GlobalClass.Instance.SetWindowSizeAndPosition(600, 600, this);
            this.Activated += SendToArchiveWindow_Activated;
            // Convert the incoming MediaPlayerItem list to MediaPlayerArchiveViewModel list
            var archiveViewModels = new ObservableCollection<MediaPlayerArchiveViewModel>(
                medias.Select(item => new MediaPlayerArchiveViewModel(item))
            );

            ViewModel = new SendToArchiveViewModel(archiveViewModels);

            MainGrid.DataContext = ViewModel;
        }

        private void SendToArchiveWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated) return;
            //ArchiveServer =await GlobalClass.Instance.GetArchiveServer(this.Content.XamlRoot);
            //GlobalClass.Instance.ArchivePath=Path.Combine(ArchiveServer.ServerName, ArchiveServer.ArchivePath);
            if (!File.Exists(GlobalClass.Instance.ArchivePath))
                ArchiveDropDown.Content = GlobalClass.Instance.ArchivePath;
        }

        public static void ShowWindow(ObservableCollection<MediaPlayerItem> medias, Action onAarchived)
        {

            _instance = new SendToArchiveWindow(medias);
            _instance.Activate(); // Show the window
            _instance.ViewModel.ArchiveUpdatedCallback = onAarchived;

        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _instance = null;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var checkedItems = ViewModel.ArchiveList.Where(item => item.IsChecked).ToList();
                foreach (var media in checkedItems)
                {
                    App.MainAppWindow.StatusBar.ShowStatus($"Archiving {media}...");
                    if (!media.Model.IsArchived)
                    {
                        if (File.Exists(media.Model.MediaSource.LocalPath))
                    {
                        
                            string title = media.Model.Title;
                            string now = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                            string archiveDirectory = Path.Combine(GlobalClass.Instance.ArchivePath,now );
                            Directory.CreateDirectory(archiveDirectory);
                            string destinationPath = Path.Combine(archiveDirectory, title);
                            string destinationProxyPath = Path.Combine(archiveDirectory, Path.GetFileName(media.Model.ProxyPath));

                            File.Move(media.Model.MediaSource.LocalPath, destinationPath, false);
                            File.Move(media.Model.ProxyPath, destinationProxyPath, false);

                            Dictionary<string, object> propsList = new Dictionary<string, object>
                            {
                                {"is_archived", true },
                                {"archive_path",now }
                            };
                            await dataAccess.UpdateRecord("Asset", "asset_id", media.Model.MediaId, propsList);
                            // ViewModel.ArchiveUpdatedCallback?.Invoke(); // Call back to MediaLibraryPage
                            media.Model.ProxyPath = destinationProxyPath;
                            media.Model.IsArchived = true;
                            media.Model.ArchivePath = destinationPath;
                            await GlobalClass.Instance.AddtoHistoryAsync("Send to archive", $"Send asset '{media.Model.MediaPath}' to archive .");
                        }
                        else
                            await GlobalClass.Instance.ShowDialogAsync($"'{media.Model.MediaSource.LocalPath}' does not exist.", this.Content.XamlRoot);
                    }
                    else
                    {
                        await GlobalClass.Instance.ShowDialogAsync($"'{media.Model.MediaSource.LocalPath}' is already archived.", this.Content.XamlRoot);
                    }
                }
                    App.MainAppWindow.StatusBar.HideStatus();
                    this.Close();
                }
            catch (IOException ioEx)
            {
                await GlobalClass.Instance.ShowDialogAsync($"File operation failed: {ioEx.Message}", this.Content.XamlRoot);
            }
            catch (UnauthorizedAccessException authEx)
            {
                await GlobalClass.Instance.ShowDialogAsync($"Permission error: {authEx.Message}", this.Content.XamlRoot);
            }
            catch (Exception ex)
            {
                await GlobalClass.Instance.ShowDialogAsync($"An error occurred: {ex.Message}", this.Content.XamlRoot);
            }

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ChbActive_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                ArchiveDropDown.Content = menuItem.Text;
            }
        }
    }
    public class MediaPlayerArchiveViewModel : ObservableObject
    {
        private MediaPlayerItem model;
        private bool isChecked = true;
        public MediaPlayerItem Model
        {
            get => model;
            set => SetProperty(ref model, value);
        }
        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }
        public MediaPlayerArchiveViewModel(MediaPlayerItem model)
        {
            Model = model;
        }

    }

    public class SendToArchiveViewModel : ObservableObject
    {
        private bool isChecked;
        private ObservableCollection<MediaPlayerArchiveViewModel> archiveList = new();

        public ObservableCollection<MediaPlayerArchiveViewModel> ArchiveList
        {
            get => archiveList;
            set => SetProperty(ref archiveList, value);
        }
        public SendToArchiveViewModel(ObservableCollection<MediaPlayerArchiveViewModel> mediaPlayerItems)
        {
            foreach (var item in mediaPlayerItems)
            {
                ArchiveList.Add(item);
            }

        }
        public Action ArchiveUpdatedCallback { get; set; }

    }
}
