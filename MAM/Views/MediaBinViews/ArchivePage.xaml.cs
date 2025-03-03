using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
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
        public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get; set; }
       // MediaLibraryPage archivePage = new MediaLibraryPage();
        public ArchivePage()
        {
            this.InitializeComponent();
            //LoadFiles();
        }
        private void LoadFiles()
        {
            //if (Directory.Exists(System.IO.Path.Combine(archive.BinName, "Proxy")))
            //{
            //    archive.ProxyFolder = System.IO.Path.Combine(archive.BinName, "Proxy");
          //  MediaLibraryPage.GetAsset(GlobalClass.Instance.MediaLibraryPath);
            //}
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
    }
    //public class MediaPlayerItem
    //{
    //    public Uri MediaSource { get; set; }
    //    public string Title { get; set; } // Optional: Add a title or other metadata
    //}
}
