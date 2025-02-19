using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using MAM.Utilities;
using MAM.Views.MediaBinViews;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.UserControls
{
    public sealed partial class CustomMedia : UserControl
    {
		private MediaPlayer _mediaPlayer;
		private MediaAssetViewModel viewModel;
        public event EventHandler DeleteRequested;

        public CustomMedia()
		{
			this.InitializeComponent();
			// Initialize MediaPlayer
			_mediaPlayer = new MediaPlayer();
			mediaPlayerElement.SetMediaPlayer(_mediaPlayer);
            // Update icon based on playback state change
            _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

			// Update slider based on media position
			_mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
           
            // Subscribe to MediaFailed to capture load errors
            _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
			viewModel = new MediaViewModel();
			//DataContext = this;
        }
        public MediaPlayerItem MediaItem
        {
            get => (MediaPlayerItem)GetValue(MediaItemProperty);
			set => SetValue(MediaItemProperty, value);
        }

		public static readonly DependencyProperty MediaItemProperty =
			DependencyProperty.Register("MediaItem", typeof(MediaPlayerItem), typeof(CustomMedia),
			new PropertyMetadata(null, OnMediaItemChanged));
        //public ICommand DeleteCommand
        //{
        //    get => (ICommand)GetValue(DeleteCommandProperty);
        //    set => SetValue(DeleteCommandProperty, value);
        //}

        //public static readonly DependencyProperty DeleteCommandProperty =
        //    DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(CustomMedia), new PropertyMetadata(null));


        private static void OnMediaItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
		    var control = (CustomMedia)d;
		    var newItem = (MediaPlayerItem)e.NewValue;
		    // Handle the update logic here
			control.OnMediaItemChangedInstance(newItem);
		}
		private void OnMediaItemChangedInstance(MediaPlayerItem item)
        {
            if (item != null)
			{
                SetMediaSource(item.MediaSource);
                viewModel.MediaObj.Title = item.Title;
                viewModel.MediaObj.Duration = item.Duration;
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
		public void SetProps(string title,string duration)
		{
			viewModel.MediaObj.Title = title;
			viewModel.MediaObj.Duration = duration;
		}
        public async void SetLocalMediaSource(string filePath)
        {
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            _mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			// Toggle play/pause
			if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
			{
				_mediaPlayer.Pause();
			}
			else
			{
				_mediaPlayer.Play();
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
			mediaPlayerElement.MediaPlayer.Play();
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

            DeleteRequested?.Invoke(this, EventArgs.Empty);

        }
		

	}
	public class Media:ObservableObject
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

    public class MediaViewModel : ObservableObject
    {
        private MediaAsset media;
        public MediaViewModel()
        {
            media = new MediaAsset();
        }
        public MediaAsset MediaObj { get => media; set => SetProperty(ref media, value); }
    }
}
