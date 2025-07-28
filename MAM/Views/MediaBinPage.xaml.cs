using MAM.Views.MediaBinViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaBinPage : Page
	{
		// Collection of media player items
		public ObservableCollection<MediaPlayerItem> MediaPlayerItems { get; set; }
		public MediaBinPage()
		{
			this.InitializeComponent();
			
		} 
		
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("MediaLibraryPage",typeof(MediaLibraryPage)),
            ("ArchivePage",typeof(ArchivePage)),
        };
        private void navMediaBin_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            navMediaBin_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
        }
        private void navMediaBin_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
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



        private void navMediaBin_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }



        private void navMediaBin_BackRequested(NavigationView sender,
                                           NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (navMediaBin.IsPaneOpen &&
                (navMediaBin.DisplayMode == NavigationViewDisplayMode.Compact ||
                 navMediaBin.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }
        private void navMediaBin_Loaded(object sender, RoutedEventArgs e)
        {


            ////	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;

            //////	// navMediaBin doesn't load any page by default, so load home page.
            navMediaBin.SelectedItem = navMediaBin.MenuItems[0];
            //////	// If navigation occurs on SelectionChanged, this isn't needed.
            //////	// Because we use ItemInvoked to navigate, we need to call Navigate
            //////	// here to load the home page.
            navMediaBin_Navigate("MediaLibraryPage", new EntranceNavigationTransitionInfo());
            //  ContentFrame.Navigate(typeof([0]), null, new EntranceNavigationTransitionInfo());

        }
        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            navMediaBin.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of navMediaBin.MenuItems, and doesn't have a Tag.
                navMediaBin.SelectedItem = (NavigationViewItem)navMediaBin.SettingsItem;
                navMediaBin.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                navMediaBin.SelectedItem = navMediaBin.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                navMediaBin.Header =
                    ((NavigationViewItem)navMediaBin.SelectedItem)?.Content?.ToString();

            }
        }
    }
	//public class MediaPlayerItem
	//{
	//	public Uri MediaSource { get; set; }
	//	public string Title { get; set; } // Optional: Add a title or other metadata
	//}
}
