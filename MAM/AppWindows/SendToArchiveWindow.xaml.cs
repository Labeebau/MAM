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
            ViewModel.ArchiveServerList = GlobalClass.Instance.ArchiveServerList;
            ViewModel.SelectedArchiveServer = GlobalClass.Instance.ActiveArchiveServer;
            MainGrid.DataContext = ViewModel;

        }

        private void SendToArchiveWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated) return;

            //if (!File.Exists(GlobalClass.Instance.ArchivePath))
            //    ArchiveDropDown.Content = GlobalClass.Instance.ArchivePath;
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

            var checkedItems = ViewModel.MediaList.Where(item => item.IsChecked).ToList();
            foreach (var media in checkedItems)
            {
                App.MainAppWindow.StatusBar.ShowStatus($"Archiving {media}...");
                if (!media.Model.IsArchived)
                {
                    //string normalizedPath =PathHelper.NormalizePath(media.Model.MediaSource.LocalPath);
                    //string normalizedProxyPath = PathHelper.NormalizePath(media.Model.ProxyPath);
                    string normalizedArchivePath = PathHelper.NormalizePath(media.Model.MediaSource.LocalPath);
                    string title = media.Model.Title;
                    string now = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                    string archiveDirectory = Path.Combine(GlobalClass.Instance.ActiveArchiveServer.ServerName, GlobalClass.Instance.ActiveArchiveServer.ArchivePath, now);
                    archiveDirectory = PathHelper.NormalizePath(archiveDirectory);
                    Directory.CreateDirectory(archiveDirectory);
                    //string destinationPath = Path.Combine(archiveDirectory, title);
                    //string destinationProxyPath = Path.Combine(archiveDirectory, Path.GetFileName(media.Model.ProxyPath));

                    string sourceMain = PathHelper.NormalizePath(media.Model.MediaSource.LocalPath);
                    string sourceProxy = PathHelper.NormalizePath(media.Model.ProxyPath);
                    string destMain = Path.Combine(archiveDirectory, title); ;
                    string destProxy = Path.Combine(archiveDirectory, Path.GetFileName(media.Model.ProxyPath));

                    string movedMain = null;
                    string movedProxy = null;
                    try
                    {
                        if (File.Exists(sourceMain) && File.Exists(sourceProxy) )
                        {
                            File.Move(sourceMain, destMain, false);
                            movedMain = destMain;
                            File.Move(sourceProxy, destProxy, false);
                            movedProxy = destProxy;
                            Dictionary<string, object> propsList = new Dictionary<string, object>
                            {
                                {"archive_server_id",GlobalClass.Instance.ActiveArchiveServer.ServerId },
                                {"is_archived", true },
                                {"archive_path",now }
                            };
                            await dataAccess.UpdateRecord("Asset", "asset_id", media.Model.MediaId, propsList);
                            media.Model.ProxyPath = destProxy;
                            media.Model.IsArchived = true;
                            media.Model.ArchivePath = destMain;
                            await GlobalClass.Instance.AddtoHistoryAsync("Send to archive", $"Send asset '{media.Model.MediaPath}' to archive .");
                        }
                        else
                            await GlobalClass.Instance.ShowDialogAsync($"'{sourceMain}' does not exist.", this.Content.XamlRoot);
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
                        // ❌ If proxy move failed, rollback main file
                        if (movedMain != null && !File.Exists(sourceMain))
                        {
                            try
                            {
                                File.Move(movedMain, sourceMain, overwrite: false);
                            }
                            catch (Exception rollbackEx)
                            {
                                await GlobalClass.Instance.ShowDialogAsync(
                                    $"Rollback failed! Manual fix required.\n{rollbackEx.Message}",
                                    this.Content.XamlRoot);
                            }
                        }

                        // ❌ If main move failed after proxy moved (rare case), rollback proxy
                        if (movedProxy != null && !File.Exists(sourceProxy))
                        {
                            try
                            {
                                File.Move(movedProxy, sourceProxy, overwrite: false);
                            }
                            catch (Exception rollbackEx)
                            {
                                await GlobalClass.Instance.ShowDialogAsync(
                                    $"Rollback of proxy failed! Manual fix required.\n{rollbackEx.Message}",
                                    this.Content.XamlRoot);
                            }
                        }

                    }

                }

                else
                {
                    await GlobalClass.Instance.ShowDialogAsync($"'{media.Model.MediaSource.LocalPath}' is already archived.", this.Content.XamlRoot);
                }
            }
            App.MainAppWindow.StatusBar.HideStatus();
            this.Close();

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ChbActive_Checked(object sender, RoutedEventArgs e)
        {

        }


        private void ArchiveServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedArchiveServer = (ArchiveServer)((ComboBox)sender).SelectedItem;
        }



        private void ArchiveServerListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ArchiveServer server)
            {
                // This sets the VM property via two-way SelectedItem binding too,
                // but we can set it explicitly for clarity.
                ViewModel.SelectedArchiveServer = server;

                // Close the flyout after selection
                if (ArchiveServerDropDown.Flyout is Flyout f)
                    f.Hide();
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
        private ObservableCollection<MediaPlayerArchiveViewModel> mediaList = new();
        private ObservableCollection<ArchiveServer> archiveServerList = new();
        private ArchiveServer selectedArchiveServer;

        public ObservableCollection<MediaPlayerArchiveViewModel> MediaList
        {
            get => mediaList;
            set => SetProperty(ref mediaList, value);
        }
        public ObservableCollection<ArchiveServer> ArchiveServerList
        {
            get => archiveServerList;
            set => SetProperty(ref archiveServerList, value);
        }
        public ArchiveServer SelectedArchiveServer
        {
            get => selectedArchiveServer;
            set => SetProperty(ref selectedArchiveServer, value);
        }
        public SendToArchiveViewModel(ObservableCollection<MediaPlayerArchiveViewModel> mediaPlayerItems)
        {
            foreach (var item in mediaPlayerItems)
            {
                MediaList.Add(item);
            }
            // default to first server if available
            if (ArchiveServerList.Any())
                SelectedArchiveServer = ArchiveServerList.First();
        }
        public Action ArchiveUpdatedCallback { get; set; }
    }
}
