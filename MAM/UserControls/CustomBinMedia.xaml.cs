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
using Windows.Media.Core;
using Windows.Media.Playback;
using System.Diagnostics;
using Windows.Storage;
using MAM.Utilities;
using MAM.Views.AdminPanelViews.Metadata;
using MAM.Views.MediaBinViews;
using Google.Protobuf.WellKnownTypes;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using MAM.Windows;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.UserControls
{
    public sealed partial class CustomBinMedia : UserControl
    {
        private MediaPlayer _mediaPlayer;
        private MediaAssetViewModel viewModel;
        public event EventHandler<CustomBinMedia> DeleteRequested;
        public event EventHandler<CustomBinMedia> PlayButtonClicked;


        public CustomBinMedia()
        {
            this.InitializeComponent();
            viewModel = new MediaAssetViewModel();
        }
        public MediaPlayerItem MediaItem
        {
            get => (MediaPlayerItem)GetValue(MediaItemProperty);
            set => SetValue(MediaItemProperty, value);
        }

        public static readonly DependencyProperty MediaItemProperty =
            DependencyProperty.Register("MediaItem", typeof(MediaPlayerItem), typeof(CustomBinMedia),
            new PropertyMetadata(null, OnMediaItemChanged));

        private static void OnMediaItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomBinMedia control)
            {
                if (e.OldValue is MediaPlayerItem oldItem)
                {
                    // Unsubscribe from the old item to prevent memory leaks
                    oldItem.PropertyChanged -= control.MediaItem_PropertyChanged;
                }

                if (e.NewValue is MediaPlayerItem newItem)
                {
                    // Update UI elements based on the new item
                    control.OnMediaItemChangedInstance(newItem);

                    // Subscribe to listen for property changes
                    newItem.PropertyChanged += control.MediaItem_PropertyChanged;
                }
            }
        }
        private void OnMediaItemChangedInstance(MediaPlayerItem item)
        {
            if (item != null)
            {
                // Load thumbnail image
                if (item.ThumbnailPath != null)
                {
                    var bitmapImage = new BitmapImage(new Uri(item.ThumbnailPath));
                    Thumbnail.Source = bitmapImage;
                }
                // Update ViewModel properties
                viewModel.MediaObj.Title = item.Title;
                viewModel.MediaObj.Duration = item.DurationString;

                // Update ArchiveLabel visibility
                UpdateArchiveLabelVisibility(item.IsArchived);
            }
        }
        private void MediaItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MediaPlayerItem mediaItem && e.PropertyName == nameof(MediaPlayerItem.IsArchived))
            {
                UpdateArchiveLabelVisibility(mediaItem.IsArchived);
            }
        }
        private void UpdateArchiveLabelVisibility(bool isArchived)
        {
            ArchiveLabel.Visibility = isArchived ? Visibility.Visible : Visibility.Collapsed;
        }


      
        // Method to set media source
        public void SetMediaSource(Uri mediaUri)
        {
            if (_mediaPlayer != null && mediaUri != null)
            {
                _mediaPlayer.Source = MediaSource.CreateFromUri(mediaUri);

            }
            //_mediaPlayer.Source = MediaSource.CreateFromUri(mediaUri);
        }
        public void SetProps(string title, string duration)
        {
            viewModel.MediaObj.Title = title;
            viewModel.MediaObj.Duration = duration;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClicked?.Invoke(this, this);


        }
       


        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            //mediaPlayerElement.MediaPlayer.Play();
            // Handle custom button click

        }
        private void VersionListButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle custom button click

        }
        private void AuthorizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle custom button click

        }
        public void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, this);

        }


    }
    public class MediaAsset : ObservableObject
    {
        private string title = string.Empty;
        private string duration;// = DateTime.Now.Hour;

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }
        public string Duration
        {
            get => duration;
            set => SetProperty(ref duration, value);
        }
    }

    public class MediaAssetViewModel : ObservableObject
    {
        private MediaAsset media;
        public MediaAssetViewModel()
        {
            media = new MediaAsset();
        }

        public MediaAsset MediaObj { get => media; set => SetProperty(ref media, value); }
    }
}
