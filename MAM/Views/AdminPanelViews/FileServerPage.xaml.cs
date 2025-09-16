using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
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
    public sealed partial class FileServerPage : Page
	{
        public DataAccess dataAccess = new();
        public ObservableCollection<FileServer> FileServerList { get; set; } = [];
        public FileServer NewFileServer { get; set; }
        public FileServer EditingFileServer { get; set; }

        public FileServerPage()
        {
            this.InitializeComponent();
            LoadFileServers();
            DataContext = this;

        }

       
        private async void LoadFileServers()
        { 
            var servers = await GetFileServers();
            FileServerList.Clear();
            foreach (var server in servers)
            {
                FileServerList.Add(server);
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NewFileServer = new FileServer
            {
                ServerId = -1,
                ServerName = string.Empty,
                Domain = string.Empty,
                UserName = string.Empty,
                Password = string.Empty,
                FileFolder = string.Empty,
                ProxyFolder = string.Empty,
                ThumbnailFolder = string.Empty,
                RecycleFolder=string.Empty,
                Active = false,
                IsReadOnly = false,
            };
            FileServerList.Add(NewFileServer);
            DgvFileServer.SelectedIndex = FileServerList.Count - 1;
            DgvFileServer.BeginEdit();
            if (DgvFileServer.ItemsSource is IList<FileServer> items)
            {
                DgvFileServer.SelectedIndex = items.Count - 1;
                DgvFileServer.CurrentColumn = DgvFileServer.Columns[0];

                DgvFileServer.ScrollIntoView(items[DgvFileServer.SelectedIndex], DgvFileServer.Columns[0]);
            }
        }
        private async Task<ObservableCollection<FileServer>> GetFileServers()
        {
            ObservableCollection<FileServer> fileServerList = new();
            DataTable dt = new DataTable();
            dt =await dataAccess.GetDataAsync("select * from file_server");
            foreach (DataRow row in dt.Rows)
            {
                NewFileServer = new FileServer();
                NewFileServer.ServerId =Convert.ToInt32(row["server_Id"]);
                NewFileServer.ServerName = row["server_name"].ToString();
                NewFileServer.Domain = row["domain"].ToString();
                NewFileServer.UserName = row["user_name"].ToString();
                NewFileServer.Password = row["password"].ToString();
                NewFileServer.FileFolder = row["file_folder"].ToString();
                NewFileServer.ProxyFolder = row["proxy_folder"].ToString();
                NewFileServer.ThumbnailFolder = row["thumbnail_folder"].ToString();
                NewFileServer.RecycleFolder = row["recycle_folder"].ToString();
                NewFileServer.Active =Convert.ToBoolean(row["active"].ToString());
                fileServerList.Add(NewFileServer);
            }
            return fileServerList;
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ChbACtive_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void BtnProxyServer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnThumbnailServer_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnQCServer_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnFileServer_Click(object sender, RoutedEventArgs e)
        {

        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private async Task<int> InsertFileServer(FileServer fileServer)
        {
            // Hash the password
            // Prepare parameters for INSERT
            List<MySqlParameter> parameters = new()
            {
                new MySqlParameter("@ServerName", fileServer.ServerName),
                new MySqlParameter("@Domain", fileServer.Domain),
                new MySqlParameter("@UserName", fileServer.UserName),
                new MySqlParameter("@Password", fileServer.Password),
                new MySqlParameter("@FileFolder", fileServer.FileFolder),
                new MySqlParameter("@ProxyFolder", fileServer.ProxyFolder),
                new MySqlParameter("@ThumbnailFolder", fileServer.ThumbnailFolder),
                new MySqlParameter("@RecycleFolder", fileServer.RecycleFolder),
                new MySqlParameter("@Active", fileServer.Active)
            };


            string query = "INSERT INTO file_server (server_name, domain,user_name, password,file_folder,proxy_folder,thumbnail_folder,recycle_folder,active) " +
                           "VALUES (@ServerName, @Domain, @UserName, @Password, @FileFolder, @ProxyFolder,@ThumbnailFolder,@RecycleFolder, @Active)";

            // Execute insert
            var (affectedRows, newFileServerId,errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);

            if (affectedRows > 0)
            {
                // Update user object with new ID
                fileServer.ServerId = newFileServerId;
                // GlobalClass.Instance.AddtoHistoryAsync("Create User", $"New user '{user.UserName}' created.");
                return newFileServerId;
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync($"Unable to add file server {fileServer.ServerName} \n {errorMessage}", XamlRoot);
                return -1;
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<FileServer> dbFileServerList = await GetFileServers();
                Dictionary<PropertyInfo, string> DbFields = new Dictionary<PropertyInfo, string>();
                foreach (FileServer server in FileServerList)
                {
                    if (server.ServerId == -1)
                    {
                        server.ServerId = await InsertFileServer(server);
                        server.IsReadOnly = true;
                    }
                    else
                    {
                        foreach (FileServer dbFileServer in dbFileServerList)
                        {
                            if (server.ServerId == dbFileServer.ServerId)
                            {
                                Dictionary<string, object> propsList = new Dictionary<string, object>();
                                GlobalClass.Instance.CompareProperties(server, dbFileServer, out propsList);
                                propsList = UpdateFieldNames(propsList);
                                if (propsList.Count > 0)
                                    await dataAccess.UpdateRecord("file_server", "server_id", server.ServerId, propsList);
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
                if (key == "ServerName")
                    editedProps.Add("server_name", props[key]);
                else if (key == "ComputerName")
                    editedProps.Add("computer_name", props[key]);
                else if (key == "Domain")
                    editedProps.Add("domain", props[key]);
                else if (key == "UserName")
                    editedProps.Add("user_name", props[key]);
                else if (key == "Password")
                    editedProps.Add("password", props[key]);
                else if (key == "FileFolder")
                    editedProps.Add("file_folder", props[key]);
                else if (key == "ProxyFolder")
                    editedProps.Add("proxy_folder", props[key]);
                else if (key == "ThumbnailFolder")
                    editedProps.Add("thumbnail_folder", props[key]);
                else if (key == "Active")
                    editedProps.Add("active", props[key]);
            }
            return editedProps;
        }
        private  void CancelButton_Click(object sender, RoutedEventArgs e)
        {
           LoadFileServers();
        }

        private void DgvFileServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditingFileServer != null && e.RemovedItems.Count > 0 && EditingFileServer.ServerId == ((FileServer)e.RemovedItems[0]).ServerId)
                EditingFileServer.IsReadOnly = true;
        }

        private void DgvFileServer_CurrentCellChanged(object sender, EventArgs e)
        {
            DgvFileServer.CommitEdit();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var fileServer = button?.Tag as FileServer;
            if (fileServer != null)
            {
                // Make sure all rows are not in edit mode
                foreach (var u in FileServerList)
                {
                    u.IsReadOnly = true;
                }
                // Set the current user row to edit mode
                fileServer.IsReadOnly = false;
            }
            EditingFileServer = fileServer;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ActiveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FileServer selected)
            {
                foreach (var server in FileServerList)
                {
                    if (server != selected)
                        server.Active = false;
                }
            }
        }
    }
    public class FileServer:ObservableObject
    {
        private int serverId;
        private string name;
        private string domain;
        private string userName;
        private string password;
        private string fileFolder;
        private string proxyFolder;
        private string thumbnailFolder;
        private bool active;
        private bool isReadOnly=true;
        private string recycleFolder;

        public int ServerId
        {
            get => serverId;
            set => SetProperty(ref serverId, value);
        }
        public string ServerName
        {
            get => name;
            set => SetProperty(ref name, value);
        }
        
        public string Domain
        {
            get => domain;
            set => SetProperty(ref domain, value);
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
        public string FileFolder
        {
            get => fileFolder;
            set => SetProperty(ref fileFolder, value);
        }
        public string ProxyFolder
        {
            get => proxyFolder;
            set => SetProperty(ref proxyFolder, value);
        }
        public string ThumbnailFolder
        {
            get => thumbnailFolder;
            set => SetProperty(ref thumbnailFolder, value);
        }
        public string RecycleFolder
        {
            get =>recycleFolder;
            set => SetProperty(ref recycleFolder, value);
        }
        public bool Active
        {
            get => active;
            set => SetProperty(ref active, value);
        }
        public bool IsReadOnly
        {
            get => isReadOnly;
            set=> SetProperty(ref isReadOnly, value);
        }
    }
}
