using MAM.Data;
using MAM.Utilities;
using MAM.Views.AdminPanelViews;
using MAM.Views.SystemAdminPanelViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.System;
using User = MAM.Views.AdminPanelViews.User;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginWindow : Window
    {
        private DataAccess dataAccess = new DataAccess();
        private Login viewModel;
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
            GlobalClass.Instance.DisableMaximizeButton(this);
            GlobalClass.Instance.SetWindowSizeAndPosition(500, 300, this);
            viewModel = new Login();
            UserPanel.DataContext = viewModel;
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
        private bool ValidateInput()
        {
            bool isValid = true;

            // Reset visuals
            TxtError.Visibility = Visibility.Collapsed;
            TxtError.Text = string.Empty;
            TxtUserName.ClearValue(Control.BorderBrushProperty);
            PwdBox.ClearValue(Control.BorderBrushProperty);

            // Validate username
            if (string.IsNullOrWhiteSpace(TxtUserName.Text))
            {
                TxtUserName.BorderBrush = new SolidColorBrush(Colors.Red);
                isValid = false;
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(PwdBox.Password))
            {
                PwdBox.BorderBrush = new SolidColorBrush(Colors.Red);
                isValid = false;
            }

            // Show error if invalid
            if (!isValid)
            {
                TxtError.Text = "Please fill in both username and password.";
                TxtError.Visibility = Visibility.Visible;
            }

            return isValid;
        }


        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;
            bool isLoginSuccessful = ValidateLogin();
            if (isLoginSuccessful)
            {
                // Safely access the MainFrame and navigate
                if (App.MainAppWindow != null && App.MainAppWindow.Mainframe != null)
                {
                    this.Close();
                    App.MainAppWindow.ShowSystemAdminPanel = true;
                    App.MainAppWindow.Mainframe.Navigate(typeof(SystemAdminPanel));  // Navigate to HomePage
                    //GlobalClass.Instance.CurrentUser=new User() {UserId= viewModel.UserId,UserName= viewModel.UserName,u };
                    //GlobalClass.Instance.CurrentUser.UserName = viewModel.UserName;
                    //GlobalClass.Instance.CurrentUser.UserId = viewModel.UserId;
                    //GlobalClass.Instance.CurrentUserGroup.UserGroupId = viewModel.GroupId;
                    //GlobalClass.Instance.CurrentUserGroup.GroupName = viewModel.GroupName;
                    App.MainAppWindow.CurrentUser = viewModel.UserName;
                    GlobalClass.Instance.CurrentUser = GlobalClass.Instance.GetUserRights(viewModel.UserId);
                    GlobalClass.Instance.CurrentUserGroup=GlobalClass.Instance.GetUserGroupWithRights(viewModel.GroupId);
                    GlobalClass.Instance.IsAdmin = viewModel.GroupName == "Admin" ? true : false;
                    await GlobalClass.Instance.AddtoHistoryAsync("Login", "User logged in", viewModel.UserId);
                }
            }
            else
            {
                if (TxtError.Text == string.Empty)
                {
                    TxtError.Text = "Invalid username or password.";
                }
                    TxtError.Visibility = Visibility.Visible;
            }

        }

        private bool ValidateLogin()
        {
            int userId, groupId;
            string passwordHash, passwordSalt,groupName;
            (userId, passwordHash, passwordSalt, groupId, groupName) = dataAccess.GetUserCredentials(viewModel.UserName);// ?? (null, null, null,null,null);

            if (userId>0 && passwordHash != null && passwordSalt != null)
            {
                if (PasswordHelper.VerifyPassword(viewModel.Password, passwordHash, passwordSalt))
                {
                    viewModel.UserId = userId;
                    viewModel.GroupId = groupId;
                    viewModel.GroupName = groupName;
                    return true;
                }
                else
                {
                    TxtError.Text = "Invalid Password!!!";
                    return false;
                }
            }
            else if (userId<=0)
            {
                TxtError.Text = "Invalid User Name!!!";
                return false;
            }
            else
                return false;
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
            //isUSerNameValid = !string.IsNullOrWhiteSpace(TxtUserName.Text);

            //BtnLogin.IsEnabled = isUSerNameValid && isPasswordValid;
        }

        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            //isPasswordValid = !string.IsNullOrWhiteSpace(PwdBox.Password);
            //BtnLogin.IsEnabled = isUSerNameValid && isPasswordValid;
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
    public class Login : ObservableObject
    {
        private int userId;
        private string userName;
        private string password;
        private int groupId;
        private string groupName;

        public int UserId
        {
            get => userId;
            set => SetProperty(ref userId, value);
        }
        public string UserName
        {
            get => userName;
            set => SetProperty(ref userName, value);
        }
       
        public int GroupId
        {
            get => groupId;
            set => SetProperty(ref groupId, value);
        }
        public string GroupName
        {
            get => groupName;
            set => SetProperty(ref groupName, value);
        }
        public string Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

    }
}
