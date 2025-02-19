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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.UserControls
{
    public sealed partial class CustomBinMedia : UserControl
    {
        private MediaPlayer _mediaPlayer;
        private MediaAssetViewModel viewModel;
        public event EventHandler<CustomBinMedia> DeleteRequested;

        public CustomBinMedia()
        {
            this.InitializeComponent();
            //_mediaPlayer = new MediaPlayer();
            //mediaPlayerElement.SetMediaPlayer(_mediaPlayer);
            //         _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            //_mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            //         _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
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
        //public ICommand DeleteCommand
        //{
        //	get => (ICommand)GetValue(DeleteCommandProperty);
        //	set => SetValue(DeleteCommandProperty, value);
        //}

        //public static readonly DependencyProperty DeleteCommandProperty =
        //	DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(CustomMedia), new PropertyMetadata(null));



        private static void OnMediaItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CustomBinMedia)d;
            var newItem = (MediaPlayerItem)e.NewValue;
            control.OnMediaItemChangedInstance(newItem);
        }
        private void OnMediaItemChangedInstance(MediaPlayerItem item)
        {
            if (item != null)
            {
                //SetMediaSource(item.MediaSource);
                var bitmapImage = new BitmapImage(item.ThumbnailPath);
                Thumbnail.Source = bitmapImage;
                viewModel.MediaObj.Title = item.Title;
                viewModel.MediaObj.Duration = item.DurationString;
            }
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
            // Toggle play/pause
            if (_mediaPlayer != null)
            {

                if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    _mediaPlayer.Pause();
                }
                else
                {
                    _mediaPlayer.Play();
                }
            }

        }
        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            // Capture the error details
            var errorMessage = $"Error loading media: {args.ErrorMessage}";
            System.Diagnostics.Debug.WriteLine(errorMessage);

            // Optionally, display the error to the user
        }
        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            // Update the button icon based on the playback state
            DispatcherQueue.TryEnqueue(() =>
            {
                PlayPauseIcon.Text = _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
                    ? "\uf04c" // Unicode for pause
                    : "\uf04b"; // Unicode for play
            });
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            // Update the slider position based on media playback
            //DispatcherQueue.TryEnqueue(() =>
            //{
            //	progressSlider.Value = (_mediaPlayer.PlaybackSession.Position.TotalSeconds /
            //							_mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds) * 100;
            //});
        }

        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // Update media position when slider is changed by the user
            if (_mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds > 0)
            {
                var position = _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds * (e.NewValue / 100);
                _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(position);
            }
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
