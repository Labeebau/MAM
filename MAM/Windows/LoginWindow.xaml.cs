using MAM.Data;
using MAM.ViewModels;
using MAM.Views.SystemAdminPanelViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginWindow : Window
    {
        private LoginViewModel viewModel;
        // Static instance to track the window
        private static LoginWindow _instance;
        // Constants for window styles
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x00010000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public LoginWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;

            // Customize the title bar
            var titleBar = AppWindow.TitleBar;
            // Set the background colors for active and inactive states
            titleBar.BackgroundColor = Colors.Black;
            titleBar.InactiveBackgroundColor = Colors.DarkGray;
            // Set the foreground colors (text/icons) for active and inactive states
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveForegroundColor = Colors.Gray;

            // Get the window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            // Remove the maximize button
            DisableMaximizeButton(hwnd);
            SetWindowSizeAndPosition(500, 270);
            viewModel = new LoginViewModel();
            UserPanel.DataContext = viewModel;
        }

        private void DisableMaximizeButton(IntPtr hwnd)
        {
            // Get the current window style
            int style = GetWindowLong(hwnd, GWL_STYLE);

            // Modify the style to disable the maximize button
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_MAXIMIZEBOX);
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
        public static void ShowWindow()
        {
            //if (_instance == null)
            //{
            _instance = new LoginWindow();
            _instance.Activate(); // Show the window
                                  //}
                                  //else
                                  //{
            _instance.Activate(); // Bring the existing window to the front
            //}
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            bool isLoginSuccessful = ValidateLogin(viewModel.UserName, viewModel.Password);

            if (isLoginSuccessful)
            {

                // Safely access the MainFrame and navigate
                if (App.MainAppWindow != null && App.MainAppWindow.Mainframe != null)
                {
                    this.Close();
                    App.MainAppWindow.ShowSystemAdminPanel = true;
                    App.MainAppWindow.Mainframe.Navigate(typeof(SystemAdminPanel));  // Navigate to HomePage
                    GlobalClass.Instance.CurrentUserName = viewModel.UserName;
                    App.MainAppWindow.CurrentUser = viewModel.UserName;


                }
                else
                {
                    // Handle the case where MainAppWindow or MainFrame is null
                    ShowNavigationError();
                }
            }
            else
            {
                ShowLoginError();
            }
        }

        private bool ValidateLogin(string userName, string password)
        {
            string query = "select count(1) from user where user_name=@userName and password=@password";
            Dictionary<string, object> userCred = new Dictionary<string, object>();
            userCred.Add("username", userName);
            userCred.Add("password", password);

            Data.DataAccess dataAccess = new Data.DataAccess();
            // Implement actual login logic
            return dataAccess.ExecuteScalar(query, userCred); // Simulate successful login
        }

        private void ShowLoginError()
        {
            // Display login error message
            TxtError.Text = "Password does not match";
        }

        private void ShowNavigationError()
        {
            // Display navigation error message (optional)
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            TxtUserName.Focus(FocusState.Programmatic);
        }
        bool isUSerNameValid = false;
        bool isPasswordValid = false;
        private void TxtUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            isUSerNameValid = !string.IsNullOrWhiteSpace(TxtUserName.Text);

            BtnLogin.IsEnabled = isUSerNameValid && isPasswordValid;
        }

        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            isPasswordValid = !string.IsNullOrWhiteSpace(PwdBox.Password);
            BtnLogin.IsEnabled = isUSerNameValid && isPasswordValid;
        }

        private void PwdBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                BtnLogin.Focus(FocusState.Programmatic);
                // BtnLogin.BorderBrush=new SolidColorBrush(Colors.White);
                LoginButton_Click(BtnLogin, null);
            }
        }

        private void BtnLogin_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (BtnLogin.IsEnabled)
            {
                BtnLogin.Focus(FocusState.Programmatic);
            }
        }
    }
    public class User
    {
        public string UserName { get; set; }
        public string Password { get; set; }

    }
}
