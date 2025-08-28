using MAM.Data;
using MAM.Utilities;
using MAM.Views.AdminPanelViews.Metadata;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;
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
       
        private DataAccess dataAccess = new DataAccess();
        public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get; set; }
        public ArchiveViewModel ViewModel { get; set; }
        public ArchivePage()
        {
            this.InitializeComponent();
            ViewModel = new ArchiveViewModel();
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
            LoadFiles();
            DataContext = ViewModel;
        }
        private async void LoadFiles()
        {
            ViewModel.MediaLibraryObj.FileServer = GlobalClass.Instance.FileServer;
            ViewModel.MediaLibraryObj.ProxyFolder = GlobalClass.Instance.ProxyFolder;
            ViewModel.MediaLibraryObj.ThumbnailFolder = GlobalClass.Instance.ThumbnailFolder;
            ViewModel.MediaLibraryObj.RecycleFolder = GlobalClass.Instance.RecycleFolder;

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
            dt = dataAccess.GetData($"SELECT a.asset_id, a.asset_name, a.relative_path," +
                 "CONCAT(fs.file_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS media_path," +
                 "CONCAT(fs.thumbnail_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) AS thumbnail_path," +
                 "CASE " +
                    "WHEN a.is_archived = 1 " +
                        "THEN CONCAT(REPLACE(asr.archive_path, '/', '\\\\'), '\\\\', REPLACE(a.archive_path, '/', '\\\\')) " +
                "END AS archive_path," +
                "CASE " +
                    "WHEN a.is_deleted = 1 " +
                        "THEN CONCAT(fs.recycle_folder, '\\\\', REPLACE(a.relative_path, '/', '\\\\')) " +
                "END AS recyclebin_path," +
                $"a.duration, a.description, a.version, a.type," +
                $"a.size,a.created_user, a.created_at, a.updated_user, a.updated_at, a.is_archived, a.archive_path," +
                $"IFNULL(GROUP_CONCAT(DISTINCT t.tag_name ORDER BY tag_name),'') AS tag_name," +
                $"IFNULL(GROUP_CONCAT(DISTINCT CONCAT(m.metadata_id, ':', m.metadata_name, ':', am.metadata_value, ':', m.metadata_type) " +
                $"ORDER BY m.metadata_name SEPARATOR '; '),'') AS metadata_info " +
                "FROM asset a " +
                "JOIN file_server fs on a.file_server_id=fs.server_id " +
                "JOIN archive_server asr on a.archive_server_id=asr.server_id " +
                $"LEFT JOIN asset_tag ta ON a.asset_id = ta.asset_id " +
                $"LEFT JOIN tag t ON t.tag_id = ta.tag_id " +
                $"LEFT JOIN asset_metadata am ON a.asset_id = am.asset_id " +
                $"LEFT JOIN metadata m ON am.metadata_id = m.metadata_id " +
                $"WHERE a.is_archived =true AND a.is_deleted=false " +
                $"GROUP BY a.asset_id;", parameters);


            foreach (DataRow row in dt.Rows)
            {
                ObservableCollection<AssetsMetadata> metadataList = new();
                string file = row["asset_name"].ToString();
                string archivePath = Path.Combine(row["archive_path"].ToString(), file);
                string proxyPath = Path.Combine(row["archive_path"].ToString(), Path.GetFileNameWithoutExtension(file) + "_Proxy.MP4");

                string thumbnailPath = Path.Combine(row["thumbnail_path"].ToString(), Path.GetFileNameWithoutExtension(file) + "_Thumbnail.JPG");
                TimeSpan duration = (TimeSpan)(row["duration"]);
                string description = string.Empty;
                string updated_user = string.Empty;
                DateTime updated_at = DateTime.MinValue;
                string mediaPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, row["media_path"].ToString());
                string source = Path.Combine(mediaPath, row["asset_name"].ToString());
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
                    RelativePath = row["relative_path"].ToString(),
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
                    ThumbnailPath = thumbnailPath,
                    ProxyPath = proxyPath,
                    Tags = tags,
                    Keywords = keywords,
                    Rating = string.IsNullOrEmpty(rating) ? 0 : Convert.ToDouble(rating),
                    AssetMetadataList = metadataList,
                    IsArchived = isArchived,
                    ArchivePath = archivePath
                });

                //ViewModel.AllMediaPlayerItems.Add(ViewModel.MediaPlayerItems[ViewModel.MediaPlayerItems.Count - 1]);
            }
            parameters.Clear();
            //}
            //}
            ViewModel.MediaLibraryObj.FileCount = ViewModel.MediaPlayerItems.Count;
            ViewModel.AssetCount = $"{ViewModel.MediaLibraryObj.FileCount} results found";
            ViewModel.ApplyFilter();
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
            //RightVerticalSplitter.ReleasePointerCapture(e.Pointer);
            HorizontalSplitter.ReleasePointerCapture(e.Pointer);

            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        private void MediaBinGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var gridView = sender as GridView;
            ViewModel.SelectedItems = new ObservableCollection<MediaPlayerItem>();
            foreach (var item in gridView.SelectedItems)
            {
                MediaPlayerItem mediaPlayerItem = item as MediaPlayerItem;
                ViewModel.SelectedItems.Add(mediaPlayerItem);
            }
        }
        private void MediaBinGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MediaBinGridView.Focus(FocusState.Programmatic);

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
        private async void DeleteAsync()
        {
            var result = await GlobalClass.Instance.ShowDialogAsync($"Do you want to delete selected items ?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");
            if (result == ContentDialogResult.Primary)
            {
                foreach (var item in ViewModel.SelectedItems)
                {
                    App.MainAppWindow.StatusBar.ShowStatus($"Deleting{item} ...");
                    await DeleteFilePermanently(item);
                }
            }
            ViewModel.MediaPlayerItems.Clear();
            await GetAllAssetsAsync(GlobalClass.Instance.ArchivePath);
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
                    File.Delete(item.ThumbnailPath);
                    File.Delete(Path.Combine(item.ProxyPath));
                    await DeleteAssetAsync(item.MediaId, item.Title, item.MediaPath);
                    await GlobalClass.Instance.AddtoHistoryAsync("Asset Deleted Permanently", $"Deleted asset '{ViewModel.MediaObj.MediaPath}' permanently.");
                }
                catch (IOException ex)
                {
                    await GlobalClass.Instance.ShowDialogAsync($"File Deletion Error !!!\n {ex.ToString()}", this.XamlRoot);
                }
            }
        }
        private async Task<int> DeleteAssetAsync(int asset_Id, string assetName, string assetPath)
        {
            List<MySqlParameter> parameters = new();
            parameters.Add(new MySqlParameter("@Asset_path", assetPath));
            parameters.Add(new MySqlParameter("@Asset_name", assetName));
            //int id = dataAccess.GetId($"Select asset_id from asset where asset_name = @Asset_name and asset_path = @Asset_path;", parameters);
            parameters.Add(new MySqlParameter("@Asset_id", asset_Id));
            var (affectedRows, newId, errorMessage) = await dataAccess.ExecuteNonQuery($"Delete from asset where asset_id=@Asset_id", parameters);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.ShowDialogAsync("Deletion Failed.An unknown error occurred while trying to delete asset.", this.XamlRoot);
                return -1;
            }
            else
            {
                //await GlobalClass.Instance.AddtoHistoryAsync("Delete from Library", $"Deleted '{assetPath}\\{assetName}' from library .");
                return affectedRows;
            }
        }
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("UploadHistory",typeof(TransactionHistoryPage)),
            ("DownloadHistory",typeof(TransactionHistoryPage)),
            ("ExportHistory",typeof(ExportHistoryPage)),
        };
        private void NavFileHistory_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            NavFileHistory_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
        }
        private void NavFileHistory_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
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



        private void NavFileHistory_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }
        private void NavFileHistory_Loaded(object sender, RoutedEventArgs e)
        {


            //	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;

            ////	// NavFileHistory doesn't load any page by default, so load home page.
            //NavFileHistory.SelectedItem = NavFileHistory.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            //NavFileHistory_Navigate("HomePage", new EntranceNavigationTransitionInfo());
        }




        private void NavFileHistory_BackRequested(NavigationView sender,
                                           NavigationViewBackRequestedEventArgs args)
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

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MediaBinGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

        }

        private void MediaBinGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ViewModel.SelectedItems != null && ViewModel.SelectedItems.Count > 0)
            {
                DeleteMenuItem.IsEnabled = true;
                UnArchiveMenuItem.IsEnabled = true;
            }
        }

        private void CustomMediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeleteAsync();
            App.MainAppWindow.StatusBar.HideStatus();
        }

        private async void UnArchive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var mediaItem in ViewModel.SelectedItems)
                {
                    App.MainAppWindow.StatusBar.ShowStatus($"Unarchiving {mediaItem}...");
                    string archiveFolder = GlobalClass.Instance.ArchivePath;
                    if (Directory.Exists(archiveFolder))
                    {
                        string proxyName = Path.GetFileNameWithoutExtension(mediaItem.Title) + "_Proxy.MP4";
                        //string archivePath = Path.Combine(mediaItem.ArchivePath, mediaItem.Title);
                        string destinationPath = Path.Combine(mediaItem.MediaPath, mediaItem.Title);
                        //string archiveProxyPath = Path.Combine(mediaItem.ProxyPath,);
                        //string baseRoot = Directory.GetParent(ViewModel.MediaLibraryObj.FileServer.FileFolder).FullName;
                        //string relativePath = Path.GetRelativePath(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ThumbnailFolder), Path.GetDirectoryName(mediaItem.ThumbnailPath));
                        string destinationProxyPath = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ProxyFolder, mediaItem.RelativePath);

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
                    {
                        await GlobalClass.Instance.ShowDialogAsync("The file does not exist.", this.Content.XamlRoot);
                    }
                }
                App.MainAppWindow.StatusBar.HideStatus();
                ViewModel.MediaPlayerItems.Clear();
                await GetAllAssetsAsync(GlobalClass.Instance.ArchivePath);
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
        private ObservableCollection<MediaPlayerItem> selectedItems;
        private int pageSize;
        private int _currentPageIndex;
        private int _selectedPageIndex;
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
        public ObservableCollection<MediaPlayerItem> SelectedItems
        {
            get => selectedItems;
            set { SetProperty(ref selectedItems, value); }
        }
        private ObservableCollection<MetadataClass> _metadataList = new();
        private AssetsMetadata _filterMetadata;
        private ObservableCollection<MetadataClass> allMetadataList = new();
        private string selectedTag;
        private List<MediaPlayerItem> _filteredItems = new();
        private ObservableCollection<MediaPlayerItem> _pagedMediaPlayerItems = new();
        private MediaViewMode _currentViewMode = MediaViewMode.Grid;
        public MediaViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set => SetProperty(ref _currentViewMode, value); // Assuming ObservableObject
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
        public ArchiveViewModel()
        {
            MediaObj = new MediaPlayerItem();
            MediaLibraryObj = new MediaLibrary();
            ApplyFilter();
        }
        public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }
        private ObservableCollection<MediaPlayerItem> pagedMediaPlayerItems = new();

        public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get; set; } = new();
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
}
