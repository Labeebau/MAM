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
using MAM.Utilities;
using MAM.Views;
using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;
using Windows.Media.Core;
using Windows.Media.Playback;
using System.Windows.Input;
using MAM.Windows;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.UserControls
{
    public sealed partial class CustomRecycleBinMedia : UserControl
    {
        private MediaPlayer _mediaPlayer;
        private RecycleBinMediaViewModel viewModel;
       

        public CustomRecycleBinMedia()
        {
            this.InitializeComponent();
            viewModel = new RecycleBinMediaViewModel();
        }
        public RecycleBinMediaPlayerItem MediaItem
        {
            get => (RecycleBinMediaPlayerItem)GetValue(MediaItemProperty);
            set => SetValue(MediaItemProperty, value);
        }

        public static readonly DependencyProperty MediaItemProperty =
            DependencyProperty.Register("MediaItem", typeof(RecycleBinMediaPlayerItem), typeof(CustomRecycleBinMedia),
            new PropertyMetadata(null, OnMediaItemChanged));

        private static void OnMediaItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomRecycleBinMedia control)
            {
                if (e.NewValue is RecycleBinMediaPlayerItem newItem)
                {
                    // Update UI elements based on the new item
                    control.OnMediaItemChangedInstance(newItem);
                  
                }
            }
        }
        private void OnMediaItemChangedInstance(RecycleBinMediaPlayerItem item)
        {
            if (item != null)
            {
                // Load thumbnail image
                var bitmapImage = new BitmapImage(new Uri( item.RecycleBinThumbnailPath));//\\Sims - demo\share\MAM\Recycle Bin\Thumbnail\\Kadhal Rojave - Roja
                Thumbnail.Source = bitmapImage;                                            

                // Update ViewModel properties
                viewModel.MediaObj.Title = item.Title;
                //viewModel.MediaObj.Duration = item.DurationString;

            }
        }
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand),
                typeof(CustomRecycleBinMedia), new PropertyMetadata(null));

        public ICommand RestoreCommand
        {
            get => (ICommand)GetValue(RestoreCommandProperty);
            set => SetValue(RestoreCommandProperty, value);
        }

        public static readonly DependencyProperty RestoreCommandProperty =
            DependencyProperty.Register(nameof(RestoreCommand), typeof(ICommand),
                typeof(CustomRecycleBinMedia), new PropertyMetadata(null));
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            //mediaPlayerElement.MediaPlayer.Play();
            // Handle custom button click

        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeleteCommand?.CanExecute(MediaItem) == true)
            {
                DeleteCommand.Execute(MediaItem);
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (RestoreCommand?.CanExecute(MediaItem) == true)
            {
                RestoreCommand.Execute(MediaItem);
            }
        }
    }
    public class RecycleBinMedia : ObservableObject
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

    public class RecycleBinMediaViewModel : ObservableObject
    {
        private RecycleBinMedia media;
        public RecycleBinMediaViewModel()
        {
            media = new RecycleBinMedia();
        }
        public RecycleBinMedia MediaObj { get => media; set => SetProperty(ref media, value); }
    }
}
