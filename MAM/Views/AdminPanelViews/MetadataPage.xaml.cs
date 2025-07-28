using MAM.Views.AdminPanelViews.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MetadataPage : Page
    {
        public MetadataPage()
        {
            this.InitializeComponent();
            //NavigationViewItemInvokedEventArgs args;
            //navMetadata_Navigate("Metadata", recommendedNavigationTransitionInfo);

        }
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("Metadata",typeof(Metadata.Metadata)),
            //("Metadata",typeof(MetadataTab)),
            // ("MetadataGroups",typeof(MetadataGroups)),
            ("Categories",typeof(Categories)),
            ("SupportedFormats",typeof(SupportedFormats)),

        };
        private void navMetadata_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            navMetadata_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);



        }
        private void navMetadata_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
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



        private void navMetadata_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }
       


        private void navMetadata_BackRequested(NavigationView sender,
                                           NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (navMetadata.IsPaneOpen &&
                (navMetadata.DisplayMode == NavigationViewDisplayMode.Compact ||
                 navMetadata.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }
        private void navMetadata_Loaded(object sender, RoutedEventArgs e)
        {


            ////	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;

            //////	// navMetadata doesn't load any page by default, so load home page.
            navMetadata.SelectedItem = navMetadata.MenuItems[0];
            //////	// If navigation occurs on SelectionChanged, this isn't needed.
            //////	// Because we use ItemInvoked to navigate, we need to call Navigate
            //////	// here to load the home page.
            navMetadata_Navigate("Metadata", new EntranceNavigationTransitionInfo());
          //  ContentFrame.Navigate(typeof([0]), null, new EntranceNavigationTransitionInfo());

        }
        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            navMetadata.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of navMetadata.MenuItems, and doesn't have a Tag.
                navMetadata.SelectedItem = (NavigationViewItem)navMetadata.SettingsItem;
                navMetadata.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                navMetadata.SelectedItem = navMetadata.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                navMetadata.Header =
                    ((NavigationViewItem)navMetadata.SelectedItem)?.Content?.ToString();

            }
        }
    }
    
}
