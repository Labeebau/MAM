using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class AuthorizationSetingsPage : Page
    {
        Data.DataAccess dataAccess = new Data.DataAccess();
        public ObservableCollection<UserGroupRight> UserGroupRightsList { get; set; } = new ObservableCollection<UserGroupRight>();
        public ObservableCollection<UserRight> UserRightsList { get; set; } = new ObservableCollection<UserRight>();
        public ObservableCollection<User> UserList { get; set; }
        public ObservableCollection<UserGroup> UserGroupList { get; set; } = new ObservableCollection<UserGroup>();

        public List<string> UsersList = new List<string>();
        //  public List<string> UserGroupList = new List<string>();
        public UserGroup NewUserGroup { get; set; }
        public UserGroupRight NewUserGroupRight { get; set; }

        public AuthorizationSetingsPage()
        {
            this.InitializeComponent();
            //{
            //    new UserGroupRight{GroupName="Admin", Read=true,Write=true,Edit=true,Delete=true,OrgDownload=true,PrxDownload=true,Archive=true,Broadcast=true}
            //};
            //UserRights = new ObservableCollection<UserRight>
            //{
            //    new UserRight{UserName="Shilpa", Read=true,Write=true,Edit=true,Delete=true,OrgDownload=true,PreDownload=true,Archive=true,Broadcast=true}
            //};
            UserList = new ObservableCollection<User>((IEnumerable<User>)dataAccess.GetUsers());
            GetUserGroup();
            GetUserRights();
            DataContext = this;
            //foreach (string user in UsersList)
            //{
            //    this.SelectLanguageMenuLayout.Items.Add(
            //        new MenuFlyoutItem
            //        {
            //            Text = user.ToString(),
            //            //Command = ViewModel.ChangeLanguageCommand,
            //            //CommandParameter = language,
            //        });
            //}
            //public DropDownViewModel ViewModel { get; } = new();


        }
        private UserGroup _selectedUserGroup;
        public UserGroup SelectedUserGroup
        {
            get => _selectedUserGroup;
            set => _selectedUserGroup = value;
        }
        private User _selectedUser;
        public User SelectedUser
        {
            get => _selectedUser;
            set => _selectedUser = value;
        }
        public UserRight NewUserRight { get; private set; }

        private void GetUserGroup()
        {
            UserGroupList.Clear();
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select group_id,group_name,ad_group,active from user_group");
            foreach (DataRow row in dt.Rows)
            {
                NewUserGroup = new UserGroup();
                NewUserGroup.UserGroupId = Convert.ToInt32(row[0]);
                NewUserGroup.GroupName = row[1].ToString();
                NewUserGroup.IsAdGroup = Convert.ToBoolean(row[2].ToString());
                NewUserGroup.IsActive = Convert.ToBoolean(row[3].ToString());
                UserGroupList.Add(NewUserGroup);
            }
        }
        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnAddUserGroup_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SavePermissions();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            GetUserGroupRights(SelectedUserGroup.UserGroupId);
        }
        private void ComboBoxUSer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboBoxUserGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(SelectedUserGroup!=null)
            GetUserGroupRights(SelectedUserGroup.UserGroupId);
        }
        private void GetUserGroupRights(int groupId, ObservableCollection<UserGroupRight> userGroupRightList = null)
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
                                    "FROM user_group ug " +
                                    "LEFT JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "LEFT JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    $"WHERE ug.group_id={groupId}");

            DataRow row = dt.Rows[0];
            if (!string.IsNullOrEmpty(row["group_name"].ToString()))
            {
                NewUserGroupRight = new UserGroupRight();
                NewUserGroupRight.GroupId = Convert.ToInt32(row["group_id"]);
                NewUserGroupRight.GroupName = row["group_name"].ToString();
                if (row["permissions"].ToString().Contains("Read"))
                    NewUserGroupRight.Read = true;
                if (row["permissions"].ToString().Contains("Write"))
                    NewUserGroupRight.Write = true;
                if (row["permissions"].ToString().Contains("Edit"))
                    NewUserGroupRight.Edit = true;
                if (row["permissions"].ToString().Contains("Delete"))
                    NewUserGroupRight.Delete = true;
                if (row["permissions"].ToString().Contains("OrgDownload"))
                    NewUserGroupRight.OrgDownload = true;
                if (row["permissions"].ToString().Contains("PrxDownload"))
                    NewUserGroupRight.PrxDownload = true;
                if (row["permissions"].ToString().Contains("Archive"))
                    NewUserGroupRight.Archive = true;
                if (row["permissions"].ToString().Contains("Broadcast"))
                    NewUserGroupRight.Broadcast = true;
                if (userGroupRightList == null)
                {
                    UserGroupRightsList.Clear();
                    UserGroupRightsList.Add(NewUserGroupRight);
                }
                else
                    userGroupRightList.Add(NewUserGroupRight);

            }
        }
        private void GetUserGroupRights(ObservableCollection<UserGroupRight> userGroupRight)
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
                                    "FROM user_group ug " +
                                    "JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    $"GROUP BY ug.group_id");

            DataRow row = dt.Rows[0];
            if (!string.IsNullOrEmpty(row["group_name"].ToString()))
            {
                NewUserGroupRight = new UserGroupRight();
                NewUserGroupRight.GroupId = Convert.ToInt32(row["group_id"]);
                NewUserGroupRight.GroupName = row["group_name"].ToString();
                if (row["permissions"].ToString().Contains("Read"))
                    NewUserGroupRight.Read = true;
                if (row["permissions"].ToString().Contains("Write"))
                    NewUserGroupRight.Write = true;
                if (row["permissions"].ToString().Contains("Edit"))
                    NewUserGroupRight.Edit = true;
                if (row["permissions"].ToString().Contains("Delete"))
                    NewUserGroupRight.Delete = true;
                if (row["permissions"].ToString().Contains("OrgDownload"))
                    NewUserGroupRight.OrgDownload = true;
                if (row["permissions"].ToString().Contains("PrxDownload"))
                    NewUserGroupRight.PrxDownload = true;
                if (row["permissions"].ToString().Contains("Archive"))
                    NewUserGroupRight.Archive = true;
                if (row["permissions"].ToString().Contains("Broadcast"))
                    NewUserGroupRight.Broadcast = true;
                userGroupRight.Add(NewUserGroupRight);
            }
        }
        private void GetUserRights()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("SELECT u.user_name,ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions FROM user u " +
                                    "join user_roles ur on u.user_id = ur.user_id " +
                                    "join user_group ug on ur.group_id = ug.group_id " +
                                    "left JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "left JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    "group by ug.group_id, u.user_name " +
                                    "order by ug.group_id, u.user_name;");

            UserRightsList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                if (!string.IsNullOrEmpty(row["user_name"].ToString()))
                {
                    NewUserRight = new UserRight();
                    NewUserRight.UserGroup = new UserGroup();
                    NewUserRight.UserGroup.GroupName = row["group_name"].ToString();
                    NewUserRight.UserName = row["user_name"].ToString();
                    if (row["permissions"].ToString().Contains("Read"))
                        NewUserRight.Read = true;
                    if (row["permissions"].ToString().Contains("Write"))
                        NewUserRight.Write = true;
                    if (row["permissions"].ToString().Contains("Edit"))
                        NewUserRight.Edit = true;
                    if (row["permissions"].ToString().Contains("Delete"))
                        NewUserRight.Delete = true;
                    if (row["permissions"].ToString().Contains("OrgDownload"))
                        NewUserRight.OrgDownload = true;
                    if (row["permissions"].ToString().Contains("PrxDownload"))
                        NewUserRight.PrxDownload = true;
                    if (row["permissions"].ToString().Contains("Archive"))
                        NewUserRight.Archive = true;
                    if (row["permissions"].ToString().Contains("Broadcast"))
                        NewUserRight.Broadcast = true;
                    UserRightsList.Add(NewUserRight);
                }
            }
        }
        private void SavePermissions()
        {
            int groupId = UserGroupRightsList[0].GroupId;
            ObservableCollection<UserGroupRight> dbUserGroupRightList = new ObservableCollection<UserGroupRight>();
            GetUserGroupRights(groupId, dbUserGroupRightList);
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            GlobalClass.Instance.Equals(UserGroupRightsList[0], dbUserGroupRightList[0], out propsList);
            UpdatePermission(groupId, propsList);
            GetUserRights();
           
        }
        private Dictionary<string, object> UpdatePermission(int groupId, Dictionary<string, object> props)
        {
            Dictionary<string, object> editedProps = new Dictionary<string, object>();
            int id = -1;
            foreach (KeyValuePair<string,object> permission in props)
            {
                    id = dataAccess.GetId($"select permission_id from permissions where permission_name='{permission.Key}'");
                if (Convert.ToBoolean(permission.Value))
                    InsertPermission(groupId, id);
                else
                    DeletePermission(groupId, id);
            }
            return editedProps;
        }
        private async Task<int> InsertPermission(int groupId, int permissionId)
        {
            List<MySqlParameter> parameters = new ();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@GroupId", groupId));
            parameters.Add(new MySqlParameter("@PermissionId", permissionId));
            query = "INSERT INTO group_permissions (group_id,permission_id) " +
                    $"SELECT {groupId},{permissionId} " +
                    "WHERE NOT EXISTS(" +
                    "SELECT 1 " +
                    "FROM group_permissions " +
                    $"WHERE group_id = {groupId} AND permission_id = {permissionId})";
           // int newUserGroupId = 0,errorCode = 0;
           var (_,lastInsertedId,errorCode)= await dataAccess.ExecuteNonQuery(query, parameters);
            return lastInsertedId;
        }
        private async Task<int> DeletePermission(int groupId, int permissionId)
        {
            List<MySqlParameter> parameters = new ();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@GroupId", groupId));
            parameters.Add(new MySqlParameter("@PermissionId", permissionId));
            query = $"DELETE FROM group_permissions where group_id=@GroupId AND permission_id=@PermissionId ";
            // Execute query and retrieve affected rows
            var (affectedRows, _, errorCode) =await dataAccess.ExecuteNonQuery(query, parameters);

            if (errorCode != 0)
            {
                Console.WriteLine($"Failed to delete permission. Error code: {errorCode}");
            }

            return affectedRows; // Return number of deleted rows
        }
        private void SaveUserGroupRights_Click(object sender, RoutedEventArgs e)
        {
        }
        //private object InsertUserGroupRight(UserGroupRight userGroup)
        //{
        //    throw new NotImplementedException();
        //}
    }
    public class UserGroupRight : ObservableObject
    {
        private int groupId;
        private string groupName;
        private bool read;
        private bool write;
        private bool edit;
        private bool delete;
        private bool orgDownload;
        private bool prxDownload;
        private bool archive;
        private bool broadcast;
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
        public bool Read
        {
            get => read;
            set => SetProperty(ref read, value);
        }
        public bool Write
        {
            get => write;
            set => SetProperty(ref write, value);
        }
        public bool Edit
        {
            get => edit;
            set => SetProperty(ref edit, value);
        }
        public bool Delete
        {
            get => delete;
            set => SetProperty(ref delete, value);
        }
        public bool OrgDownload
        {
            get => orgDownload;
            set => SetProperty(ref orgDownload, value);
        }
        public bool PrxDownload
        {
            get => prxDownload;
            set => SetProperty(ref prxDownload, value);
        }
        public bool Archive
        {
            get => archive;
            set => SetProperty(ref archive, value);
        }
        public bool Broadcast
        {
            get => broadcast;
            set => SetProperty(ref broadcast, value);
        }
    }
    public class UserRight : User
    {
        public bool Read { get; set; }
        public bool Write { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool OrgDownload { get; set; }
        public bool PrxDownload { get; set; }
        public bool Archive { get; set; }
        public bool Broadcast { get; set; }
    }
}
