using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using System.Collections.ObjectModel;

namespace MAM.Data
{
   
    public class GlobalClass
    {
        #region Global Data
        // Step 1: Private static instance of the same class
        private static GlobalClass _instance = null;

        // Lock synchronization object for thread safety
        private static readonly object _lock = new object();

        // Step 2: Private constructor to prevent direct instantiation
        private GlobalClass()
        {
            // Initialize default values
           // CurrentUserName = "admin";
            AppTheme = "Dark";
        }

        // Step 3: Public static property to provide global access to the instance
        public static GlobalClass Instance
        {
            get
            {
                // Ensure thread-safety
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GlobalClass();
                    }
                    return _instance;
                }
            }
        }

        // Step 4: Define properties for global data
        public string CurrentUserName { get; set; } = string.Empty;
        public int CurrentUserId { get; set; } = 0;

        public string AppTheme { get; set; }
        public string Language { get; set; }

        // You can add more properties as needed, like:
        public int SessionTimeout { get; set; }
        public bool IsLoggedIn { get; set; }
        public string MediaLibraryPath { get; set; } = @"F:\MAM\Data";
        public string ArchivePath { get; set; } = @"F:\MAM\Archive";

        public string ffmpegPath = @"C:\Program Files\ffmpeg\ffmpeg-2024-12-04-git-2f95bc3cb3-full_build\bin\ffmpeg.exe";
        public string ffprobePath = @"C:\Program Files\ffmpeg\ffmpeg-2024-12-04-git-2f95bc3cb3-full_build\bin\ffprobe.exe";


        //public List<string> SupportedFiles = new List<string> { ".avi", ".mpeg", ".mpg", ".mov", ".mkv", ".mp4", ".mp3", ".flac", ".wav", ".aac", ".ogg", ".jpeg", ".jpg", ".png", ".gif", ".tiff", ".bmp", ".svg" };
        public List<string> SupportedVideos = new List<string> { ".avi", ".mpeg", ".mpg", ".mov", ".mkv", ".mp4" ,".wmv"};
        public List<string> SupportedAudios = new List<string> { ".mp3", ".flac",".wma", ".wav", ".aac", ".ogg" };
        public List<string> SupportedImages = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".tiff", ".bmp", ".svg",".webp" };
        public List<string> SupportedDocuments = new List<string> { ".txt", ".rtf", ".doc", ".docx", ".csv", ".pdf", ".xls", ".xlsx",".html",".ppt", ".pptx",".odt" };
        public List<string> SupportedFiles => SupportedVideos
    .Concat(SupportedAudios)
    .Concat(SupportedImages)
    .Concat(SupportedDocuments)
    .ToList();
        public ObservableCollection<string> RecentFiles = new ObservableCollection<string>();
        #endregion Global Data
        #region Global Methods
        // Helper method for showing ContentDialog
        public async Task ShowErrorDialogAsync(string message, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }
        //public void ShowErrorDialog(string message,XamlRoot xamlRoot)
        //{
        //    ContentDialog errorDialog = new ContentDialog
        //    {
        //        Title = "Error",
        //        Content = message,
        //        CloseButtonText = "OK",
        //        XamlRoot = xamlRoot // Set the XamlRoot to ensure proper display
        //    };

        //    errorDialog.ShowAsync();
        //}
        public void DisableMaximizeButton(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false; // Disable maximize button
            }
        }
        public void SetWindowSizeAndPosition(int width, int height,Window window)
        {
            // Get the native window handle of the current window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

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
        public bool Equals(object obj1,object obj2, out Dictionary<string, object> props)
        {
            props = new Dictionary<string, object>();
            if (obj1 == null || obj2 == null)
                return obj1 == obj2;

            if (obj1.GetType() != obj2.GetType())
                return false;
           
            foreach (PropertyInfo userProperty in obj1.GetType().GetProperties())
            {
                if ((userProperty.Name== "IsReadOnly") || (userProperty.Name == "IsEnabled"))
                    continue;
                var value1 = userProperty.GetValue(obj1);
                var value2 = userProperty.GetValue(obj2);
                if (!object.Equals(value1, value2))
                    props.Add($"{userProperty.Name}", value1);
            }
            return props.Count > 0;
        }
        #endregion Global Methods
    }


}
