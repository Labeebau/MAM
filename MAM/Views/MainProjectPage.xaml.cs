using MAM.Data;
using MAM.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainProjectPage : Page
    {
        public MainProjectPage()
        {
            this.InitializeComponent();
            ContentFrame.Navigated += ContentFrame_Navigated;
        }
       
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("MediaBinPage",typeof(MediaBinPage)),
            ("AdminPanelPage",typeof(AdminPanelPage)),
            ("ProcessesPage",typeof(ProcessesPage)),
            ("TransferJobPage",typeof(TransferJobsPage)),
            ("HistoryPage",typeof(HistoryPage)),
            ("RecycleBinPage",typeof(RecycleBinPage)),

        };
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            //if (args.IsSettingsInvoked)
            //{
            //	// Navigate to Settings Page
            //	ContentFrame.Navigate(typeof(SettingsPage));
            //}
            //else
            //{
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);

            //}

        }
        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
        {
            Type _page = null;
            if (navItemTag == "HomePage")
            {
                _page = typeof(MediaBinPage);
            }
            else if (navItemTag == "Settings")
            {
                _page = typeof(SettingsPage);
            }
            else
            {
                var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
                _page = item.Page;
            }
            var prevNavPAgeType = ContentFrame.CurrentSourcePageType;
            if (!(_page is null) && !Type.Equals(prevNavPAgeType, _page))
            {
                ContentFrame.Navigate(_page, null, recommendedNavigationTransitionInfo);
            }

        }


        private double NavViewCompactModeThresholdWidth { get { return NavView.CompactModeThresholdWidth; } }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView_Navigate("MediaBinPage", new EntranceNavigationTransitionInfo());
            if (!GlobalClass.Instance.IsAdmin)
            {
                AdminPanelItem.Visibility = Visibility.Collapsed;
            }
        }
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var pageType = e.SourcePageType;

            // Match the page type with tag or name of your Nav items
            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
            {
                if ((item.Tag as string) == pageType.Name)
                {
                    NavView.SelectedItem = item;
                    break;
                }
            }

            // Optional: also handle Settings
            if (pageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem;
            }
        }



        private void NavView_BackRequested(NavigationView sender,
                                           NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (NavView.IsPaneOpen &&
                (NavView.DisplayMode == NavigationViewDisplayMode.Compact ||
                 NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavView.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
                NavView.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                NavView.SelectedItem = NavView.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                NavView.Header =
                    ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();

            }
        }

    }
}
