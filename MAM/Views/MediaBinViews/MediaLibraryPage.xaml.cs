using MAM.Data;
using MAM.UserControls;
using MAM.Utilities;
using MAM.Views.AdminPanelViews;
using MAM.Views.AdminPanelViews.Metadata;
using MAM.Windows;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using WinRT; // needed for As<IDataObject>
using Path = System.IO.Path;
using Type = System.Type;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaLibraryPage : Page
    {
        private DataAccess dataAccess = new();
        public ObservableCollection<string> FilteredTags { get; set; } = [];
        public ObservableCollection<MetadataClass> FilteredMetadataList { get; set; } = [];
        private readonly List<string> ExcludedFolders = new() { "Proxy" };
        public MediaLibraryViewModel ViewModel { get; set; }
        private readonly IDropTargetHelper _dropHelper;

        public MediaLibraryPage()
        {
            this.InitializeComponent();
            _dropHelper = (IDropTargetHelper)new DragDropHelper();

            ViewModel = new MediaLibraryViewModel();
            ViewModel.FileSystemItems = new ObservableCollection<FileSystemItem>();
            this.Loaded += MediaLibraryPage_Loaded;
            ViewModel.PageSize = SettingsService.Get(SettingKeys.DefaultPageSize, 8);
            ViewModel.SelectedPageIndex = 0;
            UpdatePageButtons();
            ViewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.SelectedPageIndex) ||
                    e.PropertyName == nameof(ViewModel.TotalPages))
                {
                    UpdatePageButtons();
                }

            };
            ViewModel.PaginationUpdated += UpdatePageButtons;
            DataContext = ViewModel;

        }
        private async void MediaLibraryPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadServers();
            if (GlobalClass.Instance.FileServer != null)
            {
                ViewModel.MediaLibraryObj.FileServer = GlobalClass.Instance.FileServer;
                var builder = new MySqlConnectionStringBuilder(DataAccess.ConnectionString);

                string server = builder.Server;  // "192.168.29.191"
                string fileServer = ViewModel.MediaLibraryObj.FileServer.ServerName.Replace("\\", "");
                if (fileServer == server)
                {
                    string fileFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder);
                    string baseRoot = Directory.GetParent(fileFolder).FullName;
                    string mediaLibrary = fileFolder.Substring(baseRoot.Length).TrimStart('\\');
                    //string RecycleBin = Path.Combine(baseRoot, "Recycle Bin");
                    //string RecycleBinMediaLibrary = Path.Combine(RecycleBin, mediaLibrary);

                    //string RecycleBinThumbnailFolder = Path.Combine(RecycleBin, "Thumbnail");
                    //string RecycleBinProxyFolder = Path.Combine(RecycleBin, "Proxy");



                    ViewModel.MediaLibraryObj.ProxyFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, GlobalClass.Instance.ProxyFolder);
                    ViewModel.MediaLibraryObj.ThumbnailFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, GlobalClass.Instance.ThumbnailFolder);
                    ViewModel.MediaLibraryObj.RecycleFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, GlobalClass.Instance.RecycleFolder, mediaLibrary);
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.FileServer.FileFolder))
                        Directory.CreateDirectory(ViewModel.MediaLibraryObj.FileServer.FileFolder);
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.ProxyFolder))
                        Directory.CreateDirectory(ViewModel.MediaLibraryObj.ProxyFolder);
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.ThumbnailFolder))
                        Directory.CreateDirectory(ViewModel.MediaLibraryObj.ThumbnailFolder);

                    ViewModel.MediaLibraryObj.ArchiveServer = await GlobalClass.Instance.GetArchiveServer(XamlRoot);
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.ArchiveServer.ArchivePath))
                        Directory.CreateDirectory(ViewModel.MediaLibraryObj.ArchiveServer.ArchivePath);
                    // ViewModel.MediaLibraryObj.RecycleFolder = RecycleBinMediaLibrary;
                    ViewModel.MediaLibraryObj.RecycleThumbnailFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, GlobalClass.Instance.RecycleFolder, "Thumbnail");
                    ViewModel.MediaLibraryObj.RecycleProxyFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, GlobalClass.Instance.RecycleFolder, "Proxy");
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.RecycleFolder))
                        Directory.CreateDirectory(ViewModel.MediaLibraryObj.RecycleFolder);
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.RecycleThumbnailFolder))
                        Directory.CreateDirectory(ViewModel.MediaLibraryObj.RecycleThumbnailFolder);
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.RecycleProxyFolder))
                        Directory.CreateDirectory(ViewModel.MediaLibraryObj.RecycleProxyFolder);
                    if (ViewModel.MediaLibraryObj.FileServer != null)
                    {
                        UIThreadHelper.RunOnUIThread(() =>
                        {
                            App.MainAppWindow.StatusBar.ShowStatus("Connecting...", true);
                        });
                        var root = await LoadRootItemsAsync(ViewModel.MediaLibraryObj.FileServer.ServerName);
                        if (root != null)
                        {
                            ViewModel.FileSystemItems.Clear();
                            ViewModel.FileSystemItems.Add(root);
                            SelectRootNode();
                            try
                            {
                                GetAllTags();
                                GetAllMetadata();
                                UIThreadHelper.RunOnUIThread(() =>
                                {
                                    App.MainAppWindow.StatusBar.ShowStatus("Connected.", false);
                                    App.MainAppWindow.StatusBar.HideStatus();
                                });
                            }
                            catch (Exception ex)
                            {
                                await GlobalClass.Instance.ShowDialogAsync($"Unable to create Directory. Check file server settings ", this.XamlRoot);
                            }
                        }
                        else
                            UIThreadHelper.RunOnUIThread(() =>
                            {
                                App.MainAppWindow.StatusBar.ShowStatus("Connection Failed.", false);
                            });
                    }
                }
            }
        }
        private async void LoadServers()
        {
            await GlobalClass.Instance.GetArchiveServer(this.Content.XamlRoot);
            await GlobalClass.Instance.GetFileServer(this.Content.XamlRoot);
        }
        public async Task<FileSystemItem> LoadRootItemsAsync(string rootPath)
        {
            var credentials = new NetworkCredential(
                ViewModel.MediaLibraryObj.FileServer.UserName,
                ViewModel.MediaLibraryObj.FileServer.Password,
                ViewModel.MediaLibraryObj.FileServer.Domain);

            var items = new FileSystemItem();
            string fileFolder = Path.Combine(rootPath, ViewModel.MediaLibraryObj.FileServer.FileFolder);
            var connection = await NetworkConnection.CreateAsync(rootPath, credentials, this.XamlRoot);
            if (connection != null)
            {
                using (connection)
                {
                    // Start traversal once connected
                    if (Directory.Exists(fileFolder))
                    {
                        items = CreateFileSystemItem(fileFolder, true);
                        items.IsRoot = true;
                    }
                }

                return items;
            }
            else
                return null;
        }
        private FileSystemItem CreateFileSystemItem(string path, bool isDirectory)
        {
            var item = new FileSystemItem
            {
                Name = System.IO.Path.GetFileName(path),
                Path = path,
                IsDirectory = isDirectory
            };

            // If it's a directory, populate its children
            if (isDirectory)
            {
                try
                {
                    string archivePath = string.Empty;
                    if (ViewModel.MediaLibraryObj.ArchiveServer != null)
                        archivePath = Path.Combine(ViewModel.MediaLibraryObj.ArchiveServer.ServerName, ViewModel.MediaLibraryObj.ArchiveServer.ArchivePath);
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        // Skip folders named "Proxy"
                        if (Path.Equals(dir, ViewModel.MediaLibraryObj.ProxyFolder) || Path.Equals(dir, ViewModel.MediaLibraryObj.ThumbnailFolder) || Path.Equals(dir, archivePath) || Path.Equals(dir, ViewModel.MediaLibraryObj.RecycleFolder))
                        {
                            continue;
                        }
                        item.Children.Add(CreateFileSystemItem(dir, true));
                    }

                    //foreach (var file in Directory.GetFiles(path))
                    //{
                    //    item.Children.Add(CreateFileSystemItem(file, false));
                    //}
                }
                catch (UnauthorizedAccessException)
                {
                    // Handle inaccessible directories
                }
            }

            return item;
        }

        private void SelectRootNode()
        {
            if (FileTreeView.RootNodes.Count > 0)
            {
                var firstRootNode = FileTreeView.RootNodes[0];
                FileTreeView.SelectedItem = firstRootNode.Content;
            }
        }
        //private void CustomMediaPlayer_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (sender is CustomBinMedia customMedia && customMedia.DataContext is MediaPlayerItem mediaItem)
        //    {
        //        customMedia.DeleteRequested += async (s, args) =>
        //        {
        //            await DeleteMediaItemAsync(args.MediaItem);
        //        };
        //        customMedia.PlayButtonClicked += async (s, args) =>
        //        {
        //            await ShowAssetWindow();
        //        };
        //    }
        //}
        private void ViewModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string viewMode = selectedItem.Tag as string;

                if (Resources["MediaTemplateSelector"] is MediaTemplateSelector selector)
                {
                    selector.CurrentViewMode = viewMode;
                    MediaBinGridView.ItemTemplateSelector = null; // Force refresh
                    MediaBinGridView.ItemTemplateSelector = selector;
                }
                // Show/hide header depending on view mode
                if (viewMode == "Details")
                {
                    DetailsHeader.Visibility = Visibility.Visible;
                }
                else
                {
                    DetailsHeader.Visibility = Visibility.Collapsed;
                }
            }
        }



        private bool _isDraggingLeftVertical;
        private bool _isDraggingRightVertical;
        private bool _isDraggingHorizontal;

        private double _originalSplitterPosition;
        private GridLength _originalLeftWidth;
        private GridLength _originalRightWidth;
        private GridLength _originalTopHeight;
        // LEFT SPLITTER EVENTS
        private void VerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingLeftVertical = true;
            _originalSplitterPosition = e.GetCurrentPoint(MainGrid).Position.X;
            _originalLeftWidth = LeftColumn.Width;

            LeftVerticalSplitter.CapturePointer(e.Pointer);
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        private void VerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingLeftVertical)
            {
                double currentPosition = e.GetCurrentPoint(MainGrid).Position.X;
                double delta = currentPosition - _originalSplitterPosition;
                double newWidth = _originalLeftWidth.Value + delta;

                if (newWidth >= 200 && newWidth <= 600)
                {
                    LeftColumn.Width = new GridLength(newWidth, GridUnitType.Pixel);
                    CenterColumn.Width = new GridLength(1, GridUnitType.Star);
                }
            }
        }

        private void VerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        private void VerticalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        // RIGHT SPLITTER EVENTS
        private void RightVerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingRightVertical = true;
            _originalSplitterPosition = e.GetCurrentPoint(MainGrid).Position.X;
            _originalRightWidth = RightColumn.Width;

            RightVerticalSplitter.CapturePointer(e.Pointer);
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        private void RightVerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingRightVertical)
            {
                double currentPosition = e.GetCurrentPoint(MainGrid).Position.X;
                double delta = _originalSplitterPosition - currentPosition;
                double newWidth = _originalRightWidth.Value + delta;

                if (newWidth >= 200 && newWidth <= 600)
                {
                    RightColumn.Width = new GridLength(newWidth, GridUnitType.Pixel);
                    CenterColumn.Width = new GridLength(1, GridUnitType.Star);
                }
            }
        }

        private void RightVerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        // HORIZONTAL SPLITTER EVENTS
        private void HorizontalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingHorizontal = true;
            _originalSplitterPosition = e.GetCurrentPoint(null).Position.Y;

            _originalTopHeight = new GridLength(TopRow.ActualHeight, GridUnitType.Pixel);

            HorizontalSplitter.CapturePointer(e.Pointer);
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        }

        private void HorizontalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingHorizontal)
            {
                double currentPosition = e.GetCurrentPoint(null).Position.Y;
                double delta = currentPosition - _originalSplitterPosition;
                double newHeight = _originalTopHeight.Value + delta;

                if (double.IsNaN(newHeight) || double.IsInfinity(newHeight))
                    return;

                if (newHeight >= 200 && newHeight <= MainGrid.ActualHeight - 200)
                {
                    TopRow.Height = new GridLength(newHeight, GridUnitType.Pixel);
                    BottomRow.Height = new GridLength(1, GridUnitType.Star);
                }
            }
        }


        private void HorizontalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        }

        private void HorizontalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        // POINTER RELEASED (ALL)
        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingLeftVertical = false;
            _isDraggingRightVertical = false;
            _isDraggingHorizontal = false;

            LeftVerticalSplitter.ReleasePointerCapture(e.Pointer);
            RightVerticalSplitter.ReleasePointerCapture(e.Pointer);
            HorizontalSplitter.ReleasePointerCapture(e.Pointer);

            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {

        }
        private void LoadSubfolders(TreeViewNode parentNode, string path)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    var node = new TreeViewNode() { Content = System.IO.Path.GetFileName(directory) };
                    parentNode.Children.Add(node);
                    // Load subfolders recursively
                    LoadSubfolders(node, directory);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Handle the case where access to a folder is denied
            }
            catch (Exception ex)
            {
                // Handle other exceptions as necessary
            }
        }
        public async Task GetVideoPropertiesAsync(string folderPath)
        {
            // Get the folder
            var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            var files = await folder.GetFilesAsync();

            foreach (var file in files)
            {
                if (file.ContentType.StartsWith("video/"))
                {
                    // Get video properties
                    VideoProperties properties = await file.Properties.GetVideoPropertiesAsync();

                    Debug.WriteLine($"File: {file.Name}");
                    Debug.WriteLine($"Title: {properties.Title}");
                    Debug.WriteLine($"Duration: {properties.Duration}");
                    Debug.WriteLine($"Width: {properties.Width}");
                    Debug.WriteLine($"Height: {properties.Height}");
                    Debug.WriteLine($"Bitrate: {properties.Bitrate}");
                }
            }
        }
        public async Task<List<string>> GetAllSubdirectoriesAsync(string rootPath)
        {
            List<string> directories = new();
            try
            {
                // Get all subdirectories, excluding those named "Proxy"
                var subDirs = Directory.GetDirectories(rootPath)
                                       .Where(dir => new DirectoryInfo(dir).Name != ViewModel.MediaLibraryObj.ProxyFolder && new DirectoryInfo(dir).Name != ViewModel.MediaLibraryObj.ThumbnailFolder && new DirectoryInfo(dir).Name != ViewModel.MediaLibraryObj.ArchiveServer.ArchivePath)
                                       .ToList();
                if (subDirs.Count > 0)
                    directories.AddRange(subDirs);

                // Recursively retrieve subdirectories from each found subdirectory
                foreach (var subDir in Directory.GetDirectories(rootPath))
                {
                    directories.AddRange(await GetAllSubdirectoriesAsync(subDir));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing {rootPath}: {ex.Message}");
            }

            return directories;
        }

        public async Task<ObservableCollection<MediaPlayerItem>> GetAllAssetsAsync(string FolderPath)
        {
            //GlobalClass.Instance.MediaLibraryPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder);
            DataTable dt = new();
            Dictionary<string, object> parameters = new();
            List<string> directories = new();
            directories.Add(FolderPath);
            directories.AddRange(await GetAllSubdirectoriesAsync(FolderPath));
            ViewModel.MediaPlayerItems.Clear();
            // ViewModel.AllMediaPlayerItems.Clear();
            foreach (var dir in directories)
            {
                if (Directory.Exists(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ProxyFolder)))
                {
                    string relativePath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder), dir);  // "Songs\Hindi"
                    parameters.Add("@Relative_path", relativePath);
                    dt = dataAccess.GetData($"SELECT a.asset_id, a.asset_name,a.relative_path," +
                                             "CONCAT(fs.file_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS media_path," +
                                            "CONCAT(fs.proxy_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS proxy_path," +
                                            "CONCAT(fs.thumbnail_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS thumbnail_path," +
                                            "CASE " +
                                                "WHEN a.is_archived = 1 " +
                                                "THEN CONCAT(REPLACE(asr.archive_path, '/', '\\\\'), '\\\\', REPLACE(a.archive_path, '/', '\\\\')) " +
                                            "END AS archive_path," +
                                            "CASE " +
                                                "WHEN a.is_deleted = 1 " +
                                                "THEN CONCAT(fs.recycle_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) " +
                                            "END AS recyclebin_path," +
                                            "a.duration, a.description, a.version, a.type," +
                                            "a.size,a.created_user, a.created_at, a.updated_user, a.updated_at, a.is_archived, a.archive_path," +
                                            "IFNULL(GROUP_CONCAT(DISTINCT t.tag_name ORDER BY tag_name),'') AS tag_name," +
                                            "IFNULL(GROUP_CONCAT(DISTINCT CONCAT(m.metadata_id, ':', m.metadata_name, ':', am.metadata_value, ':', m.metadata_type) " +
                                            "ORDER BY m.metadata_name SEPARATOR '; '),'') AS metadata_info " +
                                            "FROM asset a " +
                                            "JOIN file_server fs on a.file_server_id=fs.server_id " +
                                            "JOIN archive_server asr on a.archive_server_id=asr.server_id " +
                                            "LEFT JOIN asset_tag ta ON a.asset_id = ta.asset_id " +
                                            "LEFT JOIN tag t ON t.tag_id = ta.tag_id " +
                                            "LEFT JOIN asset_metadata am ON a.asset_id = am.asset_id " +
                                            "LEFT JOIN metadata m ON am.metadata_id = m.metadata_id " +
                                            "WHERE a.relative_path = @Relative_path AND a.is_deleted=false " +
                                            "GROUP BY a.asset_id;", parameters);

                    foreach (DataRow row in dt.Rows)
                    {
                        ObservableCollection<AssetsMetadata> metadataList = new();
                        string file = row["asset_name"].ToString();
                        //string baseRoot = Directory.GetParent(ViewModel.MediaLibraryObj.ThumbnailFolder).FullName;
                        //string relativePath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder), dir);  // "Songs\Hindi"
                        //string thumbnailMediaPath = Path.Combine(ViewModel.MediaLibraryObj.ThumbnailFolder, relativePath);
                        //string thumbnailPath = Path.Combine(thumbnailMediaPath, Path.GetFileNameWithoutExtension(file) + "_Thumbnail.JPG");
                        //Uri thumbnailUri = new Uri(thumbnailPath);
                        string proxyPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, row["proxy_path"].ToString(), Path.GetFileNameWithoutExtension(file) + "_Proxy.MP4");
                        ////string proxyPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.ProxyFolder,  (file)+ "+Proxy" );
                        //string proxyPath = row["proxy_path"].ToString();
                        string thumbnailPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, row["thumbnail_path"].ToString(), Path.GetFileNameWithoutExtension(file) + "_Thumbnail.JPG");
                        TimeSpan duration = (TimeSpan)(row["duration"]);
                        string description = string.Empty;
                        string updated_user = string.Empty;
                        DateTime updated_at = DateTime.MinValue;
                        string mediaPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, row["media_path"].ToString());
                        string source = Path.Combine(mediaPath, row["asset_name"].ToString());
                        bool isArchived = Convert.ToBoolean(row["is_archived"]);
                        string archivePath = string.Empty;
                        if (isArchived)
                            archivePath = Path.Combine(row["archive_path"].ToString(), Path.GetFileName(file));
                        if (!File.Exists(source))
                        {
                            if ((File.Exists(proxyPath)) && !isArchived)
                                continue;
                        }
                        if (row["description"] != DBNull.Value)
                            description = row["description"].ToString();
                        if (row["updated_user"] != DBNull.Value)
                            updated_user = row["updated_user"].ToString();
                        if (row["updated_at"] != DBNull.Value)
                            updated_at = Convert.ToDateTime(row["updated_at"]);

                        Dictionary<string, object> metadata = null;
                        List<string> keywords = null;
                        string rating = string.Empty;
                        List<string> tags = row["tag_name"].ToString().Split(',').ToList();

                        if (row["metadata_info"] != DBNull.Value && row["metadata_info"].ToString().Contains(';'))
                        {
                            foreach (var mdata in row["metadata_info"].ToString().Split(';'))
                            {
                                AssetsMetadata assetsMetadata = new();
                                assetsMetadata.MetadataId = Convert.ToInt32(mdata.Split(':')[0]);
                                assetsMetadata.Metadata = mdata.Split(':')[1].ToString();
                                assetsMetadata.MetadataValue = mdata.Split(':')[2].ToString();
                                assetsMetadata.MetadataType = mdata.Split(':')[3].ToString();
                                metadataList.Add(assetsMetadata);
                            }
                        }
                        ViewModel.MediaPlayerItems.Add(new MediaPlayerItem
                        {
                            MediaId = Convert.ToInt32(row["asset_id"]),
                            RelativePath = row["relative_path"].ToString(),
                            Title = row["asset_name"].ToString(),
                            MediaSource = new Uri(source),
                            MediaPath = mediaPath,
                            //OriginalPath = row["original_path"].ToString(),
                            Duration = duration,
                            DurationString = duration.ToString(@"hh\:mm\:ss"),
                            Description = description,
                            Version = row["version"].ToString(),
                            Type = row["type"].ToString(),
                            Size = Convert.ToDouble(row["size"]),
                            CreatedUser = row["created_user"].ToString(),
                            CreationDate = DateOnly.FromDateTime(Convert.ToDateTime(row["created_at"]).Date),
                            UpdatedUser = updated_user,
                            LastUpdated = updated_at,
                            ThumbnailPath = string.IsNullOrWhiteSpace(thumbnailPath) ? null : thumbnailPath,
                            ProxyPath = string.IsNullOrWhiteSpace(proxyPath) ? null : proxyPath,
                            Tags = tags,
                            Keywords = keywords,
                            Rating = string.IsNullOrEmpty(rating) ? 0 : Convert.ToDouble(rating),
                            AssetMetadataList = metadataList,
                            IsArchived = isArchived,
                            ArchivePath = archivePath
                        });

                        // ViewModel.AllMediaPlayerItems.Add(ViewModel.MediaPlayerItems[ViewModel.MediaPlayerItems.Count - 1]);
                    }
                    parameters.Clear();
                }
            }
            ViewModel.MediaLibraryObj.FileCount = ViewModel.MediaPlayerItems.Count;
            ViewModel.AssetCount = $"{ViewModel.MediaLibraryObj.FileCount} results found";
            return ViewModel.MediaPlayerItems;
        }
        public async Task<MediaPlayerItem> GetAssetsAsync(MediaPlayerItem media)
        {
            string assetPath = media.RelativePath;
            string assetName = media.Title;
            GlobalClass.Instance.MediaLibraryPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder);
            DataTable dt = new();
            Dictionary<string, object> parameters = new();
            if (Directory.Exists(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.ProxyFolder)))
            {
                //MediaLibrary.ProxyFolder = System.IO.Path.Combine(assetPath, "Proxy");
                parameters.Add("@Relative_path", assetPath);
                parameters.Add("@Asset_name", assetName);
                string query = string.Empty;

                query = $"select a.asset_id," +
                       $"a.asset_name ," +
                       $"a.relative_path ," +
                       "CONCAT(fs.file_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS media_path," +
                       "CONCAT(fs.proxy_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS proxy_path," +
                       "CONCAT(fs.thumbnail_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS thumbnail_path," +
                       "CASE " +
                        "WHEN a.is_archived = 1 " +
                            "THEN CONCAT(fs.server_name, '\\\\', REPLACE(a.archive_path, '/', '\\\\')) " +
                        "END AS archive_path," +
                       "CASE " +
                        "WHEN a.is_deleted = 1 " +
                            "THEN CONCAT(fs.recycle_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) " +
                       "END AS recyclebin_path," +

                       $"a.duration," +
                       $"a.description," +
                       $"a.version," +
                       $"a.type," +
                       $"a.size," +
                       $"a.created_user," +
                       $"a.created_at," +
                       $"a.updated_user," +
                       $"a.updated_at, " +
                       $"a.is_archived, " +
                       $"a.archive_path, " +
                       $" group_concat( distinct t.tag_name order by tag_name) as tag_name," +
                       $" group_concat(distinct concat(m.metadata_id,':',m.metadata_name, ':', am.metadata_value,':',m.metadata_type) ORDER BY m.metadata_name SEPARATOR '; ') AS metadata_info" +
                       $" from asset a " +
                       "JOIN file_server fs on a.file_server_id=fs.server_id " +
                       "JOIN archive_server asr on a.archive_server_id=asr.server_id " +
                       $" left join asset_tag ta on a.asset_id=ta.asset_id" +
                       $" left join tag t on t.tag_id=ta.tag_id" +
                       $" left join asset_metadata am on a.asset_id = am.asset_id" +
                       $" left join metadata m ON am.metadata_id = m.metadata_id" +
                       $" WHERE a.relative_path = @Relative_path and a.asset_name=@Asset_name  AND a.is_deleted=false" +
                       " GROUP BY " +
                       " a.asset_id," +
                       " a.asset_name," +
                       " a.relative_path," +
                      " a.duration," +
                      " a.description," +
                      " a.version," +
                      " a.type," +
                      " a.size," +
                      " a.created_user," +
                      " a.created_at," +
                      " a.updated_user," +
                      " a.updated_at," +
                      " a.is_archived," +
                      " a.archive_path;";
                //AND a.is_archived =false
                dt = dataAccess.GetData(query, parameters);
                if (dt.Rows.Count > 0)
                {
                    ObservableCollection<AssetsMetadata> metadataList = new();
                    string file = dt.Rows[0]["asset_name"].ToString();
                    //string baseRoot = Directory.GetParent(ViewModel.MediaLibraryObj.ThumbnailFolder).FullName;
                    //string relativePath = assetPath.Substring(baseRoot.Length).TrimStart('\\');
                    //string thumbnailMediaPath = Path.Combine(ViewModel.MediaLibraryObj.ThumbnailFolder, relativePath);
                    //string thumbnailPath = Path.Combine(thumbnailMediaPath, Path.GetFileNameWithoutExtension(file) + "_Thumbnail" + ".jpg");
                    //Uri thumbnailUri = new Uri(thumbnailPath);
                    //var proxyPath = Path.Combine(ViewModel.MediaLibraryObj.ProxyFolder, relativePath, file + "_Proxy");
                    string proxyPath = Path.Combine(dt.Rows[0]["proxy_path"].ToString(), Path.GetFileNameWithoutExtension(file) + "_Proxy.MP4");
                    string thumbnailPath = Path.Combine(dt.Rows[0]["thumbnail_path"].ToString(), Path.GetFileNameWithoutExtension(file) + "_Thumbnail.JPG");

                    TimeSpan duration = (TimeSpan)(dt.Rows[0]["duration"]);
                    string description = string.Empty;
                    string updated_user = string.Empty;
                    DateTime updated_at = DateTime.MinValue;
                    string path = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, dt.Rows[0]["media_path"].ToString());

                    string source = Path.Combine(path, dt.Rows[0]["asset_name"].ToString());
                    bool isArchived = Convert.ToBoolean(dt.Rows[0]["is_archived"]);

                    if (!File.Exists(source))
                    {
                        if ((File.Exists(proxyPath)) && !isArchived)
                            return null;
                    }
                    if (dt.Rows[0]["description"] != DBNull.Value)
                        description = dt.Rows[0]["description"].ToString();
                    if (dt.Rows[0]["updated_user"] != DBNull.Value)
                        updated_user = dt.Rows[0]["updated_user"].ToString();
                    if (dt.Rows[0]["updated_at"] != DBNull.Value)
                        updated_at = Convert.ToDateTime(dt.Rows[0]["updated_at"]);
                    Dictionary<string, object> metadata = new();
                    List<string> keywords = new();
                    string rating = string.Empty;
                    //string mediaPath = dt.Rows[0]["asset_path"].ToString();

                    List<string> tags = dt.Rows[0]["tag_name"].ToString().Split(',').ToList();

                    if (dt.Rows[0]["metadata_info"] != DBNull.Value)
                    {
                        foreach (var mdata in dt.Rows[0]["metadata_info"].ToString().Split(';'))
                        {
                            AssetsMetadata assetsMetadata = new();
                            assetsMetadata.MetadataId = Convert.ToInt32(mdata.Split(':')[0]);
                            assetsMetadata.Metadata = mdata.Split(':')[1].ToString();
                            assetsMetadata.MetadataValue = mdata.Split(':')[2].ToString();
                            assetsMetadata.MetadataType = mdata.Split(':')[3].ToString();
                            metadataList.Add(assetsMetadata);
                        }

                    }
                    MediaPlayerItem mediaPlayerItem = new()
                    {
                        MediaId = Convert.ToInt32(dt.Rows[0]["asset_id"]),
                        Title = dt.Rows[0]["asset_name"].ToString(),
                        MediaSource = new Uri(source),
                        MediaPath = path,
                        //OriginalPath = dt.Rows[0]["original_path"].ToString(),
                        Duration = duration,
                        DurationString = duration.ToString(@"hh\:mm\:ss"),
                        Description = description,
                        Version = dt.Rows[0]["version"].ToString(),
                        Type = dt.Rows[0]["type"].ToString(),
                        Size = Convert.ToDouble(dt.Rows[0]["size"]),
                        CreatedUser = dt.Rows[0]["created_user"].ToString(),
                        CreationDate = DateOnly.FromDateTime(Convert.ToDateTime(dt.Rows[0]["created_at"]).Date),
                        UpdatedUser = updated_user,
                        LastUpdated = updated_at,
                        ThumbnailPath = thumbnailPath,
                        ProxyPath = proxyPath,
                        Tags = tags,
                        Keywords = keywords,
                        Rating = string.IsNullOrEmpty(rating) ? 0 : Convert.ToDouble(rating),
                        AssetMetadataList = metadataList,
                        IsArchived = Convert.ToBoolean(dt.Rows[0]["is_archived"]),
                        ArchivePath = dt.Rows[0]["archive_path"].ToString()
                    };
                    return mediaPlayerItem;
                }
                else
                    return null;
            }
            else
                return null;
        }
        private FileSystemItem _rightClickedItem;
        private bool isNewBin;

        private async void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            //FileSystemItem selectedNode = (FileSystemItem)args.AddedItems.FirstOrDefault();
            //if (selectedNode != null && !selectedNode.IsEditing)
            //{
            await LoadAssetsAsync();
            ViewModel.Path = ViewModel.MediaLibraryObj.BinName;
            //}
        }
        private void TreeViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is FileSystemItem item)
            {
                _rightClickedItem = item;
            }
        }

        private async Task LoadAssetsAsync()
        {
            if (FileTreeView.SelectedNode != null)
            {
                FileSystemItem selectedNode = (FileSystemItem)FileTreeView.SelectedNode.Content;
                if (selectedNode != null && !selectedNode.IsEditing)
                {
                    string dirName = new DirectoryInfo(selectedNode.Path).Name;

                    if (dirName == selectedNode.Name)
                        ViewModel.MediaLibraryObj.BinName = selectedNode.Path;
                    else
                        ViewModel.MediaLibraryObj.BinName = Path.Combine(selectedNode.Path, selectedNode.Name);

                    ViewModel.MediaPlayerItems.Clear();
                    await GetAllAssetsAsync(ViewModel.MediaLibraryObj.BinName);
                    ViewModel.MediaLibraryObj.FileCount = ViewModel.MediaPlayerItems.Count;
                    ViewModel.MediaLibraryObj = ViewModel.MediaLibraryObj;
                    ViewModel.ApplyFilter();
                }
            }
        }
        private string GetFullPath(TreeViewNode selectedNode)
        {
            if (selectedNode == null) return string.Empty;

            var path = new List<string>();
            var currentNode = selectedNode;

            // Traverse up the hierarchy to construct the path
            while (currentNode.Content != null)
            {
                path.Insert(0, currentNode.Content.ToString());
                currentNode = currentNode.Parent;
            }

            // Combine all parts of the path with a delimiter (e.g., "/")
            return string.Join("\\", path);
        }


        private readonly List<(string Tag, Type Page)> _pages =
        [
            ("UploadHistory",typeof(TransactionHistoryPage)),
            ("DownloadHistory",typeof(TransactionHistoryPage)),
            ("ExportHistory",typeof(TransactionHistoryPage)),

        ];

        private void NavFileHistory_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavFileHistory_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }
        private void NavFileHistory_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
        {
            Type _page = null;
            var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
            _page = item.Page;
            var prevNavPAgeType = ContentFrame.CurrentSourcePageType;
            if (!(_page is null))
            {
                ContentFrame.Navigate(_page, navItemTag, recommendedNavigationTransitionInfo);
            }

        }
        private void NavFileHistory_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }
        private void NavFileHistory_Loaded(object sender, RoutedEventArgs e)
        {

            //	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;

            ////	// NavFileHistory doesn't load any page by default, so load home page.
            NavFileHistory.SelectedItem = NavFileHistory.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            NavFileHistory_Navigate("UploadHistory", new EntranceNavigationTransitionInfo());
        }
        private void NavFileHistory_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (NavFileHistory.IsPaneOpen &&
                (NavFileHistory.DisplayMode == NavigationViewDisplayMode.Compact ||
                 NavFileHistory.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }
        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            NavFileHistory.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavFileHistory.MenuItems, and doesn't have a Tag.
                NavFileHistory.SelectedItem = (NavigationViewItem)NavFileHistory.SettingsItem;
                NavFileHistory.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                NavFileHistory.SelectedItem = NavFileHistory.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                NavFileHistory.Header =
                    ((NavigationViewItem)NavFileHistory.SelectedItem)?.Content?.ToString();

            }
        }
        private void OnDeleteClick()
        {

        }
        public async Task DeleteMediaItemAsync(MediaPlayerItem item)
        {
            ContentDialogResult result = ContentDialogResult.None;
            if (item.IsArchived)
            {
                result = await GlobalClass.Instance.ShowDialogAsync($"'{item.Title}' is archived.Do you want to delete it from archive ?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");
                if (result == ContentDialogResult.Primary)
                {
                    await DeleteFileFromArchive(item);

                }
            }
            else
            {
                if (SettingsService.Get<bool>(SettingKeys.MoveToRecycleBin, true))
                {
                    result = await GlobalClass.Instance.ShowDialogAsync($"Do you want to delete \n\n '{item.Title}' ?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");
                    if (result == ContentDialogResult.Primary)
                    {
                        await MoveItemToRecycleBinAsync(item);
                    }
                    else
                        return;
                }
                else
                {
                    result = await GlobalClass.Instance.ShowDialogAsync($"Are you sure that you want to permanently delete this file ? \n\n '{item.Title}' ?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");
                    if (result == ContentDialogResult.Primary)
                    {
                        await DeleteFilePermanently(item);
                    }
                    else
                        return;
                }
            }
            ViewModel.ApplyFilter();
        }
        private async Task MoveItemToRecycleBinAsync(MediaPlayerItem item)
        {
            string relativePath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder), item.MediaPath);  // "Songs\Hindi"
            string recycleBinFolderPath = Path.Combine(ViewModel.MediaLibraryObj.RecycleFolder, relativePath);
            string recycleBinThumbnailFolder = Path.Combine(ViewModel.MediaLibraryObj.RecycleThumbnailFolder, relativePath);
            string recycleBinProxyFolder = Path.Combine(ViewModel.MediaLibraryObj.RecycleProxyFolder, relativePath);
            if (!Directory.Exists(recycleBinFolderPath))
                Directory.CreateDirectory(recycleBinFolderPath);
            if (!Directory.Exists(recycleBinThumbnailFolder))
                Directory.CreateDirectory(recycleBinThumbnailFolder);
            if (!Directory.Exists(recycleBinProxyFolder))
                Directory.CreateDirectory(recycleBinProxyFolder);
            string fileName = Path.GetFileNameWithoutExtension(item.Title);
            string recycleBinPath = Path.Combine(recycleBinFolderPath, item.Title);
            string recycleBinThumbnailPath = Path.Combine(recycleBinThumbnailFolder, fileName + "_Thumbnail.JPG");
            string recycleBinProxyPath = Path.Combine(recycleBinProxyFolder, fileName + "_Proxy.MP4");
            if (File.Exists(item.MediaSource.LocalPath))
            {
                if (File.Exists(recycleBinPath))
                {
                    string backupPath = recycleBinPath + ".bak";
                    File.Replace(item.MediaSource.LocalPath, recycleBinPath, backupPath);
                }
                else
                {
                    File.Move(item.MediaSource.LocalPath, recycleBinPath);
                }
                if (File.Exists(item.ThumbnailPath))
                {
                    if (File.Exists(recycleBinThumbnailPath))
                    {
                        string backupPath = recycleBinThumbnailPath + ".bak";
                        File.Replace(item.ThumbnailPath, recycleBinThumbnailPath, backupPath);
                    }
                    else
                    {
                        File.Move(item.ThumbnailPath, recycleBinThumbnailPath);
                    }
                }
                if (File.Exists(item.ProxyPath))
                {
                    if (File.Exists(recycleBinProxyPath))
                    {
                        string backupPath = recycleBinProxyPath + ".bak";
                        File.Replace(item.ProxyPath, recycleBinProxyPath, backupPath);
                    }
                    else
                    {

                        File.Move(item.ProxyPath, recycleBinProxyPath);
                    }
                }
            }
            ViewModel.MediaPlayerItems.Remove(item);
            //ViewModel.ApplyFilter();
            Dictionary<string, object> propsList = new Dictionary<string, object>
                    {
                        {"is_deleted", true },
                    };

            if (await dataAccess.UpdateRecord("asset", "asset_id", item.MediaId, propsList) > 0)
            {
                item.IsDeleted = true;
                item.RecycleBinPath = recycleBinFolderPath;
                await GlobalClass.Instance.AddtoHistoryAsync("Asset Deleted", $"Deleted asset '{item.MediaPath}'.");
            }
            else
                await GlobalClass.Instance.ShowDialogAsync($"Unable to delete '{item.Title}'", this.XamlRoot);
        }
        private async Task DeleteFilePermanently(MediaPlayerItem item)
        {
            if (ViewModel.MediaPlayerItems.Contains(item))
            {
                ViewModel.MediaPlayerItems.Remove(item);
                //ViewModel.ApplyFilter();
                try
                {
                    File.Delete(Path.Combine(item.MediaPath, item.Title));
                    File.Delete(Path.Combine(item.MediaPath, item.ThumbnailPath));
                    File.Delete(Path.Combine(item.MediaPath, item.ProxyPath));
                    await DeleteAssetAsync(item.MediaId, item.Title, item.MediaPath);
                    await GlobalClass.Instance.AddtoHistoryAsync("Asset Deleted Permanently", $"Deleted asset '{ViewModel.MediaObj.MediaPath}' permanently.");
                }
                catch (IOException ex)
                {
                    await GlobalClass.Instance.ShowDialogAsync($"File Deletion Error !!!\n {ex.ToString()}", this.XamlRoot);
                }
            }
        }
        private async Task DeleteFileFromArchive(MediaPlayerItem item)
        {
            if (ViewModel.MediaPlayerItems.Contains(item))
            {
                ViewModel.MediaPlayerItems.Remove(item);
                try
                {
                    File.Delete(item.ArchivePath);
                    File.Delete(item.ThumbnailPath);
                    File.Delete(Path.Combine(Path.GetDirectoryName(item.ArchivePath), item.Title + "_Proxy.MP4"));
                    await DeleteAssetAsync(item.MediaId, item.Title, item.MediaPath);
                    await GlobalClass.Instance.AddtoHistoryAsync("Asset Deleted Permanently from archive", $"Deleted asset  '{ViewModel.MediaObj.MediaPath}' permanently from archive.");
                }
                catch (IOException ex)
                {
                    await GlobalClass.Instance.ShowDialogAsync($"File Deletion Error !!!\n {ex.ToString()}", this.XamlRoot);
                }
            }
        }
        private async Task<int> DeleteAssetAsync(int assetId, string assetName, string assetPath)
        {
            List<MySqlParameter> parameters = new();
            parameters.Add(new MySqlParameter("@Asset_path", assetPath));
            parameters.Add(new MySqlParameter("@Asset_name", assetName));
            parameters.Add(new MySqlParameter("@Asset_id", assetId));
            var (affectedRows, newId, errorMessage) = await dataAccess.ExecuteNonQuery($"Delete from asset where asset_id=@Asset_id", parameters);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.ShowDialogAsync("Deletion Failed.An unknown error occurred while trying to delete asset.", this.XamlRoot);
                return -1;
            }
            else
            {
                await GlobalClass.Instance.AddtoHistoryAsync("Delete from Library", $"Deleted '{assetPath}\\{assetName}' from library .");
                return affectedRows;
            }
        }
        private void EditAsset_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Authorization_Click(object sender, RoutedEventArgs e)
        {

        }
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var result = await GlobalClass.Instance.ShowDialogAsync($"Do you want to delete selected items ?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");
            if (result == ContentDialogResult.Primary)
            {
                foreach (var item in ViewModel.SelectedItems)
                {
                    await MoveItemToRecycleBinAsync(item);
                }
            }
            ViewModel.ApplyFilter();
        }
        private void DownloadProxy_Click(object sender, RoutedEventArgs e)
        {
            var gridView = sender as GridView;
            MediaPlayerItem mediaPlayerItem = ViewModel.SelectedItems[0];//  (MediaPlayerItem)gridView.SelectedItem;
            if (mediaPlayerItem != null)
            {
                DownloadProxyWindow.ShowWindow(mediaPlayerItem);
                NavFileHistory_Navigate("DownloadHistory", new EntranceNavigationTransitionInfo());
                NavFileHistory.SelectedItem = "DownloadHistory";
            }
        }
        private void DownloadOriginalFile_Click(object sender, RoutedEventArgs e)
        {
            var gridView = sender as GridView;
            MediaPlayerItem mediaPlayerItem = ViewModel.SelectedItems[0];// (MediaPlayerItem)gridView.SelectedItem;
            if (mediaPlayerItem != null)
            {
                DownloadOriginalFile.ShowWindow(mediaPlayerItem);
                NavFileHistory_Navigate("DownloadHistory", new EntranceNavigationTransitionInfo());
                NavFileHistory.SelectedItem = "DownloadHistory";
            }
        }
        private void MakeQC_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SendToArchive_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<MediaPlayerItem> mediaPlayerItems = ViewModel.SelectedItems;
            if (mediaPlayerItems != null && mediaPlayerItems.Count > 0)
                SendToArchiveWindow.ShowWindow(mediaPlayerItems, async () =>
                {
                    await LoadAssetsAsync();
                });
            //AssetWindow.ShowWindow(mediaPlayerItem, ViewModel.MediaPlayerItems, async () =>
            //{
            //    await GetAllAssetsAsync(ViewModel.MediaLibraryObj.BinName);
            //});
        }

        private void SendToTarget_Click(object sender, RoutedEventArgs e)
        {
            SendToTargetWindow.ShowWindow();
        }
        private void Cut_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateMetadata_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void AddAsset_Click(object sender, RoutedEventArgs e)
        {
            string dirName = new DirectoryInfo(((FileSystemItem)FileTreeView.SelectedNode.Content).Path).Name;

            if (dirName == ((FileSystemItem)FileTreeView.SelectedNode.Content).Name)
                ViewModel.MediaLibraryObj.BinName = ((FileSystemItem)FileTreeView.SelectedNode.Content).Path;
            else
                ViewModel.MediaLibraryObj.BinName = Path.Combine(((FileSystemItem)FileTreeView.SelectedNode.Content).Path, ((FileSystemItem)FileTreeView.SelectedNode.Content).Name);
            if (!string.IsNullOrEmpty(ViewModel.MediaLibraryObj.BinName))
            {
                AddNewAssetWindow.ShowWindow(ViewModel, async () => await LoadAssetsAsync());
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync("Select any Data folder", this.XamlRoot);
            }
        }
        private async void AddNewBin_Click(object sender, RoutedEventArgs e)
        {
            if (FileTreeView.SelectedNode == null)
                ViewModel.MediaLibraryObj.BinName = string.Empty;
            if (!string.IsNullOrEmpty(ViewModel.MediaLibraryObj.BinName))
                AddNewBinWindow.ShowWindow(ViewModel);
            else
            {
                await GlobalClass.Instance.ShowDialogAsync("Select any Data folder", this.XamlRoot);
            }
        }

        private async void ViewAsset_Click(object sender, RoutedEventArgs e)
        {
            await ShowAssetWindow();

        }
        private void Rename_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void MediaBinGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var gridView = sender as GridView;
            ViewModel.SelectedItems = new ObservableCollection<MediaPlayerItem>();
            foreach (var item in gridView.SelectedItems)
            {
                MediaPlayerItem mediaPlayerItem = item as MediaPlayerItem;
                ViewModel.SelectedItems.Add(mediaPlayerItem);
            }
            // ViewModel.SelectedItems = (MediaPlayerItem)gridView.SelectedItems;

            // Enable or disable menu items based on selection state
            bool isItemSelected = ViewModel.SelectedItems != null && ViewModel.SelectedItems.Count == 1;
            bool isMultipleItemsSelected = ViewModel.SelectedItems != null && ViewModel.SelectedItems.Count > 1;


            //ViewMenuItem.IsEnabled = isItemSelected;
            //RenameMenuItem.IsEnabled = isItemSelected;
            //DeleteMenuItem.IsEnabled = isMultipleItemsSelected;
            //CutMenuItem.IsEnabled = isMultipleItemsSelected;
            //MakeQCMenuItem.IsEnabled = isItemSelected;
            if (isItemSelected)
            {
                MediaPlayerItem media = ViewModel.SelectedItems[0];
                ViewModel.MediaObj = await GetAssetsAsync(media);

                if (ViewModel.MediaObj != null)
                {
                    //UnarchiveItem.IsEnabled = ViewModel.MediaObj.IsArchived;
                    if (ViewModel.MediaObj.IsArchived)
                        ViewModel.Path = GlobalClass.Instance.ArchivePath;
                    else
                        ViewModel.Path = (ViewModel.SelectedItems[0]).MediaPath;
                }
            }
        }
        private void MediaBinGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MediaBinGridView.Focus(FocusState.Programmatic);
            var gridView = sender as GridView;
            if (gridView == null) return;
            // Get the clicked point relative to the GridView
            var point = e.GetCurrentPoint(gridView).Position;
            // Access the items panel (for layout information)
            var itemsPanel = gridView.ItemsPanelRoot as Panel;
            if (itemsPanel != null)
            {
                foreach (var item in gridView.Items)
                {
                    var container = gridView.ContainerFromItem(item) as GridViewItem;
                    if (container != null)
                    {
                        var transform = container.TransformToVisual(gridView);
                        var containerBounds = transform.TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));

                        if (containerBounds.Contains(point))
                        {
                            return; // Click was on an item, do not clear selection
                        }
                    }
                }

                // Click was not on any item — clear selection
                gridView.SelectedItem = null;
            }
        }
        //private void MediaBinGridView_KeyDown(object sender, KeyRoutedEventArgs e)
        //{
        //    var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        //    bool isCtrlDown = ctrlState.HasFlag(CoreVirtualKeyStates.Down);

        //    if (e.Key == VirtualKey.A && isCtrlDown)
        //    {
        //        MediaBinGridView.SelectAll();
        //        e.Handled = true;
        //    }
        //}


        private async Task ShowAssetWindow()
        {
            if (ViewModel.SelectedItems != null && ViewModel.SelectedItems.Count > 0)
            {
                MediaPlayerItem mediaPlayerItem = await GetAssetsAsync(ViewModel.SelectedItems[0]);
                AssetWindow.ShowWindow(mediaPlayerItem, ViewModel.MediaPlayerItems, async () =>
                {
                    await LoadAssetsAsync();
                });
            }
        }
        private async void MediaBinGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await ShowAssetWindow();
            //if (MediaBinGridView.SelectedItem != null)
            //{
            //    MediaPlayerItem mediaPlayerItem = await GetAssetsAsync(((MediaPlayerItem)MediaBinGridView.SelectedItem));
            //    AssetWindow.ShowWindow(mediaPlayerItem, ViewModel.MediaPlayerItems);
            //}
        }
        private void TypeMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                string selectedOption = menuItem.Text; // Get the text of the selected item
                if (this.FindName("TypeDropDown") is DropDownButton dropDownButton)
                {
                    dropDownButton.Content = selectedOption;
                    ViewModel.FilterType = selectedOption;
                }
            }
        }
        private async Task<Dictionary<string, object>> GetMetadata(string filePath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
            // Get video properties
            var videoProperties = await file.Properties.GetVideoPropertiesAsync();
            Dictionary<string, object> props = new();
            props.Add("Keywords", videoProperties.Keywords);
            props.Add("Title", videoProperties.Title);
            props.Add("Duration", videoProperties.Duration);
            props.Add("Width", videoProperties.Width);
            props.Add("Height", videoProperties.Height);
            props.Add("Bitrate", videoProperties.Bitrate);
            props.Add("Latitude", videoProperties.Latitude);
            props.Add("Longitude", videoProperties.Longitude);
            props.Add("Year", videoProperties.Year);
            props.Add("Orientation", videoProperties.Orientation);
            props.Add("Writers", videoProperties.Writers);
            props.Add("Directors", videoProperties.Directors);
            props.Add("Producers", videoProperties.Producers);
            props.Add("Publisher", videoProperties.Publisher);
            props.Add("Rating", videoProperties.Rating);
            props.Add("SubTitle", videoProperties.Subtitle);
            return props;
        }


        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.FilterTag = TagAutoSuggestBox.Text;
            }
        }
        private void GetAllTags()
        {
            DataTable dt = new();
            dt = dataAccess.GetData("select tag_id,tag_name from tag");
            ViewModel.TagsList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                ViewModel.TagsList.Add(row[1].ToString());
            }
        }
        //private void GetAssetTags()
        //{
        //    DataTable dt = new DataTable();
        //    dt = dataAccess.GetData($"select t.tag_id,t.tag_name from tag t inner join asset_tag a on t.tag_id=a.tag_id where a.asset_id={Asset.Media.MediaId}");
        //    ViewModel.AssetTags.Clear();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        ViewModel.AssetTags.Add(new Tag
        //        {
        //            TagId = Convert.ToInt32(row[0]),
        //            TagName = row[1].ToString(),
        //        });
        //        ViewModel.HasTags = ViewModel.AssetTags.Any();
        //    }
        //}


        private void TagAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length == 1)
                    GetAllTags();
                UpdateSuggestions(sender.Text);
                sender.ItemsSource = FilteredTags; // ✅ Bind updated list
            }
        }

        private void TagAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedTag)
            {
                ViewModel.SelectedTag = selectedTag; // ✅ Update text with chosen tag
            }
        }
        public void UpdateSuggestions(string query)
        {
            FilteredTags.Clear();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var filtered = ViewModel.TagsList.Where(tag => tag.StartsWith(query, StringComparison.OrdinalIgnoreCase));
                foreach (var tag in filtered)
                    FilteredTags.Add(tag);
                if (filtered.Count() == 0)
                    FilteredTags.Add("No results found");
            }
            else
            {
                foreach (var tag in ViewModel.TagsList)
                    FilteredTags.Add(tag);

            }
        }
        private void TagAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (ViewModel != null)
                ViewModel.FilterTag = TagAutoSuggestBox.Text;
        }
        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.FilterTitle = TitleFilterTextbox.Text;
            }
        }

        private void RatingControl_ValueChanged(RatingControl sender, object args)
        {
            //if (viewModel != null)
            //{
            //    viewModel.FilterRating = sender.Value;
            //}
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RatingButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                //viewModel.FilterRating = RatingControl.Value;
            }
        }

        private void KeywordButton_Click(object sender, RoutedEventArgs e)
        {
            //if (ViewModel != null)
            //{
            //    ViewModel.FilterKeyword = KeywordsFilterTextbox.Text;
            //}
        }




        private void RemoveMetadata(MetadataClass metadata)
        {

            var itemToRemove = ViewModel.MetadataList.FirstOrDefault(m => m.MetadataId == metadata.MetadataId);
            if (itemToRemove != null)
            {
                ViewModel.MetadataList.Remove(itemToRemove);
            }
        }

        private void AddMetadata(MetadataClass metadata)
        {
            if (metadata != null && !ViewModel.MetadataList.Any(m => m.MetadataId == metadata.MetadataId))
            {
                ViewModel.MetadataList.Add(new AssetsMetadata() { MetadataId = metadata.MetadataId, Metadata = metadata.Metadata, MetadataType = metadata.MetadataType, IsSelected = true });
            }
        }


        private void GetAllMetadata()
        {
            MetadataClass NewMetadata;
            DataTable dt = new();
            dt = dataAccess.GetData("select metadata_id,metadata_name,metadata_type from metadata");
            ViewModel.AllMetadataList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewMetadata = new MetadataClass();
                NewMetadata.MetadataId = Convert.ToInt32(row[0]);
                NewMetadata.Metadata = row[1].ToString();
                NewMetadata.MetadataType = row[2].ToString();
                ViewModel.AllMetadataList.Add(NewMetadata);
            }
        }
        private void MetadataButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Get the DataContext of the button (which should be AssetsMetadata)
                if (button.DataContext is AssetsMetadata metadataItem)
                {
                    if (ViewModel != null)
                    {
                        ViewModel.FilterMetadata = metadataItem;
                    }
                }
            }
        }
        private void MetadataCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton toggle && toggle.DataContext is MetadataClass item)
                {
                    item.IsSelected = toggle.IsChecked == true;
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (item.IsSelected)
                            AddMetadata(item);
                        else
                            RemoveMetadata(item);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Crash in checkbox handler: " + ex.Message);
            }
        }

        private void MetadataExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {

        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearFilters();
            //TypeDropDown.Content = "Select Type";

        }



        private async void RefreshMediaLibrary_Click(object sender, RoutedEventArgs e)
        {
            await LoadAssetsAsync();

        }

        private async void MediaBinGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var gridView = sender as GridView;
            if (Resources["AssetMenuFlyout"] is MenuFlyout menuFlyout)
            {
                if (e.OriginalSource is FrameworkElement element && element.DataContext is MediaPlayerItem mediaItem)
                {
                    // Assign the correct DataContext
                    foreach (var item in menuFlyout.Items)
                    {
                        item.DataContext = mediaItem;
                    }

                    // Show the flyout at the cursor position
                    menuFlyout.ShowAt(element);
                }
            }
            if (gridView.SelectedItems.Count == 0)
            {
                AddAssetMenuItem.IsEnabled = true;
                AddNewBinMenuItem.IsEnabled = true;
                ViewMenuItem.IsEnabled = false;
                RenameMenuItem.IsEnabled = false;
                DeleteMenuItem.IsEnabled = false;
                UpdateMenuItem.IsEnabled = false;
                AuthorizationMenuItem.IsEnabled = false;
                CutMenuItem.IsEnabled = false;
                CopyMenuItem.IsEnabled = false;
                SendToMenuItem.IsEnabled = false;
                DownloadMenuItem.IsEnabled = false;
                UnarchiveItem.IsEnabled = false;
            }
            else if (gridView.SelectedItems.Count == 1)
            {
                AddAssetMenuItem.IsEnabled = false;
                AddNewBinMenuItem.IsEnabled = false;
                ViewMenuItem.IsEnabled = true;
                RenameMenuItem.IsEnabled = true;
                DeleteMenuItem.IsEnabled = true;
                UpdateMenuItem.IsEnabled = true;
                AuthorizationMenuItem.IsEnabled = true;
                CutMenuItem.IsEnabled = true;
                CopyMenuItem.IsEnabled = true;
                SendToMenuItem.IsEnabled = true;
                DownloadMenuItem.IsEnabled = true;
                UnarchiveItem.IsEnabled = true;
            }
            else if (gridView.SelectedItems.Count > 1)
            {
                AddAssetMenuItem.IsEnabled = false;
                AddNewBinMenuItem.IsEnabled = false;
                ViewMenuItem.IsEnabled = true;
                RenameMenuItem.IsEnabled = false;
                DeleteMenuItem.IsEnabled = true;
                UpdateMenuItem.IsEnabled = false;
                AuthorizationMenuItem.IsEnabled = false;
                CutMenuItem.IsEnabled = true;
                CopyMenuItem.IsEnabled = true;
                SendToMenuItem.IsEnabled = true;
                DownloadMenuItem.IsEnabled = true;
                UnarchiveItem.IsEnabled = true;
            }
            if (ViewModel.MediaLibraryObj.BinName == string.Empty)
                await GlobalClass.Instance.ShowDialogAsync("Select Media Library", this.XamlRoot);
            else
            {
                if (ViewModel.MediaLibraryObj.BinName.StartsWith(GlobalClass.Instance.MediaLibraryPath))
                    AddAssetMenuItem.Visibility = Visibility.Visible;
                else
                    AddAssetMenuItem.Visibility = Visibility.Collapsed;
                if (GlobalClass.Instance.CurrentUser.UserRight.Read)
                {
                    ViewMenuItem.Visibility = Visibility.Visible;
                }
                else
                {
                    ViewMenuItem.Visibility = Visibility.Collapsed;
                }
                if (GlobalClass.Instance.CurrentUser.UserRight.Write)
                {
                    AddAssetMenuItem.Visibility = Visibility.Visible;
                    AddNewBinMenuItem.Visibility = Visibility.Visible;
                    AuthorizationMenuItem.Visibility = Visibility.Visible;
                }
                else
                {
                    AddAssetMenuItem.Visibility = Visibility.Collapsed;
                    AddNewBinMenuItem.Visibility = Visibility.Collapsed;
                    AuthorizationMenuItem.Visibility = Visibility.Collapsed;
                }
                if (GlobalClass.Instance.CurrentUser.UserRight.Edit)
                {
                    UpdateMenuItem.Visibility = Visibility.Visible;
                    CutMenuItem.Visibility = Visibility.Visible;
                    CopyMenuItem.Visibility = Visibility.Visible;
                    PasteMenuItem.Visibility = Visibility.Visible;
                    RenameMenuItem.Visibility = Visibility.Visible;


                }
                else
                {
                    UpdateMenuItem.Visibility = Visibility.Collapsed;
                    CutMenuItem.Visibility = Visibility.Collapsed;
                    CopyMenuItem.Visibility = Visibility.Collapsed;
                    PasteMenuItem.Visibility = Visibility.Collapsed;
                    RenameMenuItem.Visibility = Visibility.Collapsed;

                }
                if (GlobalClass.Instance.CurrentUser.UserRight.Delete)
                {
                    DeleteMenuItem.Visibility = Visibility.Visible;
                }
                else
                {
                    DeleteMenuItem.Visibility = Visibility.Collapsed;
                }
                if (GlobalClass.Instance.CurrentUser.UserRight.OriginalDownload)
                {
                    OriginalMenuItem.Visibility = Visibility.Visible;
                }
                else
                {
                    OriginalMenuItem.Visibility = Visibility.Collapsed;
                }
                if (GlobalClass.Instance.CurrentUser.UserRight.ProxyDownload)
                {
                    ProxyMenuItem.Visibility = Visibility.Visible;
                }
                else
                {
                    ProxyMenuItem.Visibility = Visibility.Collapsed;
                }
                if (GlobalClass.Instance.CurrentUser.UserRight.Archive)
                {
                    ArchiveMenuItem.Visibility = Visibility.Visible;
                    UnarchiveItem.Visibility = Visibility.Visible;

                }
                else
                {
                    ArchiveMenuItem.Visibility = Visibility.Collapsed;
                    UnarchiveItem.Visibility = Visibility.Collapsed;

                }
            }
        }
        //private async Task RestoreMediaItemAsync(RecycleBinMediaPlayerItem mediaItem)
        //{
        //    try
        //    {

        //        string recycleBinFolder = ViewModel.MediaLibraryObj.RecycleBinFolder;
        //        if (Directory.Exists(recycleBinFolder))
        //        {
        //            string mediaLibraryPath = Path.GetDirectoryName(mediaItem.MediaLibraryPath);
        //            if (!Directory.Exists(mediaLibraryPath))
        //                Directory.CreateDirectory(mediaLibraryPath);
        //            string thumbnailPath = Path.GetDirectoryName(mediaItem.ThumbnailPath);
        //            if (!Directory.Exists(thumbnailPath))
        //                Directory.CreateDirectory(thumbnailPath);
        //            string proxyPath = Path.GetDirectoryName(mediaItem.ProxyPath);
        //            if (!Directory.Exists(proxyPath))
        //                Directory.CreateDirectory(proxyPath);
        //            Dictionary<string, object> props = new Dictionary<string, object> { { "@AssetId", mediaItem.MediaId } };
        //            File.Move(mediaItem.RecycleBinPath, mediaItem.MediaLibraryPath);
        //            File.Move(mediaItem.RecycleBinThumbnailPath, mediaItem.ThumbnailPath);
        //            File.Move(mediaItem.RecycleBinProxyPath, mediaItem.ProxyPath);
        //            Dictionary<string, object> propsList = new Dictionary<string, object>
        //            {
        //                {"is_deleted", false },
        //                {"recyclebin_path",string.Empty }
        //            };

        //            int res = await dataAccess.UpdateRecord("Asset", "asset_id", mediaItem.MediaId, propsList);
        //            ViewModel.RecycleBinMediaPlayerItems.Remove(mediaItem);
        //            ViewModel.ApplyFilter();
        //            await GlobalClass.Instance.AddtoHistoryAsync("Restore", $"Restored asset '{mediaItem.Title}' .");
        //        }
        //        else
        //        {
        //            await GlobalClass.Instance.ShowDialogAsync("The file does not exist.", this.Content.XamlRoot);
        //        }
        //    }
        //    catch (IOException ioEx)
        //    {
        //        await GlobalClass.Instance.ShowDialogAsync($"File operation failed: {ioEx.Message}", this.Content.XamlRoot);
        //    }
        //    catch (UnauthorizedAccessException authEx)
        //    {
        //        await GlobalClass.Instance.ShowDialogAsync($"Permission error: {authEx.Message}", this.Content.XamlRoot);
        //    }
        //    catch (Exception ex)
        //    {
        //        await GlobalClass.Instance.ShowDialogAsync($"An error occurred: {ex.Message}", this.Content.XamlRoot);
        //    }
        //}
        private async void UnarchiveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var mediaItem in ViewModel.SelectedItems)
                {
                    App.MainAppWindow.StatusBar.ShowStatus($"Unarchiving {mediaItem}...");
                    string archiveFolder = Path.Combine(ViewModel.MediaLibraryObj.ArchiveServer.ServerName, ViewModel.MediaLibraryObj.ArchiveServer.ArchivePath);
                    if (Directory.Exists(archiveFolder))
                    {
                        if (mediaItem.IsArchived)
                        {
                            string proxyName = Path.GetFileNameWithoutExtension(mediaItem.Title) + "_Proxy.MP4";
                            //string archivePath = Path.Combine(mediaItem.ArchivePath, mediaItem.Title);
                            string destinationPath = Path.Combine(mediaItem.MediaPath, mediaItem.Title);
                            //string archiveProxyPath = Path.Combine(mediaItem.ProxyPath,);
                            string baseRoot = Directory.GetParent(ViewModel.MediaLibraryObj.FileServer.FileFolder).FullName;
                            string relativePath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ThumbnailFolder), Path.GetDirectoryName(mediaItem.ThumbnailPath));
                            string destinationProxyPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ProxyFolder, relativePath);
                            Dictionary<string, object> propsList = new Dictionary<string, object>
                            {   
                                {"is_archived", false },
                                {"archive_path",string.Empty }
                            };
                            if (!Directory.Exists(destinationProxyPath))
                                Directory.CreateDirectory(destinationProxyPath);
                            File.Move(mediaItem.ArchivePath, destinationPath, false);
                            destinationProxyPath = Path.Combine(destinationProxyPath, proxyName);
                            File.Move(mediaItem.ProxyPath, destinationProxyPath, false);

                            int res = await dataAccess.UpdateRecord("Asset", "asset_id", mediaItem.MediaId, propsList);
                            if (res > 0)
                            {
                                await GlobalClass.Instance.AddtoHistoryAsync("Unarchive", $"Unarchived asset '{destinationPath}'.");
                            }
                        }
                        else
                            await GlobalClass.Instance.ShowDialogAsync($"'{mediaItem.MediaSource.LocalPath}' is not archived.", this.Content.XamlRoot);
                    }
                    else
                    {
                        await GlobalClass.Instance.ShowDialogAsync($"'{archiveFolder}' does not exist.", this.Content.XamlRoot);
                    }
                }
                App.MainAppWindow.StatusBar.HideStatus();
                await LoadAssetsAsync();

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

        private void MediaBinGridView_DragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            var pt = GetCursor();

            var oleData = e.Data.As<IDataObject>(); // convert WinRT DataPackageView → OLE IDataObject
            _dropHelper.DragEnter(WinRT.Interop.WindowNative.GetWindowHandle(this), oleData, ref pt, (int)DragDropEffects.Copy);
        }

        private void MediaBinGridView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            var pt = GetCursor();
            _dropHelper.DragOver(ref pt, (int)DragDropEffects.Copy);
        }

        private void MediaBinGridView_DragLeave(object sender, DragEventArgs e)
        {
            _dropHelper.DragLeave();
        }

        private async void MediaBinGridView_Drop(object sender, DragEventArgs e)
        {
            var pt = GetCursor();
            var oleData = e.Data.As<IDataObject>();
            _dropHelper.Drop(oleData, ref pt, (int)DragDropEffects.Copy);

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    MediaBinGridView.Items.Add(item.Name); // Example: add filename to GridView
                }
            }
        }

        private static POINT GetCursor()
        {
            NativeMethods.GetCursorPos(out var pt);
            return pt;
        }



        private void TitleFilterTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || TitleFilterTextbox.Text == "")
            {
                if (ViewModel != null)
                {
                    ViewModel.FilterTitle = TitleFilterTextbox.Text;
                }
            }
        }

        private void KeywordsFilterTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            //if (e.Key == VirtualKey.Enter)
            //{
            //    if (ViewModel != null)
            //    {
            //        ViewModel.FilterKeyword = KeywordsFilterTextbox.Text;
            //    }
            //}
        }
        private void DescriptionFilterTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            ////if (e.Key == VirtualKey.Enter)
            ////{
            ////    if (ViewModel != null)
            ////    {
            ////        ViewModel.FilterDescription = DescriptionFilterTextbox.Text;
            ////    }
            ////}
        }

        private void OnMetadataSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void UpdatePageButtons()
        {
            PageButtonsPanel.Children.Clear();
            if (ViewModel.TotalPages > 1)
            {
                for (int i = 1; i <= ViewModel.TotalPages; i++)
                {
                    var btn = new Button
                    {
                        Content = i.ToString(),
                        VerticalAlignment = VerticalAlignment.Center,
                        Height = 30,
                        Width = 37,
                        BorderThickness = new Thickness(0),
                        Background = new SolidColorBrush(Colors.Transparent),
                        Foreground = (i - 1 == ViewModel.SelectedPageIndex)
                            ? new SolidColorBrush(Colors.Blue)
                            : new SolidColorBrush(Colors.White),
                        Tag = i  // Store the page index
                    };

                    btn.Click += PageButton_Click;
                    PageButtonsPanel.Children.Add(btn);
                }
            }
            SetVisibility(ViewModel.SelectedPageIndex + 1);

        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int pageIndex)
            {
                //SetVisibility(pageIndex);
                ViewModel.SelectedPageIndex = pageIndex;
                ViewModel.GoToPageCommand.Execute(pageIndex);
                UpdatePageButtons(); // Refresh highlight
            }
        }
        private void SetVisibility(int pageIndex)
        {
            if (ViewModel.TotalPages == 0)
            {
                ViewModel.IsPrevVisible = false;
                ViewModel.IsNextVisible = false;
            }
            else if (ViewModel.TotalPages == 1)
            {
                ViewModel.IsPrevVisible = false;
                ViewModel.IsNextVisible = false;
            }
            else
            {
                if (pageIndex == 1)
                {
                    ViewModel.IsPrevVisible = false;
                    ViewModel.IsNextVisible = true;
                }
                else if (pageIndex == ViewModel.TotalPages)
                {
                    ViewModel.IsNextVisible = false;
                    ViewModel.IsPrevVisible = true;
                }
                else
                {
                    ViewModel.IsPrevVisible = true;
                    ViewModel.IsNextVisible = true;
                }
            }
        }
        public async void CreateSubfolder(string parentDirectory, string subfolderName)
        {
            // Combine the parent directory path and the subfolder name
            string subfolderPath = Path.Combine(parentDirectory, subfolderName);

            // Check if the directory already exists
            if (!Directory.Exists(subfolderPath))
            {
                // Create the subfolder
                Directory.CreateDirectory(subfolderPath);
            }
            else
            {
                // Optional: Handle the case where the subfolder already exists
                await GlobalClass.Instance.ShowDialogAsync("Bin already exists.", XamlRoot);
            }
        }

        private void AddBin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is FileSystemItem fileSystemItem)
            {
                if (_rightClickedItem != null)
                {
                    var newBin = new FileSystemItem
                    {
                        Name = "New Bin",
                        Path = System.IO.Path.Combine(_rightClickedItem.Path, "New Bin"),
                        IsDirectory = true,
                        IsEditing = true,
                        OriginalName = "New Bin"
                    };
                    _rightClickedItem.Children.Add(newBin);
                    _rightClickedItem.IsExpanded = true; // auto-expand parent to show new folder
                    isNewBin = true;

                }
            }
        }
        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Focus(FocusState.Programmatic);
                tb.SelectAll();
            }
        }
        private async void RenameFileSystemItem(FileSystemItem item, string? newName)
        {
            if (item == null || newName == null) return;

            string trimmedName = newName.Trim();
            string oldName = item.OriginalName;
            var parent = FindParent(ViewModel.FileSystemItems, item);
            string parentPath = parent?.Path ?? "";

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                item.Name = oldName;
                item.IsEditing = false;
                return;
            }

            item.Name = trimmedName;

            if (IsDuplicateName(item))
            {
                item.Name = oldName;
                item.IsEditing = false;
                return;
            }

            string oldPath = item.Path;
            string newPath = Path.Combine(parentPath, trimmedName);

            try
            {
                // ✅ Rename folder on disk
                if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                    string relativeOldPath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder), oldPath);  // "Songs\Hindi"
                    string proxyOldPath = Path.Combine(ViewModel.MediaLibraryObj.ProxyFolder, relativeOldPath);
                    string thumbnailOldPath = Path.Combine(ViewModel.MediaLibraryObj.ThumbnailFolder, relativeOldPath);
                    string relativeNewPath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder), newPath);  // "Songs\Hindi"
                    string proxyNewPath = Path.Combine(ViewModel.MediaLibraryObj.ProxyFolder, relativeNewPath);
                    string thumbnailNewPath = Path.Combine(ViewModel.MediaLibraryObj.ThumbnailFolder, relativeNewPath);
                    if (Directory.Exists(thumbnailOldPath))
                        Directory.Move(thumbnailOldPath, thumbnailNewPath);
                    if (Directory.Exists(proxyOldPath))
                        Directory.Move(proxyOldPath, proxyNewPath);
                    await UpdateBinNameAsync(relativeOldPath, relativeNewPath);
                }
            }
            catch (Exception ex)
            {
                await GlobalClass.Instance.ShowDialogAsync($"Rename failed: {ex.Message}", XamlRoot);
                item.Name = oldName;
                item.IsEditing = false;
                return;
            }

            item.Path = newPath;
            item.IsEditing = false;
            if (!Directory.Exists(newPath))
            {
                try
                {
                    CreateSubfolder(parentPath, trimmedName);
                }
                catch (Exception ex)
                {
                    await GlobalClass.Instance.ShowDialogAsync("Failed to create folder: " + ex.Message, XamlRoot);
                }
            }
        }
        private async Task UpdateBinNameAsync(string oldRelativePath, string newRelativePath)
        {
            try
            {
                string oldPath = PathHelper.NormalizeForMySql(oldRelativePath);
                string newPath = PathHelper.NormalizeForMySql(newRelativePath);

                string updateSql = @"
    UPDATE asset
    SET relative_path = CONCAT(@NewPath, SUBSTRING(relative_path, CHAR_LENGTH(@OldPath) + 1))
    WHERE relative_path = @OldPath
       OR relative_path LIKE CONCAT(@OldPath, '\\%') ESCAPE '';";

                var parameters = new List<MySqlParameter>
        {
            new MySqlParameter("@OldPath", oldPath),
            new MySqlParameter("@NewPath", newPath)
        };
                Console.WriteLine($"OldPath = {oldPath}");
                Console.WriteLine($"OldPathLike = {PathHelper.ToMySqlLikePattern(oldPath)}");
                var (affectedRows, _, errorMessage) = await dataAccess.ExecuteNonQuery(updateSql, parameters);

                if (affectedRows == 0)
                {
                    //await GlobalClass.Instance.ShowDialogAsync("No rows matched. Check path casing.", XamlRoot);
                }
            }
            catch (Exception ex)
            {
                await GlobalClass.Instance.ShowDialogAsync($"Database update failed: {ex.Message}", XamlRoot);
            }
        }

        //private async Task UpdateBinNameAsync(string oldRelativePath, string newRelativePath)
        //{
        //    try
        //    {
        //        string oldPath = PathHelper.NormalizeForMySql(oldRelativePath);
        //        string newPath = PathHelper.NormalizeForMySql(newRelativePath);

        //        string updateSql = @"UPDATE asset
        //                             SET relative_path = CONCAT(@NewPath, SUBSTRING(relative_path, CHAR_LENGTH(@OldPath) + 1))
        //                             WHERE relative_path = @OldPath
        //                             OR relative_path LIKE @OldPathLike;";

        //        var parameters = new List<MySqlParameter>
        //        {
        //            new MySqlParameter("@OldPath", oldPath),
        //            new MySqlParameter("@NewPath", newPath),
        //            new MySqlParameter("@OldPathLike", PathHelper.ToMySqlLikePattern(oldPath))
        //        };

        //        var (affectedRows, _, errorMessage) = await dataAccess.ExecuteNonQuery(updateSql, parameters);

        //        if (affectedRows > 0)
        //        {
        //            // Optional logging
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await GlobalClass.Instance.ShowDialogAsync(
        //            $"Database update failed: {ex.Message}",
        //            XamlRoot
        //        );
        //    }
        //}

        //private async Task UpdateBinNameAsync(string oldPath, string newPath)
        //{
        //    try
        //    {
        //        string escapedOldPath = oldPath.Replace(@"\", @"\\") + "%";
        //        string updateSql = @"
        //                            UPDATE asset 
        //                            SET asset_path = CONCAT(@NewPath, SUBSTRING(asset_path, CHAR_LENGTH(@OldPath) + 1))
        //                            WHERE asset_path LIKE @OldPathLike;
        //                        ";

        //        var parameters = new List<MySqlParameter>
        //        {
        //            new MySqlParameter("@OldPath", oldPath),
        //            new MySqlParameter("@NewPath", newPath),
        //            new MySqlParameter("@OldPathLike", escapedOldPath)
        //        };

        //        var (affectedRows, _, errorMessage) = await dataAccess.ExecuteNonQuery(updateSql, parameters);

        //        if (affectedRows > 0)
        //        {
        //            // Optional: log or confirm
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await GlobalClass.Instance.ShowDialogAsync($"Database update failed: {ex.Message}", XamlRoot);
        //    }

        //}


        private async void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
       {
            if (e.Key == VirtualKey.Enter && sender is TextBox tb && tb.DataContext is FileSystemItem item)
            {
                RenameFileSystemItem(item, tb.Text);
                e.Handled = true;
                if (isNewBin)
                    await GlobalClass.Instance.AddtoHistoryAsync("Add New Bin", $"New bin '{tb.Text}' added");
                else
                    await GlobalClass.Instance.AddtoHistoryAsync("Rename Bin", $"Renamed bin from '{item.OriginalName} to '{tb.Text}' ");
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is FileSystemItem item)
            {
                if (!Directory.Exists(item.Path))
                    RenameFileSystemItem(item, tb.Text);
            }
        }


        private bool IsDuplicateName(FileSystemItem item)
        {
            var parent = FindParent(ViewModel.FileSystemItems, item);

            if (parent != null)
            {
                return parent.Children.Any(child => child != item && child.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return ViewModel.FileSystemItems.Any(child => child != item && child.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        private FileSystemItem? FindParent(ObservableCollection<FileSystemItem> items, FileSystemItem target)
        {
            foreach (var item in items)
            {
                // Direct child match
                if (item.Children.Contains(target))
                {
                    return item;
                }

                // Recursively check in children
                var found = FindParent(item.Children, target);
                if (found != null)
                {
                    return found;
                }
            }

            return null; // Not found
        }
        private void RenameBin_Click(object sender, RoutedEventArgs e)
        {
            if (_rightClickedItem != null)
            {
                _rightClickedItem.OriginalName = _rightClickedItem.Name; //  Backup name
                _rightClickedItem.IsEditing = true;                      //  Enable editing
                isNewBin = false;                                                        // Wait for UI to update and then focus/select text manually
                App.UIDispatcherQueue.TryEnqueue(async () =>
                {
                    await Task.Delay(10); // Optional: allow layout/render

                    if (FindTreeViewTextBox(FileTreeView.ContainerFromItem(_rightClickedItem)) is TextBox tb)
                    {
                        tb.Focus(FocusState.Programmatic);
                        tb.SelectAll();
                    }
                });

            }
        }
        private TextBox? FindTreeViewTextBox(DependencyObject container)
        {
            if (container == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
            {
                var child = VisualTreeHelper.GetChild(container, i);

                if (child is TextBox tb && tb.Visibility == Visibility.Visible)
                    return tb;

                var result = FindTreeViewTextBox(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private async void DeleteBin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is FileSystemItem itemToDelete)
            {
                var result = await GlobalClass.Instance.ShowDialogAsync($"Do you want to delete the bin '{itemToDelete.Name}' ?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");

                if (result == ContentDialogResult.Primary)
                {
                    var parent = FindParent(ViewModel.FileSystemItems, itemToDelete);
                    var collection = parent?.Children ?? ViewModel.FileSystemItems;

                    if (collection.Contains(itemToDelete))
                    {
                        // ✅ Delete from disk if it exists

                        if (itemToDelete.IsDirectory && Directory.Exists(itemToDelete.Path))
                        {
                            bool isDirectoryEmpty = !Directory.EnumerateFileSystemEntries(itemToDelete.Path).Any();

                            if (isDirectoryEmpty)
                            {
                                try
                                {
                                    await DeleteDirectoriesAsync(itemToDelete);
                                    collection.Remove(itemToDelete);
                                }
                                catch (Exception ex)
                                {
                                    await GlobalClass.Instance.ShowDialogAsync($"Deletion failed {ex.Message}", XamlRoot);
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    await DeleteDirectoriesAsync(itemToDelete);
                                    collection.Remove(itemToDelete);
                                }
                                catch (Exception ex)
                                {
                                    await GlobalClass.Instance.ShowDialogAsync($"Deletion failed {ex.Message}", XamlRoot);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
        private async Task DeleteDirectoriesAsync(FileSystemItem itemToDelete)
        {
            var parentBin = FileTreeView.SelectedNode.Parent;
            if (Directory.Exists(itemToDelete.Path))
            {
                string deletePath = itemToDelete.Path;

                var relatedAssets = ViewModel.MediaPlayerItems
                    .Where(asset => asset.MediaPath.StartsWith(deletePath, StringComparison.OrdinalIgnoreCase))
                    .ToList(); // ✅ copy to avoid modification during loop

                foreach (var asset in relatedAssets)
                {
                    await MoveItemToRecycleBinAsync(asset);
                }
                Directory.Delete(itemToDelete.Path, true); // true = recursive
                string relativePath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder), itemToDelete.Path);  // "Songs\Hindi"
                string proxyPath = Path.Combine(ViewModel.MediaLibraryObj.ProxyFolder, relativePath);
                string thumbnailPath = Path.Combine(ViewModel.MediaLibraryObj.ThumbnailFolder, relativePath);
                if (Directory.Exists(proxyPath))
                    Directory.Delete(proxyPath, true);
                if (Directory.Exists(thumbnailPath))
                    Directory.Delete(thumbnailPath, true);
                FileTreeView.SelectedNode = parentBin;
                await GlobalClass.Instance.AddtoHistoryAsync("Delete Bin", $"Deleted Bin '{itemToDelete.Name}' ");
            }
        }
        public FileSystemItem FindFileSystemItemByPath(ObservableCollection<FileSystemItem> items, string path)
        {
            foreach (var item in items)
            {
                if (item.Path == path)
                    return item;
                // Recursively search in children
                var found = FindFileSystemItemByPath(item.Children, path);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void TagsFlyout_Opening(object sender, object e)
        {
            if (sender is Flyout flyout && flyout.Content is ListView listView)
            {
                bool hasValidTags = listView.Items.Count > 0 &&
                                    listView.Items.Cast<object>()
                                                   .Any(item => !string.IsNullOrWhiteSpace(item?.ToString()));

                if (!hasValidTags)
                {
                    (sender as Flyout)?.Hide();
                }
            }
        }

        private async void CustomMediaPlayer_DeleteRequested(object sender, CustomBinMedia e)
        {
            await DeleteMediaItemAsync(e.MediaItem);
        }

        private async void CustomMediaPlayer_PlayButtonClicked(object sender, CustomBinMedia e)
        {
            await ShowAssetWindow();
        }


    }
    public class MediaPlayerItem : ObservableObject, IEquatable<MediaPlayerItem>
    {
        private Uri mediaSource;
        private string relativePath;
        private string thumbnailPath;
        private string title;
        private string durationString;
        private TimeSpan duration;
        private string proxyPath;
        private int mediaId;
        private string originalPath;
        private string version;
        private string type;
        private string createdUser;
        private DateOnly creationDate;
        private string updatedUser;
        private DateTime lastUpdated;
        private double size;
        private string description;
        private string mediaPath;
        private List<string> tags;
        private List<string> keywords;
        private double rating = 0;
        private bool isArchived = false;

        public int MediaId
        {
            get => mediaId;
            set => SetProperty(ref mediaId, value);
        }
        public Uri MediaSource
        {
            get => mediaSource;
            set => SetProperty(ref mediaSource, value);
        }
        public string RelativePath
        {
            get => relativePath;
            set => SetProperty(ref relativePath, value);
        }
        public string MediaPath
        {
            get => mediaPath;
            set => SetProperty(ref mediaPath, value);
        }
        public string ThumbnailPath
        {
            get => thumbnailPath;
            set => SetProperty(ref thumbnailPath, value);
        }
        public string ProxyPath
        {
            get => proxyPath;
            set => SetProperty(ref proxyPath, value);
        }
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }
        public TimeSpan Duration
        {
            get => duration;
            set => SetProperty(ref duration, value);
        }
        public string DurationString
        {
            get => durationString;
            set => SetProperty(ref durationString, value);
        }
        public string OriginalPath
        {
            get => originalPath;
            set => SetProperty(ref originalPath, value);
        }
        public string Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        public string Type
        {
            get => type;
            set => SetProperty(ref type, value);
        }

        public string CreatedUser
        {
            get => createdUser;
            set => SetProperty(ref createdUser, value);
        }
        public DateOnly CreationDate
        {
            get => creationDate;
            set => SetProperty(ref creationDate, value);
        }
        public string UpdatedUser
        {
            get => updatedUser;
            set => SetProperty(ref updatedUser, value);
        }
        public DateTime LastUpdated
        {
            get => lastUpdated;
            set => SetProperty(ref lastUpdated, value);
        }
        public double Size
        {
            get => size;
            set => SetProperty(ref size, value);
        }
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }
        public double Rating
        {
            get => rating;
            set => SetProperty(ref rating, value);
        }
        public List<string> Tags
        {
            get => tags;
            set => SetProperty(ref tags, value);
        }
        public List<string> Keywords
        {
            get => keywords;
            set => SetProperty(ref keywords, value);
        }
        public bool IsArchived
        {
            get => isArchived;
            set { SetProperty(ref isArchived, value); }
        }
        public string ArchivePath
        {
            get => archivePath;
            set { SetProperty(ref archivePath, value); }
        }
        public bool IsDeleted
        {
            get => isDeleted;
            set { SetProperty(ref isDeleted, value); }
        }
        public string RecycleBinPath
        {
            get => recycleBinPath;
            set { SetProperty(ref recycleBinPath, value); }
        }
        private ObservableCollection<AssetsMetadata> _assetMetadataList;
        private string archivePath;
        private FileServer fileServer;
        private bool isDeleted;
        private string recycleBinPath;

        public ObservableCollection<AssetsMetadata> AssetMetadataList
        {
            get => _assetMetadataList;
            set => SetProperty(ref _assetMetadataList, value);
        }
        public MediaPlayerItem()
        {
            AssetMetadataList = new ObservableCollection<AssetsMetadata>();
        }
        // Override Equals
        public override bool Equals(object obj)
        {
            return Equals(obj as MediaPlayerItem);
        }

        // Implement IEquatable<T>
        public bool Equals(MediaPlayerItem other)
        {
            if (other == null)
                return false;

            return MediaSource == other.MediaSource && MediaPath == other.MediaPath && ThumbnailPath == other.ThumbnailPath && ProxyPath == other.ProxyPath && Title == other.Title && DurationString == other.DurationString;
        }

        // Override GetHashCode
        public override int GetHashCode()
        {
            return HashCode.Combine(mediaSource, ThumbnailPath, ProxyPath, Title, DurationString);
        }
    }
    public class MediaLibrary : ObservableObject
    {
        private string binName = string.Empty;
        private string proxyFolder = string.Empty;
        private int fileCount = 0;

        public string BinName
        {
            get => binName;
            set => SetProperty(ref binName, value);
        }
        public string ProxyFolder
        {
            get => proxyFolder;
            set => SetProperty(ref proxyFolder, value);
        }
        public string ThumbnailFolder
        {
            get => thumbnailFolder;
            set => SetProperty(ref thumbnailFolder, value);
        }
        public string RecycleFolder
        {
            get => recycleFolder;
            set => SetProperty(ref recycleFolder, value);
        }
        public string RecycleThumbnailFolder
        {
            get => recycleThumbnailFolder;
            set => SetProperty(ref recycleThumbnailFolder, value);
        }
        public string RecycleProxyFolder
        {
            get => recycleProxyFolder;
            set => SetProperty(ref recycleProxyFolder, value);
        }
        public int FileCount
        {
            get => fileCount;
            set => SetProperty(ref fileCount, value);
        }
        public FileServer FileServer
        {
            get => fileServer;
            set { SetProperty(ref fileServer, value); }
        }
        public ArchiveServer ArchiveServer
        {
            get => archiveServer;
            set { SetProperty(ref archiveServer, value); }
        }
        private ObservableCollection<string> _recentFiles = new ObservableCollection<string>();
        private FileServer fileServer;
        private string thumbnailFolder;
        private ArchiveServer archiveServer;
        private string recycleFolder;
        private string recycleThumbnailFolder;
        private string recycleProxyFolder;
        public ObservableCollection<string> RecentFiles
        {
            get => _recentFiles;
            set => SetProperty(ref _recentFiles, value);
        }
    }
    public class MediaLibraryViewModel : ObservableObject
    {
        private MediaPlayerItem media;
        private MediaLibrary mediaLibrary;
        private string path;
        private string _filterTitle = string.Empty;
        private string _filterType = string.Empty;
        private string _filterTag = string.Empty;
        private string _filterKeyword = string.Empty;
        private string _filterDescription = string.Empty;
        private double _filterRating = -1;
        private string assetCount = string.Empty;
        private MetadataClass selectedMetadata;
        public string RatingCaption { get; set; } = "& Up";
        private ObservableCollection<string> tagsList = new();
        private int pageSize;
        private int _currentPageIndex;
        private int _selectedPageIndex;
        private ObservableCollection<MediaPlayerItem> selectedItems;
        private List<MediaPlayerItem> _filteredItems = new();
        private MediaViewMode _currentViewMode = MediaViewMode.Grid;
        public MediaViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set => SetProperty(ref _currentViewMode, value); // Assuming ObservableObject
        }

        public ObservableCollection<MediaPlayerItem> SelectedItems
        {
            get => selectedItems;
            set { SetProperty(ref selectedItems, value); }
        }
        public string Path
        {
            get => path;
            set { SetProperty(ref path, value); }
        }
        public string AssetCount
        {
            get => assetCount;
            set { SetProperty(ref assetCount, value); }
        }
        public string FilterType
        {
            get => _filterType;
            set { SetProperty(ref _filterType, value); ApplyFilter(); }
        }
        public string FilterTitle
        {
            get => _filterTitle;
            set { SetProperty(ref _filterTitle, value); ApplyFilter(); }
        }
        public string FilterTag
        {
            get => _filterTag;
            set { SetProperty(ref _filterTag, value); ApplyFilter(); }
        }
        public string FilterKeyword
        {
            get => _filterKeyword;
            set { SetProperty(ref _filterKeyword, value); ApplyFilter(); }
        }

        public string FilterDescription
        {
            get => _filterDescription;
            set { SetProperty(ref _filterDescription, value); ApplyFilter(); }
        }
        public double FilterRating
        {
            get => _filterRating;
            set { SetProperty(ref _filterRating, value); ApplyFilter(); }
        }
        public AssetsMetadata FilterMetadata
        {
            get => _filterMetadata;
            set { SetProperty(ref _filterMetadata, value); ApplyFilter(); }
        }
        public ObservableCollection<string> TagsList
        {
            get => tagsList;
            set { SetProperty(ref tagsList, value); ApplyFilter(); }
        }
        public string SelectedTag
        {
            get => selectedTag;
            set { SetProperty(ref selectedTag, value); ApplyFilter(); }
        }
        private ObservableCollection<MetadataClass> _metadataList = new();
        private AssetsMetadata _filterMetadata;
        private ObservableCollection<MetadataClass> allMetadataList = new();
        private string selectedTag;
        private ObservableCollection<MediaPlayerItem> pagedMediaPlayerItems = new();
        private ObservableCollection<MediaPlayerItem> mediaPlayerItems = new();

        public ObservableCollection<MetadataClass> MetadataList
        {
            get => _metadataList;
            set => SetProperty(ref _metadataList, value);
        }
        public ObservableCollection<MetadataClass> AllMetadataList
        {
            get => allMetadataList;
            set => SetProperty(ref allMetadataList, value);
        }
        public int PageSize
        {
            get => pageSize;
            set { SetProperty(ref pageSize, value); }
        }
        public int TotalPages => (int)Math.Ceiling((double)_filteredItems.Count / PageSize);
        private ObservableCollection<int> _totalPagesList = new();
        private bool isPrevVisible = true;
        private bool isNextVisible = true;

        public ObservableCollection<int> TotalPagesList
        {
            get => _totalPagesList;
            set => SetProperty(ref _totalPagesList, value);
        }
        public event Action? PaginationUpdated;



        public bool IsPrevVisible
        {
            get => isPrevVisible;
            set => SetProperty(ref isPrevVisible, value);
        }
        public bool IsNextVisible
        {
            get => isNextVisible;
            set => SetProperty(ref isNextVisible, value);
        }

        public string PageDisplayText { get; set; }
        public MediaLibraryViewModel()
        {
            MediaObj = new MediaPlayerItem();
            MediaLibraryObj = new MediaLibrary();
            ApplyFilter();
        }
        public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }

        // public ObservableCollection<MediaPlayerItem> AllMediaPlayerItems { get; set; } = new();
        public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get => mediaPlayerItems; set => SetProperty(ref mediaPlayerItems, value); }
        public MediaPlayerItem MediaObj { get => media; set => SetProperty(ref media, value); }
        public MediaLibrary MediaLibraryObj { get => mediaLibrary; set => SetProperty(ref mediaLibrary, value); }
        public void ApplyFilter()
        {
            _filteredItems = MediaPlayerItems.Where(item =>
             (string.IsNullOrEmpty(FilterType) || item.Type.Equals(FilterType, StringComparison.OrdinalIgnoreCase)) &&
             (string.IsNullOrEmpty(FilterTitle) || item.Title.Contains(FilterTitle, StringComparison.OrdinalIgnoreCase)) &&
             (string.IsNullOrEmpty(FilterTag) || item.Tags.Any(t => t.Contains(FilterTag, StringComparison.OrdinalIgnoreCase))) &&
             (string.IsNullOrEmpty(FilterKeyword) || item.Keywords.Any(k => k.Contains(FilterKeyword, StringComparison.OrdinalIgnoreCase))) &&
             (string.IsNullOrEmpty(FilterDescription) || item.Description.Contains(FilterDescription, StringComparison.OrdinalIgnoreCase)) &&
             //(!(FilterRating < 0) || item.Rating < FilterRating) &&
             (FilterMetadata == null || item.AssetMetadataList.Any(m =>
             m.MetadataId == FilterMetadata.MetadataId &&
             (string.IsNullOrEmpty(FilterMetadata.MetadataValue) || m.MetadataValue.Contains(FilterMetadata.MetadataValue, StringComparison.OrdinalIgnoreCase))
            ))
         ).ToList();

            SelectedPageIndex = 0;
            UpdatePagedMediaItems();
            MediaLibraryObj.FileCount = _filteredItems.Count;
            AssetCount = $"{MediaLibraryObj.FileCount} results found";
            OnPropertyChanged(nameof(TotalPages)); // In case filter changes page count
            PaginationUpdated?.Invoke();
        }

        public int SelectedPageIndex
        {
            get => _selectedPageIndex;
            set
            {
                SetProperty(ref _selectedPageIndex, value);
                UpdatePagedMediaItems();
                PaginationUpdated?.Invoke();
            }
        }

        public ObservableCollection<MediaPlayerItem> PagedMediaPlayerItems
        {
            get => pagedMediaPlayerItems;
            set => SetProperty(ref pagedMediaPlayerItems, value);
        }
        private void UpdatePagedMediaItems()
        {
            var items = _filteredItems
                .Skip(SelectedPageIndex * PageSize)
                .Take(PageSize)
                .ToList();

            PagedMediaPlayerItems.Clear();
            foreach (var item in items)
            {
                PagedMediaPlayerItems.Add(item);
            }

            PaginationUpdated?.Invoke(); // In case you want to refresh buttons
        }

        public void ClearFilters()
        {
            FilterType = string.Empty;
            FilterTitle = string.Empty;
            FilterTitle = string.Empty;
            FilterTag = string.Empty;
            FilterKeyword = string.Empty;
            FilterDescription = string.Empty;
            FilterRating = -1; // Assuming a negative rating means "no filter"
            FilterMetadata = null; // Reset metadata filter
            SelectedTag = string.Empty;
            pagedMediaPlayerItems.Clear();
            UpdatePagedMediaItems();
        }
        public ICommand GoToPageCommand => new RelayCommand<int>(page =>
        {
            int zeroBasedPage = page - 1;
            if (zeroBasedPage >= 0 && zeroBasedPage < TotalPages)
                SelectedPageIndex = zeroBasedPage;
        });

        public ICommand GoToPreviousPageCommand => new RelayCommand(() =>
        {
            if (SelectedPageIndex > 0)
                SelectedPageIndex--;
        });

        public ICommand GoToNextPageCommand => new RelayCommand(() =>
        {
            if (SelectedPageIndex < TotalPages - 1)
                SelectedPageIndex++;
        });

    }


    public partial class AssetsMetadata : MetadataClass
    {
        private int assetId;
        private string metadataValue;
        public int AssetId
        {
            get => assetId;
            set => SetProperty(ref assetId, value);
        }
        public string MetadataValue
        {
            get => metadataValue;
            set => SetProperty(ref metadataValue, value);
        }
        public AssetsMetadata()
        {
        }
    }

    public class FileSystemItem : ObservableObject
    {
        private string name;
        private string path;
        private bool isDirectory;
        private bool isExpanded;
        private ObservableCollection<FileSystemItem> children = new();
        private bool isEditing;
        private string originalName;
        private bool isRoot;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }
        public string Path
        {
            get => path;
            set => SetProperty(ref path, value);
        }
        public bool IsDirectory
        {
            get => isDirectory;
            set => SetProperty(ref isDirectory, value);
        }
        public bool IsExpanded
        {
            get => isExpanded;
            set => SetProperty(ref isExpanded, value);
        }
        public Boolean IsEditing
        {
            get => isEditing;
            set => SetProperty(ref isEditing, value);
        }
        public string OriginalName
        {
            get => originalName;
            set => SetProperty(ref originalName, value);
        }
        public bool IsRoot
        {
            get => isRoot;
            set => SetProperty(ref isRoot, value);
        }
        public ObservableCollection<FileSystemItem> Children
        {
            get => children;
            set => SetProperty(ref children, value);
        }
    }

    public class BoolToGlyphConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            if (value is bool isDirectory)
            {
                // Use parameter to allow overriding default glyphs
                var icons = parameter as string;
                var parts = icons?.Split('|');
                if (parts?.Length == 2)
                {
                    return isDirectory ? parts[0] : parts[1];
                }
                return isDirectory ? "\uf07b" : "\uf15b";
            }
            return "\uE10E"; // Default glyph
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // One-way binding only
        }
    }
    public enum MediaViewMode
    {
        Grid,
        List,
        Details
    }
    public class MediaTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GridViewTemplate { get; set; }
        public DataTemplate ListViewTemplate { get; set; }
        public DataTemplate TileViewTemplate { get; set; }
        public DataTemplate DetailsViewTemplate { get; set; }

        public string CurrentViewMode { get; set; } = "Grid"; // Default

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return CurrentViewMode switch
            {
                "Grid" => GridViewTemplate,
                "List" => ListViewTemplate,
                "Tile" => TileViewTemplate,
                "Details" => DetailsViewTemplate,
                _ => GridViewTemplate,
            };
        }
    }



}

