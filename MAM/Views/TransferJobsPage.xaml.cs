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
using MAM.Views.ProcessesViews;
using Microsoft.UI.Xaml.Media.Animation;
using MAM.Views.TransferJobs;
//using MAM.Views.Transfer_Jobs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TransferJobsPage : Page
    {
        public TransferJobsPage()
        {
            this.InitializeComponent();
        }
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("WaitingFileTransfer",typeof(TransferJobStatus)),
            ("FinishedFileTransfer",typeof(TransferJobStatus)),
        };
        private void navProcesses_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            navProcesses_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
        }
        private void navProcesses_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
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
            //navProcesses_Navigate("HomePage", new EntranceNavigationTransitionInfo());
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
