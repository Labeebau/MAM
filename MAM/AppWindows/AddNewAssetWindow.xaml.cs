﻿using MAM.Data;
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
using Windows.ApplicationModel.DataTransfer;
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
        private static MediaLibraryViewModel viewModel;
        //public static UploadHistoryPage uploadHistory { get; set; }
        public ObservableCollection<Asset> AssetList { get; set; } = new ObservableCollection<Asset>();

        public AddNewAssetWindow(MediaLibraryViewModel mediaLibrary)
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
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
            viewModel = mediaLibrary;
            DgvFiles.DragStarting += DgvFiles_DragStarting;
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
        public static void ShowWindow(MediaLibraryViewModel mediaLibrary)
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
            foreach (var media in viewModel.MediaPlayerItems)
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
            foreach (string ext in GlobalClass.Instance.SupportedVideos)
            {
                picker.FileTypeFilter.Add(ext); // Allow videos only
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
                        AddFilestoAssetList(file);
                    }
                }
            }
        }
        private async void AddFilestoAssetList(StorageFile file)
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
                MediaSource = new Uri(Path.Combine(viewModel.MediaLibraryObj.BinName, file.Name)),
                MediaPath = viewModel.MediaLibraryObj.BinName,
                OriginalPath = file.Path,
                Size = fileSize,
                Title = file.Name,
                Type = type,
                Version = "V1"
            };
            AssetList.Add(new Asset(media));
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
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var assetsCopy = AssetList.ToList(); // Make a copy if AssetList is used in UI after window closes

            // Fire and forget
            _ = Task.Run(() => ProcessAfterCloseAsync(assetsCopy));

            this.Close(); // Immediately close the window
        }

        private List<Asset> assetstoRemove = new List<Asset>();
        private async Task ProcessAfterCloseAsync(List<Asset> assetList)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(viewModel.MediaLibraryObj.BinName);
                var files = await folder.GetFilesAsync();
                //if (!Directory.Exists(viewModel.MediaLibraryObj.ProxyFolder))
                //    Directory.CreateDirectory(viewModel.MediaLibraryObj.ProxyFolder);
                List<MediaPlayerItem> newMediaItems = new();
                var semaphore = new SemaphoreSlim(3); // Limit to 3 concurrent tasks
                var tasks = assetList.Select(async asset =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var newFile = asset.Media.MediaSource.LocalPath;
                        var match = files.FirstOrDefault(f => f.Name == asset.Media.Title);
                        (bool confirmed, bool reWritten, string updatedFile) result = (false, false, null);
                        if (match != null)
                        {
                            var oldFileList = new List<string> { match.Path };
                            // ✅ Await the dialog on UI thread with global dialog lock inside
                            result = await UIThreadHelper.RunOnUIThreadAsync(() =>
                               ShowMessageBox(asset, oldFileList, App.MainAppWindow.Content.XamlRoot));
                            if (!result.confirmed)
                                return;
                            newFile = result.updatedFile;
                        }
                        await UIThreadHelper.RunOnUIThreadAsync(() =>
                        {
                            App.MainAppWindow.StatusBar.ShowStatus($"Copying {asset.Media.OriginalPath} to {asset.Media.MediaSource.LocalPath}...", true);
                        });
                        // 10s timeout, or use CancellationToken.None for no timeout
                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        await CopyFileWithProgressAsync(asset.Media.OriginalPath, newFile, cts.Token);
                        await UIThreadHelper.RunOnUIThreadAsync(() =>
                        {
                            App.MainAppWindow.StatusBar.ShowStatus($"{asset.Media.OriginalPath} copied to {asset.Media.MediaSource.LocalPath}...");
                        });
                        //if (!result.reWritten)//
                        //{
                        int newId = 0;
                        if (File.Exists(newFile))
                        {
                            string baseRoot = Directory.GetParent(viewModel.MediaLibraryObj.ThumbnailFolder).FullName;
                            string relativePath = asset.Media.MediaPath.Substring(baseRoot.Length).TrimStart('\\');
                            string mediaLibrary = Path.GetFileName(viewModel.MediaLibraryObj.FileServer.FileFolder);
                            relativePath = relativePath.Substring(mediaLibrary.Length).TrimStart('\\');
                            string thumbnailMediaPath = Path.Combine(viewModel.MediaLibraryObj.ThumbnailFolder, relativePath);
                            if (!Directory.Exists(thumbnailMediaPath))
                                Directory.CreateDirectory(thumbnailMediaPath);
                            var thumbnailFolder = await StorageFolder.GetFolderFromPathAsync(thumbnailMediaPath);
                            var thumbnailFile = await thumbnailFolder.CreateFileAsync($"{Path.GetFileNameWithoutExtension(newFile)}_Thumbnail.JPG", CreationCollisionOption.ReplaceExisting);
                            var originalFile = await StorageFile.GetFileFromPathAsync(asset.Media.OriginalPath);

                            var proxyFolder = Path.Combine(viewModel.MediaLibraryObj.ProxyFolder, relativePath);
                            if (!Directory.Exists(proxyFolder))
                                Directory.CreateDirectory(proxyFolder);
                            var proxyPath = Path.Combine(proxyFolder, Path.GetFileNameWithoutExtension(asset.Media.Title) + "_Proxy.MP4");
                            asset.Media.ProxyPath = proxyPath;
                            asset.Media.ThumbnailPath = thumbnailFile.Path;
                            newId = await InsertAsset(asset);
                            if (newId > 0)
                            {
                                await GenerateThumbnailAsync(originalFile, thumbnailFile);

                                await ProcessAssetAsync(asset, proxyPath);
                                var process = ProcessManager.AllProcesses.FirstOrDefault(p =>
                                    p.FilePath == asset.Media.MediaPath && p.ProcessType == ProcessType.ProxyGeneration);

                                if (process != null)
                                {
                                    // Wait for the process to reach 100%
                                    var timeout = Task.Delay(TimeSpan.FromMinutes(5));
                                    while (process.Progress < 100)
                                    {
                                        if (timeout.IsCompleted)
                                            break;
                                        await Task.Delay(500);
                                    }
                                }
                                if (IsAssetReady(newFile, thumbnailFile.Path, proxyPath))
                                {
                                    var mediaItem = new MediaPlayerItem
                                    {
                                        MediaId = newId,
                                        MediaSource = new Uri(asset.Media.MediaSource.LocalPath),
                                        MediaPath = Path.GetDirectoryName(asset.Media.MediaSource.LocalPath),
                                        ThumbnailPath = thumbnailFile.Path,
                                        ProxyPath = proxyPath,
                                        Title = asset.Media.Title,
                                        Type = asset.Media.Type,
                                        DurationString = asset.Media.DurationString
                                    };
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        if (!viewModel.MediaPlayerItems.Contains(mediaItem))
                                            viewModel.MediaPlayerItems.Add(mediaItem);
                                        viewModel.ApplyFilter();
                                        viewModel.MediaLibraryObj.FileCount = viewModel.MediaPlayerItems.Count;
                                    });
                                }
                                   
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string msg = ex is OperationCanceledException ? "Copy timed out." : $"Error: {ex.Message}";
                        await UIThreadHelper.RunOnUIThreadAsync(() =>
                        {
                            App.MainAppWindow.StatusBar.ShowStatus(msg, false);
                        });
                        File.AppendAllText("log.txt", "Error: " + ex.ToString() + "\n");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", "Unhandled Error: " + ex.ToString() + "\n");
            }
        }

        private bool IsAssetReady(string copyPath, string thumbnailPath, string proxyPath)
        {
            return File.Exists(copyPath) && File.Exists(thumbnailPath) && File.Exists(proxyPath);
        }

        public static async Task CopyFileWithProgressAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
        {

            var process = await ProcessManager.CreateProcessAsync(sourcePath, ProcessType.FileCopying, "Copying File");

            UIThreadHelper.RunOnUIThread(() =>
            {
                TransactionHistoryPage.TransactionHistoryStatic.InitializeWithParameter("UploadHistory");
            });
            try
            {
                const int bufferSize = 1024 * 1024; // 1 MB buffer for large files
                byte[] buffer = new byte[bufferSize];
                long totalBytesRead = 0;
                long totalBytes = new FileInfo(sourcePath).Length;

                using FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
                using FileStream destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

                int lastReportedProgress = 0;

                int bytesRead;
                while ((bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytesRead += bytesRead;

                    int currentProgress = (int)((totalBytesRead * 100) / totalBytes);
                    if (currentProgress > lastReportedProgress)
                    {
                        lastReportedProgress = currentProgress;

                        // Update process status (thread-safe)
                        UIThreadHelper.RunOnUIThread(() =>
                        {
                            process.Progress = currentProgress;
                            process.Status = $"Copying... {currentProgress}%";
                        });
                        // await ProcessManager.UpdateProcessStatusInDatabaseAsync(process);
                    }
                }

                // Finalize
                UIThreadHelper.RunOnUIThread(() =>
                {
                    process.Progress = 100;
                    process.Status = "File copying finished";
                    process.Result = "Finished";
                    process.CompletionTime = DateTime.Now;
                    // TransactionHistoryPage.TransactionHistoryStatic.ProcessList.Remove(process);
                });
                UIThreadHelper.RunOnUIThread(async () =>
                {
                    await ProcessManager.CompleteProcessAsync(process);
                });

            }
            catch (UnauthorizedAccessException ex)
            {
                await UIThreadHelper.RunOnUIThreadAsync(() =>
                {
                    App.MainAppWindow.StatusBar.ShowStatus($"{ex.Message}");
                });
                await ProcessManager.FailProcessAsync(process, "Access denied");

                //throw new IOException($"Access denied to source file: {sourcePath}. Check if it's locked or restricted.", ex);
            }
            catch (OperationCanceledException)
            {
                await ProcessManager.FailProcessAsync(process, "Copy operation canceled");
            }
            catch (Exception ex)
            {
                await ProcessManager.FailProcessAsync(process, $"Error: {ex.Message}");
            }

        }



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

        private async Task ProcessAssetAsync(Asset asset, string proxyPath)
        {
            var filePath = asset.Media.MediaSource.LocalPath;
            var proxyProcess = ProcessManager.AllProcesses.FirstOrDefault(p => p.FilePath == filePath && p.ProcessType == ProcessType.ProxyGeneration) ?? await ProcessManager.CreateProcessAsync(filePath, ProcessType.ProxyGeneration, "Generating Proxy");
            try
            {
                UIThreadHelper.RunOnUIThread(() => App.MainAppWindow.StatusBar.ShowStatus("Generating proxy...", true));
                TimeSpan? totalDuration = null;
                UIThreadHelper.RunOnUIThread(async () =>
                {
                    TransactionHistoryPage.TransactionHistoryStatic.LoadHistory("UploadHistory");
                });

                var progress = new Progress<string>(data =>
                {
                    if (data.Contains(" Duration:"))
                    {
                        var durationString = data.Split(new[] { " Duration: ", "," }, StringSplitOptions.RemoveEmptyEntries)[1];
                        if (TimeSpan.TryParse(durationString, out var duration))
                            totalDuration = duration;
                    }
                    if (data.Contains("time=") && totalDuration.HasValue)
                    {
                        var match = Regex.Match(data, @"time=(\d{2}:\d{2}:\d{2}\.\d{2})");
                        if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, out var currentTime))
                        {
                            var progressPercentage = (currentTime.TotalSeconds / totalDuration.Value.TotalSeconds) * 100;
                            App.UIDispatcherQueue.TryEnqueue(() =>
                            {
                                var process = ProcessManager.AllProcesses.FirstOrDefault(p => p.ProcessId == proxyProcess.ProcessId);
                                if (process != null)
                                    process.Progress = (int)progressPercentage;
                            });
                        }
                    }
                });

                //string outputFile = Path.Combine(viewModel.MediaLibraryObj.ProxyFolder, $"Proxy_{Path.GetFileNameWithoutExtension(asset.Media.Title)}.mp4");
                string outputFile = proxyPath;
                string arguments = $"-i \"{asset.Media.OriginalPath}\" -vf scale=640:-1 -c:v libx264 -b:v 200k -c:a aac -b:a 128k \"{outputFile}\"";

                await RunFFmpegProcessWithProgress(arguments, progress);

                App.UIDispatcherQueue.TryEnqueue(async () =>
                {
                    var process = ProcessManager.AllProcesses.FirstOrDefault(p => p.ProcessId == proxyProcess.ProcessId);
                    if (process != null)
                    {
                        Debug.WriteLine(process.Result);
                        ProcessStatusPage.Instance?.FilterData();
                        await ProcessManager.CompleteProcessAsync(process, "Proxy generation finished");
                        await GlobalClass.Instance.AddtoHistoryAsync("Add asset to library", $"Added '{filePath}' to library.");
                        UIThreadHelper.RunOnUIThread(() =>
                        {
                            App.MainAppWindow.StatusBar.ShowStatus("Generated proxy.", false);
                            App.MainAppWindow.StatusBar.HideStatus();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                await ProcessManager.FailProcessAsync(proxyProcess, "Proxy Generation Failed");
                UIThreadHelper.RunOnUIThread(() => App.MainAppWindow.StatusBar.ShowStatus("Proxy generation failed ..."));
                Debug.WriteLine($"Error: {ex.Message}");
                File.AppendAllText("log.txt", "Error: " + ex + "\n");
            }
        }


        private async Task RunFFmpegProcessWithProgress(string arguments, IProgress<string> progress)
        {
            File.AppendAllText("log.txt", "Trying to start FFMPEG\n");

            using var process = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GlobalClass.Instance.ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var outputCompletion = new TaskCompletionSource();
            var errorCompletion = new TaskCompletionSource();
            var exitCompletion = new TaskCompletionSource();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    progress.Report(e.Data);
                else
                    outputCompletion.TrySetResult();
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    progress.Report(e.Data);
                else
                    errorCompletion.TrySetResult();
            };

            try
            {
                process.Start();
                File.AppendAllText("log.txt", "FFMPEG started\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", "Error: " + ex + "\n");
                throw new InvalidOperationException("Failed to start the FFmpeg process.", ex);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _ = Task.Run(async () =>
            {
                await process.WaitForExitAsync();
                exitCompletion.TrySetResult();
                outputCompletion.TrySetResult();
                errorCompletion.TrySetResult();
            });

            var timeout = Task.Delay(TimeSpan.FromMinutes(2));
            var completed = await Task.WhenAny(Task.WhenAll(outputCompletion.Task, errorCompletion.Task, exitCompletion.Task), timeout);

            if (completed == timeout)
                File.AppendAllText("log.txt", "FFmpeg process timed out.\n");
            else
                File.AppendAllText("log.txt", "FFmpeg process completed successfully.\n");
        }



        private async Task GenerateThumbnailAsync(StorageFile videoFile, StorageFile thumbnailFile)
        {
            var process = await ProcessManager.CreateProcessAsync(videoFile.Path, ProcessType.ThumbnailGeneration, "Generating Thumbnail");
            UIThreadHelper.RunOnUIThread(() => { App.MainAppWindow.StatusBar.ShowStatus("Generating thumbnail...", true); });

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
                    Debug.WriteLine(process.Result);
                    await ProcessManager.CompleteProcessAsync(process, "Thumbnail generation finished");
                    UIThreadHelper.RunOnUIThread(() => { App.MainAppWindow.StatusBar.ShowStatus("Thumbnail Generated."); });
                    File.AppendAllText("log.txt", "Thumbnail Generated.\n");
                }
                else
                {
                    UIThreadHelper.RunOnUIThread(() => { App.MainAppWindow.StatusBar.ShowStatus("Thumbnail Generation Failed..."); });
                    await ProcessManager.FailProcessAsync(process, "Thumbnail Generation Failed");

                    Debug.WriteLine(process.Result);
                    File.AppendAllText("log.txt", "Thumbnail Generation Failed .\n");
                }
            }
            catch (Exception ex)
            {
                await ProcessManager.FailProcessAsync(process, "Thumbnail Generation Failed");
                Debug.WriteLine($"Error: {ex.Message}");
                File.AppendAllText("log.txt", "Error: " + ex.ToString() + "\n");
            }
        }
        private static readonly SemaphoreSlim DialogSemaphore = new(1, 1);

        private async Task<(bool, bool, string)> ShowMessageBox(Asset asset, List<string> oldFileList, XamlRoot xamlRoot)
        {
            await DialogSemaphore.WaitAsync(); // Lock the dialog queue
            var tcs = new TaskCompletionSource<(bool, bool, string)>();

            try
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
                        Text = $"' {asset.Media.Title} ' already exists!",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 15,
                    },
                }
                    },
                    Content = new StackPanel
                    {
                        Children =
                {
                    new TextBlock
                    {
                        Text = "Old Files",
                        Margin = new Thickness(0, 0, 0, 10)
                    },
                    listBox,
                    new TextBlock
                    {
                        Text = "Please select an action to create new asset",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 16,
                    },
                }
                    },
                    PrimaryButtonText = "Create New Asset",
                    CloseButtonText = "Cancel",
                    XamlRoot = xamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var resultDialog = new ContentDialog
                    {
                        Content = "Keep or delete existing file?",
                        PrimaryButtonText = "Keep Existing File",
                        SecondaryButtonText = "Delete Existing File",
                        CloseButtonText = "Cancel",
                        XamlRoot = xamlRoot
                    };

                    var result1 = await resultDialog.ShowAsync();

                    if (result1 == ContentDialogResult.Primary)
                    {
                        try
                        {
                            string sourceFile = asset.Media.OriginalPath;
                            string destinationFolder = asset.Media.MediaPath;
                            string fileName = asset.Media.Title;
                            string destinationFile = Path.Combine(destinationFolder, fileName);

                            if (File.Exists(destinationFile))
                            {
                                string ext = Path.GetExtension(fileName);
                                string name = Path.GetFileNameWithoutExtension(fileName);
                                int counter = 1;

                                do
                                {
                                    destinationFile = Path.Combine(destinationFolder, $"{name} ({counter}){ext}");
                                    counter++;
                                } while (File.Exists(destinationFile));
                            }

                            asset.Media.Title = Path.GetFileName(destinationFile);
                            asset.Media.MediaSource = new Uri(destinationFile);
                            // await CopyFileWithProgressAsync(sourceFile, destinationFile);

                            // File.Copy(sourceFile, destinationFile);
                            tcs.SetResult((true, false, destinationFile));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error copying file: {ex.Message}");
                            tcs.SetResult((false, false, string.Empty));
                        }
                    }
                    else if (result1 == ContentDialogResult.Secondary)
                    {
                        //try
                        //{
                        //    File.Copy(asset.Media.OriginalPath, asset.Media.MediaSource.LocalPath, overwrite: true);
                        //}
                        //catch (IOException)
                        //{
                        //    CopyFileWithRetry(asset.Media.OriginalPath, asset.Media.MediaSource.LocalPath);
                        //}

                        tcs.SetResult((true, true, asset.Media.MediaSource.LocalPath));
                    }
                    else
                    {
                        tcs.SetResult((true, false, string.Empty)); // Cancel case
                    }
                }
                else
                {
                    assetstoRemove.Add(asset);
                    tcs.SetResult((false, false, string.Empty));
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                DialogSemaphore.Release(); // Always release lock
            }

            return await tcs.Task;
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
        private async Task<int> InsertAsset(Asset asset)
        {
            UIThreadHelper.RunOnUIThread(() => { App.MainAppWindow.StatusBar.ShowStatus("Copying to db...", true); });

            //  App.MainAppWindow.StatusBar.ShowStatus("Copying to db...", true);
            List<MySqlParameter> parameters = [];
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@AssetName", asset.Media.Title));
            parameters.Add(new MySqlParameter("@AssetPath", asset.Media.MediaPath));
            parameters.Add(new MySqlParameter("@ProxyPath", Path.GetDirectoryName(asset.Media.ProxyPath)));
            parameters.Add(new MySqlParameter("@ThumbnailPath", Path.GetDirectoryName(asset.Media.ThumbnailPath)));
            parameters.Add(new MySqlParameter("@Duration", Convert.ToDateTime(asset.Media.DurationString)));
            parameters.Add(new MySqlParameter("@Version", asset.Media.Version));
            parameters.Add(new MySqlParameter("@Type", asset.Media.Type));
            parameters.Add(new MySqlParameter("@Size", asset.Media.Size));
            parameters.Add(new MySqlParameter("@createdUser", asset.Media.CreatedUser));
            parameters.Add(new MySqlParameter("@CreationDate", asset.Media.CreationDate.ToString("yyyy-MM-dd")));
            parameters.Add(new MySqlParameter("@IsArchived", asset.Media.IsArchived));
            query = "INSERT INTO asset(asset_name, asset_path, proxy_path,thumbnail_path, duration, version, type, size, created_user, created_at,is_archived) " +
                    "SELECT * FROM(" +
                    "SELECT " +
                    "@AssetName AS asset_name," +
                    "@AssetPath AS asset_path," +
                    "@ProxyPath AS proxy_path," +
                    "@ThumbnailPath AS thumbnail_path," +
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
                        "AND proxy_path = @ProxyPath " +
                        "AND thumbnail_path = @ThumbnailPath " +
                        "AND duration = @Duration" +
                        ") LIMIT 1";

            try
            {
                var (affectedRows, newAssetId, errorCode) = await dataAccess.ExecuteNonQuery(query, parameters);
                if (newAssetId == 0)
                {
                    await GlobalClass.Instance.ShowDialogAsync("Asset already exists.", this.Content.XamlRoot);
                    return 0;
                }
                else if (newAssetId <= 0)
                {
                    await GlobalClass.Instance.ShowDialogAsync("Unable to save asset", this.Content.XamlRoot);
                    return -1;
                }
                else
                {
                    UIThreadHelper.RunOnUIThread(() => { App.MainAppWindow.StatusBar.ShowStatus("Copied to db."); App.MainAppWindow.StatusBar.HideStatus(); });
                    return newAssetId;
                }
            }
            catch (Exception ex)
            {
                await GlobalClass.Instance.ShowDialogAsync($"An error occurred: {ex.Message}", this.Content.XamlRoot);
                return -1;
            }
        }
        //private async Task ShowContentDialogAsync(string message)
        //{
        //    if (this.Content != null) // Ensure window is still open
        //    {
        //        var dialog = new ContentDialog
        //        {
        //            Content = message,
        //            CloseButtonText = "OK",
        //            XamlRoot = this.Content.XamlRoot
        //        };
        //        await dialog.ShowAsync();
        //    }
        //}
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

        private void DgvFiles_DragOver(object sender, DragEventArgs e)
        {
            // Only allow files
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop to add files";
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsCaptionVisible = true;
        }

        private async void DgvFiles_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                foreach (var item in items)
                {
                    if (item is StorageFile file)
                    {
                        string extension = Path.GetExtension(file.Name);
                        if (!GlobalClass.Instance.SupportedFiles.Contains(extension))
                            continue;

                        AddFilestoAssetList(file);
                    }
                }
            }
        }

        private void DgvFiles_DragStarting(UIElement sender, DragStartingEventArgs args)
        {

        }

        private void DgvFiles_DragEnter(object sender, DragEventArgs e)
        {

        }
    }
    public class Asset : ObservableObject
    {
        private MediaPlayerItem media;
        public MediaPlayerItem Media
        {
            get => media;
            set => SetProperty(ref media, value);
        }
        public Asset(MediaPlayerItem media)
        {
            Media = media;
        }

    }
}
