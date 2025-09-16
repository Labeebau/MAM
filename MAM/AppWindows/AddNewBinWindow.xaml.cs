using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PdfSharp.Snippets.Drawing;
using System.Collections.ObjectModel;
using Windows.Graphics;
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
        private static MediaLibraryViewModel parentMediaLibrary { get; set; }
        public AddNewBinWindow(MediaLibraryViewModel mediaLibrary)
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
            GlobalClass.Instance.SetWindowSizeAndPosition(430,230, this);
            viewModel = new BinViewModel();
            parentMediaLibrary = mediaLibrary;
            BtnSave.Focus(FocusState.Programmatic);

        }
        public static void ShowWindow(MediaLibraryViewModel mediaLibrary)
        {
            if (_instance == null)
            {
                _instance = new AddNewBinWindow(mediaLibrary);
                _instance.Activate(); // Show the window
            }
            else
            {
                _instance.Activate(); // Bring the existing window to the front
            }

        }
       
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _instance = null;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string parentFolder, subFolder;
            if (!string.Equals(parentMediaLibrary.MediaLibraryObj.BinName, "Archive"))
            {
                parentFolder = Path.Combine(GlobalClass.Instance.MediaLibraryPath, parentMediaLibrary.MediaLibraryObj.BinName);
                subFolder = viewModel.Bin.BinName;
                CreateSubfolder(parentFolder, subFolder);
                this.Close();
                // Find the parent FileSystemItem in the tree
                FileSystemItem parentItem = FindFileSystemItemByPath(parentMediaLibrary.FileSystemItems, parentFolder);
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
