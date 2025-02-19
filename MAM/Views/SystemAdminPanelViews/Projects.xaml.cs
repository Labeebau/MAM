using MAM.Views.AdminPanelViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.SystemAdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Projects : Page
    {
        public ObservableCollection<Project> ProjectList {  get; set; }

        public Projects()
        {
            this.InitializeComponent();
           
            ProjectList = new ObservableCollection<Project>
        {
            new Project { Name = "Project 1" },
            new Project { Name = "Project 2"},
            new Project { Name = "Project 3"}
        };
            DataContext = this;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {

        }

        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("General",typeof(GeneralPage)),
            ("UserGroups",typeof(UserGroupsPage)),
            ("Users",typeof(UsersPage)),
            ("ProcessServer",typeof(ProcessServerPage)),
            ("FileServer",typeof(FileServerPage)),
            ("ArchiveServer",typeof(ArchiveServerPage)),
            ("History",typeof(HistoryPage))

        };
        private void navProject_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            navProject_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
        }
        private void navProject_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
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



        private void navProject_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //hrow new NotImplementedException();
        }
        private void navProject_Loaded(object sender, RoutedEventArgs e)
        {


            ////	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;

            ////	// navProject doesn't load any page by default, so load home page.
            //navProject.SelectedItem = navProject.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            //navProject_Navigate("HomePage", new EntranceNavigationTransitionInfo());
        }




        private void navProject_BackRequested(NavigationView sender,
                                           NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (navProject.IsPaneOpen &&
                (navProject.DisplayMode == NavigationViewDisplayMode.Compact ||
                 navProject.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            navProject.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of navProject.MenuItems, and doesn't have a Tag.
                navProject.SelectedItem = (NavigationViewItem)navProject.SettingsItem;
                navProject.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                navProject.SelectedItem = navProject.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                navProject.Header =
                    ((NavigationViewItem)navProject.SelectedItem)?.Content?.ToString();

            }
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            //navProject_Navigate("MainProjectPage");
           App.MainAppWindow.Mainframe.Navigate(typeof(MainProjectPage));  // Navigate to HomePage
        }
    }
    public class Project
    {
        public string Name { get; set; }
       // public string Description { get; set; }
    }
}
