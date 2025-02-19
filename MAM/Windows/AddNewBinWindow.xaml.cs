using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews;
using MAM.ViewModels;
using System.Collections.ObjectModel;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddNewBinWindow : Window
    {
        // Static instance to track the window
        private static AddNewBinWindow _instance;
        public BinViewModel viewModel { get; set; }
        private static ArchivePage parentArchivePage { get; set; }
        public AddNewBinWindow(ArchivePage currentPage)
        {
            this.InitializeComponent();
            SetWindowSizeAndPosition(400, 200);
            viewModel = new BinViewModel();
            parentArchivePage = currentPage;

        }
        public static void ShowWindow(ArchivePage currentPage)
        {
            if (_instance == null)
            {
                _instance = new AddNewBinWindow(currentPage);
                _instance.Activate(); // Show the window
            }
            else
            {
                _instance.Activate(); // Bring the existing window to the front
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
            _instance = null;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string parentFolder, subFolder;
            if (!string.Equals(parentArchivePage.archive.BinName, "Archive"))
            {
                parentFolder = Path.Combine(GlobalClass.Instance.ArchivePath, parentArchivePage.archive.BinName);
                subFolder = viewModel.Bin.BinName;
                CreateSubfolder(parentFolder, subFolder);
                this.Close();
                // Find the parent FileSystemItem in the tree
                FileSystemItem parentItem = FindFileSystemItemByPath(parentArchivePage.FileSystemItems, parentFolder);
                if (parentItem != null)
                {
                    // Add the new folder as a child
                    var newFolder = new FileSystemItem
                    {
                        Name = subFolder,
                        Path = parentFolder,
                        IsDirectory = true
                    };
                    parentItem.Children.Add(newFolder);

                    // Keep the parent expanded
                    parentItem.IsExpanded = true;
                }
                //parentArchivePage.CreatedNewFolder = true;
            }
        }
        FileSystemItem FindFileSystemItemByPath(ObservableCollection<FileSystemItem> items, string path)
        {
            foreach (var item in items)
            {
                if (item.Path == path)
                    return item;

                // Recursively search in children
                var found = FindFileSystemItemByPath(item.Children, path);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        public void CreateSubfolder(string parentDirectory, string subfolderName)
        {
            // Combine the parent directory path and the subfolder name
            string subfolderPath = Path.Combine(parentDirectory, subfolderName);

            // Check if the directory already exists
            if (!Directory.Exists(subfolderPath))
            {
                // Create the subfolder
                Directory.CreateDirectory(subfolderPath);
            }
            else
            {
                // Optional: Handle the case where the subfolder already exists
                Console.WriteLine("Subfolder already exists.");
            }
        }

    }
    public class Bin : ObservableObject
    {
        private string binName = string.Empty;
        public string BinName
        {
            get => binName;
            set => SetProperty(ref binName, value);
        }
    }
    public class BinViewModel : ObservableObject
    {
        private Bin bin;

        public BinViewModel()
        {
            Bin = new Bin();
        }
        public Bin Bin { get => bin; set => SetProperty(ref bin, value); }
    }
}
