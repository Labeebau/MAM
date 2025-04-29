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
            DataContext = viewModel;
            _ = LoadDbPropertiesAsync();
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
            if (await reader.ReadAsync())
            {
                viewModel.ServerIP = reader["server_IP"].ToString();
                viewModel.ServerName = reader["server_name"].ToString();
                viewModel.DBName = reader["db_name"].ToString();
                viewModel.DBUserName = reader["db_user_name"].ToString();
                viewModel.DBPassword = reader["db_password"].ToString();

                // Use the values as needed
            }
            reader.Close(); // Always close the reader after reading
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDbProperties();
            //Store ConnectionString as Json
            string connectionString = $"server={viewModel.ServerName};uid={viewModel.DBUserName};pwd={viewModel.DBPassword};database={viewModel.DBName};Connection Timeout=30;";
            SettingsService.Set("ConnectionString", EncryptionHelper.Encrypt(connectionString));
            bool isFirstLaunch = SettingsService.Get<bool>("IsFirstLaunch", defaultValue: true);
            if(isFirstLaunch)
            {
                SettingsService.Set("IsFirstLaunch", false);
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Activate();
            }
        }
        private async void SaveDbProperties()
        {
            var parameters = new Dictionary<string, object>
            {
                        { "server_IP", viewModel.ServerIP },
                        { "server_name", viewModel.ServerName },
                        { "db_name", viewModel.DBName },
                        { "db_user_name", viewModel.DBUserName },
                        { "db_password", viewModel.DBPassword },
            };

            await dataAccess.ExecuteNonQueryStoredProcedure("InsertDbProperty", parameters);

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

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
