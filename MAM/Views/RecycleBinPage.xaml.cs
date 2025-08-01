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
using MAM.UserControls;
using System.Collections.ObjectModel;
using MAM.Views.MediaBinViews;
using MAM.Utilities;
using MAM.Views.AdminPanelViews.Metadata;
using System.Windows.Input;
using Microsoft.UI;
using MAM.Data;
using MAM.Windows;
using System.Data;
using Microsoft.UI.Xaml.Shapes;
using Path = System.IO.Path;
using MySql.Data.MySqlClient;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecycleBinPage : Page
    {
        public RecycleBinViewModel ViewModel { get; set; }
        DataAccess dataAccess { get; set; }= new DataAccess();
        public RecycleBinPage()
        {
            this.InitializeComponent();
            this.SizeChanged += ProjectWindow_SizeChanged;
            ViewModel = new RecycleBinViewModel();
            this.Loaded += RecycleBinPage_Loaded;
            ViewModel.PageSize = SettingsService.Get(SettingKeys.DefaultPageSize, 8);
            ViewModel.SelectedPageIndex = 0;
            UpdatePageButtons();
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.SelectedPageIndex) ||
                    e.PropertyName == nameof(ViewModel.TotalPages))
                {
                    UpdatePageButtons();
                }
            };
            ViewModel.PaginationUpdated += UpdatePageButtons;
            ViewModel.OnDeleteRequested = async (item) => await DeleteMediaItemAsync(item);
            ViewModel.OnRestoreRequested = async (item) => await RestoreMediaItemAsync(item);
            DataContext = ViewModel;

        }
        private async void RecycleBinPage_Loaded(object sender, RoutedEventArgs e)
        {
            await GlobalClass.Instance.GetFileServer(this.Content.XamlRoot);
            ViewModel.MediaLibraryObj.FileServer = GlobalClass.Instance.FileServer;
            if (ViewModel.MediaLibraryObj.FileServer != null)
            {
                UIThreadHelper.RunOnUIThread(() =>
                {
                    App.MainAppWindow.StatusBar.ShowStatus("Connecting...", true);
                });
                try
                {
                    if (!Directory.Exists(ViewModel.MediaLibraryObj.RecycleBinFolder))
                    {
                        string fileFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.FileFolder);
                        string baseRoot = Directory.GetParent(fileFolder).FullName;
                        string mediaLibrary = fileFolder.Substring(baseRoot.Length).TrimStart('\\');
                        string RecycleBin = Path.Combine(baseRoot, "Recycle Bin");
                        string RecycleBinMediaLibrary = Path.Combine(RecycleBin, mediaLibrary);
                        string RecycleBinThumbnailFolder = Path.Combine(RecycleBin, "Thumbnail");
                        string RecycleBinProxyFolder = Path.Combine(RecycleBin, "Proxy");

                        if (!Directory.Exists(RecycleBinMediaLibrary))
                            Directory.CreateDirectory(RecycleBinMediaLibrary);
                        if (!Directory.Exists(RecycleBinThumbnailFolder))
                            Directory.CreateDirectory(RecycleBinThumbnailFolder);
                        if (!Directory.Exists(RecycleBinProxyFolder))
                            Directory.CreateDirectory(RecycleBinProxyFolder);

                        ViewModel.MediaLibraryObj.RecycleBinFolder = RecycleBinMediaLibrary;
                        ViewModel.MediaLibraryObj.RecycleBinThumbnailFolder = RecycleBinThumbnailFolder;
                        ViewModel.MediaLibraryObj.RecycleBinProxyFolder = RecycleBinProxyFolder;

                        ViewModel.MediaLibraryObj.ThumbnailFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, GlobalClass.Instance.ThumbnailFolder);
                        ViewModel.MediaLibraryObj.ProxyFolder = Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, GlobalClass.Instance.ProxyFolder);

                    }
                    await LoadAssetsAsync();
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
        private async Task LoadAssetsAsync()
        {
            ViewModel.RecycleBinMediaPlayerItems.Clear();
            await GetAllAssetsAsync(ViewModel.MediaLibraryObj.RecycleBinFolder);
            ViewModel.MediaLibraryObj.FileCount = ViewModel.RecycleBinMediaPlayerItems.Count;
            ViewModel.ApplyFilter();
        }
       
        public async Task<List<string>> GetAllSubdirectoriesAsync(string rootPath)
        {
            List<string> directories = new();
            try
            {
                // Get all subdirectories, excluding those named "Proxy"
                var subDirs = Directory.GetDirectories(rootPath)
                                       .Where(dir => new DirectoryInfo(dir).Name != ViewModel.MediaLibraryObj.RecycleBinProxyFolder && new DirectoryInfo(dir).Name != ViewModel.MediaLibraryObj.RecycleBinThumbnailFolder)
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
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            ViewModel.RecycleBinMediaPlayerItems.Clear();
            List<string> directories = new();
            directories.Add(FolderPath);
            directories.AddRange(await GetAllSubdirectoriesAsync(FolderPath));
            foreach (var dir in directories)
            {
                //if (Directory.Exists(Path.Combine(ViewModel.MediaLibraryObj.FileServer.ServerName, ViewModel.MediaLibraryObj.FileServer.ProxyFolder)))
                //{
                    if (Directory.Exists(dir))
                    {
                        parameters.Add("@recyclebin_path", dir);
                        string query = @"SELECT a.asset_id, a.asset_name, a.asset_path,a.proxy_path,a.thumbnail_path,a.recyclebin_path " +
                                        "FROM asset a " +
                                        $"WHERE a.is_deleted=true AND a.recyclebin_path = @recyclebin_path";
                        dt = dataAccess.GetData(query, parameters);
                        foreach (DataRow row in dt.Rows)
                        {
                            string title = row["asset_name"].ToString();
                        string recyclebinPath =row["recyclebin_path"].ToString();
                        string baseRoot = Directory.GetParent(ViewModel.MediaLibraryObj.RecycleBinFolder).FullName;
                        string relativePath = Path.GetRelativePath(ViewModel.MediaLibraryObj.RecycleBinFolder, recyclebinPath);  // "Songs\Hindi"
                        string recThumbnailMediaPath = Path.Combine(ViewModel.MediaLibraryObj.RecycleBinThumbnailFolder, relativePath, Path.GetFileNameWithoutExtension(title) + "_Thumbnail.JPG");
                        string thumbnailPath = Path.Combine(row["thumbnail_path"].ToString(), Path.GetFileNameWithoutExtension(title) + "_Thumbnail.JPG");
                        string recProxyMediaPath = Path.Combine(ViewModel.MediaLibraryObj.RecycleBinProxyFolder, relativePath, Path.GetFileNameWithoutExtension(title) + "_Proxy.MP4");
                        var proxyPath = Path.Combine(row["proxy_path"].ToString(), Path.GetFileNameWithoutExtension(title) + "_Proxy.MP4");
                        ViewModel.RecycleBinMediaPlayerItems.Add(new RecycleBinMediaPlayerItem
                            {
                                MediaId = Convert.ToInt32(row["asset_id"]),
                                MediaLibraryPath = Path.Combine(row["asset_path"].ToString(), title),
                                ProxyPath = proxyPath,
                                ThumbnailPath = thumbnailPath,
                                RecycleBinPath =Path.Combine(recyclebinPath,title),
                                Title = title,
                                RecycleBinThumbnailPath = recThumbnailMediaPath,
                                RecycleBinProxyPath = recProxyMediaPath,
                            });
                        }
                    }
                parameters.Clear();
            }
        }
        //ViewModel.MediaLibraryObj.FileCount = ViewModel.RecycleBinMediaPlayerItems.Count;
        //ViewModel.AssetCount = $"{ViewModel.MediaLibraryObj.FileCount} results found";
        private void ProjectWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //MainGrid.Width = e.NewSize.Width - 10;  // Adjust size with padding if necessary
            //MainGrid.Height = e.NewSize.Height - 10;  // Adjust size with padding if necessary

            //GridviewPanel.Width = e.NewSize.Width - 20;  // Adjust size with padding if necessary
            //GridviewPanel.Height = e.NewSize.Height - 20;  // Adjust size with padding if necessary
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

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MediaBinGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MediaBinGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void MediaBinGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

        }

        private void MediaBinGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

        }

        //private void CustomRecycleBinMedia_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (sender is CustomRecycleBinMedia customMedia && customMedia.DataContext is RecycleBinMediaPlayerItem mediaItem)
        //    {
        //        customMedia.DeleteRequested += async (s, args) =>
        //        {
        //            await DeleteMediaItemAsync(args.MediaItem);
        //        };
        //        customMedia.RestoreRequested += async (s, args) =>
        //        {
        //            await RestoreMediaItemAsync(args.MediaItem);
        //        };
        //    }
        //}

        private async Task RestoreMediaItemAsync(RecycleBinMediaPlayerItem mediaItem)
        {
            try
            {
                
                string recycleBinFolder = ViewModel.MediaLibraryObj.RecycleBinFolder;
                if (Directory.Exists(recycleBinFolder))
                {

                    
                    string mediaLibraryPath = Path.GetDirectoryName(mediaItem.MediaLibraryPath);
                    if (!Directory.Exists(mediaLibraryPath))
                        Directory.CreateDirectory(mediaLibraryPath);
                    string thumbnailPath = Path.GetDirectoryName(mediaItem.ThumbnailPath);
                    if (!Directory.Exists(thumbnailPath))
                        Directory.CreateDirectory(thumbnailPath);
                    string proxyPath = Path.GetDirectoryName(mediaItem.ProxyPath);
                    if (!Directory.Exists(proxyPath))
                        Directory.CreateDirectory(proxyPath);
                    Dictionary<string, object> props = new Dictionary<string, object> { { "@AssetId", mediaItem.MediaId } };
                    File.Move(mediaItem.RecycleBinPath,mediaItem.MediaLibraryPath);
                    File.Move(mediaItem.RecycleBinThumbnailPath,mediaItem.ThumbnailPath);
                    File.Move(mediaItem.RecycleBinProxyPath,mediaItem.ProxyPath);
                    Dictionary<string, object> propsList = new Dictionary<string, object>
                    {
                        {"is_deleted", false },
                        {"recyclebin_path",string.Empty }
                    };

                    int res = await dataAccess.UpdateRecord("Asset", "asset_id", mediaItem.MediaId, propsList);
                    ViewModel.RecycleBinMediaPlayerItems.Remove(mediaItem);
                    ViewModel.ApplyFilter();
                    await GlobalClass.Instance.AddtoHistoryAsync("Restore", $"Restored asset '{mediaItem.Title}' .");
                }
                else
                {
                    await GlobalClass.Instance.ShowDialogAsync("The file does not exist.", this.Content.XamlRoot);
                }
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

        private async Task DeleteMediaItemAsync(RecycleBinMediaPlayerItem mediaItem)
        {
            var result = await GlobalClass.Instance.ShowDialogAsync($"Are you sure that you want to permanently delete this file ? \n\n '{mediaItem.Title}' ?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");
            if (result == ContentDialogResult.Primary)
            {
                await DeleteFileAsync(mediaItem);
            }
        }
        private async Task DeleteFileAsync(RecycleBinMediaPlayerItem item)
        {
            if (ViewModel.RecycleBinMediaPlayerItems.Contains(item))
            {
                ViewModel.RecycleBinMediaPlayerItems.Remove(item);
                ViewModel.ApplyFilter();
                try
                {
                    File.Delete(item.RecycleBinPath);
                    File.Delete(item.RecycleBinThumbnailPath);
                    File.Delete(item.RecycleBinProxyPath);

                    //File.Delete(Path.Combine(item.MediaPath, item.ProxyPath.LocalPath));
                    await DeleteAssetAsync(item.MediaId,item.Title, item.MediaLibraryPath);
                    await GlobalClass.Instance.AddtoHistoryAsync("Asset Deleted Permenantly", $"Deleted asset '{ViewModel.MediaObj.RecycleBinPath}' permenantly.");

                }
                catch (IOException ex)
                {
                    await GlobalClass.Instance.ShowDialogAsync($"File Deletion Error !!!\n {ex.ToString()}", this.XamlRoot);
                }
            }
        }
        private async Task<int> DeleteAssetAsync(int assetId,string assetName, string assetPath)
        {
            List<MySqlParameter> parameters = new();
            parameters.Add(new MySqlParameter("@Asset_path", assetPath));
            parameters.Add(new MySqlParameter("@Asset_name", assetName));
           // int id = dataAccess.GetId($"Select asset_id from asset where asset_name = @Asset_name and asset_path = @Asset_path;", parameters);
            parameters.Add(new MySqlParameter("@Asset_id", assetId));
            var (affectedRows, newId, errorMessage) = await dataAccess.ExecuteNonQuery($"Delete from asset where asset_id=@Asset_id", parameters);
            if (affectedRows<1)
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
    }
    public class RecycleBinMediaPlayerItem : ObservableObject
    {
        private string recycleBinPath;
        private string thumbnailPath;
        private string recyclebinThumbnailPath;

        private string title;
        private int mediaId;
        private string mediaLibraryPath;
        private string proxyPath;
        private string recyclebinProxyPath;


        public int MediaId
        {
            get => mediaId;
            set { SetProperty(ref mediaId, value); }
        }
        public string RecycleBinPath
        {
            get => recycleBinPath;
            set { SetProperty(ref recycleBinPath, value); }
        }
        public string MediaLibraryPath
        {
            get => mediaLibraryPath;
            set { SetProperty(ref mediaLibraryPath, value); }
        }
        public string ThumbnailPath
        {
            get => thumbnailPath;
            set { SetProperty(ref thumbnailPath, value); }
        }
        public string ProxyPath
        {
            get => proxyPath;
            set { SetProperty(ref proxyPath, value); }
        }
        public string RecycleBinThumbnailPath
        {
            get => recyclebinThumbnailPath;
            set { SetProperty(ref recyclebinThumbnailPath, value); }
        }
        public string RecycleBinProxyPath
        {
            get => recyclebinProxyPath;
            set { SetProperty(ref recyclebinProxyPath, value); }
        }
        public string Title
        {
            get => title;
            set { SetProperty(ref title, value); }
        }
    }
    public class RecycleBinViewModel : ObservableObject
    {
        private RecycleBinMediaPlayerItem media;
        private MediaLibrary mediaLibrary;
        private string path;
        private string _filterTitle = string.Empty;
        private string assetCount = string.Empty;
        public string RatingCaption { get; set; } = "& Up";
        private ObservableCollection<string> tagsList = new();
        private int pageSize;
        private int _currentPageIndex;
        private int _selectedPageIndex;
        private RecycleBinMediaPlayerItem selectedItem;
        private List<RecycleBinMediaPlayerItem> _filteredItems = new();
        private ObservableCollection<RecycleBinMediaPlayerItem> _pagedRecycleBinRecycleBinMediaPlayerItems = new();
        public RecycleBinMediaPlayerItem SelectedItem
        {
            get => selectedItem;
            set { SetProperty(ref selectedItem, value); }
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

        public string FilterTitle
        {
            get => _filterTitle;
            set { SetProperty(ref _filterTitle, value); ApplyFilter(); }
        }


        private ObservableCollection<RecycleBinMediaPlayerItem> pagedRecycleBinRecycleBinMediaPlayerItems = new();


        public int PageSize
        {
            get => pageSize;
            set { SetProperty(ref pageSize, value); }
        }
        public int TotalPages => (int)Math.Ceiling((double)_filteredItems.Count / PageSize);
        private ObservableCollection<int> _totalPagesList = new();
        private bool isPrevVisible = true;
        private bool isNextVisible = true;
        private ObservableCollection<RecycleBinMediaPlayerItem> recycleBinMediaPlayerItems = new();

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
        public RecycleBinViewModel()
        {
            MediaObj = new RecycleBinMediaPlayerItem();
            MediaLibraryObj = new MediaLibrary();
            DeleteMediaCommand = new RelayCommand<RecycleBinMediaPlayerItem>(mediaItem =>
            {
                OnDeleteRequested?.Invoke(mediaItem);
            });

            RestoreMediaCommand = new RelayCommand<RecycleBinMediaPlayerItem>(mediaItem =>
            {
                OnRestoreRequested?.Invoke(mediaItem);
            });
            ApplyFilter();
        }
        public ObservableCollection<RecycleBinMediaPlayerItem> RecycleBinMediaPlayerItems { get => recycleBinMediaPlayerItems; set => SetProperty(ref recycleBinMediaPlayerItems, value); }
        public RecycleBinMediaPlayerItem MediaObj { get => media; set => SetProperty(ref media, value); }
        public MediaLibrary MediaLibraryObj { get => mediaLibrary; set => SetProperty(ref mediaLibrary, value); }
        public void ApplyFilter()
        {
            if (RecycleBinMediaPlayerItems != null )
            {
                _filteredItems = RecycleBinMediaPlayerItems.Where(item =>
                                 (string.IsNullOrEmpty(FilterTitle) || item.Title.Contains(FilterTitle, StringComparison.OrdinalIgnoreCase))).ToList();

                SelectedPageIndex = 0;
                UpdatePagedMediaItems();
                MediaLibraryObj.FileCount = _filteredItems.Count;
                AssetCount = $"{MediaLibraryObj.FileCount} results found";
                OnPropertyChanged(nameof(TotalPages)); // In case filter changes page count
                PaginationUpdated?.Invoke();
            }

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
        public ObservableCollection<RecycleBinMediaPlayerItem> PagedRecycleBinMediaPlayerItems
        {
            get => pagedRecycleBinRecycleBinMediaPlayerItems;
            set => SetProperty(ref pagedRecycleBinRecycleBinMediaPlayerItems, value);
        }
        private void UpdatePagedMediaItems()
        {
            var items = _filteredItems
                .Skip(SelectedPageIndex * PageSize)
                .Take(PageSize)
                .ToList();

            PagedRecycleBinMediaPlayerItems.Clear();
            foreach (var item in items)
            {
                PagedRecycleBinMediaPlayerItems.Add(item);
            }
            PaginationUpdated?.Invoke(); // In case you want to refresh buttons
        }
        public void ClearFilters()
        {
            FilterTitle = string.Empty;
            pagedRecycleBinRecycleBinMediaPlayerItems.Clear();
            UpdatePagedMediaItems();
        }
        public ICommand DeleteMediaCommand { get; }
        public ICommand RestoreMediaCommand { get; }

        public Action<RecycleBinMediaPlayerItem> OnDeleteRequested;
        public Action<RecycleBinMediaPlayerItem> OnRestoreRequested;
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


