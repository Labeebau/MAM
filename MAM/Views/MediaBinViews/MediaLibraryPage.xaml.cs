using MAM.Data;
using MAM.UserControls;
using MAM.Utilities;
using MAM.Views.AdminPanelViews.Metadata;
using MAM.Windows;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
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


        public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }
        public ObservableCollection<string> FilteredTags { get; set; } = new();


        public ObservableCollection<MetadataClass> FilteredMetadataList { get; set; } = new();
        //public ObservableCollection<AssetsMetadata> MetadataList { get; set; } = new();



        private readonly List<string> ExcludedFolders = new() { "Proxy" };

        public MediaLibrary MediaLibrary { get; set; } = new();
        public MediaLibraryViewModel viewModel { get; set; }

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
            GetAllTags();
            GetAllMetadata();
            DataContext = viewModel;
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
                customMedia.PlayButtonClicked += async (s, args) =>
                {
                    await ShowAssetWindow();
                };
            }
        }
        //private void VerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        //{
        //    // Set the resize cursor
        //    var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        //    this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        //}
        //// Vertical Splitter events
        //private void VerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    _isDraggingLeftVertical = true;
        //    _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.X;

        //    // Capture the pointer so we continue receiving events even when the pointer leaves the splitter
        //    LeftVerticalSplitter.CapturePointer(e.Pointer);
        //    // Set the resize cursor (for vertical resizing)
        //    var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        //    ProtectedCursor = inputCursor;
        //}

        //private void VerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        //{
        //    if (_isDraggingLeftVertical)
        //    {
        //        // Move the vertical splitter and resize the panels
        //        double currentPosition = e.GetCurrentPoint(MainCanvas).Position.X;
        //        double delta = currentPosition - _originalSplitterPosition;
        //        double newLeft = Canvas.GetLeft(LeftVerticalSplitter) + delta;

        //        // Ensure the splitter stays within bounds
        //        if (newLeft > 300 && newLeft < MainCanvas.ActualWidth - 1200)
        //        {
        //            Canvas.SetLeft(LeftVerticalSplitter, newLeft);
        //            Canvas.SetLeft(CenterPanel, newLeft + LeftVerticalSplitter.ActualWidth);
        //            LeftPanel.Width = newLeft;
        //            CenterPanel.Width = MainCanvas.ActualWidth - newLeft - LeftVerticalSplitter.ActualWidth - RightPanel.ActualWidth;
        //            //MainCanvas.ActualWidth- (newLeft+LeftVerticalSplitter.ActualWidth+RightPanel.ActualWidth);
        //            MediaBinGridView.Width = CenterPanel.ActualWidth;
        //            _originalSplitterPosition = currentPosition;
        //        }
        //    }
        //}

        //private void RightVerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        //{

        //    // Set the resize cursor
        //    var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        //    this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        //}
        //// Vertical Splitter events
        //private void RightVerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    _isDraggingRightVertical = true;
        //    _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.X;

        //    // Capture the pointer so we continue receiving events even when the pointer leaves the splitter
        //    RightVerticalSplitter.CapturePointer(e.Pointer);
        //    // Set the resize cursor (for vertical resizing)
        //    var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        //    ProtectedCursor = inputCursor;
        //}

        //private void RightVerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        //{
        //    if (_isDraggingRightVertical)
        //    {
        //        // Move the vertical splitter and resize the panels
        //        double currentPosition = e.GetCurrentPoint(MainCanvas).Position.X;
        //        double delta = currentPosition - _originalSplitterPosition;
        //        double newRight = Canvas.GetLeft(RightVerticalSplitter) + delta;

        //        // Ensure the splitter stays within bounds
        //        if (newRight > 1250 && newRight < 1500)
        //        {
        //            Canvas.SetLeft(RightVerticalSplitter, newRight);
        //            Canvas.SetLeft(RightPanel, newRight + RightVerticalSplitter.ActualWidth);
        //            RightPanel.Width = newRight;
        //            CenterPanel.Width = newRight - RightVerticalSplitter.ActualWidth - LeftPanel.ActualWidth;
        //            MediaBinGridView.Width = CenterPanel.ActualWidth - 50;
        //            _originalSplitterPosition = currentPosition;
        //        }
        //    }
        //}
        //private void VerticalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        //{
        //    //_isDraggingVertical = false;

        //    //// Reset cursor to default
        //    var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        //    this.ProtectedCursor = defaultCursor;  // Assign it directly to the page
        //}
        //// Horizontal Splitter events
        //private void HorizontalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        //{
        //    // Set the resize cursor
        //    var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        //    this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        //}

        //private void HorizontalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        //{
        //    //_isDraggingHorizontal = false;

        //    //// Reset cursor to default
        //    var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        //    this.ProtectedCursor = defaultCursor;  // Assign it directly to the page
        //}
        //private void HorizontalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    _isDraggingHorizontal = true;
        //    _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.Y;

        //    // Capture the pointer for horizontal splitter. This ensures that the control continues to receive pointer events even if the pointer moves quickly or leaves the splitter area.
        //    _ = HorizontalSplitter.CapturePointer(e.Pointer);

        //    // Set the resize cursor (for horizontal resizing)
        //    var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        //    this.ProtectedCursor = inputCursor;
        //}

        //private void HorizontalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        //{
        //    if (_isDraggingHorizontal)
        //    {
        //        // Move the horizontal splitter and resize the top and bottom panels
        //        double currentPosition = e.GetCurrentPoint(MainCanvas).Position.Y;
        //        double delta = currentPosition - _originalSplitterPosition;
        //        double newTop = Canvas.GetTop(HorizontalSplitter) + delta;

        //        // Ensure the splitter stays within bounds
        //        if (newTop > 100 && newTop < MainCanvas.ActualHeight - 100)
        //        {
        //            Canvas.SetTop(HorizontalSplitter, newTop);
        //            Canvas.SetTop(BottomPanel, newTop + HorizontalSplitter.Height);
        //            LeftPanel.Height = newTop;
        //            CenterPanel.Height = newTop;
        //            LeftVerticalSplitter.Height = newTop;
        //            RightVerticalSplitter.Height = newTop;
        //            BottomPanel.Height = MainCanvas.ActualHeight - newTop - HorizontalSplitter.Height;
        //            _originalSplitterPosition = currentPosition;
        //        }
        //    }
        //}

        //private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        //{
        //    _isDraggingLeftVertical = false;
        //    _isDraggingRightVertical = false;
        //    _isDraggingHorizontal = false;

        //    // Release the pointer capture
        //    LeftVerticalSplitter.ReleasePointerCapture(e.Pointer);
        //    RightVerticalSplitter.ReleasePointerCapture(e.Pointer);
        //    HorizontalSplitter.ReleasePointerCapture(e.Pointer);

        //    // Reset cursor to default
        //    var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        //    this.ProtectedCursor = defaultCursor;
        //}
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
            List<string> directories = new();
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

        public async Task GetAllAssetsAsync(string FolderPath)
        {
            DataTable dt = new();
            Dictionary<string, object> parameters = new();
            List<string> directories = new();
            directories.Add(FolderPath);
            directories.AddRange(await GetAllSubdirectoriesAsync(FolderPath));
            viewModel.AllMediaPlayerItems.Clear();
            foreach (var dir in directories)
            {
                if (Directory.Exists(System.IO.Path.Combine(dir, "Proxy")))
                {
                    MediaLibrary.ProxyFolder = System.IO.Path.Combine(dir, "Proxy");
                    parameters.Add("@Asset_path", dir);
                    dt = dataAccess.GetData($"select a.asset_id," +
                        $"ANY_VALUE(a.asset_name) as asset_name," +
                        $"ANY_VALUE(a.asset_path) as asset_path," +
                        $"ANY_VALUE(a.original_path) as original_path," +
                        $"ANY_VALUE(a.duration) as duration," +
                        $"ANY_VALUE(a.description) as description," +
                        $"ANY_VALUE(a.version) as version," +
                        $"ANY_VALUE(a.type) as type," +
                        $"ANY_VALUE(a.size) as size," +
                        $"ANY_VALUE(a.created_user) as created_user," +
                        $"ANY_VALUE(a.created_at) as created_at," +
                        $"ANY_VALUE(a.updated_user) as updated_user," +
                        $"ANY_VALUE(a.updated_at) as updated_at, " +
                        $"ANY_VALUE(a.is_archived) as is_archived, " +
                        $"ANY_VALUE(a.archive_path) as archive_path, " +
                        $" group_concat( distinct t.tag_name order by tag_name) as tag_name," +
                        $" group_concat(distinct concat(m.metadata_id,':',m.metadata_name, ':', am.metadata_value,':',m.metadata_type) ORDER BY m.metadata_name SEPARATOR '; ') AS metadata_info" +
                        $" from asset a" +
                        $" left join asset_tag ta on a.asset_id=ta.asset_id" +
                        $" left join tag t on t.tag_id=ta.tag_id" +
                        $" left join asset_metadata am on a.asset_id = am.asset_id" +
                        $" left join metadata m ON am.metadata_id = m.metadata_id" +
                        $" WHERE a.asset_path = @Asset_path" +
                        $" GROUP BY a.asset_id", parameters);
                    foreach (DataRow row in dt.Rows)
                    {
                        ObservableCollection<AssetsMetadata> metadataList = new();
                        string file = row["asset_name"].ToString();
                        string thumbnailPath = System.IO.Path.Combine(MediaLibrary.ProxyFolder, "Thumbnail_" + System.IO.Path.GetFileNameWithoutExtension(file) + ".jpg");
                        string proxyPath = System.IO.Path.Combine(MediaLibrary.ProxyFolder, "Proxy_" + (file));
                        TimeSpan duration = (TimeSpan)(row["duration"]);
                        string description = string.Empty;
                        string updated_user = string.Empty;
                        DateTime updated_at = DateTime.MinValue;
                        string source = Path.Combine(row["asset_path"].ToString(), row["asset_name"].ToString());
                        bool isArchived = Convert.ToBoolean(row["is_archived"]);
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
                        string mediaPath = row["asset_path"].ToString();
                        if (!isArchived)
                        {
                            metadata = await GetMetadata(source);
                            keywords = new List<string>();

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
                        }
                        else
                        {
                            source = row["archive_path"].ToString();
                            mediaPath = Path.GetDirectoryName(source);
                        }

                        List<string> tags = row["tag_name"].ToString().Split(',').ToList();

                        if (row["metadata_info"] != DBNull.Value)
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
                        viewModel.MediaPlayerItems.Add(new MediaPlayerItem
                        {
                            MediaId = Convert.ToInt32(row["asset_id"]),
                            Title = row["asset_name"].ToString(),
                            MediaSource = new Uri(source),
                            MediaPath = mediaPath,
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
                            Rating = string.IsNullOrEmpty(rating) ? 0 : Convert.ToDouble(rating),
                            AssetMetadataList = metadataList,
                            IsArchived = isArchived,
                            ArchivePath = row["archive_path"].ToString()
                        });

                        viewModel.AllMediaPlayerItems.Add(viewModel.MediaPlayerItems[viewModel.MediaPlayerItems.Count - 1]);
                    }
                    parameters.Clear();
                }
            }
            MediaLibrary.FileCount = viewModel.MediaPlayerItems.Count;
            viewModel.AssetCount = $"{MediaLibrary.FileCount} results found";
        }
        public async Task<MediaPlayerItem> GetAssetsAsync(string assetPath, string assetName)
        {
            DataTable dt = new();
            Dictionary<string, object> parameters = new();
            if (Directory.Exists(System.IO.Path.Combine(assetPath, "Proxy")))
            {
                MediaLibrary.ProxyFolder = System.IO.Path.Combine(assetPath, "Proxy");
                parameters.Add("@Asset_path", assetPath);
                parameters.Add("@Asset_name", assetName);

                dt = dataAccess.GetData($"select a.asset_id," +
                        $"ANY_VALUE(a.asset_name) as asset_name," +
                        $"ANY_VALUE(a.asset_path) as asset_path," +
                        $"ANY_VALUE(a.original_path) as original_path," +
                        $"ANY_VALUE(a.duration) as duration," +
                        $"ANY_VALUE(a.description) as description," +
                        $"ANY_VALUE(a.version) as version," +
                        $"ANY_VALUE(a.type) as type," +
                        $"ANY_VALUE(a.size) as size," +
                        $"ANY_VALUE(a.created_user) as created_user," +
                        $"ANY_VALUE(a.created_at) as created_at," +
                        $"ANY_VALUE(a.updated_user) as updated_user," +
                        $"ANY_VALUE(a.updated_at) as updated_at, " +
                        $"ANY_VALUE(a.is_archived) as is_archived, " +
                        $"ANY_VALUE(a.archive_path) as archive_path, " +
                        $" group_concat( distinct t.tag_name order by tag_name) as tag_name," +
                        $" group_concat(distinct concat(m.metadata_id,':',m.metadata_name, ':', am.metadata_value,':',m.metadata_type) ORDER BY m.metadata_name SEPARATOR '; ') AS metadata_info" +
                        $" from asset a" +
                        $" left join asset_tag ta on a.asset_id=ta.asset_id" +
                        $" left join tag t on t.tag_id=ta.tag_id" +
                        $" left join asset_metadata am on a.asset_id = am.asset_id" +
                        $" left join metadata m ON am.metadata_id = m.metadata_id" +
                        $" WHERE a.asset_path = @Asset_path and a.asset_name=@Asset_name" +
                        $" GROUP BY a.asset_id", parameters);
                ObservableCollection<AssetsMetadata> metadataList = new();
                string file = dt.Rows[0]["asset_name"].ToString();
                string thumbnailPath = System.IO.Path.Combine(MediaLibrary.ProxyFolder, "Thumbnail_" + System.IO.Path.GetFileNameWithoutExtension(file) + ".jpg");
                string proxyPath = System.IO.Path.Combine(MediaLibrary.ProxyFolder, "Proxy_" + (file));
                TimeSpan duration = (TimeSpan)(dt.Rows[0]["duration"]);
                string description = string.Empty;
                string updated_user = string.Empty;
                DateTime updated_at = DateTime.MinValue;
                string source = Path.Combine(dt.Rows[0]["asset_path"].ToString(), dt.Rows[0]["asset_name"].ToString());
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
                string mediaPath = dt.Rows[0]["asset_path"].ToString();
                if (!isArchived)
                {
                    metadata = await GetMetadata(source);
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
                }
                else
                {
                    source = dt.Rows[0]["archive_path"].ToString();
                    mediaPath = Path.GetDirectoryName(source);
                }
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
                    MediaPath = mediaPath,
                    OriginalPath = dt.Rows[0]["original_path"].ToString(),
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
                    ThumbnailPath = new Uri(thumbnailPath),
                    ProxyPath = new Uri(proxyPath),
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

        private async void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            var selectedNode = args.AddedItems.FirstOrDefault();
            if (selectedNode != null)
            {
                await LoadAssetsAsync();
            }
            viewModel.Path = viewModel.MediaLibraryObj.BinName;
        }
        private async Task LoadAssetsAsync()
        {

            string dirName = new DirectoryInfo(((FileSystemItem)FileTreeView.SelectedNode.Content).Path).Name;

            if (dirName == ((FileSystemItem)FileTreeView.SelectedNode.Content).Name)
                MediaLibrary.BinName = ((FileSystemItem)FileTreeView.SelectedNode.Content).Path;
            else
                MediaLibrary.BinName = Path.Combine(((FileSystemItem)FileTreeView.SelectedNode.Content).Path, ((FileSystemItem)FileTreeView.SelectedNode.Content).Name);

            viewModel.MediaPlayerItems.Clear();
            await GetAllAssetsAsync(MediaLibrary.BinName);
            MediaLibrary.FileCount = viewModel.MediaPlayerItems.Count;
            viewModel.MediaLibraryObj = MediaLibrary;
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
                        File.Delete(System.IO.Path.Combine(item.MediaPath, item.Title));
                        File.Delete(System.IO.Path.Combine(item.MediaPath, item.ThumbnailPath.LocalPath));
                        File.Delete(System.IO.Path.Combine(item.MediaPath, item.ProxyPath.LocalPath));
                        await DeleteAssetAsync(item.Title, item.MediaPath);
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
        private async Task<int> DeleteAssetAsync(string assetName, string assetPath)
        {
            List<MySqlParameter> parameters = new();
            parameters.Add(new MySqlParameter("@Asset_path", assetPath));
            parameters.Add(new MySqlParameter("@Asset_name", assetName));
            int id = dataAccess.GetId($"Select asset_id from asset where asset_name = @Asset_name and asset_path = @Asset_path;", parameters);
            parameters.Add(new MySqlParameter("@Asset_id", id));
            var (affectedRows, newId, errorCode) = await dataAccess.ExecuteNonQuery($"Delete from asset where asset_id=@Asset_id", parameters);
            if (errorCode < 0)
            {
                await GlobalClass.Instance.ShowErrorDialogAsync("Delete Failed.An unknown error occurred while trying to delete asset.", this.XamlRoot);
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

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DownloadProxy_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayerItem mediaPlayerItem = (MediaPlayerItem)MediaBinGridView.SelectedItem;
            if (mediaPlayerItem != null)
            {
                DownloadProxyWindow.ShowWindow(mediaPlayerItem);
                NavFileHistory_Navigate("DownloadHistory", new EntranceNavigationTransitionInfo());
                NavFileHistory.SelectedItem = "DownloadHistory";
            }
        }
        private void DownloadOriginalFile_Click(object sender, RoutedEventArgs e)
        {

            MediaPlayerItem mediaPlayerItem = (MediaPlayerItem)MediaBinGridView.SelectedItem;
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
            MediaPlayerItem mediaPlayerItem = (MediaPlayerItem)MediaBinGridView.SelectedItem;
            if (mediaPlayerItem != null)
                SendToArchiveWindow.ShowWindow(mediaPlayerItem);
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

        private async void ViewAsset_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayerItem mediaPlayerItem = await GetAssetsAsync(((Views.MediaBinViews.MediaPlayerItem)MediaBinGridView.SelectedItem).MediaPath, ((Views.MediaBinViews.MediaPlayerItem)MediaBinGridView.SelectedItem).Title);
            AssetWindow.ShowWindow(mediaPlayerItem, viewModel.MediaPlayerItems);
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void MediaBinGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable or disable menu items based on selection state
            bool isItemSelected = MediaBinGridView.SelectedItem != null;
            
            ViewMenuItem.IsEnabled = isItemSelected;
            RenameMenuItem.IsEnabled = isItemSelected;
            DeleteMenuItem.IsEnabled = isItemSelected;
            CutMenuItem.IsEnabled = isItemSelected;
            MakeQCMenuItem.IsEnabled = isItemSelected;
            if (MediaBinGridView.SelectedItem != null)
            {
                MediaPlayerItem media = (MediaPlayerItem)MediaBinGridView.SelectedItem;
                viewModel.MediaObj = await GetAssetsAsync(media.MediaPath, media.Title);
                //(MediaPlayerItem)MediaBinGridView.SelectedItem;
                //viewModel.MediaObj.IsArchived = ((MediaPlayerItem)MediaBinGridView.SelectedItem).IsArchived;
                if (viewModel.MediaObj != null)
                {
                    UnarchiveItem.IsEnabled = viewModel.MediaObj.IsArchived;
                    if (viewModel.MediaObj.IsArchived)
                        viewModel.Path = GlobalClass.Instance.ArchivePath;
                    else
                        viewModel.Path = ((MediaPlayerItem)MediaBinGridView.SelectedItem).MediaPath;
                }
            }
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
        private async Task ShowAssetWindow()
        {
            if (MediaBinGridView.SelectedItem != null)
            {
                MediaPlayerItem mediaPlayerItem = await GetAssetsAsync(((MediaPlayerItem)MediaBinGridView.SelectedItem).MediaPath, ((Views.MediaBinViews.MediaPlayerItem)MediaBinGridView.SelectedItem).Title);
                AssetWindow.ShowWindow(mediaPlayerItem, viewModel.MediaPlayerItems);
            }
        }
        private async void MediaBinGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await ShowAssetWindow();
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


        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.FilterTag = TagAutoSuggestBox.Text;
            }
        }
        private void GetAllTags()
        {
            DataTable dt = new();
            dt = dataAccess.GetData("select tag_id,tag_name from tag");
            viewModel.TagsList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                viewModel.TagsList.Add(row[1].ToString());
            }
        }


        private void TagAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateSuggestions(sender.Text);
                sender.ItemsSource = FilteredTags; // ✅ Bind updated list
            }
        }

        private void TagAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedTag)
            {
                viewModel.SelectedTag = selectedTag; // ✅ Update text with chosen tag
            }
        }
        public void UpdateSuggestions(string query)
        {
            FilteredTags.Clear();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var filtered = viewModel.TagsList.Where(tag => tag.StartsWith(query, StringComparison.OrdinalIgnoreCase));
                foreach (var tag in filtered)
                    FilteredTags.Add(tag);
                if (filtered.Count() == 0)
                    FilteredTags.Add("No results found");
            }
            else
            {
                foreach (var tag in viewModel.TagsList)
                    FilteredTags.Add(tag);
            }
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.FilterTitle = FilenameFilterTextbox.Text;
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

        private void KeywordButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.FilterKeyword = KeywordsFilterTextbox.Text;
            }
        }

        private void MetadataVisiblityButton_Click(object sender, RoutedEventArgs e)
        {

        }



        private void RemoveMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is MediaLibraryViewModel vm)
            {
                var itemToRemove = viewModel.MetadataList.FirstOrDefault(m => m.MetadataId == vm.SelectedMetadata.MetadataId);
                if (itemToRemove != null)
                {
                    viewModel.MetadataList.Remove(itemToRemove);
                    viewModel.SelectedMetadata = null;
                }
            }
        }

        private async void AddMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedMetadata != null && !viewModel.MetadataList.Any(m => m.MetadataId == viewModel.SelectedMetadata.MetadataId))
            {
                var newMetadata = new AssetsMetadata
                {

                    MetadataId = viewModel.SelectedMetadata.MetadataId,
                    //MetadataValue = viewModel.SelectedMetadata.Metadata,
                    Metadata = viewModel.SelectedMetadata.Metadata,// Consider deep copying if needed
                    MetadataType = viewModel.SelectedMetadata.MetadataType
                };

                viewModel.MetadataList.Add(newMetadata);
                viewModel.SelectedMetadata = null;
            }
            else
                await GlobalClass.Instance.ShowErrorDialogAsync($"{viewModel.SelectedMetadata.Metadata} already added.", this.Content.XamlRoot);
        }


        private void GetAllMetadata()
        {
            MetadataClass NewMetadata;
            DataTable dt = new();
            dt = dataAccess.GetData("select metadata_id,metadata_name,metadata_type from metadata");
            viewModel.AllMetadataList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewMetadata = new MetadataClass();
                NewMetadata.MetadataId = Convert.ToInt32(row[0]);
                NewMetadata.Metadata = row[1].ToString();
                NewMetadata.MetadataType = row[2].ToString();
                viewModel.AllMetadataList.Add(NewMetadata);
            }
        }
        public void UpdateMetadataSuggestions(string query)
        {
            FilteredMetadataList.Clear();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var filtered = viewModel.AllMetadataList.Where(m => m.Metadata.Contains(query, StringComparison.OrdinalIgnoreCase));
                foreach (var meta in filtered)
                    FilteredMetadataList.Add(meta);
                if (filtered.Count() == 0)
                    FilteredMetadataList.Add(new MetadataClass { Metadata = "No results found" });
            }
            else
            {
                // If the query is empty (like after pressing Backspace), show all metadata
                foreach (var meta in viewModel.AllMetadataList)
                    FilteredMetadataList.Add(meta);
            }
        }
        private void MetadataAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateMetadataSuggestions(sender.Text);
                sender.ItemsSource = FilteredMetadataList.Any() ? FilteredMetadataList : null;
            }
        }

        private void MetadataAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is MetadataClass selected)
            {
                viewModel.SelectedMetadata = selected; // ✅ Update text with chosen tag
                sender.Text = selected.Metadata;
            }
        }
        private void MetadataButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Get the DataContext of the button (which should be AssetsMetadata)
                if (button.DataContext is AssetsMetadata metadataItem)
                {
                    if (viewModel != null)
                    {
                        viewModel.FilterMetadata = metadataItem;
                    }
                }
            }
        }

        private void MetadataExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {

        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearFilters();
            TypeDropDown.Content = "Select Type";

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void DescriptionButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void RefreshMediaLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (FileTreeView.SelectedNode != null)
                if (((FileSystemItem)FileTreeView.SelectedNode.Content) != null)
                {
                    await LoadAssetsAsync();
                }

        }

        private void MediaBinGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
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
            if(MediaBinGridView.SelectedItems.Count==0)
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
            else
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
            if (MediaLibrary.BinName.StartsWith(GlobalClass.Instance.MediaLibraryPath))
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
            if (GlobalClass.Instance.CurrentUser.UserRight.OrgDownload)
            {
                OriginalMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                OriginalMenuItem.Visibility = Visibility.Collapsed;
            }
            if (GlobalClass.Instance.CurrentUser.UserRight.PrxDownload)
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

        private async void UnarchiveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(viewModel.MediaObj.ArchivePath))
                {
                    //Dictionary<string, object> propsList = new Dictionary<string, object>
                    //{
                    //    {"@AssetId",viewModel.MediaObj.MediaId }
                    //};
                    //string destinationPath = dataAccess.GetString("select asset_path from asset where asset_id=@AssetId", propsList);
                    string destinationPath = Path.Combine(viewModel.MediaObj.MediaPath, viewModel.MediaObj.Title);
                    File.Copy(viewModel.MediaObj.ArchivePath, destinationPath, false);
                    File.Delete(viewModel.MediaObj.ArchivePath);
                    Dictionary<string, object> propsList = new Dictionary<string, object>
                    {
                        {"is_archived", false },
                        {"archive_path",string.Empty }
                    };

                    int res = await dataAccess.UpdateRecord("Asset", "asset_id", viewModel.MediaObj.MediaId, propsList);
                    if (res > 0)
                    {
                        viewModel.MediaObj.IsArchived = false;
                        await GlobalClass.Instance.AddtoHistoryAsync("Unarchive", $"Unarchived asset '{destinationPath}' .");
                    }
                }
                else
                {
                    await GlobalClass.Instance.ShowErrorDialogAsync("The file does not exist.", this.Content.XamlRoot);
                }
            }
            catch (IOException ioEx)
            {
                await GlobalClass.Instance.ShowErrorDialogAsync($"File operation failed: {ioEx.Message}", this.Content.XamlRoot);
            }
            catch (UnauthorizedAccessException authEx)
            {
                await GlobalClass.Instance.ShowErrorDialogAsync($"Permission error: {authEx.Message}", this.Content.XamlRoot);
            }
            catch (Exception ex)
            {
                await GlobalClass.Instance.ShowErrorDialogAsync($"An error occurred: {ex.Message}", this.Content.XamlRoot);
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
        private ObservableCollection<AssetsMetadata> _assetMetadataList;
        private string archivePath;

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
        public int FileCount
        {
            get => fileCount;
            set => SetProperty(ref fileCount, value);
        }
        private ObservableCollection<string> _recentFiles = new ObservableCollection<string>();
        public ObservableCollection<string> RecentFiles
        {
            get => _recentFiles;
            set => SetProperty(ref _recentFiles, value);
        }
    }
    public class MediaLibraryViewModel : ObservableObject
    {
        private MediaPlayerItem media;
        private MediaLibrary MediaLibrary;
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
        public MetadataClass SelectedMetadata
        {
            get => selectedMetadata;
            set { SetProperty(ref selectedMetadata, value); ApplyFilter(); }
        }
        private ObservableCollection<MetadataClass> _metadataList = new();
        private AssetsMetadata _filterMetadata;
        private ObservableCollection<MetadataClass> allMetadataList = new();
        private string selectedTag;

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
        public MediaLibraryViewModel()
        {
            MediaObj = new MediaPlayerItem();
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
             (string.IsNullOrEmpty(FilterTag) || item.Tags.Any(t => t.Contains(FilterTag, StringComparison.OrdinalIgnoreCase))) &&
             (string.IsNullOrEmpty(FilterKeyword) || item.Keywords.Any(k => k.Contains(FilterKeyword, StringComparison.OrdinalIgnoreCase))) &&
             (string.IsNullOrEmpty(FilterDescription) || item.Description.Contains(FilterDescription, StringComparison.OrdinalIgnoreCase)) &&
             //(!(FilterRating < 0) || item.Rating < FilterRating) &&
             (FilterMetadata == null || item.AssetMetadataList.Any(m =>
             m.MetadataId == FilterMetadata.MetadataId &&
             (string.IsNullOrEmpty(FilterMetadata.MetadataValue) || m.MetadataValue.Contains(FilterMetadata.MetadataValue, StringComparison.OrdinalIgnoreCase))
            ))
         ).ToList();

            MediaPlayerItems.Clear();
            foreach (var item in filteredItems)
            {
                MediaPlayerItems.Add(item);
            }
            MediaLibrary.FileCount = MediaPlayerItems.Count;
            AssetCount = $"{MediaLibrary.FileCount} results found";
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
            MediaPlayerItems.Clear();
            foreach (var item in AllMediaPlayerItems)
            {
                MediaPlayerItems.Add(item);
            }

        }
    }

    public class AssetsMetadata : MetadataClass
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
    ////public class NavigationParameter
    ////{
    ////    public MediaPlayerItem MediaObj { get; set; }
    ////    public MediaLibrary MediaLibraryObj { get; set; }

    ////    public NavigationParameter(MediaPlayerItem media, MediaLibrary library)
    ////    {
    ////        MediaObj = media;
    ////        MediaLibraryObj = library;
    ////    }
    ////}

}

