using MAM.Data;
using MAM.Views.ProcessesViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Data;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProcessesPage : Page
    {
        private static DataAccess dataAccess = new DataAccess();
        private string openedTab = string.Empty;
        public ObservableCollection<Process> ProcessList { get; set; } = new();
        public ProcessesPage()
        {
            this.InitializeComponent();


            DataContext = this;
        }
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("WaitingProcesses",typeof(ProcessStatusPage)),
            ("FinishedProcesses",typeof(ProcessStatusPage))
        };
        private void navProcesses_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
                openedTab = args.InvokedItemContainer.Tag.ToString();
            //if (openedTab == "WaitingProcesses")
            //    ProcessList = GetProcesses("Waiting");
            //else if (openedTab == "FinishedProcesses")
            //    ProcessList = GetProcesses("Finished");
            navProcesses_Navigate(openedTab, args.RecommendedNavigationTransitionInfo);
        }
        private void navProcesses_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
        {
            Type _page = null;

            var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
            if (string.IsNullOrEmpty(item.Tag))
                return;
            _page = item.Page;
            var prevNavPAgeType = ContentFrame.CurrentSourcePageType;
            if (!(_page is null))
            {
                ContentFrame.Navigate(_page, openedTab, recommendedNavigationTransitionInfo);
            }
        }


        private void navProcesses_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }
        private void navProcesses_Loaded(object sender, RoutedEventArgs e)
        {


            ////	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;

            ////	// navProcesses doesn't load any page by default, so load home page.
            //navProcesses.SelectedItem = navProcesses.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            navProcesses.SelectedItem = navProcesses.MenuItems[0];// _pages[0];
            navProcesses_Navigate("WaitingProcesses", new EntranceNavigationTransitionInfo());

            //navMediaBin.SelectedItem = navMediaBin.MenuItems[0];
            //navMediaBin_Navigate("MediaLibraryPage", new EntranceNavigationTransitionInfo());
        }

        private void navProcesses_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (navProcesses.IsPaneOpen &&
                (navProcesses.DisplayMode == NavigationViewDisplayMode.Compact ||
                 navProcesses.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            navProcesses.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of navProcesses.MenuItems, and doesn't have a Tag.
                navProcesses.SelectedItem = (NavigationViewItem)navProcesses.SettingsItem;
                navProcesses.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                navProcesses.SelectedItem = navProcesses.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                navProcesses.Header =
                    ((NavigationViewItem)navProcesses.SelectedItem)?.Content?.ToString();

            }
        }
    }
}
