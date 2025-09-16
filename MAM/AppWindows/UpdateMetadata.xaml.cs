using MAM.Data;
using MAM.Views.MediaBinViews;
using MAM.Views.MediaBinViews.AssetMetadata;
using MAM.Windows;
using Microsoft.UI;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.AppWindows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UpdateMetadata : Window
    {
        private static UpdateMetadata _instance;
        public static MediaPlayerViewModel ViewModel { get; set; }
        public UpdateMetadata()
        {
            this.InitializeComponent();
            // Customize the title bar
            var titleBar = AppWindow.TitleBar;
            // Set the background colors for active and inactive states
            titleBar.BackgroundColor = Colors.Black;
            titleBar.InactiveBackgroundColor = Colors.DarkGray;
            // Set the foreground colors (text/icons) for active and inactive states
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveForegroundColor = Colors.Gray;
            GlobalClass.Instance.DisableMaximizeButton(this);
            GlobalClass.Instance.SetWindowSizeAndPosition(700, 600, this);
        }
        public static void ShowWindow(MediaPlayerViewModel viewModel)
        {
            if (_instance == null)
            {
                ViewModel = viewModel;
                _instance = new UpdateMetadata();
                // When the window closes, clear the instance so it can be opened again
                _instance.Closed += (s, e) => _instance = null;
            }
            _instance.Activate();

        }
        private readonly List<(string Tag, Type Page)> _pages = new()
        {
            ("General",typeof(General)),
            ("FileInfo",typeof(FileInfoPage)),
            ("Categories",typeof(AssetCategoriesPage)),
            ("Collection",typeof(CollectionPage)),
            ("Tags",typeof(TagsPage)),
            ("Metadata",typeof(AssetMetadata)),

        };


        private void navMetadata_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            navMetadata_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo, ViewModel);
        }
        private void navMetadata_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo, object parameter)
        {
            Type _page = null;
            var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
            _page = item.Page;
            var prevNavPageType = ContentFrame.CurrentSourcePageType;
            if (!(_page is null))// && !Type.Equals(prevNavPageType, _page))
            {
                // Pass the parameter here
                ContentFrame.Navigate(_page, parameter, recommendedNavigationTransitionInfo);
            }
        }

        private void navMetadata_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //throw new NotImplementedException();
        }
        private void navMetadata_Loaded(object sender, RoutedEventArgs e)
        {


            //	// Add handler for ContentFrame navigation.
            //ContentFrame.Navigated += On_Navigated;
            string navItemTag = "General";

            //parameter = (Dictionary<string, object>)GetParameterBasedOnTag(navItemTag, metadata);
            //parameter = (string)GetParameterBasedOnTag("FileInfo"); // Example: determine the parameter to pass

            ////	// navMetadata doesn't load any page by default, so load home page.
            navMetadata.SelectedItem = navMetadata.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            navMetadata_Navigate("General", new EntranceNavigationTransitionInfo(), ViewModel);
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
    }
}
