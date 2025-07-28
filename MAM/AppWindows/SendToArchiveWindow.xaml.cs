using MAM.Data;
using MAM.Utilities;
using MAM.Views.AdminPanelViews;
using MAM.Views.MediaBinViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.ObjectModel;
using System.Data;
using Windows.Graphics;
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
        public SendToArchiveViewModel ViewModel { get; set; }
        public ObservableCollection<SendToArchiveViewModel> ArchiveList { get; set; } = new();
        private DataAccess dataAccess { get; } = new DataAccess();
        public MediaPlayerItem Asset { get; set; }
        public ArchiveServer ArchiveServer { get; set; }

        public SendToArchiveWindow(MediaPlayerItem media)
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
            GlobalClass.Instance.SetWindowSizeAndPosition(600, 400,this);
            this.Activated += SendToArchiveWindow_Activated; ;

            Asset = media;
            ViewModel = new SendToArchiveViewModel
            {
                MediaPath = media.MediaSource.LocalPath,
                ArchivePath = GlobalClass.Instance.ArchivePath,
                AssetId = media.MediaId
            };

            ViewModel.ArchiveList.Add(ViewModel);
            MainGrid.DataContext = ViewModel;
        }

        private  void SendToArchiveWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if(args.WindowActivationState==WindowActivationState.Deactivated) return;
            //ArchiveServer =await GlobalClass.Instance.GetArchiveServer(this.Content.XamlRoot);
            //GlobalClass.Instance.ArchivePath=Path.Combine(ArchiveServer.ServerName, ArchiveServer.ArchivePath);
            if(!File.Exists(GlobalClass.Instance.ArchivePath))
            ArchiveDropDown.Content = GlobalClass.Instance.ArchivePath;
        }

        public static void ShowWindow(MediaPlayerItem media)
        {
          
            _instance = new SendToArchiveWindow(media);
            _instance.Activate(); // Show the window
        }
       
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _instance = null;
        }
       
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            //GlobalClass.Instance.ArchivePath = Path.Combine(Asset.ArchivePath..FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder);
            try
            {
                if (File.Exists(ViewModel.MediaPath))
                {
                    string title = Path.GetFileName(ViewModel.MediaPath);
                    string archiveDirectory = Path.Combine(GlobalClass.Instance.ArchivePath, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"));
                    Directory.CreateDirectory(archiveDirectory);
                    string destinationPath =Path.Combine(archiveDirectory,title);
                    Dictionary<string, object> propsList = new Dictionary<string, object>
                    {
                        {"is_archived", true },
                        {"archive_path",destinationPath }
                    };
                    File.Copy(ViewModel.MediaPath, destinationPath, false);
                    File.Delete(ViewModel.MediaPath);
                    

                    await dataAccess.UpdateRecord("Asset", "asset_id", ViewModel.AssetId, propsList);
                    Asset.IsArchived = true;
                    Asset.ArchivePath = destinationPath;
                    await GlobalClass.Instance.AddtoHistoryAsync("Send to archive", $"Send asset '{ViewModel.MediaPath}' to archive .");
                }
                else
                {
                   await GlobalClass.Instance.ShowDialogAsync("The file does not exist.", this.Content.XamlRoot);
                }
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
    public class SendToArchiveViewModel : ObservableObject
    {
        private string mediaPath;
        private string archivePath;
        private int assetId;
        private ObservableCollection<SendToArchiveViewModel> archiveList=new();

        public int AssetId
        {
            get => assetId;
            set => SetProperty(ref assetId, value);
        }
        public string MediaPath
        {
            get => mediaPath;
            set => SetProperty(ref mediaPath, value);
        }
        public string ArchivePath
        {
            get => archivePath;
            set => SetProperty(ref archivePath, value);
        }
        public ObservableCollection<SendToArchiveViewModel> ArchiveList
        {
            get => archiveList;
            set => SetProperty(ref archiveList, value);
        }
    }
}
