using MAM.Data;
using MAM.Utilities;
using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Tls;
using System.Xml.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeneralPage : Page
    {
        private DataAccess dataAccess = new DataAccess();
        private DB viewModel;
        public GeneralPage()
        {
            this.InitializeComponent();
            viewModel = new DB();
            GlobalClass.Instance.IsFirstLaunch = SettingsService.Get<bool>(SettingKeys.IsFirstLaunch, defaultValue: true);

            if (!GlobalClass.Instance.IsFirstLaunch)
                _ = LoadDbPropertiesAsync();
            else
                SaveButton.Content = "Save";
            DataContext = viewModel;

        }
        private void RevealModeCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (revealModeCheckBox.IsChecked == true)
            {
                passworBoxWithRevealmode.PasswordRevealMode = PasswordRevealMode.Visible;
            }
            else
            {
                passworBoxWithRevealmode.PasswordRevealMode = PasswordRevealMode.Hidden;
            }
        }
        private async Task LoadDbPropertiesAsync()
        {
            MySqlDataReader reader = await dataAccess.ExecuteReaderStoredProcedure("GetDbProperties");
            if (reader != null && await reader.ReadAsync())
            {
                viewModel.ServerIP = reader["server_IP"].ToString();
                viewModel.ServerName = reader["server_name"].ToString();
                viewModel.DBName = reader["db_name"].ToString();
                viewModel.DBUserName = reader["db_user_name"].ToString();
                viewModel.DBPassword = reader["db_password"].ToString();

                // Use the values as needed

                reader.Close(); // Always close the reader after reading
                SaveButton.Content = "Update";
            }
        }

        private async void  SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if(GlobalClass.Instance.IsFirstLaunch)
            {
                string connectionString = $"server={viewModel.ServerName};uid={viewModel.DBUserName};pwd={viewModel.DBPassword};database={viewModel.DBName};Connection Timeout=30;";

                DataAccess.ConnectionString = connectionString;
            }

            if (await SaveDbProperties() > 0)
            {
                App.MainAppWindow.StatusBar.ShowStatus($"DB properties saved successfully.");
                //Store ConnectionString as Json
                string connectionString = $"server={viewModel.ServerName};uid={viewModel.DBUserName};pwd={viewModel.DBPassword};database={viewModel.DBName};Connection Timeout=30;";
                SettingsService.Set(SettingKeys.ConnectionString, EncryptionHelper.Encrypt(connectionString));
                SaveButton.Content = "Update";
                DataAccess.ConnectionString = $"server={viewModel.ServerName};uid={viewModel.DBUserName};pwd={viewModel.DBPassword};database={viewModel.DBName};Connection Timeout=30;";

            }
            else
            {
                DataAccess.ConnectionString = $"server={viewModel.ServerName};uid={viewModel.DBUserName};pwd={viewModel.DBPassword};database={viewModel.DBName};Connection Timeout=30;";
                //LoginWindow loginWindow = new LoginWindow();
                //loginWindow.Activate();
            }
            if (GlobalClass.Instance.IsFirstLaunch)
            {

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Activate();
                SettingsService.Set(SettingKeys.IsFirstLaunch, false);
                App.MainAppWindow.IsLoggingOut = true;
                App.MainAppWindow.Close();

            }

        }
        private async Task<int> SaveDbProperties()
        {
            var parameters = new List<MySqlParameter>
            {
                        { new MySqlParameter("server_IP", viewModel.ServerIP) },
                        { new MySqlParameter("server_name", viewModel.ServerName) },
                        { new MySqlParameter("db_name", viewModel.DBName) },
                        { new MySqlParameter("db_user_name", viewModel.DBUserName) },
                        { new MySqlParameter("db_password", viewModel.DBPassword) },
            };

            return await dataAccess.ExecuteNonQueryStoredProcedure("InsertDbProperty", parameters);

               //await GlobalClass.Instance.ShowErrorDialogAsync("Unable to connect to the database. Please check your connection settings and try again.", this.XamlRoot);

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel = new DB { ServerIP = "", ServerName = "", DBName = "", DBUserName = "", DBPassword = "" };
        }
    }
    public class DB : ObservableObject
    {
        private string serverIP;
        private string serverName;
        private string dbName;
        private string dbUserName;
        private string dbPassword;
        public string ServerIP
        {
            get => serverIP;
            set => SetProperty(ref serverIP, value);
        }
        public string ServerName
        {
            get => serverName;
            set => SetProperty(ref serverName, value);
        }
        public string DBName
        {
            get => dbName;
            set => SetProperty(ref dbName, value);
        }
        public string DBUserName
        {
            get => dbUserName;
            set => SetProperty(ref dbUserName, value);
        }
        public string DBPassword
        {
            get => dbPassword;
            set => SetProperty(ref dbPassword, value);
        }
      
    }
}
