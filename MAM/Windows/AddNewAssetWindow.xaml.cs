using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddNewAssetWindow : Window
    {
        DataAccess dataAccess = new DataAccess();
        // Static instance to track the window
        public static AddNewAssetWindow _instance;
        private static MediaLibraryPage MediaLibraryPage;
        //public static UploadHistoryPage uploadHistory { get; set; }
        public ObservableCollection<Asset> AssetList { get; set; } = new ObservableCollection<Asset>();

        public AddNewAssetWindow(MediaLibraryPage mediaLibrary)
        {
            this.InitializeComponent();
            SetWindowSizeAndPosition(1000, 800);
            DgvFiles.DataContext = this;
            MediaLibraryPage = mediaLibrary;
            //uploadHistory = new UploadHistoryPage();
            // LoadDataGrid();
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
        // Method to get the instance of the window or create it if it doesn't exist
        public static void ShowWindow(MediaLibraryPage mediaLibrary)
        {
            if (_instance == null)
            {
                _instance = new AddNewAssetWindow(mediaLibrary);
                _instance.Activate(); // Show the window
            }
            else
            {
                _instance.Activate(); // Bring the existing window to the front
            }
        }
        private void LoadDataGrid()
        {
            AssetList.Clear();
            foreach (var media in MediaLibraryPage.viewModel.MediaPlayerItems)
            {
                AssetList.Add(new Asset(media));// { FileName = media.Title, AssetPath = archivePage.archive.BinName, OriginalPath = media.MediaSource.ToString() });
            }
        }
        private async void OpenFileExplorer()
        {
            var picker = new FileOpenPicker();

            // Initialize the picker with the current window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            // Set file types and options
            foreach (string ext in GlobalClass.Instance.SupportedFiles)
            {
                picker.FileTypeFilter.Add(ext); // Allow all files
            }
            // Allow selecting multiple files
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Show picker and get selected files
            var files = await picker.PickMultipleFilesAsync();

            if (files != null && files.Count > 0)
            {
                // Handle the selected files
                foreach (var file in files)
                {
                    if (file != null)
                    {
                        
                        var videoProperties = await file.Properties.GetVideoPropertiesAsync();
                        
                        TimeSpan duration = videoProperties.Duration;
                        //var path = file.Path.Replace("\\", "/");
                        string extension = Path.GetExtension(file.Name);
                        IList<string> props = videoProperties.Keywords;
                        string type = string.Empty;
                        if (GlobalClass.Instance.SupportedVideos.Contains(extension))
                            type = "Video";
                        else if (GlobalClass.Instance.SupportedAudios.Contains(extension))
                            type = "Audio";
                        else if (GlobalClass.Instance.SupportedImages.Contains(extension))
                            type = "Image";
                        else if (GlobalClass.Instance.SupportedDocuments.Contains(extension))
                            type = "Document";
                        FileInfo fileInfo = new FileInfo(file.Path);
                        long fileSize = fileInfo.Length/(1024*1024);//Converts bytes to MB
                        MediaPlayerItem media = new MediaPlayerItem
                        {
                            CreatedUser = GlobalClass.Instance.CurrentUserName,
                            CreationDate=DateOnly.FromDateTime(DateTime.Today.Date),
                            Duration = duration,
                            DurationString = duration.ToString(@"hh\:mm\:ss"),
                            MediaSource=new Uri(Path.Combine(MediaLibraryPage.MediaLibrary.BinName,file.Name)),
                            MediaPath= MediaLibraryPage.MediaLibrary.BinName,
                            OriginalPath = file.Path,
                            Size = fileSize,
                            Title = file.Name,
                            Type = type,
                            Version="V1"
                        };
                        AssetList.Add(new Asset(media));
                    }
                }
            }
            // DgvFiles.ItemsSource = AssetList;
        }
        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileExplorer();
        }

        private void navFile_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {

        }

        private void navFile_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void navFile_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {

        }

        private void navFile_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {

        }
        private void OpenModalButton_Click(object sender, RoutedEventArgs e)
        {
            //var modalWindow = new ModalWindow();

            //// Get the parent window handle
            //var parentHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

            //// Get the modal window handle
            //var modalHandle = WinRT.Interop.WindowNative.GetWindowHandle(modalWindow);

            //// Set the parent window for the modal
            //Win32Interop.SetWindowOwner(modalHandle, parentHandle);

            //// Show the modal window
            //modalWindow.Activate();

            //// Disable the parent window
            //Win32Interop.EnableWindow(parentHandle, false);

            //// Handle modal window closing
            //modalWindow.Closed += (s, args) =>
            //{
            //    // Re-enable the parent window
            //    Win32Interop.EnableWindow(parentHandle, true);
            //};
        }
        private List<Asset> assetstoRemove;

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            assetstoRemove = new List<Asset>();
            List<string> oldFileList;
            bool isExist = false;
            bool closeWindow = true;
            var folder = await StorageFolder.GetFolderFromPathAsync(MediaLibraryPage.MediaLibrary.BinName);

            var files = await folder.GetFilesAsync();
            // Create a Proxy Folder
            var proxyFolder = await folder.CreateFolderAsync("Proxy", CreationCollisionOption.OpenIfExists);
            if (proxyFolder != null)
            {
                MediaLibraryPage.MediaLibrary.ProxyFolder = proxyFolder.Path;
            }

            foreach (Asset asset in AssetList)
            {
                var newFile = asset.Media.MediaSource.LocalPath;
                foreach (var file in files)
                {
                    if (Path.Equals(file.Name, asset.Media.Title))
                    {
                        oldFileList = new List<string>();
                        oldFileList.Add(file.Path);
                        closeWindow = await ShowMessageBox(asset, oldFileList);// shows AssetCreationConfirmation message box
                        isExist = true;
                        continue;
                        // AssetCreationConfirmationWindow.ShowWindow(asset.FileName, new List<string>());
                    }
                    else
                        isExist = false;
                }
                try
                {
                    if (!isExist)
                    {
                        if (closeWindow)
                        {
                            File.Copy(asset.Media.OriginalPath, newFile, overwrite: true);

                            if (await InsertAsset(asset))
                            {
                                // Generate Thumbnail
                                var thumbnailFile = await proxyFolder.CreateFileAsync($"Thumbnail_{Path.GetFileNameWithoutExtension(asset.Media.OriginalPath)}.jpg", CreationCollisionOption.ReplaceExisting);
                                var originalFile = await StorageFile.GetFileFromPathAsync(asset.Media.OriginalPath);
                                await GenerateThumbnailAsync(originalFile, thumbnailFile);
                                var proxyPath = new Uri(Path.Combine(proxyFolder.Path, "Proxy_" + asset.Media.Title));
                                MediaLibraryPage.viewModel.MediaPlayerItems.Add(new MediaPlayerItem { MediaSource = new Uri(asset.Media.MediaSource.LocalPath),MediaPath= Path.GetDirectoryName(asset.Media.MediaSource.LocalPath), ThumbnailPath = new Uri(thumbnailFile.Path), ProxyPath = proxyPath, Title = asset.Media.Title, DurationString = asset.Media.DurationString });
                                MediaLibraryPage.viewModel.AllMediaPlayerItems.Add(new MediaPlayerItem { MediaSource = new Uri(asset.Media.MediaSource.LocalPath),MediaPath= Path.GetDirectoryName(asset.Media.MediaSource.LocalPath), ThumbnailPath = new Uri(thumbnailFile.Path), ProxyPath = proxyPath, Title = asset.Media.Title, DurationString = asset.Media.DurationString });
                                MediaLibraryPage.MediaLibrary.FileCount = MediaLibraryPage.viewModel.MediaPlayerItems.Count;
                                //if (closeWindow)
                                //    this.Close();
                                if (UploadHistoryPage.uploadHistory != null)
                                {
                                    UploadHistoryPage.uploadHistory.UploadHistories.Clear();
                                    foreach (var ast in AssetList)
                                    {
                                        UploadHistoryPage.uploadHistory.UploadHistories.Add(new UploadHistory { AssetObj = ast });
                                    }
                                }
                            }
                            if (closeWindow)
                                this.Close();
                            //string[] fileList = AssetList.Select(asset => asset.OriginalPath).ToArray();
                            //string outputDirectory = archivePage.archive.BinName;
                        }
                    }
                }
                catch (IOException ex)
                {
                    var dialog = new ContentDialog
                    {
                        Content = ex.ToString(),
                        CloseButtonText = "OK",
                        Height = 5,
                        XamlRoot = this.Content.XamlRoot // Assuming this code is in a page or window with access to XamlRoot
                    };
                    await dialog.ShowAsync();
                }

            }
            await ProcessAssetsAsync(AssetList);
            foreach (Asset item in assetstoRemove)
            {
                AssetList.Remove(item);
            }
        }

        //public void RefreshUI()
        //{
        //    var tempList = UploadHistoryPage.uploadHistory.AssetList;
        //    UploadHistoryPage.uploadHistory.AssetList = null;
        //    UploadHistoryPage.uploadHistory.AssetList = tempList;
        //    OnPropertyChanged(nameof(UploadHistoryPage.uploadHistory.AssetList)); // Notify binding
        //}
        public async Task RunFFmpegWithProgressAsync(Asset asset, string[] inputFiles, string outputFolder, IProgress<string> progress)
        {

            var folder = await StorageFolder.GetFolderFromPathAsync(MediaLibraryPage.MediaLibrary.BinName);
            var files = await folder.GetFilesAsync();
            // Create a Proxy Folder
            var proxyFolder = await folder.CreateFolderAsync("Proxy", CreationCollisionOption.OpenIfExists);


            var tasks = inputFiles.Select(async inputFile =>
            {
                // Create a compressed proxy file (using FFmpeg or similar)
                //var proxyFile = await proxyFolder.CreateFileAsync($"Proxy_{Path.GetFileNameWithoutExtension(inputFile)}", CreationCollisionOption.ReplaceExisting);
                string outputFile = Path.Combine(proxyFolder.Path, "Proxy_" + Path.GetFileNameWithoutExtension(inputFile) + ".mp4");
                string arguments = $"-i \"{inputFile}\" -vf scale=640:-1 -c:v libx264 -b:v 200k  -c:a aac -b:a 128k \"{outputFile}\"";
                //await ProcessAssetAsync(asset,arguments);
                await ProcessAssetsAsync(AssetList);
                //await Task.Run(() =>
                //{
                //    try
                //    {

                //        var progress = new Progress<string>(data =>
                //        {
                //            if (data == null)
                //                return; // Skip if data is null.

                //            App.UIDispatcherQueue.TryEnqueue(() =>
                //            {
                //                if (asset == null)
                //                {
                //                    Debug.WriteLine("Asset is null at progress update.");
                //                    return; // Prevent crash if asset is null.
                //                }
                //                asset.Progress = ParseFFmpegOutput(data); // Update Progress property.
                //            });
                //        });




                //            RunFFmpegProcessWithProgress(arguments, progress);
                //        }
                //        catch (Exception ex)
                //        {
                //            Debug.WriteLine($"Error running FFmpeg: {ex.Message}");
                //        }
                //        //RunFFmpegProcessWithProgress(arguments, progress);
                //    });
            });

            await Task.WhenAll(tasks);
        }

        //private ReadOnlySpan<byte> ParseFFmpegOutput(string data)
        //{
        //    throw new NotImplementedException();
        //}

        private TimeSpan? totalDuration;

        private int ParseFFmpegOutput(string data)
        {
            double progressPercentage = 0;
            if (string.IsNullOrWhiteSpace(data)) return 0;

            // Look for the duration line
            if (data.Contains(" Duration:"))
            {
                var durationString = data.Split(new[] { " Duration: ", "," }, StringSplitOptions.RemoveEmptyEntries)[1];
                if (TimeSpan.TryParse(durationString, out var duration))
                {
                    totalDuration = duration;
                }
            }

            // Look for the time field in progress output
            if (data.Contains("time="))
            {
                var match = Regex.Match(data, @"time=(\d{2}:\d{2}:\d{2}\.\d{2})");
                if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, out var currentTime))
                {
                    if (totalDuration.HasValue)
                    {
                        progressPercentage = (currentTime.TotalSeconds / totalDuration.Value.TotalSeconds) * 100;
                        // Debug.WriteLine($"Progress: {progressPercentage}%");
                        // Update UI or asset's progress property
                        // asset.Progress = (int)progressPercentage;
                    }
                }
            }
            return (int)progressPercentage;
        }

        private async Task ProcessAssetAsync(Asset asset)
        {
           // var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            TimeSpan? totalDuration = null;
            // Record the start time
            App.UIDispatcherQueue.TryEnqueue(() =>
            {
                asset.Status = "Waiting...";
            });
            var progress = new Progress<string>(data =>
            {
                if (data.Contains(" Duration:"))
                {
                    var durationString = data.Split(new[] { " Duration: ", "," }, StringSplitOptions.RemoveEmptyEntries)[1];
                    if (TimeSpan.TryParse(durationString, out var duration))
                    {
                        totalDuration = duration;
                    }
                }

                if (data.Contains("time=") && totalDuration.HasValue)
                {
                    var match = Regex.Match(data, @"time=(\d{2}:\d{2}:\d{2}\.\d{2})");
                    if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, out var currentTime))
                    {
                        var progressPercentage = (currentTime.TotalSeconds / totalDuration.Value.TotalSeconds) * 100;
                        App.UIDispatcherQueue.TryEnqueue(() =>
                        {
                            asset.Progress = (int)progressPercentage;
                            if (asset.Status != "Transfering...")
                            {
                                asset.Status = "Transfering..."; // Update status to 'transferring'
                            }
                        });
                    }
                }
            });
            string outputFile = Path.Combine(MediaLibraryPage.MediaLibrary.ProxyFolder, "Proxy_" + Path.GetFileNameWithoutExtension(asset.Media.OriginalPath) + ".mp4");
            string arguments = $"-i \"{asset.Media.OriginalPath}\" -vf scale=640:-1 -c:v libx264 -b:v 200k  -c:a aac -b:a 128k \"{outputFile}\"";
            // Update status to 'transferring' just before starting
            App.UIDispatcherQueue.TryEnqueue(() =>
            {
                asset.Status = "Transferring...";
                asset.StartTime = DateTime.Now;
            });
            await Task.Run(() => RunFFmpegProcessWithProgress(arguments, progress));
            // Record the completion time
            App.UIDispatcherQueue.TryEnqueue(() =>
            {
                asset.Status = "Finished";
                asset.CompletionTime = DateTime.Now;
                
            });
        }
        public async Task ProcessAssetsAsync(IEnumerable<Asset> assets)
        {
            //string ffmpegPath = @"C:\Path\To\ffmpeg.exe";
            var tasks = assets.Select(asset => ProcessAssetAsync(asset));
            await Task.WhenAll(tasks); // Process all assets concurrently
            UploadHistoryPage.uploadHistory.UploadHistories.Clear();
        }

        private void RunFFmpegProcessWithProgress(string arguments, IProgress<string> progress)
        {
            //string ffmpegPath = @"C:\Program Files\ffmpeg\ffmpeg-2024-12-04-git-2f95bc3cb3-full_build\bin\ffmpeg.exe";

            using var process = new Process();
            process.StartInfo.FileName = GlobalClass.Instance.ffmpegPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    progress.Report(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    progress.Report(e.Data);
                }
            };
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start the FFmpeg process.", ex);
            }
            // process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
















        private async Task CreateProxyFileAsync(StorageFile originalFile, StorageFolder folder)
        {
            // Create a Proxy Folder
            var proxyFolder = await folder.CreateFolderAsync("Proxy", CreationCollisionOption.OpenIfExists);

            // Create a compressed proxy file (using FFmpeg or similar)
            var proxyFile = await proxyFolder.CreateFileAsync($"Proxy_{originalFile.Name}", CreationCollisionOption.ReplaceExisting);

            // Example: Use FFmpeg to compress the video
            //string ffmpegPath = @"C:\Program Files\ffmpeg\ffmpeg-2024-12-04-git-2f95bc3cb3-full_build\bin\ffmpeg.exe";

            // string arguments = $"-i \"D:\\sample.mp4\" - c:v libx264 -crf 23 - preset medium - c:a aac -b:a 128k -f mp4 \"D:\\output.mp4\"";

            //string arguments = $"-i \"{originalFile.Path}\" -c:v libx264 -crf 23 -preset medium -c:a aac -b:a 128k -f mp4 \"{proxyFile.Path}\"";
            //await RunProcessAsync(ffmpegPath, arguments);

            //string inputFile = @"D:\VIDEO 1080p\dd.mp4";
            //string outputFile = @"F:\MAM\Data\Songs\Proxy\Proxy_dd.mp4";
            // string arguments = $"-y -i \"{originalFile.Path}\" -c:v libx264 -crf 23 - preset fast \"{proxyFile.Path}\"";
            // string arguments = $"-i \"{inputFile}\" -c:v libx264 -crf 23 -preset medium -c:a aac -b:a 128k \"{outputFile}\"";

            // await RunProcessAsync(ffmpegPath, arguments);

            await RunFFmpegAsync(originalFile.Path, proxyFile.Path, GlobalClass.Instance.ffmpegPath);


            // Generate Thumbnail
            var thumbnailFile = await proxyFolder.CreateFileAsync($"Thumbnail_{Path.GetFileNameWithoutExtension(originalFile.Name)}.jpg", CreationCollisionOption.ReplaceExisting);
            await GenerateThumbnailAsync(originalFile, thumbnailFile);
        }
        private async Task RunFFmpegAsync(string inputFile, string outputFile, string ffmpegPath)
        {
            string arguments = $"-y -i \"{inputFile}\" -c:v libx264 -b:v 200k -vf scale=640:-1  -preset fast  -c:a aac -b:a 128k \"{outputFile}\"";
            await Task.Run(() => RunProcess(ffmpegPath, arguments));
        }

        private void RunProcess(string executable, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }

        private async Task GenerateThumbnailAsync(StorageFile videoFile, StorageFile thumbnailFile)
        {
            // Obtain a thumbnail of the video
            using (var thumbnail = await videoFile.GetThumbnailAsync(ThumbnailMode.VideosView))
            {
                if (thumbnail != null)
                {
                    // Open the destination thumbnail file for writing
                    using (var outputStream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        // Copy the thumbnail's stream to the output file
                        await RandomAccessStream.CopyAsync(thumbnail, outputStream);
                    }
                }
            }
        }
        //private async Task RunProcessAsync(string fileName, string arguments)
        //{
        //    var process = new Process
        //    {
        //        StartInfo = new ProcessStartInfo
        //        {
        //            FileName = fileName,
        //            Arguments = arguments,
        //            RedirectStandardOutput = true,
        //            RedirectStandardError = true,
        //            UseShellExecute = false,
        //            CreateNoWindow = true
        //        }
        //    };

        //    var standardError = new List<string>();
        //    var standardOutput = new List<string>();

        //    // Event handlers to capture output and error streams.
        //    process.OutputDataReceived += (sender, e) =>
        //    {
        //        if (!string.IsNullOrWhiteSpace(e.Data))
        //        {
        //            Debug.WriteLine($"FFmpeg Output: {e.Data}");
        //            standardOutput.Add(e.Data);
        //        }
        //    };

        //    process.ErrorDataReceived += (sender, e) =>
        //    {
        //        if (!string.IsNullOrWhiteSpace(e.Data))
        //        {
        //            Debug.WriteLine($"FFmpeg Error: {e.Data}");
        //            standardError.Add(e.Data);
        //        }
        //    };

        //    // Start the process and begin reading streams.
        //    process.Start();
        //    process.BeginOutputReadLine();
        //    process.BeginErrorReadLine();

        //    // Await process exit.
        //    await Task.Run(() => process.WaitForExit());

        //    // Check for non-zero exit codes indicating errors.
        //    if (process.ExitCode != 0)
        //    {
        //        var errorMessages = string.Join(Environment.NewLine, standardError);
        //        throw new Exception($"FFmpeg process failed with exit code {process.ExitCode}. Error details: {errorMessages}");
        //    }

        //    // Log output if needed.
        //    Debug.WriteLine(string.Join(Environment.NewLine, standardOutput));
        //}


        private async Task<bool> ShowMessageBox(Asset asset, List<string> oldFileList)
        {
            var listBox = new ListBox
            {
                ItemsSource = oldFileList,
                IsEnabled = false
            };
            var dialog = new ContentDialog
            {
                Title = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {

                            Text = $"' {asset.Media.Title} ' already exist !",
                            TextWrapping=TextWrapping.Wrap,
                            FontSize=15,
                        },
                    }
                },
                //$"{fileName} already exist ! Please select an action to create new asset and",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Old Files",
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        listBox ,// Add the ListBox to the dialog
                        new TextBlock
                        {

                            Text = $"Please select an action to create new asset ",
                            TextWrapping=TextWrapping.Wrap,
                            FontSize=16,
                        },
                    }
                },
                PrimaryButtonText = "Create New Asset",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot // Required in WinUI 3
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                //Create New Asset
                ContentDialog resultDialog = new ContentDialog
                {
                    //Title = "Keep or delete existing file",
                    Content = "Keep or delete existing file ?",
                    PrimaryButtonText = "Keep Existing File",
                    SecondaryButtonText = "Delete Existing file",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.Content.XamlRoot
                };

                var result1 = await resultDialog.ShowAsync();
                if (result1 == ContentDialogResult.Primary)//Keep Existing File
                {
                    try
                    {
                        // Define paths
                        string sourceFile = asset.Media.OriginalPath; // Path of the source file
                        string destinationFolder = asset.Media.MediaPath; // Target folder
                        string fileName = asset.Media.Title; // Original file name
                        string destinationFile = Path.Combine(destinationFolder, fileName);

                        // Check if file exists and create a unique name if necessary
                        if (File.Exists(destinationFile))
                        {
                            string fileExtension = Path.GetExtension(fileName); // Get file extension
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName); // Get file name without extension

                            // Generate a new unique file name
                            int counter = 1;
                            do
                            {
                                destinationFile = Path.Combine(destinationFolder, $"{fileNameWithoutExtension} ({counter}){fileExtension}");
                                counter++;
                            } while (File.Exists(destinationFile));
                        }
                        asset.Media.Title = destinationFile;
                        // Copy the file to the destination
                        File.Copy(sourceFile, destinationFile);
                        Console.WriteLine($"File copied to: {destinationFile}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error copying file: {ex.Message}");
                    }

                }
                else if (result1 == ContentDialogResult.Secondary)
                {
                    try
                    {
                        File.Copy(asset.Media.OriginalPath,asset.Media.MediaSource.LocalPath, overwrite: true);
                    }
                    catch (IOException ex)
                    {
                        CopyFileWithRetry(asset.Media.OriginalPath, asset.Media.MediaSource.LocalPath);
                    }
                }
                else
                {
                    dialog.Hide();
                }
                return true;
            }
            else
            {
                assetstoRemove.Add(asset);
                dialog.Hide();
                return false;
            }
        }
        public void CopyFileWithRetry(string sourceFile, string destinationFile)
        {
            int maxRetries = 5;
            int retryDelay = 500; // Delay in milliseconds between retries

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Attempt to copy the file
                    File.Copy(sourceFile, destinationFile, overwrite: true);
                    Console.WriteLine("File copied successfully.");
                    return;
                }
                catch (IOException ex)
                {
                    // Check if it's the file access error (locked by another process)
                    if (ex.Message.Contains("The process cannot access the file"))
                    {
                        Console.WriteLine($"File is in use, retrying... (Attempt {attempt + 1}/{maxRetries})");
                        Thread.Sleep(retryDelay); // Wait for a while before retrying
                    }
                    else
                    {
                        // If it's some other IOException, rethrow the exception
                        throw;
                    }
                }
            }

            Console.WriteLine("Max retries reached, file could not be copied.");
        }
        private async Task<bool> InsertAsset(Asset asset)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = string.Empty;
            parameters.Add("@AssetName", asset.Media.Title);
            parameters.Add("@AssetPath", asset.Media.MediaPath);
            parameters.Add("@OriginalPath", asset.Media.OriginalPath);
            parameters.Add("@Duration", Convert.ToDateTime(asset.Media.DurationString));
            parameters.Add("@Version", asset.Media.Version);
            parameters.Add("@Type",asset.Media.Type);
            parameters.Add("@Size", asset.Media.Size);
            parameters.Add("@createdUser", asset.Media.CreatedUser);
            parameters.Add("@CreationDate", asset.Media.CreationDate.ToString("yyyy-MM-dd"));

            //query = "INSERT INTO asset (asset_name, asset_path, original_path, duration,version,type,size,created_user,created_at) " +
            //        $"SELECT * FROM(SELECT @AssetName,@AssetPath,@OriginalPath,@Duration,@Version,@Type,@Size,@createdUser,@CreationDate ) AS tmp " +
            //        "WHERE NOT EXISTS( SELECT 1 FROM asset WHERE " +
            //        $"asset_name = @AssetName AND asset_path = @AssetPath " +
            //        $"AND original_path = @OriginalPath AND duration = @Duration) LIMIT 1;";
            query= "INSERT INTO asset(asset_name, asset_path, original_path, duration, version, type, size, created_user, created_at) "+
                    "SELECT * FROM(" +
                    "SELECT "+
                    "@AssetName AS asset_name,"+
                    "@AssetPath AS asset_path,"+
                    "@OriginalPath AS original_path,"+
                    "@Duration AS duration,"+
                    "@Version AS version,"+
                    "@Type AS type,"+
                    "@Size AS size,"+
                    "@createdUser AS created_user,"+
                    "@CreationDate AS created_at"+
                    ") AS tmp "+
                    "WHERE NOT EXISTS("+
                        "SELECT 1 FROM asset "+
                        "WHERE asset_name = @AssetName "+
                        "AND asset_path = @AssetPath "+
                        "AND original_path = @OriginalPath "+
                        "AND duration = @Duration"+
                        ") LIMIT 1";

            int newAssetId = 0;
            try
            {
                if (dataAccess.ExecuteNonQuery(query, parameters, out newAssetId) == 0)
                {
                    await ShowContentDialogAsync("Asset already exists.");
                    return false;
                }
                else if (newAssetId <= 0)
                {
                    await ShowContentDialogAsync("Can't save asset.");
                    return false;
                }
                else
                    return true;
            }
            catch (Exception ex)
            {
                await ShowContentDialogAsync($"An error occurred: {ex.Message}");
                return false;
            }
        }
        private async Task ShowContentDialogAsync(string message)
        {
            if (this.Content != null) // Ensure window is still open
            {
                var dialog = new ContentDialog
                {
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDataGrid();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var asset = button?.Tag as Asset;
            if (asset != null)
            {
                AssetList.Remove(asset);
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _instance = null;
        }
    }
    public class Asset : ObservableObject
    {
        private int assetId;
        private string fileName = string.Empty;
        private string assetPath = string.Empty;
        private string originalPath = string.Empty;
        private string duration = string.Empty;
        private int progress = 0;
        private DateTime startTime;
        private DateTime completionTime;
        private string status = "Waiting...";

        private int id;
        private string name = string.Empty;
        private string version = "1";
        //  private TimeSpan duration = TimeSpan.Zero;
        private string type = string.Empty;
        private string file = string.Empty;
        private string createdUser = string.Empty;
        private DateOnly creationDate = DateOnly.MinValue;
        private string updatedUser = string.Empty;
        private DateTime lastUpdated = DateTime.Now;
        private double size = 0;
        private string description = string.Empty;
        private MediaPlayerItem media;

        public int AssetId
        {
            get => assetId;
            set => SetProperty(ref assetId, value);
        }
        //public string FileName
        //{
        //    get => fileName;
        //    set => SetProperty(ref fileName, value);
        //}
        //public string AssetPath
        //{
        //    get => assetPath;
        //    set => SetProperty(ref assetPath, value);
        //}
        //public string OriginalPath
        //{
        //    get => originalPath;
        //    set => SetProperty(ref originalPath, value);
        //}
        //public string Duration
        //{
        //    get => duration;
        //    set => SetProperty(ref duration, value);
        //}
        public int Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }
        public DateTime StartTime
        {
            get => startTime;
            set => SetProperty(ref startTime, value);
        }
        public DateTime CompletionTime
        {
            get => completionTime;
            set => SetProperty(ref completionTime, value);
        }
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }
        public MediaPlayerItem Media
        {
            get => media;
            set => SetProperty(ref media, value);
        }
        public Asset(MediaPlayerItem media)
        {
            Media = media;
        }

        //public string Version
        //{
        //    get => version;
        //    set => SetProperty(ref version, value);
        //}

        //public string Type
        //{
        //    get => type;
        //    set => SetProperty(ref type, value);
        //}
        //public string File
        //{
        //    get => file;
        //    set => SetProperty(ref file, value);
        //}
        //public string CreatedUser
        //{
        //    get => createdUser;
        //    set => SetProperty(ref createdUser, value);
        //}
        //public DateOnly CreationDate
        //{
        //    get => creationDate;
        //    set => SetProperty(ref creationDate, value);
        //}
        //public string UpdatedUser
        //{
        //    get => updatedUser;
        //    set => SetProperty(ref updatedUser, value);
        //}
        //public DateTime LastUpdated
        //{
        //    get => lastUpdated;
        //    set => SetProperty(ref lastUpdated, value);
        //}
        //public double Size
        //{
        //    get => size;
        //    set => SetProperty(ref size, value);
        //}
        //public string Description
        //{
        //    get => description;
        //    set => SetProperty(ref description, value);
        //}
    }
    //public static class Win32Interop
    //{
    //    private const int GWL_HWNDPARENT = -8;

    //    [DllImport("user32.dll", SetLastError = true)]
    //    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    //    [DllImport("user32.dll", SetLastError = true)]
    //    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    //    [DllImport("user32.dll", SetLastError = true)]
    //    public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    //    /// <summary>
    //    /// Sets the owner of a window.
    //    /// </summary>
    //    /// <param name="child">Handle to the child window.</param>
    //    /// <param name="owner">Handle to the owner window.</param>
    //    public static void SetWindowOwner(IntPtr child, IntPtr owner)
    //    {
    //        SetWindowLongPtr(child, GWL_HWNDPARENT, owner);
    //    }
    //}

}
