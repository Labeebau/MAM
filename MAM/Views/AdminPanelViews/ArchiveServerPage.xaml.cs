using K4os.Compression.LZ4.Internal;
using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArchiveServerPage : Page
	{
        public ObservableCollection<ArchiveServer> ArchiveServerList { get; set; }
        public DataAccess dataAccess = new();
        public ArchiveServer NewArchiveServer { get; set; }
        public ArchiveServer EditingArchiveServer { get; set; }

        public ArchiveServerPage()
        {
            this.InitializeComponent();
            ArchiveServerList = new ObservableCollection<ArchiveServer>();
            DataContext = this;
            LoadArchiveServers();
        }

        private async void LoadArchiveServers()
        {
            var servers = await GetArchiveServers();
            ArchiveServerList.Clear();
            foreach (var server in servers)
            {
                ArchiveServerList.Add(server);
            }
        }

        private async Task<ObservableCollection<ArchiveServer>> GetArchiveServers()
        {
            ObservableCollection<ArchiveServer> archiveServerList = new();
            DataTable dt = new DataTable();
            dt =await dataAccess.GetDataAsync("select * from archive_server");
            foreach (DataRow row in dt.Rows)
            {
                NewArchiveServer = new ArchiveServer();
                NewArchiveServer.ServerId = Convert.ToInt32(row["server_Id"]);
                NewArchiveServer.ServerName = row["server_name"].ToString();
                NewArchiveServer.UserName = row["user_name"].ToString();
                NewArchiveServer.Password = row["password"].ToString();
                NewArchiveServer.ArchivePath = row["archive_path"].ToString();
                NewArchiveServer.LTOServerId =Convert.ToInt32(row["lto_server_id"].ToString());
                NewArchiveServer.LTOServer = row["lto_server"].ToString();
                NewArchiveServer.Active = Convert.ToBoolean(row["active"].ToString());
                archiveServerList.Add(NewArchiveServer);
            }
            return archiveServerList;
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ChbACtive_Checked(object sender, RoutedEventArgs e)
        {

        }
       
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NewArchiveServer = new ArchiveServer
            {
                ServerId = -1,
                ServerName = string.Empty,
                ComputerName = string.Empty,
                UserName = string.Empty,
                Password = string.Empty,
                ArchivePath = string.Empty,
                LTOServer = string.Empty,
                Active = false,
                IsReadOnly = false,

            };
            ArchiveServerList.Add(NewArchiveServer);
            DgvArchiveServer.SelectedIndex = ArchiveServerList.Count - 1;
            DgvArchiveServer.BeginEdit();
            if (DgvArchiveServer.ItemsSource is IList<ArchiveServer> items)
            {
                DgvArchiveServer.SelectedIndex = items.Count - 1;
                DgvArchiveServer.CurrentColumn = DgvArchiveServer.Columns[0];

                DgvArchiveServer.ScrollIntoView(items[DgvArchiveServer.SelectedIndex], DgvArchiveServer.Columns[0]);
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private async Task<int> InsertArchiveServer(ArchiveServer archiveServer)
        {
            // Hash the password
            // Prepare parameters for INSERT
            List<MySqlParameter> parameters = new()
            {
                new MySqlParameter("@ServerType", archiveServer.ServerType),
                new MySqlParameter("@ServerName", archiveServer.ServerName),
                new MySqlParameter("@ComputerName", archiveServer.ComputerName),
                new MySqlParameter("@UserName", archiveServer.UserName),
                new MySqlParameter("@Password", archiveServer.Password),
                new MySqlParameter("@ArchivePath", archiveServer.ArchivePath),
                new MySqlParameter("@LTOServerId", archiveServer.LTOServerId),
                new MySqlParameter("@LTOServer", archiveServer.LTOServer),
                new MySqlParameter("@Active", archiveServer.Active)
            };


            string query = "INSERT INTO archive_server(server_type,server_name,computer_name,user_name,password,archive_path,lto_server_id,lto_server,active) " +
                           "values(@ServerType,@ServerName,@ComputerName,@UserName,@Password,@ArchivePath,@LTOServerId,@LTOServer,@Active)";

            // Execute insert
            var (affectedRows, NewArchiveServerId,errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);

            if (affectedRows > 0)
            {
                // Update user object with new ID
                archiveServer.ServerId = NewArchiveServerId;
                //await GlobalClass.Instance.AddtoHistoryAsync("Create User", $"New user '{user.UserName}' created.");
                return NewArchiveServerId;
            }
            else 
            {
                await GlobalClass.Instance.ShowDialogAsync($"Unable to insert archive server {archiveServer.ServerName}\n{errorMessage} ", this.XamlRoot);
                return -1;
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ArchiveServer> dbArchiveServerList =await GetArchiveServers();
            Dictionary<PropertyInfo, string> DbFields = new Dictionary<PropertyInfo, string>();
            foreach (ArchiveServer server in ArchiveServerList)
            {
                if (server.ServerId == -1)
                {
                    server.ServerId = await InsertArchiveServer(server);
                    server.IsReadOnly = true;
                }
                else
                {
                    foreach (ArchiveServer dbArchiveServer in dbArchiveServerList)
                    {
                        if (server.ServerId == dbArchiveServer.ServerId)
                        {
                            Dictionary<string, object> propsList = new Dictionary<string, object>();
                            GlobalClass.Instance.CompareProperties(server, dbArchiveServer, out propsList);
                            propsList = UpdateFieldNames(propsList);
                            if (propsList.Count > 0)
                                await dataAccess.UpdateRecord("Archive_server", "server_id", server.ServerId, propsList);
                        }
                    }
                }
            }
        }
        private Dictionary<string, object> UpdateFieldNames(Dictionary<string, object> props)
        {
            Dictionary<string, object> editedProps = new Dictionary<string, object>();
            foreach (string key in props.Keys)
            {
                if (key == "Type")
                    editedProps.Add("server_type", props[key]);
                if (key == "ServerName")
                    editedProps.Add("server_name", props[key]);
                else if (key == "ComputerName")
                    editedProps.Add("computer_name", props[key]);
                else if (key == "UserName")
                    editedProps.Add("user_name", props[key]);
                else if (key == "Password")
                    editedProps.Add("password", props[key]);
                else if (key == "ArchivePath")
                    editedProps.Add("archive_path", props[key]);
                else if (key == "LTOServerId")
                    editedProps.Add("lto_server_id", props[key]);
                else if (key == "LTOServer")
                    editedProps.Add("lto_server", props[key]);
                else if (key == "Active")
                    editedProps.Add("active", props[key]);
            }
            return editedProps;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadArchiveServers();
        }

        private void DgvArchiveServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditingArchiveServer != null && e.RemovedItems.Count > 0 && EditingArchiveServer.ServerId == ((ArchiveServer)e.RemovedItems[0]).ServerId)
                EditingArchiveServer.IsReadOnly = true;
        }

        private void DgvArchiveServer_CurrentCellChanged(object sender, EventArgs e)
        {
            DgvArchiveServer.CommitEdit();

        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var archiveServer = button?.Tag as ArchiveServer;
            if (archiveServer != null)
            {
                // Make sure all rows are not in edit mode
                foreach (var u in ArchiveServerList)
                {
                    u.IsReadOnly = true;
                }
                // Set the current user row to edit mode
                archiveServer.IsReadOnly = false;
            }
            EditingArchiveServer = archiveServer;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ActiveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is ArchiveServer selected)
            {
                foreach (var server in ArchiveServerList)
                {
                    if (server != selected)
                        server.Active = false;
                }
            }
        }
    }
    public class ArchiveServer:ObservableObject
    {
        private int serverId;
        private string type="Network Storage";
        private string serverName;
        private string computerName;
        private string domain;
        private string userName;
        private string password;
        private string archivePath;
        private int ltoServerId;
        private string ltoServer;
        private bool active;
        private bool isReadOnly = true;
        public int ServerId
        {
            get => serverId;
            set => SetProperty(ref serverId, value);
        }
        public string ServerType
        {
            get => type;
            set => SetProperty(ref type, value);
        }
        public string ServerName
        {
            get => serverName;
            set => SetProperty(ref serverName, value);
        }
        public string ComputerName
        {
            get => computerName;
            set => SetProperty(ref computerName, value);
        }
        
        public string UserName
        {
            get => userName;
            set => SetProperty(ref userName, value);
        }
        public string Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }
        public string ArchivePath
        {
            get => archivePath;
            set => SetProperty(ref archivePath, value);
        }
        public int LTOServerId
        {
            get => ltoServerId;
            set => SetProperty(ref ltoServerId, value);
        }
        public string LTOServer
        {
            get => ltoServer;
            set => SetProperty(ref ltoServer, value);
        }
        public bool Active
        {
            get => active;
            set => SetProperty(ref active, value);
        }
        public bool IsReadOnly
        {
            get => isReadOnly;
            set => SetProperty(ref isReadOnly, value);
        }


    }
}
