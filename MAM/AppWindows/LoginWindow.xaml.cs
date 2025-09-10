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
using CommunityToolkit.WinUI.Helpers;
using Windows.Storage;
using User = MAM.Views.AdminPanelViews.User;
using System.Security.Cryptography;
using MAM.Views;
using MySql.Data.MySqlClient;
using Windows.UI;

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
            GlobalClass.Instance.SetWindowSizeAndPosition(500, 320, this);
            viewModel = new Login();
            UserPanel.DataContext = viewModel;
            LoadSavedLogin();
            // _ = LoadDbPropertiesAsync();
            Closed += LoginWindow_Closed;
          
        }

        private void LoginWindow_Closed(object sender, WindowEventArgs args)
        {
            if(!isLoggedIn)
                Application.Current.Exit();
        }

        private void LoadSavedLogin()
        {
            var savedUserName = SettingsService.Get<string>(SettingKeys.SavedUserName);
            var savedPassword = SettingsService.Get<string>(SettingKeys.SavedPassword);
            var keepSignedIn = SettingsService.Get<bool>(SettingKeys.KeepSignedIn);
            viewModel.UserName = savedUserName;

            if (!string.IsNullOrEmpty(savedPassword))
            {
                try
                {
                    // Try to decrypt
                    viewModel.Password = EncryptionHelper.Decrypt(savedPassword);
                }
                catch (CryptographicException)
                {
                    // If decryption fails, treat it as plain text
                    viewModel.Password = savedPassword;
                }
            }
            viewModel.KeepSignedIn = keepSignedIn;
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
            //TxtError.Visibility = Visibility.Collapsed;
            //TxtError.Text = string.Empty;
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
                //TxtError.Text = "Please fill in both username and password.";
                //TxtError.Visibility = Visibility.Visible;
                ShowStatusMessage("", "Please fill in both username and password.", InfoBarSeverity.Warning);
            }

            return isValid;
        }
        private void ShowStatusMessage(string title, string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Title = title;
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.Visibility = Visibility.Visible;
            StatusInfoBar.IsOpen = true;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoggingIn) return; // Ignore if already logging in
            _isLoggingIn = true;
            BtnLogin.IsEnabled = false;

            try
            {
                App.MainAppWindow = new MainWindow();
                UIThreadHelper.RunOnUIThread(() =>
                {
                    ShowStatusMessage("Connecting", "Attempting to connect to database...", InfoBarSeverity.Informational);
                });

                await Task.Yield(); // Let UI render

                if (await LoadDbPropertiesAsync())
                {
                    ShowStatusMessage("Success", "Connection succeeded.", InfoBarSeverity.Success);

                    if (!ValidateInput())
                        return;

                    bool isLoginSuccessful = await ValidateLogin();
                    if (isLoginSuccessful)
                    {
                        isLoggedIn = true;

                        if (viewModel.KeepSignedIn)
                        {
                            SettingsService.Set(SettingKeys.SavedUserName, viewModel.UserName);
                            SettingsService.Set(SettingKeys.SavedPassword, EncryptionHelper.Encrypt(viewModel.Password));
                            SettingsService.Set(SettingKeys.KeepSignedIn, viewModel.KeepSignedIn);
                        }
                        else
                        {
                            SettingsService.Remove(SettingKeys.SavedUserName);
                            SettingsService.Remove(SettingKeys.SavedPassword);
                            SettingsService.Remove(SettingKeys.KeepSignedIn);
                        }

                        App.MainAppWindow.CurrentUser = viewModel.UserName;
                        GlobalClass.Instance.CurrentUser =await GlobalClass.Instance.GetUserRightsAsync(viewModel.UserId);
                        if (GlobalClass.Instance.CurrentUser != null)
                        {
                            GlobalClass.Instance.CurrentUser.UserName = viewModel.UserName;
                            GlobalClass.Instance.CurrentUserGroup = GlobalClass.Instance.GetUserGroupWithRights(viewModel.GroupId);
                            GlobalClass.Instance.IsAdmin = viewModel.GroupName == "Admin";
                            await GlobalClass.Instance.AddtoHistoryAsync("Login", "User logged in", viewModel.UserId);

                            this.Close();
                            App.MainAppWindow = new MainWindow();
                            App.MainAppWindow.Activate();
                        }
                        else
                        {
                            ShowStatusMessage("Connection Failed", "Unable to connect to the database.", InfoBarSeverity.Error);
                        }
                    }
                    else
                    {
                        ShowStatusMessage("Login Failed", "Invalid username or password.", InfoBarSeverity.Error);
                    }
                }
                else
                {
                    ShowStatusMessage("Connection Failed", "Unable to connect to the database.", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", "Unexpected error: " + ex.ToString());
                ShowStatusMessage("Error", "An unexpected error occurred.", InfoBarSeverity.Error);
            }
            finally
            {
                _isLoggingIn = false;
                BtnLogin.IsEnabled = true;
            }
        }


        private async Task<bool> ValidateLogin()
        {
            int userId, groupId;
            string passwordHash, passwordSalt,groupName;
            (userId, passwordHash, passwordSalt, groupId, groupName) =await dataAccess.GetUserCredentials(viewModel.UserName);// ?? (null, null, null,null,null);

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
                    ShowStatusMessage("", "Invalid Password!!!", InfoBarSeverity.Error);

                   // TxtError.Text = "Invalid Password!!!";
                    return false;
                }
            }
            else if (userId<=0)
            {
                //TxtError.Text = "Invalid User Name!!!";
                ShowStatusMessage("", "Invalid User Name!!!", InfoBarSeverity.Error);

                return false;
            }
            else
                return false;
        }
        private async Task<bool> LoadDbPropertiesAsync()
        {
            ShowStatusMessage("Connecting", "Attempting to connect to database...", InfoBarSeverity.Informational);

            await Task.Delay(100); // ✅ Give UI time to show message

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Timeout after 5 seconds

                Task<MySqlDataReader> dbTask = dataAccess.ExecuteReaderStoredProcedure("GetDbProperties");
                var completedTask = await Task.WhenAny(dbTask, Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask == dbTask)
                {
                    var reader = await dbTask;

                    if (reader != null && await reader.ReadAsync())
                    {
                        string serverIP = reader["server_IP"].ToString();
                        string serverName = reader["server_name"].ToString();
                        string dBName = reader["db_name"].ToString();
                        string dBUserName = reader["db_user_name"].ToString();
                        string dBPassword = reader["db_password"].ToString();

                        DataAccess.ConnectionString =
                            $"server={serverName};uid={dBUserName};pwd={dBPassword};database={dBName};Connection Timeout=30;";

                        reader.Close();
                        return true;
                    }
                }
                else
                {
                    ShowStatusMessage("Connection Timeout", "The connection attempt timed out. Please try again.", InfoBarSeverity.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Connection Failed", "Unable to connect to the database.", InfoBarSeverity.Error);
                File.AppendAllText("log.txt", "DB connection failed: " + ex.Message + "\n");
            }

            return false;
        }


        private void ShowNavigationError()
        {
            // Display navigation error message (optional)
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel = new Login() { UserName = string.Empty, Password = string.Empty };
            UserPanel.DataContext = viewModel;
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if(viewModel.KeepSignedIn)
                BtnLogin.Focus(FocusState.Programmatic);    
            else
                TxtUserName.Focus(FocusState.Programmatic);

        }
        bool isUSerNameValid = false;
        bool isPasswordValid = false;
        private bool _isLoggingIn=false;

        public bool isLoggedIn { get; private set; } = false;
        
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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void KeepSignedInCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
    public class Login : ObservableObject
    {
        private int userId;
        private string userName;
        private string password;
        private int groupId;
        private string groupName;
        private bool keepSignedIn;

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
        public bool KeepSignedIn
        {
            get => keepSignedIn;
            set => SetProperty(ref keepSignedIn, value); 

        }

    }
}
