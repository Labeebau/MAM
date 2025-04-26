using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews;
using MAM.Views.ProcessesViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
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
        DataAccess dataAccess = new();
        // Static instance to track the window
        public static AddNewAssetWindow _instance;
        private static MediaLibraryPage MediaLibraryPage;
        //public static UploadHistoryPage uploadHistory { get; set; }
        public ObservableCollection<Asset> AssetList { get; set; } = new ObservableCollection<Asset>();
        //public Process ThumbnailGeneration { get; set; }
        //public Process ProxyGeneration { get; set; }
        //public Process Transcoding { get; set; }
        //public Process QualityCheck { get; set; }

        public AddNewAssetWindow(MediaLibraryPage mediaLibrary)
        {
            this.InitializeComponent();
            var titleBar = AppWindow.TitleBar;
            // Set the background colors for active and inactive states
            titleBar.BackgroundColor = Colors.Black;
            titleBar.InactiveBackgroundColor = Colors.DarkGray;
            // Set the foreground colors (text/icons) for active and inactive states
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveForegroundColor = Colors.Gray;
            GlobalClass.Instance.DisableMaximizeButton(this);
            GlobalClass.Instance.SetWindowSizeAndPosition(1000, 800, this);
            DgvFiles.DataContext = this;
            MediaLibraryPage = mediaLibrary;

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
                AssetList.Add(new Asset(media));
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
                        FileInfo fileInfo = new(file.Path);
                        long fileSize = fileInfo.Length / (1024 * 1024);//Converts bytes to MB
                        MediaPlayerItem media = new()
                        {
                            CreatedUser = GlobalClass.Instance.CurrentUser.UserName,
                            CreationDate = DateOnly.FromDateTime(DateTime.Today.Date),
                            Duration = duration,
                            DurationString = duration.ToString(@"hh\:mm\:ss"),
                            MediaSource = new Uri(Path.Combine(MediaLibraryPage.MediaLibrary.BinName, file.Name)),
                            MediaPath = MediaLibraryPage.MediaLibrary.BinName,
                            OriginalPath = file.Path,
                            Size = fileSize,
                            Title = file.Name,
                            Type = type,
                            Version = "V1"
                        };
                        AssetList.Add(new Asset(media));
                    }
                }
            }
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
                        var result = await ShowMessageBox(asset, oldFileList);// shows AssetCreationConfirmation message
                        closeWindow = result.Item1;
                        newFile = result.Item2;
                        isExist = false;
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
                                var thumbnailFile = await proxyFolder.CreateFileAsync($"Thumbnail_{Path.GetFileNameWithoutExtension(newFile)}.jpg", CreationCollisionOption.ReplaceExisting);

                                var originalFile = await StorageFile.GetFileFromPathAsync(asset.Media.OriginalPath);
                                await GenerateThumbnailAsync(originalFile, thumbnailFile);
                                var proxyPath = new Uri(Path.Combine(proxyFolder.Path, "Proxy_" + asset.Media.Title));
                                MediaLibraryPage.viewModel.MediaPlayerItems.Add(new MediaPlayerItem { MediaSource = new Uri(asset.Media.MediaSource.LocalPath), MediaPath = Path.GetDirectoryName(asset.Media.MediaSource.LocalPath), ThumbnailPath = new Uri(thumbnailFile.Path), ProxyPath = proxyPath, Title = asset.Media.Title, DurationString = asset.Media.DurationString });
                                MediaLibraryPage.viewModel.AllMediaPlayerItems.Add(new MediaPlayerItem { MediaSource = new Uri(asset.Media.MediaSource.LocalPath), MediaPath = Path.GetDirectoryName(asset.Media.MediaSource.LocalPath), ThumbnailPath = new Uri(thumbnailFile.Path), ProxyPath = proxyPath, Title = asset.Media.Title, DurationString = asset.Media.DurationString });
                                MediaLibraryPage.MediaLibrary.FileCount = MediaLibraryPage.viewModel.MediaPlayerItems.Count;
                                //if (closeWindow)
                                //    this.Close();
                                if (UploadHistoryPage.UploadHistory != null)
                                {
                                    //UploadHistoryPage.UploadHistory.UploadHistories.Clear();
                                    foreach (var ast in AssetList)
                                    {
                                        UploadHistoryPage.UploadHistory.UploadHistories.Add(new UploadHistory { AssetObj = ast });
                                    }
                                }
                            }
                            if (closeWindow)
                                this.Close();

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
            this.Close();


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
        //public static void RemoveProcessFromAllAndFiltered(Process process)
        //{
        //    var matchAll = ProcessStore.AllProcesses.FirstOrDefault(p => p.ProcessId == process.ProcessId);
        //    if (matchAll != null)
        //        ProcessStore.AllProcesses.Remove(matchAll);

        //    var matchFiltered = ProcessStatusPage.Instance?.FilteredProcesses?.FirstOrDefault(p => p.ProcessId == process.ProcessId);
        //    if (matchFiltered != null)
        //        ProcessStatusPage.Instance.FilteredProcesses.Remove(matchFiltered);
        //}


        private async Task ProcessAssetAsync(Asset asset)
        {
            TimeSpan? totalDuration = null;
            int newProcessId = 0;
            // Record the start time
            Process ProxyGeneration = ProcessStore.AllProcesses.FirstOrDefault(p => p.FilePath == asset.Media.MediaSource.LocalPath);

            App.UIDispatcherQueue.TryEnqueue(async () =>
            {
               // asset.Status = "Waiting...";

                if (ProxyGeneration == null)
                {
                    ProxyGeneration = new Process(asset.Media.MediaSource.LocalPath);
                }
                ProxyGeneration.ProcessType = "Proxy Generation";
                ProxyGeneration.StartTime = DateTime.Now;
                ProxyGeneration.Status = "Waiting...";
                ProxyGeneration.Result = "Waiting";
                Debug.WriteLine(ProxyGeneration.Status);
                newProcessId = await ProcessStore.InsertProcessInDatabaseAsync(ProxyGeneration);
                ProxyGeneration.ProcessId = newProcessId;
                ProcessStore.AllProcesses.Add(ProxyGeneration);
                TransactionHistoryPage.TransactionHistoryStatic.InitializeWithParameter("UploadHistory");

            });
            try
            {
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
                               // asset.Progress = (int)progressPercentage;
                                var process = ProcessStore.AllProcesses.FirstOrDefault(p => p.ProcessId == ProxyGeneration.ProcessId);
                                //Debug.WriteLine($"Updating Process. Found: {process != null}, ProxyGen: {ProxyGeneration.GetHashCode()}, Found: {process?.GetHashCode()}");
                                if (process != null)
                                {
                                    process.Progress = (int)progressPercentage;
                                    process.Status = "Generating Proxy...";

                                }
                                //if (asset.Status != "Transferring...")
                                //{
                                //    asset.Status = "Transferring..."; // Update status to 'transferring'
                                //}
                            });
                        }
                    }
                });
                string outputFile = Path.Combine(MediaLibraryPage.MediaLibrary.ProxyFolder, "Proxy_" + Path.GetFileNameWithoutExtension(asset.Media.Title) + ".mp4");
                string arguments = $"-i \"{asset.Media.OriginalPath}\" -vf scale=640:-1 -c:v libx264 -b:v 200k  -c:a aac -b:a 128k \"{outputFile}\"";
                // Update status to 'transferring' just before starting
                //App.UIDispatcherQueue.TryEnqueue(() =>
                //{
                //   // asset.Status = "Generating Proxy";
                //    ProxyGeneration.Status = "Generating Proxy";
                //   // asset.StartTime = DateTime.Now;
                //    //ProxyGeneration.StartTime = DateTime.Now;

                //});

                await RunFFmpegProcessWithProgress(arguments, progress);

                // Record the completion time
                App.UIDispatcherQueue.TryEnqueue(async () =>
                {
                    //asset.Status = "Finished";
                    //asset.CompletionTime = DateTime.Now;
                    var process = ProcessStore.AllProcesses.FirstOrDefault(p => p.ProcessId == ProxyGeneration.ProcessId);
                    ////Debug.WriteLine($"Updating Process. Found: {process != null}, ProxyGen: {ProxyGeneration.GetHashCode()}, Found: {process?.GetHashCode()}");
                    if (process != null)
                    {
                        process.Status = "Finished";
                        process.CompletionTime = DateTime.Now;
                        process.Result = "Finished";
                        Debug.WriteLine(process.Result);
                        ProcessStatusPage.Instance?.FilterData();
                        await ProcessStore.UpdateProcessStatusInDatabaseAsync(process);
                        await GlobalClass.Instance.AddtoHistoryAsync("Add asset to library", $"Added '{asset.Media.MediaSource.LocalPath}' to library .");
                    }
                });
               
            }
            catch (Exception ex)
            {
                ProxyGeneration.Result = $"Error: {ex.Message}";
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task ProcessAssetsAsync(IEnumerable<Asset> assets)
        {
            var tasks = assets.Select(asset => ProcessAssetAsync(asset));
            await Task.WhenAll(tasks); // Process all assets concurrently
            //UploadHistoryPage.UploadHistory.UploadHistories.Clear();
        }

        private async Task RunFFmpegProcessWithProgress(string arguments, IProgress<string> progress)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = GlobalClass.Instance.ffmpegPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var outputCompletion = new TaskCompletionSource<bool>();
            var errorCompletion = new TaskCompletionSource<bool>();
            var processExitCompletion = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    progress.Report(e.Data);
                }
                else
                {
                    outputCompletion.TrySetResult(true); // Ensure completion
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    progress.Report(e.Data);
                }
                else
                {
                    errorCompletion.TrySetResult(true); // Ensure completion
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

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _ = Task.Run(async () =>
            {
                await process.WaitForExitAsync();
                processExitCompletion.TrySetResult(true);
                outputCompletion.TrySetResult(true); // Forcefully complete in case it's stuck
                errorCompletion.TrySetResult(true);
            });

            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2)); // Avoid infinite waiting

            var completedTask = await Task.WhenAny(Task.WhenAll(outputCompletion.Task, errorCompletion.Task, processExitCompletion.Task), timeoutTask);

            if (completedTask == timeoutTask)
            {
                //throw new TimeoutException("FFmpeg process took too long and was forcefully stopped.");
            }

            Console.WriteLine("FFmpeg process completed successfully.");
        }


















        private async Task RunFFmpegAsync(string inputFile, string outputFile, string ffmpegPath)
        {
            string arguments = $"-y -i \"{inputFile}\" -c:v libx264 -b:v 200k -vf scale=640:-1  -preset fast  -c:a aac -b:a 128k \"{outputFile}\"";
            await Task.Run(() => RunProcess(ffmpegPath, arguments));
        }

        private void RunProcess(string executable, string arguments)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo);
            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        private async Task GenerateThumbnailAsync(StorageFile videoFile, StorageFile thumbnailFile)
        {
            Process ThumbnailGeneration = new Process(videoFile.Path);
            ThumbnailGeneration.FilePath = videoFile.Path;
            ThumbnailGeneration.ProcessType = "Thumbnail Generation";
            ThumbnailGeneration.StartTime = DateTime.Now;
            ThumbnailGeneration.Status = "Generating Thumbnail";
            ThumbnailGeneration.Progress = 0;
            ThumbnailGeneration.Result = "Waiting";
            Debug.WriteLine(ThumbnailGeneration.Status);
            int newProcessId = await ProcessStore.InsertProcessInDatabaseAsync(ThumbnailGeneration);
            try
            {
                // Obtain a thumbnail of the video
                using var thumbnail = await videoFile.GetThumbnailAsync(ThumbnailMode.VideosView);
                if (thumbnail != null)
                {
                    // Open the destination thumbnail file for writing
                    using var outputStream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite);
                    // Copy the thumbnail's stream to the output file
                    await RandomAccessStream.CopyAsync(thumbnail, outputStream);
                    ThumbnailGeneration.ProcessId = newProcessId;
                    ThumbnailGeneration.CompletionTime = DateTime.Now;
                    ThumbnailGeneration.Status = "Finished";
                    ThumbnailGeneration.Progress = 100;
                    ThumbnailGeneration.Result = "Finished";
                    Debug.WriteLine(ThumbnailGeneration.Result);
                    await ProcessStore.UpdateProcessStatusInDatabaseAsync(ThumbnailGeneration);
                }
                else
                {
                    ThumbnailGeneration.Result = "Thumbnail Generation Failed";
                    Debug.WriteLine(ThumbnailGeneration.Result);
                }
            }
            catch (Exception ex)
            {
                ThumbnailGeneration.Result = $"Error: {ex.Message}";
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }
       



        private async Task<(bool, string)> ShowMessageBox(Asset asset, List<string> oldFileList)
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
                ContentDialog resultDialog = new()
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
                        asset.Media.Title = Path.GetFileName(destinationFile);
                        asset.Media.MediaSource = new Uri(destinationFile);
                        // Copy the file to the destination
                        File.Copy(sourceFile, destinationFile);
                        Console.WriteLine($"File copied to: {destinationFile}");
                        return (true, destinationFile);
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
                        File.Copy(asset.Media.OriginalPath, asset.Media.MediaSource.LocalPath, overwrite: true);
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
                return (true, string.Empty);
            }
            else
            {
                assetstoRemove.Add(asset);
                dialog.Hide();
                return (false, string.Empty);
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
            List<MySqlParameter> parameters = [];
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@AssetName", asset.Media.Title));
            parameters.Add(new MySqlParameter("@AssetPath", asset.Media.MediaPath));
            parameters.Add(new MySqlParameter("@OriginalPath", asset.Media.OriginalPath));
            parameters.Add(new MySqlParameter("@Duration", Convert.ToDateTime(asset.Media.DurationString)));
            parameters.Add(new MySqlParameter("@Version", asset.Media.Version));
            parameters.Add(new MySqlParameter("@Type", asset.Media.Type));
            parameters.Add(new MySqlParameter("@Size", asset.Media.Size));
            parameters.Add(new MySqlParameter("@createdUser", asset.Media.CreatedUser));
            parameters.Add(new MySqlParameter("@CreationDate", asset.Media.CreationDate.ToString("yyyy-MM-dd")));
            parameters.Add(new MySqlParameter("@IsArchived", asset.Media.IsArchived));
            query = "INSERT INTO asset(asset_name, asset_path, original_path, duration, version, type, size, created_user, created_at,is_archived) " +
                    "SELECT * FROM(" +
                    "SELECT " +
                    "@AssetName AS asset_name," +
                    "@AssetPath AS asset_path," +
                    "@OriginalPath AS original_path," +
                    "@Duration AS duration," +
                    "@Version AS version," +
                    "@Type AS type," +
                    "@Size AS size," +
                    "@createdUser AS created_user," +
                    "@CreationDate AS created_at," +
                    "@IsArchived AS is_archived" +
                    ") AS tmp " +
                    "WHERE NOT EXISTS(" +
                        "SELECT 1 FROM asset " +
                        "WHERE asset_name = @AssetName " +
                        "AND asset_path = @AssetPath " +
                        "AND original_path = @OriginalPath " +
                        "AND duration = @Duration" +
                        ") LIMIT 1";

            try
            {
                var (affectedRows, newAssetId, errorCode) = await dataAccess.ExecuteNonQuery(query, parameters);
                if (newAssetId == 0)
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
        //private int assetId;
        //private string fileName = string.Empty;
        //private string assetPath = string.Empty;
        //private string originalPath = string.Empty;
        //private string duration = string.Empty;
        //private int progress = 0;
        //private DateTime startTime;
        //private DateTime completionTime;
        //private string status = "Waiting...";

        //private int id;
        //private string name = string.Empty;
        //private string version = "1";
        ////  private TimeSpan duration = TimeSpan.Zero;
        //private string type = string.Empty;
        //private string file = string.Empty;
        //private string createdUser = string.Empty;
        //private DateOnly creationDate = DateOnly.MinValue;
        //private string updatedUser = string.Empty;
        //private DateTime lastUpdated = DateTime.Now;
        //private double size = 0;
        //private string description = string.Empty;
        private MediaPlayerItem media;

        //public int AssetId
        //{
        //    get => assetId;
        //    set => SetProperty(ref assetId, value);
        //}
        //public int Progress
        //{
        //    get => progress;
        //    set => SetProperty(ref progress, value);
        //}
        //public DateTime StartTime
        //{
        //    get => startTime;
        //    set => SetProperty(ref startTime, value);
        //}
        //public DateTime CompletionTime
        //{
        //    get => completionTime;
        //    set => SetProperty(ref completionTime, value);
        //}
        //public string Status
        //{
        //    get => status;
        //    set => SetProperty(ref status, value);
        //}
        public MediaPlayerItem Media
        {
            get => media;
            set => SetProperty(ref media, value);
        }
        public Asset(MediaPlayerItem media)
        {
            Media = media;
        }
        //public Asset()
        //{

        //}

    }
}
