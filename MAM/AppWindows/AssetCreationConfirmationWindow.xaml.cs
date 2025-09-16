using MAM.AppWindows;
using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AssetCreationConfirmationWindow : Window
    {
        private static AssetCreationConfirmationWindow _instance;
        public AssetCreationConfirmationViewModel viewModel { get;  set; }
        public AssetCreationConfirmationWindow(string fileName, List<string> pathList)
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
            GlobalClass.Instance.SetWindowSizeAndPosition(400, 400, this);
            viewModel = new AssetCreationConfirmationViewModel();
            viewModel.Asset.FileName = fileName;
            viewModel.Asset.PathList = pathList;
        }
        public static void ShowWindow(string fileName,List<string>pathList)
        {
            if (_instance == null)
            {
                _instance = new AssetCreationConfirmationWindow(fileName, pathList);
                // When the window closes, clear the instance so it can be opened again
                _instance.Closed += (s, e) => _instance = null;
            }
            _instance.Activate();
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

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
    public class AssetCreationConfirmation : ObservableObject
    {
        private string fileName = string.Empty;
        private List<string> pathList;// = DateTime.Now.Hour;

        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        public List<string> PathList
        {
            get => pathList;
            set => SetProperty(ref pathList, value);
        }
    }
    public class AssetCreationConfirmationViewModel : ObservableObject
    {
        private AssetCreationConfirmation asset;

        public AssetCreationConfirmationViewModel()
        {
            asset = new AssetCreationConfirmation();
        }
        public AssetCreationConfirmation Asset { get => asset; set => SetProperty(ref asset, value); }
    }
}
