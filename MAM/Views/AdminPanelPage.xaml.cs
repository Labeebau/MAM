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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using MAM.Views;
using MAM.Views.AdminPanelViews;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class AdminPanelPage : Page
	{

		public AdminPanelPage()
		{
			this.InitializeComponent();
            ContentFrame.Navigated += ContentFrame_Navigated;

        }


        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
		{
			("GeneralPage",typeof(GeneralPage)),
			("MailSettingsPage",typeof(MailSettingsPage)),
			("UserGroupsPage",typeof(UserGroupsPage)),
			("UsersPage",typeof(UsersPage)),
			("AuthorizationSettingsPage",typeof(AuthorizationSetingsPage)),
			("MetaDataPage",typeof(MetadataPage)),
            ("ProcessServerPage",typeof(ProcessServerPage)),
            ("FileServerPage",typeof(FileServerPage)),
            ("ArchiveServerPage",typeof(ArchiveServerPage)),
            ("TargetLocationsPage",typeof(TargetLocationsPage)),
            ("RSSListPage",typeof(RSSListPage))

        };
		private void navAdmin_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			var navItemTag = args.InvokedItemContainer.Tag.ToString();
			navAdmin_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
		}
		private void navAdmin_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
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


	
		private void navAdmin_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			//hrow new NotImplementedException();
		}
		private void navAdmin_Loaded(object sender, RoutedEventArgs e)
		{


			////	// Add handler for ContentFrame navigation.
			//ContentFrame.Navigated += On_Navigated;

			////	// navAdmin doesn't load any page by default, so load home page.
			navAdmin.SelectedItem = navAdmin.MenuItems[0];
			////	// If navigation occurs on SelectionChanged, this isn't needed.
			////	// Because we use ItemInvoked to navigate, we need to call Navigate
			////	// here to load the home page.
			navAdmin_Navigate("GeneralPage", new EntranceNavigationTransitionInfo());
		}
		



		private void navAdmin_BackRequested(NavigationView sender,
										   NavigationViewBackRequestedEventArgs args)
		{
			TryGoBack();
		}

		private bool TryGoBack()
		{
			if (!ContentFrame.CanGoBack)
				return false;

			// Don't go back if the nav pane is overlayed.
			if (navAdmin.IsPaneOpen &&
				(navAdmin.DisplayMode == NavigationViewDisplayMode.Compact ||
				 navAdmin.DisplayMode == NavigationViewDisplayMode.Minimal))
				return false;

			ContentFrame.GoBack();
			return true;
		}
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var pageType = e.SourcePageType;

            // Match the page type with tag or name of your Nav items
            foreach (var item in navAdmin.MenuItems.OfType<NavigationViewItem>())
            {
                if ((item.Tag as string) == pageType.Name)
                {
                    navAdmin.SelectedItem = item;
                    break;
                }
            }
            // Optional: also handle Settings
            if (pageType == typeof(SettingsPage))
            {
                navAdmin.SelectedItem = navAdmin.SettingsItem;
            }
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
		{
			navAdmin.IsBackEnabled = ContentFrame.CanGoBack;

			if (ContentFrame.SourcePageType == typeof(SettingsPage))
			{
				// SettingsItem is not part of navAdmin.MenuItems, and doesn't have a Tag.
				navAdmin.SelectedItem = (NavigationViewItem)navAdmin.SettingsItem;
				navAdmin.Header = "Settings";
			}
			else if (ContentFrame.SourcePageType != null)
			{
				// Select the nav view item that corresponds to the page being navigated to.
				navAdmin.SelectedItem = navAdmin.MenuItems
							.OfType<NavigationViewItem>()
							.First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

				navAdmin.Header =
					((NavigationViewItem)navAdmin.SelectedItem)?.Content?.ToString();

			}
		}

	}
}
