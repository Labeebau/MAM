using MAM.Data;
using MAM.UserControls;
using MAM.Utilities;
using MAM.ViewModels;
using MAM.Views.MediaBinViews.AssetMetadata;
using MAM.Windows;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaLibraryPage : Page
    {
        private DataAccess dataAccess = new DataAccess();
        private bool _isDraggingLeftVertical;
        private bool _isDraggingRightVertical;

        private bool _isDraggingHorizontal;
        private double _originalSplitterPosition;

        public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }
        public ObservableCollection<string> TagsList { get; set; } = new();
        public ObservableCollection<string> FilteredTags { get; set; } = new();

        private readonly List<string> ExcludedFolders = new List<string> { "Proxy" };

        public MediaLibrary MediaLibrary = new MediaLibrary();
        public MediaLibraryViewModel viewModel;

        public MediaLibraryPage()
        {
            this.InitializeComponent();

            FileSystemItems = new ObservableCollection<FileSystemItem>();
            // Add root item
            var root = CreateFileSystemItem(@"F:\MAM", true);
            FileSystemItems.Add(root);
            //MediaPlayerItems = new ObservableCollection<MediaPlayerItem>();
            PopulateComboBox();
            // Bind the collection to the GridView
            viewModel = new MediaLibraryViewModel();

            DataContext = this;
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
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        // Skip folders named "Proxy"
                        if (System.IO.Path.GetFileName(dir).Equals("Proxy", StringComparison.OrdinalIgnoreCase))
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



        private void CustomMediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            //if (sender is CustomMedia customMediaPlayer && customMediaPlayer.DataContext is MediaPlayerItem item)
            //{
            //   // customMediaPlayer.SetMediaSource(item.MediaSource);
            //    //customMediaPlayer.SetProps(item.Title, item.Duration);
            //}
            if (sender is CustomBinMedia customMedia && customMedia.DataContext is MediaPlayerItem mediaItem)
            {
                customMedia.DeleteRequested += async (s, args) =>
                {
                    await DeleteMediaItemAsync(args.MediaItem);
                };
            }
        }
        private void VerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Set the resize cursor
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        }
        // Vertical Splitter events
        private void VerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingLeftVertical = true;
            _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.X;

            // Capture the pointer so we continue receiving events even when the pointer leaves the splitter
            LeftVerticalSplitter.CapturePointer(e.Pointer);
            // Set the resize cursor (for vertical resizing)
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            ProtectedCursor = inputCursor;
        }

        private void VerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingLeftVertical)
            {
                // Move the vertical splitter and resize the panels
                double currentPosition = e.GetCurrentPoint(MainCanvas).Position.X;
                double delta = currentPosition - _originalSplitterPosition;
                double newLeft = Canvas.GetLeft(LeftVerticalSplitter) + delta;

                // Ensure the splitter stays within bounds
                if (newLeft > 300 && newLeft < MainCanvas.ActualWidth - 1200)
                {
                    Canvas.SetLeft(LeftVerticalSplitter, newLeft);
                    Canvas.SetLeft(CenterPanel, newLeft + LeftVerticalSplitter.ActualWidth);
                    LeftPanel.Width = newLeft;
                    CenterPanel.Width = MainCanvas.ActualWidth - newLeft - LeftVerticalSplitter.ActualWidth - RightPanel.ActualWidth;
                    //MainCanvas.ActualWidth- (newLeft+LeftVerticalSplitter.ActualWidth+RightPanel.ActualWidth);
                    MediaBinGridView.Width = CenterPanel.ActualWidth;
                    _originalSplitterPosition = currentPosition;
                }
            }
        }

        private void RightVerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

            // Set the resize cursor
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        }
        // Vertical Splitter events
        private void RightVerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingRightVertical = true;
            _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.X;

            // Capture the pointer so we continue receiving events even when the pointer leaves the splitter
            RightVerticalSplitter.CapturePointer(e.Pointer);
            // Set the resize cursor (for vertical resizing)
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            ProtectedCursor = inputCursor;
        }

        private void RightVerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingRightVertical)
            {
                // Move the vertical splitter and resize the panels
                double currentPosition = e.GetCurrentPoint(MainCanvas).Position.X;
                double delta = currentPosition - _originalSplitterPosition;
                double newRight = Canvas.GetLeft(RightVerticalSplitter) + delta;

                // Ensure the splitter stays within bounds
                if (newRight > 1250 && newRight < 1500)
                {
                    Canvas.SetLeft(RightVerticalSplitter, newRight);
                    Canvas.SetLeft(RightPanel, newRight + RightVerticalSplitter.ActualWidth);
                    RightPanel.Width = newRight;
                    CenterPanel.Width = newRight - RightVerticalSplitter.ActualWidth - LeftPanel.ActualWidth;
                    MediaBinGridView.Width = CenterPanel.ActualWidth - 50;
                    _originalSplitterPosition = currentPosition;
                }
            }
        }
        private void VerticalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //_isDraggingVertical = false;

            //// Reset cursor to default
            var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            this.ProtectedCursor = defaultCursor;  // Assign it directly to the page
        }
        // Horizontal Splitter events
        private void HorizontalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Set the resize cursor
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
            this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        }

        private void HorizontalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //_isDraggingHorizontal = false;

            //// Reset cursor to default
            var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            this.ProtectedCursor = defaultCursor;  // Assign it directly to the page
        }
        private void HorizontalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingHorizontal = true;
            _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.Y;

            // Capture the pointer for horizontal splitter. This ensures that the control continues to receive pointer events even if the pointer moves quickly or leaves the splitter area.
            _ = HorizontalSplitter.CapturePointer(e.Pointer);

            // Set the resize cursor (for horizontal resizing)
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
            this.ProtectedCursor = inputCursor;
        }

        private void HorizontalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingHorizontal)
            {
                // Move the horizontal splitter and resize the top and bottom panels
                double currentPosition = e.GetCurrentPoint(MainCanvas).Position.Y;
                double delta = currentPosition - _originalSplitterPosition;
                double newTop = Canvas.GetTop(HorizontalSplitter) + delta;

                // Ensure the splitter stays within bounds
                if (newTop > 100 && newTop < MainCanvas.ActualHeight - 100)
                {
                    Canvas.SetTop(HorizontalSplitter, newTop);
                    Canvas.SetTop(BottomPanel, newTop + HorizontalSplitter.Height);
                    LeftPanel.Height = newTop;
                    CenterPanel.Height = newTop;
                    LeftVerticalSplitter.Height = newTop;
                    BottomPanel.Height = MainCanvas.ActualHeight - newTop - HorizontalSplitter.Height;
                    _originalSplitterPosition = currentPosition;
                }
            }
        }

        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingLeftVertical = false;
            _isDraggingRightVertical = false;
            _isDraggingHorizontal = false;

            // Release the pointer capture
            LeftVerticalSplitter.ReleasePointerCapture(e.Pointer);
            RightVerticalSplitter.ReleasePointerCapture(e.Pointer);
            HorizontalSplitter.ReleasePointerCapture(e.Pointer);

            // Reset cursor to default
            var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            this.ProtectedCursor = defaultCursor;
        }






        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {

        }
        private void LoadTreeView()
        {

            //FolderTreeView.RootNodes.Clear();
            //string rootPath = GlobalClass.Instance.MediaLibraryPath;
            //var rootNode = new TreeViewNode() { Content = rootPath };
            //FolderTreeView.RootNodes.Add(rootNode);
            //LoadSubfolders(rootNode, rootPath);
            //var newNode = FolderTreeView.RootNodes[0].Children[1].Children.FirstOrDefault(p => p.Content.Equals(MediaLibrary.BinName));
            //if (!string.IsNullOrEmpty(MediaLibrary.BinName) && newNode != null)
            //{
            //    FolderTreeView.RootNodes[0].IsExpanded = true;
            //    FolderTreeView.RootNodes[0].Children[1].IsExpanded = true;
            //    FolderTreeView.Expand(newNode);
            //}
            // FolderTreeView.Expand(FolderTreeView.RootNodes[0].Children[1].Children.FirstOrDefault(p => p.Content.Equals(MediaLibrary.BinName)));
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
            List<string> directories = new List<string>();
            try
            {
                // Get all subdirectories, excluding those named "Proxy"
                var subDirs = Directory.GetDirectories(rootPath)
                                       .Where(dir => new DirectoryInfo(dir).Name != "Proxy")
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

        public async Task GetAssetAsync(string assetPath)
        {
            DataTable dt = new DataTable();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            List<string> directories = new List<string>();
            directories.Add(assetPath);
            directories.AddRange(await GetAllSubdirectoriesAsync(assetPath));
            viewModel.AllMediaPlayerItems.Clear();
            foreach (var dir in directories)
            {
                if (Directory.Exists(System.IO.Path.Combine(dir, "Proxy")))
                {
                    MediaLibrary.ProxyFolder = System.IO.Path.Combine(dir, "Proxy");
                    parameters.Add("@Asset_path", dir);
                    dt = dataAccess.GetData($"select a.asset_id,a.asset_name,a.asset_path,a.original_path,a.duration,a.description,a.version,a.type,a.size,a.created_user,a.created_at,a.updated_user,a.updated_at, group_concat( distinct t.tag_name order by tag_name) as tag_name  from asset a" +
                        $" left join asset_tag ta on a.asset_id=ta.asset_id" +
                        $" left join tag t on t.tag_id=ta.tag_id" +
                        $" WHERE a.asset_path = @Asset_path" +
                        $" GROUP BY a.asset_id", parameters);
                    // MediaPlayerItems.Clear();
                    foreach (DataRow row in dt.Rows)
                    {
                        string file = row["asset_name"].ToString();
                        string thumbnailPath = System.IO.Path.Combine(MediaLibrary.ProxyFolder, "Thumbnail_" + System.IO.Path.GetFileNameWithoutExtension(file) + ".jpg");
                        string proxyPath = System.IO.Path.Combine(MediaLibrary.ProxyFolder, "Proxy_" + (file));
                        TimeSpan duration = (TimeSpan)(row["duration"]);
                        string description = string.Empty;
                        string updated_user = string.Empty;
                        DateTime updated_at = DateTime.MinValue;
                        string source = Path.Combine(row["asset_path"].ToString(), row["asset_name"].ToString());
                        if (!File.Exists(source))
                            continue;
                        if (row["description"] != DBNull.Value)
                            description = row["description"].ToString();
                        if (row["updated_user"] != DBNull.Value)
                            updated_user = row["updated_user"].ToString();
                        if (row["updated_at"] != DBNull.Value)
                            updated_at = Convert.ToDateTime(row["updated_at"]);
                        Dictionary<string, object> metadata = await GetMetadata(source);
                        List<string> keywords = new List<string>();
                        string rating=string.Empty;

                        if (metadata.TryGetValue("Keywords", out object value) && value is IList<string> keywordList)
                        {
                            keywords = keywordList.ToList(); // Convert to List<string>
                        }
                        else
                        {
                            keywords = new List<string>(); // Assign an empty list to avoid null issues
                        }
                        if (metadata.TryGetValue("Rating", out object val) && val is string mediaRating)
                        {
                            rating = mediaRating.ToString();
                        }
                        List<string> tags = row["tag_name"].ToString().Split(',').ToList();
                        viewModel.MediaPlayerItems.Add(new MediaPlayerItem
                        {
                            MediaId = Convert.ToInt32(row["asset_id"]),
                            Title = row["asset_name"].ToString(),
                            MediaSource = new Uri(source),
                            MediaPath = row["asset_path"].ToString(),
                            OriginalPath = row["original_path"].ToString(),
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
                            ThumbnailPath = new Uri(thumbnailPath),
                            ProxyPath = new Uri(proxyPath),
                            Tags = tags,
                            Keywords = keywords,
                            Rating= string.IsNullOrEmpty(rating)?0:Convert.ToDouble(rating)
                        });

                        viewModel.AllMediaPlayerItems.Add(viewModel.MediaPlayerItems[viewModel.MediaPlayerItems.Count - 1]);
                    }
                    parameters.Clear();
                }
            }
            MediaLibrary.FileCount = viewModel.MediaPlayerItems.Count;
        }
        private async void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            var selectedNode = args.AddedItems.FirstOrDefault();
            if (selectedNode != null)
            {
                string dirName = new DirectoryInfo(((FileSystemItem)FileTreeView.SelectedNode.Content).Path).Name;

                if (dirName == ((FileSystemItem)FileTreeView.SelectedNode.Content).Name)
                    MediaLibrary.BinName = ((FileSystemItem)FileTreeView.SelectedNode.Content).Path;
                else
                    MediaLibrary.BinName = Path.Combine(((FileSystemItem)FileTreeView.SelectedNode.Content).Path, ((FileSystemItem)FileTreeView.SelectedNode.Content).Name);

                //MediaLibrary.BinName = GetFullPath(FileTreeView.SelectedNode);
                //if (Directory.Exists(System.IO.Path.Combine(MediaLibrary.BinName, "Proxy")))
                //{
                //    MediaLibrary.ProxyFolder = System.IO.Path.Combine(MediaLibrary.BinName, "Proxy");
                viewModel.MediaPlayerItems.Clear();
                await GetAssetAsync(MediaLibrary.BinName);
                //}
                //else
                //{
                //}
                MediaLibrary.FileCount = viewModel.MediaPlayerItems.Count;
                viewModel.MediaLibraryObj = MediaLibrary;

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

        private void PopulateComboBox()
        {
            int i;
            for (i = 0; i <= 250; i++)
            {

                NoofItemscomboBox.Items.Add(i.ToString());
            }
            //NoofItemscomboBox.SelectedItem = i;
        }
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("UploadHistory",typeof(UploadHistoryPage)),
            ("DownloadHistory",typeof(DownloadHistoryPage)),
            ("ExportHistory",typeof(ExportHistoryPage)),

        };
        private void navFileHistory_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            navFileHistory_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
        }
        private void navFileHistory_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
        {
            Type _page = null;

            var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
            _page = item.Page;
            var prevNavPAgeType = ContentFrame.CurrentSourcePageType;
            if (!(_page is null) && !Type.Equals(prevNavPAgeType, _page))
            {
                ContentFrame.Navigate(_page, null, recommendedNavigationTransitionInfo);
            }

        }



        private void navFileHistory_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }
        private void navFileHistory_Loaded(object sender, RoutedEventArgs e)
        {


            //	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;

            ////	// navFileHistory doesn't load any page by default, so load home page.
            navFileHistory.SelectedItem = navFileHistory.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            navFileHistory_Navigate("UploadHistory", new EntranceNavigationTransitionInfo());
        }




        private void navFileHistory_BackRequested(NavigationView sender,
                                           NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (navFileHistory.IsPaneOpen &&
                (navFileHistory.DisplayMode == NavigationViewDisplayMode.Compact ||
                 navFileHistory.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            navFileHistory.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of navFileHistory.MenuItems, and doesn't have a Tag.
                navFileHistory.SelectedItem = (NavigationViewItem)navFileHistory.SettingsItem;
                navFileHistory.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                navFileHistory.SelectedItem = navFileHistory.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                navFileHistory.Header =
                    ((NavigationViewItem)navFileHistory.SelectedItem)?.Content?.ToString();

            }
        }
        private void OnDeleteClick()
        {

        }
        public async Task DeleteMediaItemAsync(MediaPlayerItem item)
        {
            var dialog = new ContentDialog
            {
                Content = $"Do you want to delete \n\n '{item.Title}' ?",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                //Height = 5,
                Width = 100,
                XamlRoot = this.Content.XamlRoot // Assuming this code is in a page or window with access to XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {

                if (viewModel.MediaPlayerItems.Contains(item))
                {
                    viewModel.MediaPlayerItems.Remove(item);
                    try
                    {
                        File.Delete(System.IO.Path.Combine(MediaLibrary.BinName, item.Title));
                        File.Delete(System.IO.Path.Combine(MediaLibrary.BinName, item.ThumbnailPath.LocalPath));
                        File.Delete(System.IO.Path.Combine(MediaLibrary.BinName, item.ProxyPath.LocalPath));
                        DeleteAssetAsync(item.Title, MediaLibrary.BinName);
                    }
                    catch (IOException ex)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "File Deletion Error !!!",
                            Content = ex.ToString(),
                            CloseButtonText = "OK",
                            Height = 5,
                            XamlRoot = this.Content.XamlRoot // Assuming this code is in a page or window with access to XamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                }
            }
        }
        private async void DeleteAssetAsync(string assetName, string assetPath)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@Asset_path", assetPath);
            parameters.Add("@Asset_name", assetName);
            int id = dataAccess.GetId($"Select asset_id from asset where asset_name = @Asset_name and asset_path = @Asset_path;", parameters);
            parameters.Add("@Asset_id", id);
            int errorCode = 0;
            if (dataAccess.ExecuteNonQuery($"Delete from asset where asset_id=@Asset_id", parameters, out id, out errorCode) <= 0)
            {
                var dialog = new ContentDialog
                {
                    Title = "Delete Failed",
                    Content = string.IsNullOrWhiteSpace("An unknown error occurred while trying to delete asset."),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot // Assuming this code is in a page or window with access to XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        private void EditAsset_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Authorization_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DownloadProxy_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow.ShowWindow();
        }
        private void DownloadOriginalFile_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow.ShowWindow();
        }

        private void MakeQC_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SendToArchive_Click(object sender, RoutedEventArgs e)
        {
            SendToArchiveWindow.ShowWindow();
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
            if (FileTreeView.SelectedNode != null)
                MediaLibrary.BinName = ((FileSystemItem)FileTreeView.SelectedNode.Content).Path;

            //MediaLibrary.BinName = GetFullPath(FileTreeView.SelectedNode);
            else
                MediaLibrary.BinName = string.Empty;
            if (!(string.IsNullOrEmpty(MediaLibrary.BinName) || Equals(MediaLibrary.BinName, FileTreeView.RootNodes[0].Content.ToString()) ||
                Equals(System.IO.Path.GetFileNameWithoutExtension(MediaLibrary.BinName), FileTreeView.RootNodes[0].Children[0].Content.ToString()) ||
                Equals(System.IO.Path.GetFileNameWithoutExtension(MediaLibrary.BinName), FileTreeView.RootNodes[0].Children[1].Content.ToString())))
            {
                AddNewAssetWindow.ShowWindow(this);
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Content = "Select any Data folder",
                    CloseButtonText = "OK",
                    Height = 5,
                    XamlRoot = this.XamlRoot // Assuming this code is in a page or window with access to XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void AddNewBin_Click(object sender, RoutedEventArgs e)
        {
            if (FileTreeView.SelectedNode == null)
                MediaLibrary.BinName = string.Empty;
            if (!(string.IsNullOrEmpty(MediaLibrary.BinName) || System.IO.Path.Equals(MediaLibrary.BinName, FileTreeView.RootNodes[0].Content.ToString()) || System.IO.Path.Equals(MediaLibrary.BinName, FileTreeView.RootNodes[0].Children[0].Content.ToString())))
                AddNewBinWindow.ShowWindow(this);
            else
            {
                var dialog = new ContentDialog
                {
                    Content = "Select any Data folder",
                    CloseButtonText = "OK",
                    Height = 5,
                    XamlRoot = this.XamlRoot // Assuming this code is in a page or window with access to XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void ViewAsset_Click(object sender, RoutedEventArgs e)
        {
            //AssetWindow assetWindow;
            //assetWindow=await AssetWindow.CreateAsync((Views.MediaBinViews.MediaPlayerItem)MediaBinGridView.SelectedItem, MediaPlayerItems);
            AssetWindow.ShowWindow((Views.MediaBinViews.MediaPlayerItem)MediaBinGridView.SelectedItem, viewModel.MediaPlayerItems);
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MediaBinGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable or disable menu items based on selection state
            bool isItemSelected = MediaBinGridView.SelectedItem != null;
            ViewMenuItem.IsEnabled = isItemSelected;
            RenameMenuItem.IsEnabled = isItemSelected;
            DeleteMenuItem.IsEnabled = isItemSelected;
            CutMenuItem.IsEnabled = isItemSelected;
            MakeQCMenuItem.IsEnabled = isItemSelected;

        }
        private void MediaBinGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Get the clicked point relative to the GridView
            var point = e.GetCurrentPoint(MediaBinGridView).Position;

            // Check if the click is outside the bounds of any item
            var itemsPanel = MediaBinGridView.ItemsPanelRoot as Panel;
            if (itemsPanel != null)
            {
                // Check if the click was on the empty area within the GridView
                foreach (var item in MediaBinGridView.Items)
                {
                    var container = MediaBinGridView.ContainerFromItem(item) as GridViewItem;
                    if (container != null)
                    {
                        // Get the bounding box of each item
                        var transform = container.TransformToVisual(MediaBinGridView);
                        var containerBounds = transform.TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));

                        // If the click is within any item bounds, exit the function
                        if (containerBounds.Contains(point))
                        {
                            return;
                        }
                    }
                }
                // If no item bounds contained the click point, clear the selection
                MediaBinGridView.SelectedItem = null;
            }
        }

        private void MediaBinGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            AssetWindow.ShowWindow((Views.MediaBinViews.MediaPlayerItem)MediaBinGridView.SelectedItem, viewModel.MediaPlayerItems);
        }
        private void TypeMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                string selectedOption = menuItem.Text; // Get the text of the selected item
                if (this.FindName("TypeDropDown") is DropDownButton dropDownButton)
                {
                    dropDownButton.Content = selectedOption;
                    viewModel.FilterType = selectedOption;
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

        private void KeywordsFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.FilterKeyword = KeywordsFilterTextbox.Text;
            }
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.FilterTag = TagAutoSuggestBox.Text;
            }
        }
        private void GetAllTags()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select tag_id,tag_name from tag");
            TagsList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                TagsList.Add(row[1].ToString());
            }
        }

        private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {
            GetAllTags();
        }
        private void MyAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateSuggestions(sender.Text);
                sender.ItemsSource = FilteredTags; // ✅ Bind updated list
            }
        }

        private void MyAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedTag)
            {
                sender.Text = selectedTag; // ✅ Update text with chosen tag
            }
        }
        public void UpdateSuggestions(string query)
        {
            FilteredTags.Clear();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var filtered = TagsList.Where(tag => tag.StartsWith(query, StringComparison.OrdinalIgnoreCase));
                foreach (var tag in filtered)
                    FilteredTags.Add(tag);
            }
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.FilterFileName = FilenameFilterTextbox.Text;
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
            if (viewModel != null)
            {
                //viewModel.FilterRating = RatingControl.Value;
            }
        }
    }
    public class MediaPlayerItem : ObservableObject, IEquatable<MediaPlayerItem>
    {
        private Uri mediaSource;
        private Uri thumbnailPath;
        private string title;
        private string durationString;
        private TimeSpan duration;
        private Uri proxyPath;
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
        private Dictionary<string, object> metadata;
        private List<string> tags;
        private List<string> keywords;
        private double rating = 0;

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
        public string MediaPath
        {
            get => mediaPath;
            set => SetProperty(ref mediaPath, value);
        }
        public Uri ThumbnailPath
        {
            get => thumbnailPath;
            set => SetProperty(ref thumbnailPath, value);
        }
        public Uri ProxyPath
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
        public int FileCount
        {
            get => fileCount;
            set => SetProperty(ref fileCount, value);
        }
    }
    public class MediaLibraryViewModel : ObservableObject
    {
        private MediaPlayerItem media;
        private MediaLibrary MediaLibrary;
        private string path;
        private string _filterTitle = string.Empty;
        private string _filterType = string.Empty;
        private string _filterFileName = string.Empty;
        private string _filterTag = string.Empty;
        private string _filterKeyword = string.Empty;
        private string _filterDescription = string.Empty;
        private double _filterRating;
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
        public string FilterFileName
        {
            get => _filterFileName;
            set { SetProperty(ref _filterFileName, value); ApplyFilter(); }
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
        public MediaLibraryViewModel()
        {
            media = new MediaPlayerItem();
            MediaLibrary = new MediaLibrary();

            ApplyFilter();
        }
        public ObservableCollection<MediaPlayerItem> AllMediaPlayerItems { get; set; } = new();
        public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get; set; } = new();
        public MediaPlayerItem MediaObj { get => media; set => SetProperty(ref media, value); }
        public MediaLibrary MediaLibraryObj { get => MediaLibrary; set => SetProperty(ref MediaLibrary, value); }
        public void ApplyFilter()
        {
            var filteredItems = AllMediaPlayerItems.Where(item =>
             (string.IsNullOrEmpty(FilterType) || item.Type.Equals(FilterType, StringComparison.OrdinalIgnoreCase)) &&
             (string.IsNullOrEmpty(FilterTitle) || item.Title.Contains(FilterTitle, StringComparison.OrdinalIgnoreCase)) &&
             (string.IsNullOrEmpty(FilterFileName) || item.Title.Contains(FilterFileName, StringComparison.OrdinalIgnoreCase)) &&
             (string.IsNullOrEmpty(FilterTag) || item.Tags.Any(t => t.Contains(FilterTag, StringComparison.OrdinalIgnoreCase))) &&
             (string.IsNullOrEmpty(FilterKeyword) || item.Keywords.Any(k => k.Contains(FilterKeyword, StringComparison.OrdinalIgnoreCase))) &&
             (string.IsNullOrEmpty(FilterDescription) || item.Description.Contains(FilterDescription, StringComparison.OrdinalIgnoreCase)) &&
             (!(FilterRating<0) || item.Rating < FilterRating) 
         ).ToList();

            MediaPlayerItems.Clear();
            foreach (var item in filteredItems)
            {
                MediaPlayerItems.Add(item);
            }
        }
    }

    public class FileSystemItem : ObservableObject
    {
        private string name;
        private string path;
        private bool isDirectory;
        private bool isExpanded;
        private ObservableCollection<FileSystemItem> children = new ObservableCollection<FileSystemItem>();
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
        public ObservableCollection<FileSystemItem> Children
        {
            get => children;
            set => SetProperty(ref children, value);
        }
    }

    public class BoolToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
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
}

