using MAM.Data;
using MAM.Utilities;
using MAM.Views.AdminPanelViews.Metadata;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Data;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArchivePage : Page
    {
        private bool _isDraggingVertical;
        private bool _isDraggingHorizontal;
        private double _originalSplitterPosition;
        private DataAccess dataAccess = new DataAccess();
        public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get; set; }
        public ArchiveViewModel ViewModel { get; set; }
        public ArchivePage()
        {
            this.InitializeComponent();
            ViewModel = new ArchiveViewModel();

            LoadFiles();
        }
        private async void LoadFiles()
        {
            ViewModel.MediaLibraryObj.FileServer = GlobalClass.Instance.FileServer;
            ViewModel.MediaLibraryObj.ProxyFolder = GlobalClass.Instance.ProxyFolder;
            ViewModel.MediaLibraryObj.ThumbnailFolder = GlobalClass.Instance.ThumbnailFolder;

            //ViewModel.MediaLibraryObj.ArchiveServer = await GlobalClass.Instance.GetArchiveServer(this.XamlRoot);
            //GlobalClass.Instance.ArchivePath = Path.Combine(ViewModel.MediaLibraryObj.ArchiveServer.ServerName, ViewModel.MediaLibraryObj.ArchiveServer.ArchivePath);

            ViewModel.MediaPlayerItems.Clear();
            await GetAllAssetsAsync(GlobalClass.Instance.ArchivePath);
            ViewModel.MediaLibraryObj.FileCount = ViewModel.MediaPlayerItems.Count;
            ViewModel.MediaLibraryObj = ViewModel.MediaLibraryObj;
        }
        public async Task GetAllAssetsAsync(string FolderPath)
        {
            //GlobalClass.Instance.MediaLibraryPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder);

            DataTable dt = new();
            Dictionary<string, object> parameters = new();
            //List<string> directories = new();
            //directories.Add(FolderPath);
            //directories.AddRange(await GetAllSubdirectoriesAsync(FolderPath));
            //ViewModel.AllMediaPlayerItems.Clear();
            //foreach (var dir in FolderPath)
            //{
            //    if (Directory.Exists(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ProxyFolder)))
            //    {
                    // MediaLibrary.ProxyFolder = System.IO.Path.Combine(dir, "Proxy");
                    parameters.Add("@Asset_path", FolderPath);
                    dt = dataAccess.GetData($"SELECT a.asset_id, a.asset_name, a.asset_path, a.proxy_path,a.thumbnail_path,a.duration, a.description, a.version, a.type," +
                        $"a.size,a.created_user, a.created_at, a.updated_user, a.updated_at, a.is_archived, a.archive_path," +
                        $"IFNULL(GROUP_CONCAT(DISTINCT t.tag_name ORDER BY tag_name),'') AS tag_name," +
                        $"IFNULL(GROUP_CONCAT(DISTINCT CONCAT(m.metadata_id, ':', m.metadata_name, ':', am.metadata_value, ':', m.metadata_type) " +
                        $"ORDER BY m.metadata_name SEPARATOR '; '),'') AS metadata_info " +
                        "FROM asset a " +
                        $"LEFT JOIN asset_tag ta ON a.asset_id = ta.asset_id " +
                        $"LEFT JOIN tag t ON t.tag_id = ta.tag_id " +
                        $"LEFT JOIN asset_metadata am ON a.asset_id = am.asset_id " +
                        $"LEFT JOIN metadata m ON am.metadata_id = m.metadata_id " +
                        $"WHERE a.is_archived =true AND a.is_deleted=false "  +
                        $"GROUP BY a.asset_id;", parameters);


                    foreach (DataRow row in dt.Rows)
                    {
                        ObservableCollection<AssetsMetadata> metadataList = new();
                        string file = row["asset_name"].ToString();
                //string thumbnailPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ThumbnailFolder, "Thumbnail_" + System.IO.Path.GetFileNameWithoutExtension(file) + ".jpg");

                ////string thumbnailPath = System.IO.Path.Combine(ViewModel.MediaLibraryObj.ThumbnailFolder, "Thumbnail_" + System.IO.Path.GetFileNameWithoutExtension(file) + ".jpg");
                //string proxyPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName,ViewModel.MediaLibraryObj.ProxyFolder, "Proxy_" + (file));
                TimeSpan duration = (TimeSpan)(row["duration"]);
                string description = string.Empty;
                        string updated_user = string.Empty;
                        DateTime updated_at = DateTime.MinValue;
                        string source = Path.Combine(row["asset_path"].ToString(), row["asset_name"].ToString());
                        bool isArchived = Convert.ToBoolean(row["is_archived"]);
                        //if (!File.Exists(source))
                        //{
                        //    if ((File.Exists(proxyPath)) && !isArchived)
                        //        continue;
                        //}
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
                        //if (!isArchived)
                        //{
                        //    metadata = await GetMetadata(source);
                        //    keywords = new List<string>();

                        //    if (metadata.TryGetValue("Keywords", out object value) && value is IList<string> keywordList)
                        //    {
                        //        keywords = keywordList.ToList(); // Convert to List<string>
                        //    }
                        //    else
                        //    {
                        //        keywords = new List<string>(); // Assign an empty list to avoid null issues
                        //    }
                        //    if (metadata.TryGetValue("Rating", out object val) && val is string mediaRating)
                        //    {
                        //        rating = mediaRating.ToString();
                        //    }
                        //}
                        //else
                        //{
                        //    //source = row["archive_path"].ToString();
                        //    //mediaPath = Path.GetDirectoryName(source);
                        //}

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
                            ThumbnailPath = row["thumbnail_path"].ToString(),
                            ProxyPath = row["proxy_path"].ToString(),
                            Tags = tags,
                            Keywords = keywords,
                            Rating = string.IsNullOrEmpty(rating) ? 0 : Convert.ToDouble(rating),
                            AssetMetadataList = metadataList,
                            IsArchived = isArchived,
                            ArchivePath = row["archive_path"].ToString()
                        });

                        ViewModel.AllMediaPlayerItems.Add(ViewModel.MediaPlayerItems[ViewModel.MediaPlayerItems.Count - 1]);
                    }
                    parameters.Clear();
                //}
            //}
            ViewModel.MediaLibraryObj.FileCount = ViewModel.MediaPlayerItems.Count;
            ViewModel.AssetCount = $"{ViewModel.MediaLibraryObj.FileCount} results found";
        }
        // Vertical Splitter events
        private void VerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingVertical = true;
            _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.X;

            // Capture the pointer so we continue receiving events even when the pointer leaves the splitter
            VerticalSplitter.CapturePointer(e.Pointer);

            // Set the resize cursor (for vertical resizing)
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
#pragma warning disable CA1416 // Validate platform compatibility
            ProtectedCursor = inputCursor;
#pragma warning restore CA1416 // Validate platform compatibility
        }

        private void VerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingVertical)
            {
                // Move the vertical splitter and resize the panels
                double currentPosition = e.GetCurrentPoint(MainCanvas).Position.X;
                double delta = currentPosition - _originalSplitterPosition;
                double newLeft = Canvas.GetLeft(VerticalSplitter) + delta;

                // Ensure the splitter stays within bounds
                if (newLeft > 100 && newLeft < MainCanvas.ActualWidth - 100)
                {
                    Canvas.SetLeft(VerticalSplitter, newLeft);
                    Canvas.SetLeft(RightPanel, newLeft + VerticalSplitter.Width);
                    LeftPanel.Width = newLeft;
                    RightPanel.Width = MainCanvas.ActualWidth - newLeft - VerticalSplitter.Width;
                    _originalSplitterPosition = currentPosition;
                }
            }
        }

        // Horizontal Splitter events
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
                    RightPanel.Height = newTop;
                    VerticalSplitter.Height = newTop;
                    BottomPanel.Height = MainCanvas.ActualHeight - newTop - HorizontalSplitter.Height;
                    _originalSplitterPosition = currentPosition;
                }
            }
        }

        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingVertical = false;
            _isDraggingHorizontal = false;

            // Release the pointer capture
            VerticalSplitter.ReleasePointerCapture(e.Pointer);
            HorizontalSplitter.ReleasePointerCapture(e.Pointer);

            // Reset cursor to default
            var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            this.ProtectedCursor = defaultCursor;
        }


        private void VerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

            // Set the resize cursor
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        }

        private void HorizontalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Set the resize cursor
            var inputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
            this.ProtectedCursor = inputCursor;  // Assign it directly to the page
        }
        private void VerticalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //_isDraggingVertical = false;

            //// Reset cursor to default
            var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            this.ProtectedCursor = defaultCursor;  // Assign it directly to the page
        }
        private void HorizontalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //_isDraggingHorizontal = false;

            //// Reset cursor to default
            var defaultCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            this.ProtectedCursor = defaultCursor;  // Assign it directly to the page
        }
        private void MediaBinGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable or disable menu items based on selection state
            //bool isItemSelected = MediaBinGridView.SelectedItem != null;
            //ViewMenuItem.IsEnabled = isItemSelected;
            //RenameMenuItem.IsEnabled = isItemSelected;
            //DeleteMenuItem.IsEnabled = isItemSelected;
            //CutMenuItem.IsEnabled = isItemSelected;
            //MakeQCMenuItem.IsEnabled = isItemSelected;
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
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("UploadHistory",typeof(TransactionHistoryPage)),
            ("DownloadHistory",typeof(TransactionHistoryPage)),
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
            //navFileHistory.SelectedItem = navFileHistory.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            //navFileHistory_Navigate("HomePage", new EntranceNavigationTransitionInfo());
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

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MediaBinGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

        }

        private void MediaBinGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

        }

        private void CustomMediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
    //public class MediaPlayerItem
    //{
    //    public Uri MediaSource { get; set; }
    //    public string Title { get; set; } // Optional: Add a title or other metadata
    //}
    public class ArchiveViewModel : ObservableObject
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
        public ArchiveViewModel()
        {
            MediaObj = new MediaPlayerItem();
            MediaLibraryObj = new MediaLibrary();
            ApplyFilter();
        }
        public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }

        public ObservableCollection<MediaPlayerItem> AllMediaPlayerItems { get; set; } = new();
        public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get; set; } = new();
        public MediaPlayerItem MediaObj { get => media; set => SetProperty(ref media, value); }
        public MediaLibrary MediaLibraryObj { get => mediaLibrary; set => SetProperty(ref mediaLibrary, value); }
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
            MediaLibraryObj.FileCount = MediaPlayerItems.Count;
            AssetCount = $"{MediaLibraryObj.FileCount} results found";
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

}
