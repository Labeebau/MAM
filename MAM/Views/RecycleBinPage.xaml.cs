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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecycleBinPage : Page
    {
        public ObservableCollection<MediaItem> MediaItems { get; set; }

        public RecycleBinPage()
        {
            this.InitializeComponent();
            this.SizeChanged += ProjectWindow_SizeChanged;
            MediaItems = new ObservableCollection<MediaItem>
            {
                //new MediaItem { MediaSource = new Uri("https://www.w3schools.com/html/mov_bbb.mp4"), Title = "Video 1" },
                //new MediaItem { MediaSource = new Uri("E:\\Labeeba\\Projects\\MAM\\MAM\\MAM\\Assets\\Elysium_trailer_4K.mkv"), Title = "Video 2" },
                //new MediaItem { MediaSource = new Uri("ms-appx:///Assets/AlanWalkerFaded.mp4"), Title = "Video 2" },
                //new MediaItem { MediaSource = new Uri("ms-appx:///Assets/Nilamanaltharikalil.mp4"), Title = "Video 3" },
                //new MediaItem { MediaSource = new Uri("ms-appx:///Assets/HamariAdhuriKahani.mp4"), Title = "Video 5" },
                //new MediaItem { MediaSource = new Uri("ms-appx:///Assets/Shawn Mendes - Treat You Better.mp4"), Title = "Video 5" },
            };
        }
        private void CustomMediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is CustomBinMedia customMediaPlayer && customMediaPlayer.DataContext is MediaPlayerItem item)
            {
                customMediaPlayer.SetMediaSource(item.MediaSource);
                // customMediaPlayer.SetLocalMediaSource(item.MediaSource.ToString());

            }
        }
        private void ProjectWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainGrid.Width = e.NewSize.Width - 10;  // Adjust size with padding if necessary
            MainGrid.Height = e.NewSize.Height - 10;  // Adjust size with padding if necessary

            //GridviewPanel.Width = e.NewSize.Width - 20;  // Adjust size with padding if necessary
            //GridviewPanel.Height = e.NewSize.Height - 20;  // Adjust size with padding if necessary
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
        //private void ProjectWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        //{
        //    GDV.Width = e.Size.Width - 10; ; // Adjust size with padding if necessary
        //}
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class MediaItem
    {
        public Uri MediaSource { get; set; }
        public string Title { get; set; } // Optional: Add a title or other metadata
    }
}
