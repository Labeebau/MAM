using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews.AssetMetadata;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Windows.Graphics;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;
using Color = Windows.UI.Color;
using Composition = Microsoft.UI.Composition;
using MediaPlayerItem = MAM.Views.MediaBinViews.MediaPlayerItem;
using Path = System.IO.Path;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AssetWindow : Window
    {
        private static MediaPlayer mediaPlayer;

        // Static instance to track the window
        private static AssetWindow _instance;
        private bool isDraggingVerticalSplitter = false;
        private bool isDraggingHorizontalSplitter = false;
        private double initialPointerX, initialPointerY;
        private bool isSeeking;// To prevent conflicts between playback and seeking

        public MediaPlayerViewModel ViewModel { get; }
        // public static Uri MediaPath { get; private set; }
        public static MediaPlayerItem MediaItem { get; private set; }
        private bool _isDraggingLeft = false;
        private bool _isDraggingRight = false;
        private double _startX;
        // Event to handle the seek position
        public event Action<string> OnLeftTrimPositionChanged;
        public event Action<string> OnRightTrimPositionChanged;
        private Composition.Compositor _compositor;
        private Composition.SpriteVisual _blurVisual;
        private Composition.CompositionEffectBrush _blurBrush;



        public AssetWindow(ObservableCollection<MediaPlayerItem> mediaPlayerItemList)
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
            SetWindowSizeAndPosition(1300, 1000);

            mediaPlayer = new MediaPlayer();
            mpAsset.SetMediaPlayer(mediaPlayer);
            ViewModel = new MediaPlayerViewModel(MediaItem);

            ViewModel.MediaPlayer = mediaPlayer;
            foreach (MediaPlayerItem item in mediaPlayerItemList)
            {
                ViewModel.BinThumbnails.Add(new BinThumbnail { ThumbnailUri = item.ThumbnailPath, MediaUri = item.ProxyPath });
            }

            // Set the initial media source
            if (ViewModel.Media.MediaSource != null)
            {
                mediaPlayer.Source = MediaSource.CreateFromUri(ViewModel.Media.ProxyPath);
            }
            ViewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.Media) && ViewModel.Media.ProxyPath != null)
                {
                    mediaPlayer.Source = MediaSource.CreateFromUri(ViewModel.Media.ProxyPath);
                    ViewModel.Media = mediaPlayerItemList.FirstOrDefault(m => m.ProxyPath == ViewModel.Media.ProxyPath);
                    var file =await StorageFile.GetFileFromPathAsync(ViewModel.Media.MediaSource.LocalPath.ToString());
                    await GetAllMetadataAsync(file);
                    navMetadata_Navigate("FileInfo", new EntranceNavigationTransitionInfo(), ViewModel.Metadata);
                }
            };

            // Set event handlers for playback updates


            AddSpeedButtons();
            AddTimeButtons();
            UpdateProgress(75);
            RootGrid.DataContext = ViewModel;

            // Attach Pointer Events for the Thumbs
            LeftThumb.PointerPressed += OnLeftThumbPointerPressed;
            LeftThumb.PointerMoved += OnLeftThumbPointerMoved;
            LeftThumb.PointerReleased += OnThumbPointerReleased;

            RightThumb.PointerPressed += OnRightThumbPointerPressed;
            RightThumb.PointerMoved += OnRightThumbPointerMoved;
            RightThumb.PointerReleased += OnThumbPointerReleased;
            OnLeftTrimPositionChanged += UpdateLeftTrimTime;
            OnRightTrimPositionChanged += UpdateRighgtTrimTime;

            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.PlaybackSession.PlaybackStateChanged += MediaPlayer_PlaybackStateChanged;
            mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            mediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
            //Canvas.SetZIndex(TxtClipName, -1);
            TxtClipName.TextWrapping = TextWrapping.Wrap;
            this.Activated += MainWindow_Activated;

        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            StorageFile file = null;
            if (ViewModel.Media.MediaSource != null)
                file = await StorageFile.GetFileFromPathAsync(ViewModel.Media.MediaSource.LocalPath.ToString());
            if (file != null)
                await GetAllMetadataAsync(file);
        }

        private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
        {
            App.UIDispatcherQueue.TryEnqueue(() =>
            {
                VolumeBar.Value = sender.Volume;
            });
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            // Update the Slider value as the media plays
            if (!isSeeking && !(sender.PlaybackState == MediaPlaybackState.None)) // Avoid updating Slider when the user is seeking
            {
                App.UIDispatcherQueue.TryEnqueue(() =>
                {
                    SeekBar.Value = sender.Position.TotalSeconds;
                    if (sender.Position.Hours > 0)
                        ViewModel.CurrentPosition = sender.Position.ToString(@"hh\:mm\:ss");
                    else
                        ViewModel.CurrentPosition = sender.Position.ToString(@"mm\:ss");

                });
                
            }
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // Set the Slider maximum to the media duration when the media is loaded
            DispatcherQueue.TryEnqueue(async () =>
            {
                ViewModel.CurrentPosition = "00:00:00";
                var naturalDuration = mediaPlayer.PlaybackSession.NaturalDuration;
                if (naturalDuration > TimeSpan.Zero) // Check for valid duration
                {
                    SeekBar.Maximum = naturalDuration.TotalSeconds;
                    if (naturalDuration.Hours > 0)
                        ViewModel.TotalDuration = naturalDuration.ToString(@"hh\:mm\:ss");
                    else
                        ViewModel.TotalDuration = naturalDuration.ToString(@"mm\:ss");
                    await GenerateThumbnailsAsync();
                }
                //TxtClipName.Visibility = Visibility.Collapsed;
                //TBTrimmedClipDuration.Visibility = Visibility.Collapsed;
                Canvas.SetLeft(LeftThumb, 0);
                Canvas.SetLeft(RightThumb, ThumbnailScrollViewer.ActualWidth + ThumbnailScrollViewer.Margin.Right);
                Canvas.SetLeft(LeftOverlay, 0);
                Canvas.SetLeft(RightOverlay, ThumbnailPanel.Width);
                LeftOverlay.Width = 0;
                txtLeftThumbX = 0;
                txtRightThumbX = 0;
                ViewModel.TrimmedClip.InTime = "0";
                ViewModel.TrimmedClip.OutTime = ViewModel.TotalDuration;
                ViewModel.TrimmedClip.Duration = ViewModel.TotalDuration;
                TransformHorizontally(TxtInTime, 0);
                TransformHorizontally(TxtOutTime, 0);
                TransformHorizontally(TBTrimmedClipDuration, Canvas.GetLeft(RightThumb) / 2);
                // await LoadTrimmedClipsAsync(ViewModel.Media.MediaSource.LocalPath.ToString());

                var file = await StorageFile.GetFileFromPathAsync(ViewModel.Media.MediaSource.LocalPath.ToString());
                if (file != null)
                    await GetAllMetadataAsync(file);
            });
        }
        private void MediaPlayer_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            App.UIDispatcherQueue.TryEnqueue(() =>
            {
                ViewModel.PlayPauseIcon = mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
                     ? "\uf04c" // Unicode for pause
                     : "\uf04b"; // Unicode for play
            });
        }
        public static void ShowWindow(MediaPlayerItem mediaPlayerItem, ObservableCollection<MediaPlayerItem> mediaPlayerItemList)
        {
            MediaItem = mediaPlayerItem;
            if (_instance == null)
            {
                _instance = new AssetWindow(mediaPlayerItemList);
                _instance.Activate(); // Show the window
            }
            else
            {
                // _instance = new AssetWindow(mediaPlayerItemList);
                _instance.Activate(); // Bring the existing window to the front
            }
        }
        public static void SetMediaSource(Uri mediaUri)
        {
            if (mediaPlayer != null && mediaUri != null)
            {
                mediaPlayer.Source = MediaSource.CreateFromUri(mediaUri);
            }
        }
        private void SetWindowSizeAndPosition(int width, int height)
        {
            // Get the native window handle of the current window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Get the window ID from the handle
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            // Retrieve the AppWindow using the static method GetFromWindowId
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // Resize the window to the specified size
                appWindow.Resize(new SizeInt32(width, height));

                // Get the screen size
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                var workArea = displayArea.WorkArea;

                // Calculate the center position
                int centerX = (workArea.Width - width) / 2;
                int centerY = (workArea.Height - height) / 2;

                // Move the window to the center of the screen
                appWindow.Move(new PointInt32(centerX, centerY));
            }
        }
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            //DisposeResources();

            //DetachDataContext();


            //this.Close();
            _instance = null;

        }
        private void DetachDataContext()
        {
            try
            {
                if (RootGrid.DataContext is IDisposable disposableDataContext)
                {
                    disposableDataContext.Dispose();
                }
                RootGrid.DataContext = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during disposal of DataContext: {ex.Message}");
            }
        }


        private void DisposeResources()
        {
            try
            {

                if (mediaPlayer != null)
                {
                    if (mediaPlayer?.PlaybackSession != null)
                    {
                        mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                        mediaPlayer.PlaybackSession.PlaybackStateChanged -= MediaPlayer_PlaybackStateChanged;
                    }
                    mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                    mediaPlayer.VolumeChanged -= MediaPlayer_VolumeChanged;

                    mediaPlayer?.Dispose();
                    mediaPlayer = null;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during disposal of mediaplayer: {ex.Message}");
            }
        }
        double[] speed = { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0.7, 0.5, 0.3 };
        private Button _activeSpeedButton; // Tracks the currently highlighted speed button
        private Button _activeTimeButton; // Tracks the currently highlighted time button


        private void AddSpeedButtons()
        {
            foreach (var item in speed)
            {
                Button button = new Button
                {
                    Content = item.ToString() + "x",
                    Width = 45,
                    Height = 25,
                    Foreground = new SolidColorBrush(Colors.Beige),
                    FontSize = 10,
                    Margin = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) // Default background color
                };
                if (item == 1)
                    SpeedButton_Click(button, new RoutedEventArgs());
                button.Click += SpeedButton_Click;
                SpeedButtonPanel.Children.Add(button);
            }

        }

        private void SpeedButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset the background of the previously active button
            if (_activeSpeedButton != null)
            {
                _activeSpeedButton.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)); // Default background color
            }
            // Highlight the clicked button
            if (sender is Button clickedButton)
            {
                clickedButton.Background = new SolidColorBrush(Colors.DarkGray); // Highlight color
                _activeSpeedButton = clickedButton; // Update the active button reference

                // Perform logic for speed adjustment
                string speedText = clickedButton.Content.ToString().TrimEnd('x');
                if (double.TryParse(speedText, out double playbackSpeed))
                {
                    mediaPlayer.PlaybackSession.PlaybackRate = playbackSpeed;
                }
            }
        }
        double[] time = { 60, 30, 10, 5, 1, -5, -10, -30, -60 };

        public event PropertyChangedEventHandler PropertyChanged;

        private void AddTimeButtons()
        {
            foreach (var item in time)
            {
                Button button = new Button
                {
                    Content = item.ToString(),
                    Width = 45,
                    Height = 25,
                    Foreground = new SolidColorBrush(Colors.Beige),
                    FontSize = 10,
                    Margin = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) // Default background color

                };
                button.Click += TimeButton_Click;
                TimeButtonPanel.Children.Add(button);
            }
        }

        private void TimeButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset the background of the previously active button
            if (_activeTimeButton != null)
            {
                //  _activeTimeButton.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)); // Default background color
            }
            // Highlight the clicked button
            if (sender is Button clickedButton)
            {
                //clickedButton.Background = new SolidColorBrush(Colors.DarkGray); // Highlight color
                _activeTimeButton = clickedButton; // Update the active button reference

                // Perform logic for speed adjustment
                string speedText = clickedButton.Content.ToString();
                if (double.TryParse(speedText, out double playbackPosition))
                {
                    mediaPlayer.Position += new TimeSpan(00, 00, Convert.ToInt32(playbackPosition));
                }
            }
        }


        // Method to update the progress bar's value
        private void UpdateProgress(double newValue)
        {
            if (newValue >= VerticalProgressBar.Minimum && newValue <= VerticalProgressBar.Maximum)
            {
                VerticalProgressBar.Value = newValue; // Set the new value for the progress bar
            }
        }

        private async Task ExtractThumbnailsInParallelAsync(StorageFile videoFile, List<int> times, string tempFolder)
        {
            var tasks = times.Select(async time =>
            {
                var thumbnail = await ExtractThumbnailAsync(videoFile, time, tempFolder);
                if (thumbnail != null)
                {
                    // Use the DispatcherQueue to update the ObservableCollection on the UI thread
                    await Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().EnqueueAsync(() =>
                    {
                        ViewModel.Thumbnails.Add(new Thumbnail { Image = thumbnail, Time = TimeSpan.FromSeconds(time) });
                    });
                }
            });

            await Task.WhenAll(tasks);
        }



        private async Task<BitmapImage> ExtractThumbnailAsync(StorageFile videoFile, int timeSeconds, string tempFolder, int timeoutMs = 10000)
        {
            // Clear temp folder
            // ClearTempFolder(tempFolder);

            // Generate a unique thumbnail path
            string thumbnailPath = Path.Combine(tempFolder, $"thumbnail_{timeSeconds}_{Guid.NewGuid()}.jpg");
            //string ffmpegPath = @"C:\Program Files\ffmpeg\ffmpeg-2024-12-04-git-2f95bc3cb3-full_build\bin\ffmpeg.exe";

            //string ffmpegArguments = $"-y -i \"{videoFile.Path}\" -ss {timeSeconds} -vframes 1 \"{thumbnailPath}\" -loglevel debug";
            string ffmpegArguments = $"-y -hwaccel auto -ss {timeSeconds} -i \"{videoFile.Path}\" -vframes 1 -vf scale=320:-1 -an \"{thumbnailPath}\" -loglevel error";

            Debug.WriteLine($"FFmpeg Command: {ffmpegArguments}");
            Debug.WriteLine($"Processing video file: {videoFile.Path}");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = GlobalClass.Instance.ffmpegPath,
                Arguments = ffmpegArguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    process.Start();

                    var exitTask = process.WaitForExitAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    var outputTask = process.StandardOutput.ReadToEndAsync();

                    if (await Task.WhenAny(exitTask, Task.Delay(timeoutMs)) == exitTask)
                    {
                        await Task.WhenAll(errorTask, outputTask);

                        Debug.WriteLine($"FFmpeg Output: {await outputTask}");
                        Debug.WriteLine($"FFmpeg Error: {await errorTask}");

                        if (File.Exists(thumbnailPath))
                        {
                            return new BitmapImage(new Uri(thumbnailPath));
                        }
                        else
                        {
                            Debug.WriteLine("Thumbnail not created.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("FFmpeg process timed out.");
                        process.Kill();
                        await exitTask; // Ensure cleanup
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception: {ex.Message}");
                }
            }

            return null;
        }

        private void ClearTempFolder(string tempFolder)
        {
            if (Directory.Exists(tempFolder))
            {
                foreach (var file in Directory.GetFiles(tempFolder, "thumbnail_*.jpg"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to delete file {file}: {ex.Message}");
                    }
                }
            }
        }


        private async Task GenerateThumbnailsAsync()
        {
            try
            {
                // Clear existing thumbnails
                ViewModel.Thumbnails.Clear();
                string tempFolder = Path.Combine(Path.GetTempPath(), "VideoThumbnails");
                Directory.CreateDirectory(tempFolder);
                ClearTempFolder(tempFolder);
                // Load the video file using MediaClip
                StorageFile mediaFile = await StorageFile.GetFileFromPathAsync(ViewModel.Media.ProxyPath.ToString());
                var mediaClip = await MediaClip.CreateFromFileAsync(mediaFile);
                // Calculate the total duration of the video
                TimeSpan totalDuration = mediaClip.OriginalDuration;
                List<int> times = new List<int>();
                int interval = 10; // Interval in seconds
                int maxThumbnails = 50; // Maximum number of thumbnails to generate

                if (totalDuration.TotalSeconds / interval > maxThumbnails)
                {
                    interval = (int)(totalDuration.TotalSeconds / maxThumbnails);
                }

                for (int i = 0; i < totalDuration.TotalSeconds; i += interval)
                {
                    times.Add(i);
                }

                // Debug.WriteLine($"Starting extraction for time: {i} seconds");

                await ExtractThumbnailsInParallelAsync(mediaFile, times, tempFolder);

            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error generating thumbnails: {ex.Message}");
            }
        }
        private async Task ShowErrorDialog(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot // Set the XamlRoot to ensure proper display
            };

            await errorDialog.ShowAsync();
        }



        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {

        }

        private void VerticalSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {

        }

        private void VerticalSlider1_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox1_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle play/pause
            if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                mediaPlayer.Pause();
            }
            else
            {
                mediaPlayer.Play();
            }
        }

        private void RewindButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.StepBackwardOneFrame();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                mediaPlayer.Source = null;
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.StepForwardOneFrame();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await AddTrimMetadataAsync(ViewModel.Media.MediaSource.LocalPath.ToString(), ViewModel.TrimmedClip.InTime, ViewModel.TrimmedClip.OutTime, ViewModel.TrimmedClip.ClipName);
            //TxtClipName.Visibility= Visibility.Collapsed;
            //TBTrimmedClipDuration.Visibility = Visibility.Collapsed;
            var file = await StorageFile.GetFileFromPathAsync(ViewModel.Media.MediaSource.LocalPath.ToString());
            await LoadTrimmedClipsWithThumbnailsAsync(file);
        }

        private void TrimInButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TrimOutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void IsCompleted_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RSS_Click(object sender, RoutedEventArgs e)
        {

        }



        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Volume == 0)
                ViewModel.VolumeIcon = "\uf028";
            else
            {
                mediaPlayer.Volume = 0;
                ViewModel.VolumeIcon = "\uf6a9";
            }
        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("FileInfo",typeof(FileInfoPage)),
            ("Categories",typeof(CategoriesPage)),
            ("Collection",typeof(CollectionPage)),
            ("Tags",typeof(TagsPage))
        };

        private object GetParameterBasedOnTag(string tag, Dictionary<string, object> parameters)
        {
            // Check if the dictionary contains the tag as a key
            //if (parameters.TryGetValue(tag, out var parameter))
            //{
            //    return parameter; // Return the associated value
            //}

            return parameters; // Return null if the tag isn't found
        }

        //private object GetParameterBasedOnTag(Dictionary<string,object> metadata)
        //{
        //    // Define the parameter you want to pass based on the tag
        //    return metadata switch
        //    {
        //        FileInfo => metadata,
        //        //"Categories" => "Sample Parameter for CategoriesPage",
        //        //"Collection" => "Sample Parameter for CollectionPage",
        //        //"Tags" => "Sample Parameter for TagsPage",
        //        _ => null,
        //    };
        //}
        // Dictionary<string, object> parameter;
        private void navMetadata_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            //var navItemTag = args.InvokedItemContainer.Tag.ToString();
            //navMetadata_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);


            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            navMetadata_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo, ViewModel.Metadata);
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
            string navItemTag = "FileInfo";

            //parameter = (Dictionary<string, object>)GetParameterBasedOnTag(navItemTag, metadata);
            //parameter = (string)GetParameterBasedOnTag("FileInfo"); // Example: determine the parameter to pass

            ////	// navMetadata doesn't load any page by default, so load home page.
            navMetadata.SelectedItem = navMetadata.MenuItems[0];
            ////	// If navigation occurs on SelectionChanged, this isn't needed.
            ////	// Because we use ItemInvoked to navigate, we need to call Navigate
            ////	// here to load the home page.
            navMetadata_Navigate("FileInfo", new EntranceNavigationTransitionInfo(), ViewModel.Metadata);
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
        // private InputCursor? OriginalCursor { get; set; }
        // Vertical Splitter events
        private void VerticalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDraggingVerticalSplitter = true;
            initialPointerX = e.GetCurrentPoint(MainCanvas).Position.X;
            // Capture the pointer so we continue receiving events even when the pointer leaves the splitter
            VerticalSplitter.CapturePointer(e.Pointer);
            VerticalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        private void VerticalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDraggingVerticalSplitter)
            {
                double newX = e.GetCurrentPoint(MainCanvas).Position.X;
                double offsetX = newX - initialPointerX;

                // Adjust the width of Left Panel
                //if (LeftPanel.Width > 10)
                //{
                if (LeftPanel.Width + offsetX > 0)
                {
                    LeftPanel.Width += offsetX;

                    // Move Vertical Splitter
                    Canvas.SetLeft(VerticalSplitter, Canvas.GetLeft(VerticalSplitter) + offsetX);

                    // Move Right Panel
                    //if (RightPanel.Width > 10)
                    //{
                    Canvas.SetLeft(RightPanel, Canvas.GetLeft(RightPanel) + offsetX);
                    double left = Canvas.GetLeft(RightPanel);
                    if (MainCanvas.ActualWidth - Canvas.GetLeft(RightPanel) > 10)
                    {
                        RightPanel.Width = MainCanvas.ActualWidth - Canvas.GetLeft(RightPanel);
                        //}
                        initialPointerX = newX;
                    }
                }
                //}
            }
        }

        // Horizontal Splitter events
        private void HorizontalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDraggingHorizontalSplitter = true;
            initialPointerY = e.GetCurrentPoint(MainCanvas).Position.Y;
            // Capture the pointer for horizontal splitter. This ensures that the control continues to receive pointer events even if the pointer moves quickly or leaves the splitter area.
            HorizontalSplitter.CapturePointer(e.Pointer);
            HorizontalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        }

        private void HorizontalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDraggingHorizontalSplitter)
            {
                double newY = e.GetCurrentPoint(MainCanvas).Position.Y;
                double offsetY = newY - initialPointerY;

                // Adjust the height of Top Panel
                if (TopPanel.Height + offsetY > 0)
                {
                    TopPanel.Height += offsetY;

                    // Move Horizontal Splitter
                    Canvas.SetTop(HorizontalSplitter, Canvas.GetTop(HorizontalSplitter) + offsetY);
                    if (Canvas.GetTop(BottomPanel) < RightPanel.ActualHeight)
                    {
                        // Move Bottom Panel
                        Canvas.SetTop(BottomPanel, Canvas.GetTop(BottomPanel) + offsetY);
                        BottomPanel.Height = RightPanel.ActualHeight - Canvas.GetTop(BottomPanel);
                    }
                    initialPointerY = newY;
                }
            }
        }

        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDraggingVerticalSplitter = false;
            isDraggingHorizontalSplitter = false;

            // Release the pointer capture
            VerticalSplitter.ReleasePointerCapture(e.Pointer);
            HorizontalSplitter.ReleasePointerCapture(e.Pointer);
            VerticalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            HorizontalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }


        private void VerticalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VerticalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        private void HorizontalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            HorizontalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        }
        private void VerticalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            VerticalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
        private void HorizontalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            HorizontalSplitter.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        private void SeekBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (isSeeking)
            {
                return;
            }

            // When the user changes the Slider value, seek to the corresponding position
            if (mediaPlayer.Source != null && mediaPlayer.PlaybackSession.CanSeek)
            {
                isSeeking = true;
                mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
                isSeeking = false;
            }
        }

        private void VolumeBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (mediaPlayer != null && mediaPlayer.Source != null)
                mediaPlayer.Volume = e.NewValue;
        }

        private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Adjust the height of Left Panel, Vertical Splitter, Right Panel when window resizes
            LeftPanel.Height = MainCanvas.ActualHeight;
            VerticalSplitter.Height = MainCanvas.ActualHeight;
            RightPanel.Height = MainCanvas.ActualHeight;

            // Adjust width of Top Panel, Horizontal Splitter, and Bottom Panel
            TopPanel.Width = RightPanel.ActualWidth;
            HorizontalSplitter.Width = RightPanel.ActualWidth;
            BottomPanel.Width = RightPanel.ActualWidth;
        }
        private void OnLeftThumbPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingLeft = true;
            _startX = e.GetCurrentPoint(ThumbnailScrollViewer).Position.X;
            LeftThumb.CapturePointer(e.Pointer);
            ViewModel.TrimmedClip.ClipName = "Clip1";
            TxtInTime.Visibility = Visibility.Visible;
        }

        private void OnRightThumbPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingRight = true;
            _startX = e.GetCurrentPoint(ThumbnailScrollViewer).Position.X;
            RightThumb.CapturePointer(e.Pointer);
            TxtOutTime.Visibility = Visibility.Visible;

        }
        private void TransformHorizontally(TextBlock txt, double delta)
        {
            if (txt.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                txt.RenderTransform = transform;
            }
            transform.X = delta;
        }
        private void TransformHorizontally(Control txt, double delta)
        {
            if (txt.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                txt.RenderTransform = transform;
            }
            transform.X = delta;
        }
        private double txtLeftThumbX = 0; // X position of the TextBlock
        private void OnLeftThumbPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDraggingLeft) return;

            double currentX = e.GetCurrentPoint(ThumbnailScrollViewer).Position.X;
            double deltaX = currentX - _startX;

            // Get current position of the thumb
            double currentLeft = Canvas.GetLeft(LeftThumb);

            // Calculate new position
            double newLeft = currentLeft + deltaX;
            var leftOfRightThumb = Canvas.GetLeft(RightThumb);
            var leftThumbWidth = LeftThumb.ActualWidth;
            TxtOutTime.Visibility = Visibility.Collapsed;

            // Constrain within ScrollViewer bounds
            if (newLeft >= 0 && newLeft < (leftOfRightThumb - leftThumbWidth))
            {
                Canvas.SetLeft(LeftThumb, newLeft);
                Canvas.SetLeft(TxtClipName, newLeft + LeftThumb.ActualWidth);

                _startX = currentX; // Update startX for smooth dragging

                // Update the position of the TextBlock
                txtLeftThumbX += deltaX;
                // Apply the new position using a TranslateTransform
                TransformHorizontally(TxtInTime, txtLeftThumbX);
                //TransformHorizontally(TxtClipName, txtLeftThumbX);
                TransformHorizontally(TBTrimmedClipDuration, (txtLeftThumbX - 2 * leftThumbWidth + leftOfRightThumb) / 2);
                UpdateLeftSelectionOverlay();

                //TxtClipName.Width = 50;
                //TxtClipName.Height = 25;
            }

            if (newLeft > leftOfRightThumb - (leftThumbWidth + TxtClipName.ActualWidth))
            {
                TxtClipName.Width = 17;
                TxtClipName.Height = 75;
            }
            else
            {
                TxtClipName.Width = 50;
                TxtClipName.Height = 25;
            }
            // Notify about the trim position
            OnLeftTrimPositionChanged?.Invoke(GetLeftThumbTime());
        }
        private double txtRightThumbX = 0; // X position of the TextBlock
        private void OnRightThumbPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDraggingRight) return;
            double currentX = e.GetCurrentPoint(ThumbnailScrollViewer).Position.X;
            double deltaX = currentX - _startX;
            var leftOfLeftThumb = Canvas.GetLeft(LeftThumb);
            var ThumbWidth = LeftThumb.ActualWidth;
            // Get current position of the thumb
            double currentLeft = Canvas.GetLeft(RightThumb);

            // Calculate new position
            double newLeft = currentLeft + deltaX;
            //newLeft < (leftOfRightThumb - leftThumbWidth)
            // Constrain within ScrollViewer bounds
            TxtInTime.Visibility = Visibility.Collapsed;

            if (newLeft > leftOfLeftThumb + ThumbWidth && newLeft <= ThumbnailScrollViewer.ActualWidth + ThumbWidth)
            {

                //if (TxtClipName.Visibility == Visibility.Collapsed)
                //{
                //    TxtClipName.Visibility = Visibility.Visible;
                //    TBTrimmedClipDuration.Visibility = Visibility.Visible;
                //}
                Canvas.SetLeft(RightThumb, newLeft);
                _startX = currentX; // Update startX for smooth dragging

                // Update the position of the TextBlock
                txtRightThumbX += deltaX;
                // Apply the new position using a TranslateTransform
                TransformHorizontally(TxtOutTime, txtRightThumbX);
                TransformHorizontally(TBTrimmedClipDuration, (txtLeftThumbX + currentLeft) / 2);
                UpdateRightSelectionOverlay();
            }

            // Notify about the trim position
            OnRightTrimPositionChanged?.Invoke(GetRightThumbTime());
        }
        private void OnThumbPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingLeft = false;
            _isDraggingRight = false;

            LeftThumb.ReleasePointerCaptures();
            RightThumb.ReleasePointerCaptures();
            TxtInTime.Visibility = Visibility.Visible;
            TxtOutTime.Visibility = Visibility.Visible;
        }
        // Helper Methods to Calculate Times
        private string GetLeftThumbTime()
        {
            double leftPosition = Canvas.GetLeft(LeftThumb);
            // TimeSpan videoDuration = TimeSpan.Parse(ViewModel.Media.DurationString);
            double totalVideoDurationInSeconds = ViewModel.Media.Duration.TotalSeconds;
            var pos = ((leftPosition / ThumbnailScrollViewer.ActualWidth) * totalVideoDurationInSeconds).ToString();



            TimeSpan left = TimeSpan.FromSeconds(Convert.ToDouble(pos));
            return left.ToString(@"hh\:mm\:ss");
        }

        private string GetRightThumbTime()
        {
            double rightPosition = Canvas.GetLeft(RightThumb) - 14;
            double totalVideoDurationInSeconds = ViewModel.Media.Duration.TotalSeconds;
            var pos = ((rightPosition / ThumbnailScrollViewer.ActualWidth) * totalVideoDurationInSeconds).ToString();
            TimeSpan right = TimeSpan.FromSeconds(Convert.ToDouble(pos));
            return right.ToString(@"hh\:mm\:ss");
        }
        private void UpdateLeftTrimTime(string inTime)
        {
            // Update the UI for 'In' and 'Out' times
            ViewModel.TrimmedClip.InTime = inTime;// TimeSpan.FromSeconds(inTime).ToString(@"mm\:ss");
            if (ViewModel.TrimmedClip.OutTime != null)
                ViewModel.TrimmedClip.Duration = (TimeSpan.Parse(ViewModel.TrimmedClip.OutTime) - TimeSpan.Parse(ViewModel.TrimmedClip.InTime)).ToString(@"hh\:mm\:ss");

        }
        private void UpdateRighgtTrimTime(string outTime)
        {
            // Update the UI for 'In' and 'Out' times
            ViewModel.TrimmedClip.OutTime = outTime;// TimeSpan.FromSeconds(outTime).ToString(@"mm\:ss");
            if (ViewModel.TrimmedClip.InTime != null)
                ViewModel.TrimmedClip.Duration = (TimeSpan.Parse(ViewModel.TrimmedClip.OutTime) - TimeSpan.Parse(ViewModel.TrimmedClip.InTime)).ToString(@"hh\:mm\:ss");
        }

        private void UpdateRightSelectionOverlay()
        {
            double right = Canvas.GetLeft(RightThumb) + RightThumb.ActualWidth;
            Canvas.SetLeft(RightOverlay, right);
            double rectWidth = RectangleCanvas.ActualWidth - right;
            if (rectWidth > 0)
                RightOverlay.Width = rectWidth;
        }
        private void UpdateLeftSelectionOverlay()
        {
            double left = Canvas.GetLeft(LeftThumb); ;
            LeftOverlay.Width = left;
        }
        private double GetTimeFromPosition(double position)
        {
            double scrollViewerWidth = ThumbnailScrollViewer.ActualWidth;
            double totalVideoDurationInSeconds = ViewModel.Media.Duration.TotalSeconds;
            return (position / scrollViewerWidth) * totalVideoDurationInSeconds;
        }

        public (double StartTime, double EndTime) GetTrimmedTimes()
        {
            double startTime = GetTimeFromPosition(Canvas.GetLeft(LeftThumb));
            double endTime = GetTimeFromPosition(Canvas.GetLeft(RightThumb));
            return (startTime, endTime);
        }

        public async Task AddTrimMetadataAsync(string filePath, string inTime, string outTime, string clipName)
        {
            // Open the video file
            var file = await StorageFile.GetFileFromPathAsync(filePath);

            // Get video properties
            var videoProperties = await file.Properties.GetVideoPropertiesAsync();

            // Serialize trim points as a string
            var metadata = $"InTime={inTime};OutTime={outTime};ClipName={clipName}";

            // Add to Keywords
            if (!videoProperties.Keywords.Contains(metadata))
            {
                videoProperties.Keywords.Add(metadata);
            }
            // Save the updated properties
            await videoProperties.SavePropertiesAsync();
        }
        public async Task GetAllMetadataAsync(StorageFile filePath)
        {
            if (filePath != null)
            {
                ViewModel.Metadata = await GetVideoProperies(filePath);
                ViewModel.Metadata.Add("Path", ViewModel.Media.MediaSource.LocalPath);
                await LoadTrimmedClipsWithThumbnailsAsync(filePath);
            }
        }
        private async Task<Dictionary<string, object>> GetVideoProperies(StorageFile filePath)
        {
            if (filePath != null)
            {
                var videoProperties = await filePath.Properties.GetVideoPropertiesAsync();
                Dictionary<string, object> props = new();
                props.Add("Keywords", videoProperties.Keywords);
                props.Add("Title", videoProperties.Title);
                props.Add("Duration", videoProperties.Duration);
                props.Add("Width", videoProperties.Width);
                props.Add("Height", videoProperties.Height);
                props.Add("Bitrate", videoProperties.Bitrate);
                props.Add("Latitude", videoProperties.Latitude);
                props.Add("Longitude", videoProperties.Longitude);
                props.Add("Year", videoProperties.Year);
                props.Add("Orientation", videoProperties.Orientation);
                props.Add("Writers", videoProperties.Writers);
                props.Add("Directors", videoProperties.Directors);
                props.Add("Producers", videoProperties.Producers);
                props.Add("Publisher", videoProperties.Publisher);
                props.Add("Rating", videoProperties.Rating);
                props.Add("SubTitle", videoProperties.Subtitle);
                return props;
            }
            return null;
        }
        //private async Task LoadTrimmedClipsAsync(string file)
        //{
        //    ViewModel.TrimmedClips.Clear();
        //    var trimMetadata = await GetAllTrimMetadataAsync(file); // From previous code

        //    foreach (var (inTime, outTime, clipName) in trimMetadata)
        //    {
        //        var duration = CalculateDuration(inTime, outTime); // Calculate duration from inTime and outTime
        //        ViewModel.TrimmedClips.Add(new TrimmedClip
        //        {
        //            ClipName = clipName,
        //            InTime = inTime,
        //            OutTime = outTime,
        //            Duration = duration
        //        });
        //    }
        //}
        public async Task LoadTrimmedClipsWithThumbnailsAsync(StorageFile videoFile)
        {
            ViewModel.TrimmedClips.Clear();
            List<TrimmedClip> trimmedClips = new();
            //metadata = GetVideoProperies(videoFile).Result;
            IList<string> keyWord = (IList<string>)ViewModel.Metadata["Keywords"];
            foreach (var key in keyWord)
            {
                if (key.Contains("InTime") && key.Contains("OutTime") && key.Contains("ClipName"))
                {
                    // Parse the metadata
                    var parts = key.Split(';');
                    var inTime = parts.FirstOrDefault(p => p.StartsWith("InTime="))?.Split('=')[1];
                    var outTime = parts.FirstOrDefault(p => p.StartsWith("OutTime="))?.Split('=')[1];
                    var clipName = parts.FirstOrDefault(p => p.StartsWith("ClipName="))?.Split('=')[1];
                    if (inTime != null && outTime != null && clipName != null)
                    {
                        trimmedClips.Add(new TrimmedClip { InTime = inTime, OutTime = outTime, ClipName = clipName });
                    }
                }
            }
            foreach (var clip in trimmedClips)
            {
                try
                {
                    // Load the video as a MediaClip
                    MediaClip mediaClip = await MediaClip.CreateFromFileAsync(videoFile);

                    // Create a MediaComposition
                    MediaComposition composition = new MediaComposition();
                    composition.Clips.Add(mediaClip);
                    TimeSpan inTime = TimeSpan.FromSeconds(ConvertTimeToDouble(clip.InTime));
                    // Get the thumbnail at the InTime of the clip
                    var thumbnailStream = await composition.GetThumbnailAsync(
                        inTime,
                        100, // Width of the thumbnail
                        50, // Height of the thumbnail
                        VideoFramePrecision.NearestFrame // Nearest frame to InTime
                    );

                    // Convert the stream to BitmapImage
                    if (thumbnailStream != null)
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(thumbnailStream);
                        clip.Thumbnail = bitmapImage;
                    }
                    var duration = CalculateDuration(clip.InTime, clip.OutTime); // Calculate duration from inTime and outTime
                    ViewModel.TrimmedClips.Add(new TrimmedClip
                    {
                        ClipName = clip.ClipName,
                        InTime = clip.InTime,
                        OutTime = clip.OutTime,
                        Duration = duration,
                        Thumbnail = clip.Thumbnail

                    });
                }
                catch (Exception ex)
                {
                    // Handle exceptions if needed
                    Console.WriteLine($"Error generating thumbnail for {clip.ClipName}: {ex.Message}");
                }
            }
            // return trimmedClips;
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Cast the clicked item to your data type (e.g., TrimmedClip)
            if (e.ClickedItem is TrimmedClip clickedClip)
            {

                TimeSpan timeSpan = TimeSpan.Parse(clickedClip.InTime);
                mediaPlayer.PlaybackSession.Position = timeSpan;
                var inTime = ConvertTimeToDouble(clickedClip.InTime);
                var outTime = ConvertTimeToDouble(clickedClip.OutTime);
                var dur = ConvertTimeToDouble(ViewModel.Media.DurationString);
                var leftPos = (inTime / dur) * ThumbnailScrollViewer.ActualWidth;
                var rightPos = (outTime / dur) * ThumbnailScrollViewer.ActualWidth;
                Canvas.SetLeft(LeftThumb, leftPos);
                Canvas.SetLeft(TxtClipName, leftPos + LeftThumb.ActualWidth);
                TxtOutTime.Visibility = Visibility.Visible;
                Canvas.SetLeft(RightThumb, rightPos);
                ViewModel.TrimmedClip.Duration = CalculateDuration(clickedClip.OutTime, clickedClip.InTime);
                TransformHorizontally(TxtInTime, leftPos);
                txtLeftThumbX = leftPos;
                txtRightThumbX = -(ThumbnailScrollViewer.ActualWidth - rightPos) - 20;
                TransformHorizontally(TxtOutTime, txtRightThumbX);
                TransformHorizontally(TBTrimmedClipDuration, (leftPos - 2 * LeftThumb.ActualWidth + rightPos) / 2);
                UpdateLeftTrimTime(clickedClip.InTime);
                UpdateRighgtTrimTime(clickedClip.OutTime);
                UpdateLeftSelectionOverlay();
                UpdateRightSelectionOverlay();
            }
        }
        private string CalculateDuration(string inTime, string outTime)
        {
            if (TimeSpan.TryParse(inTime, out var start) && TimeSpan.TryParse(outTime, out var end))
            {
                return (end - start).ToString(@"hh\:mm\:ss");
            }
            return "Unknown";
        }

        private void LeftThumb_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ThumbnailGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        private void LeftThumb_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ThumbnailGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        private void RightThumb_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ThumbnailGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        private void RightThumb_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ThumbnailGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        static double ConvertTimeToDouble(string time)
        {
            // Parse the time string
            string[] parts = time.Split(':');
            if (parts.Length != 3)
            {
                throw new FormatException("Time string must be in the format hh:mm:ss");
            }
            int hours = int.Parse(parts[0]);
            int minutes = int.Parse(parts[1]);
            int seconds = int.Parse(parts[2]);

            // Convert to fractional hours
            double totalHours = (hours * 3600) + (minutes * 60.0) + (seconds);
            return totalHours;
        }

    }

    public class NewGrid : Grid
    {
        public InputCursor InputCursor
        {
            get { return base.ProtectedCursor; }
            set { base.ProtectedCursor = value; }
        }
    }
    public class MediaPlayerViewModel : ObservableObject, IDisposable
    {
        private Uri mediaUri;
        private string playPauseIcon = "\uf04b";
        private string volumeIcon = "\uf028";
        private int volume;
        private string currentPosition;
        private string totalDuration;
        private bool _disposed = false;

        private int id;
        private string name = string.Empty;
        private string version = "1";
        private TimeSpan duration = TimeSpan.Zero;
        private string type = string.Empty;
        private string file = string.Empty;
        private string createdUser = string.Empty;
        private DateOnly creationDate = DateOnly.MinValue;
        private string updatedUser = string.Empty;
        private DateTime lastUpdated = DateTime.Now;
        private double size = 0;
        private string description = string.Empty;
        private MediaPlayerItem media;
        //private string inPosition="0";
        //private string outPosition="0";
        //private string clipDuration;
        private TrimmedClip trimmedClip;
        private Dictionary<string, object> metadata;

        public string PlayPauseIcon
        {
            get => playPauseIcon;
            set => SetProperty(ref playPauseIcon, value);
        }
        public string VolumeIcon
        {
            get => volumeIcon;
            set => SetProperty(ref volumeIcon, value);
        }
        public int Volume
        {
            get => volume;
            set => SetProperty(ref volume, value);
        }
        public string CurrentPosition
        {
            get => currentPosition;
            set => SetProperty(ref currentPosition, value);
        }
        public string TotalDuration
        {
            get => totalDuration;
            set => SetProperty(ref totalDuration, value);
        }
        public TrimmedClip TrimmedClip
        {
            get => trimmedClip;
            set => SetProperty(ref trimmedClip, value);
        }

        public MediaPlayerItem Media
        {
            get => media;
            set => SetProperty(ref media, value);
        }
        public Dictionary<string,object> Metadata
        {
            get => metadata;
            set => SetProperty(ref metadata, value);
        }
        public ICommand VideoThumbnailClickCommand { get; }
        public ICommand VideoListThumbnailClickCommand { get; }

        public MediaPlayer MediaPlayer { get; set; }
        public ObservableCollection<Thumbnail> Thumbnails { get; set; }
        public ObservableCollection<BinThumbnail> BinThumbnails { get; set; }
        public ObservableCollection<MediaPlayerItem> BinMedias { get; set; }
        public ObservableCollection<TrimmedClip> TrimmedClips { get; }

        public MediaPlayerViewModel(MediaPlayerItem media)
        {
            // MediaUri = new Uri("https://www.example.com/sample.mp4"); // Default or initial value
            VideoThumbnailClickCommand = new RelayCommand<TimeSpan>(ChangePlaybackPosition);
            VideoListThumbnailClickCommand = new RelayCommand<Uri>(LoadVideo);
            Thumbnails = new ObservableCollection<Thumbnail>();
            BinThumbnails = new ObservableCollection<BinThumbnail>();
            TrimmedClip = new TrimmedClip();
            TrimmedClips = new ObservableCollection<TrimmedClip>();
            Media = media;
        }

        private void LoadVideo(Uri mediaUri)
        {
            Media = new MediaPlayerItem() { ProxyPath = mediaUri };

        }

        private void ChangePlaybackPosition(TimeSpan position)
        {
            MediaPlayer.PlaybackSession.Position = position;
        }
        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Dispose managed resources
                MediaPlayer?.Dispose();
                MediaPlayer = null;

                // Clear collections if needed
                Thumbnails?.Clear();
                BinThumbnails?.Clear();
            }

            // No unmanaged resources to clean up in this case
            _disposed = true;
        }

        ~MediaPlayerViewModel()
        {
            Dispose(false);
        }
    }

    public class Thumbnail : ObservableObject
    {
        private BitmapImage image;
        private TimeSpan time;

        public BitmapImage Image
        {
            get => image;
            set => SetProperty(ref image, value);
        }
        public TimeSpan Time
        {
            get => time;
            set => SetProperty(ref time, value);
        }
    }

    public class BinThumbnail : ObservableObject
    {
        private Uri thumbnailUri;
        private Uri mediaUri;

        public Uri ThumbnailUri
        {
            get => thumbnailUri;
            set => SetProperty(ref thumbnailUri, value);
        }
        public Uri MediaUri
        {
            get => mediaUri;
            set => SetProperty(ref mediaUri, value);
        }
    }
    public class TrimmedClip : ObservableObject
    {
        private string clipName = "Clip1";
        private string inTime;
        private string outTime;
        private string duration;
        private BitmapImage thumbnail;

        public string ClipName
        {
            get => clipName;
            set => SetProperty(ref clipName, value);
        }
        public string InTime
        {
            get => inTime;
            set => SetProperty(ref inTime, value);
        }
        public string OutTime
        {
            get => outTime;
            set => SetProperty(ref outTime, value);
        }
        public string Duration
        {
            get => duration;
            set => SetProperty(ref duration, value);
            //public TimeSpan Duration => OutTime - InTime;
        }
        public BitmapImage Thumbnail
        {
            get => thumbnail;
            set => SetProperty(ref thumbnail, value);
        }
    }


    public static class DispatcherQueueExtensions
    {
        public static Task EnqueueAsync(this Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (dispatcherQueue == null || action == null)
                throw new ArgumentNullException();

            dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}

