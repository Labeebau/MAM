using MAM.Data;
using MAM.Utilities;
using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class AuthorizationSetingsPage :Page ,INotifyPropertyChanged
    {
        Data.DataAccess dataAccess = new Data.DataAccess();
        public ObservableCollection<UserGroup> UserGroupRightsList { get; set; } = new ObservableCollection<UserGroup>();
        public ObservableCollection<User> UserRightsList { get; set; } = new ObservableCollection<User>();
        public ObservableCollection<User> UserList { get; set; }
        public ObservableCollection<UserGroup> UserGroupList { get; set; } = new ObservableCollection<UserGroup>();

        public List<string> UsersList = new List<string>();
        //  public List<string> UserGroupList = new List<string>();
        public UserGroup NewUserGroup { get; set; }
        public UserGroup NewUserGroupRight { get; set; }

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
            GetUserGroupRights();
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
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private UserGroup _selectedUserGroup;
        public UserGroup SelectedUserGroup
        {
            get => _selectedUserGroup;
            set { _selectedUserGroup = value;OnPropertyChanged(); }
        }
        private User _selectedUser;

       
        public User SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value;OnPropertyChanged();}
        }
        public User NewUserRight { get; private set; }

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
        private string GetUserGroupName(int userGroupId)
        {
            DataTable dt = new DataTable();
            string groupName = string.Empty;
            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@group_id", userGroupId } };
            dt = dataAccess.GetData("select group_name from user_group where group_id=@group_id",parameters);
            foreach (DataRow row in dt.Rows)
            {
                groupName= row[0].ToString();
            }
            return groupName;
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
            if (SelectedUserGroup != null)
            {
                GetUserGroupRights(SelectedUserGroup.UserGroupId);
                GetUserRights(SelectedUserGroup.UserGroupId);
            }
        }
        private void GetUserGroupRights()
        {
            UserGroupRightsList.Clear();
            DataTable dt = new DataTable();
            dt = dataAccess.GetData(
    "SELECT ug.group_id, ug.group_name, ad_group, active, " +
    "GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
    "FROM user_group ug " +
    "LEFT JOIN group_permissions gp ON ug.group_id = gp.group_id " +
    "LEFT JOIN permissions p ON gp.permission_id = p.permission_id " +
    "GROUP BY ug.group_id, ug.group_name, ad_group, active"
);

            foreach (DataRow row in dt.Rows)
            {
                if (!string.IsNullOrEmpty(row["group_name"].ToString()))
                {
                    NewUserGroupRight = new UserGroup();
                    NewUserGroupRight.UserGroupRight = new Right();
                    NewUserGroupRight.UserGroupId = Convert.ToInt32(row["group_id"]);
                    NewUserGroupRight.GroupName = row["group_name"].ToString();
                    NewUserGroupRight.IsAdGroup = Convert.ToBoolean(row["ad_group"].ToString());
                    NewUserGroupRight.IsActive = Convert.ToBoolean(row["active"].ToString());

                    if (row["permissions"].ToString().Contains("Read"))
                        NewUserGroupRight.UserGroupRight.Read = true;
                    if (row["permissions"].ToString().Contains("Write"))
                        NewUserGroupRight.UserGroupRight.Write = true;
                    if (row["permissions"].ToString().Contains("Edit"))
                        NewUserGroupRight.UserGroupRight.Edit = true;
                    if (row["permissions"].ToString().Contains("Delete"))
                        NewUserGroupRight.UserGroupRight.Delete = true;
                    if (row["permissions"].ToString().Contains("OrgDownload"))
                        NewUserGroupRight.UserGroupRight.OrgDownload = true;
                    if (row["permissions"].ToString().Contains("PrxDownload"))
                        NewUserGroupRight.UserGroupRight.PrxDownload = true;
                    if (row["permissions"].ToString().Contains("Archive"))
                        NewUserGroupRight.UserGroupRight.Archive = true;
                    if (row["permissions"].ToString().Contains("Broadcast"))
                        NewUserGroupRight.UserGroupRight.Broadcast = true;
                        UserGroupRightsList.Add(NewUserGroupRight);

                }
            }
        }
        private void GetUserGroupRights(int groupId, ObservableCollection<UserGroup> userGroupRightList = null)
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,ad_group,active,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
                                    "FROM user_group ug " +
                                    "LEFT JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "LEFT JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    $"WHERE ug.group_id={groupId}");

            DataRow row = dt.Rows[0];
            if (!string.IsNullOrEmpty(row["group_name"].ToString()))
            {
                NewUserGroupRight = new UserGroup();
                NewUserGroupRight.UserGroupRight = new Right();
                NewUserGroupRight.UserGroupId = Convert.ToInt32(row["group_id"]);
                NewUserGroupRight.GroupName = row["group_name"].ToString();
                NewUserGroupRight.IsAdGroup =Convert.ToBoolean(row["ad_group"].ToString());
                NewUserGroupRight.IsActive =Convert.ToBoolean(row["active"].ToString());

                if (row["permissions"].ToString().Contains("Read"))
                    NewUserGroupRight.UserGroupRight.Read = true;
                if (row["permissions"].ToString().Contains("Write"))
                    NewUserGroupRight.UserGroupRight.Write = true;
                if (row["permissions"].ToString().Contains("Edit"))
                    NewUserGroupRight.UserGroupRight.Edit = true;
                if (row["permissions"].ToString().Contains("Delete"))
                    NewUserGroupRight.UserGroupRight.Delete = true;
                if (row["permissions"].ToString().Contains("OrgDownload"))
                    NewUserGroupRight.UserGroupRight.OrgDownload = true;
                if (row["permissions"].ToString().Contains("PrxDownload"))
                    NewUserGroupRight.UserGroupRight.PrxDownload = true;
                if (row["permissions"].ToString().Contains("Archive"))
                    NewUserGroupRight.UserGroupRight.Archive = true;
                if (row["permissions"].ToString().Contains("Broadcast"))
                    NewUserGroupRight.UserGroupRight.Broadcast = true;
                if (userGroupRightList == null)
                {
                    UserGroupRightsList.Clear();
                    UserGroupRightsList.Add(NewUserGroupRight);
                }
                else
                    userGroupRightList.Add(NewUserGroupRight);

            }
        }
        //private void GetUserGroupRights(ObservableCollection<UserGroup> userGroupRight)
        //{
        //    DataTable dt = new DataTable();
        //    dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
        //                            "FROM user_group ug " +
        //                            "JOIN group_permissions gp ON ug.group_id = gp.group_id " +
        //                            "JOIN permissions p ON gp.permission_id = p.permission_id " +
        //                            $"GROUP BY ug.group_id");

        //    DataRow row = dt.Rows[0];
        //    if (!string.IsNullOrEmpty(row["group_name"].ToString()))
        //    {
        //        NewUserGroupRight = new UserGroup();
        //        NewUserGroupRight.UserGroupId = Convert.ToInt32(row["group_id"]);
        //        NewUserGroupRight.GroupName = row["group_name"].ToString();
        //        if (row["permissions"].ToString().Contains("Read"))
        //            NewUserGroupRight.UserGroupRight.Read = true;
        //        if (row["permissions"].ToString().Contains("Write"))
        //            NewUserGroupRight.UserGroupRight.Write = true;
        //        if (row["permissions"].ToString().Contains("Edit"))
        //            NewUserGroupRight.UserGroupRight.Edit = true;
        //        if (row["permissions"].ToString().Contains("Delete"))
        //            NewUserGroupRight.UserGroupRight.Delete = true;
        //        if (row["permissions"].ToString().Contains("OrgDownload"))
        //            NewUserGroupRight.UserGroupRight.OrgDownload = true;
        //        if (row["permissions"].ToString().Contains("PrxDownload"))
        //            NewUserGroupRight.UserGroupRight.PrxDownload = true;
        //        if (row["permissions"].ToString().Contains("Archive"))
        //            NewUserGroupRight.UserGroupRight.Archive = true;
        //        if (row["permissions"].ToString().Contains("Broadcast"))
        //            NewUserGroupRight.UserGroupRight.Broadcast = true;
        //        userGroupRight.Add(NewUserGroupRight);
        //    }
        //}
        private void GetUserRights(int groupId)
        {
            DataTable dt = new DataTable();
            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@GroupId", groupId } };
            dt = dataAccess.GetData("SELECT u.user_name,ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions FROM user u " +
                                    "left join user_roles ur on u.user_id = ur.user_id " +
                                    "left join user_group ug on ur.group_id = ug.group_id " +
                                    "left JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "left JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    "where ug.group_id=@GroupId " +
                                    "group by ug.group_id, u.user_name " +
                                    "order by ug.group_id, u.user_name ;",parameters);

            UserRightsList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                if (!string.IsNullOrEmpty(row["user_name"].ToString()))
                {
                    NewUserRight = new User();
                    NewUserRight.UserGroup = new UserGroup();
                    NewUserRight.UserRight = new Right();
                    NewUserRight.UserGroup.GroupName = row["group_name"].ToString();
                    NewUserRight.UserName = row["user_name"].ToString();
                    if (row["permissions"].ToString().Contains("Read"))
                        NewUserRight.UserRight.Read = true;
                    if (row["permissions"].ToString().Contains("Write"))
                        NewUserRight.UserRight.Write = true;
                    if (row["permissions"].ToString().Contains("Edit"))
                        NewUserRight.UserRight.Edit = true;
                    if (row["permissions"].ToString().Contains("Delete"))
                        NewUserRight.UserRight.Delete = true;
                    if (row["permissions"].ToString().Contains("OrgDownload"))
                        NewUserRight.UserRight.OrgDownload = true;
                    if (row["permissions"].ToString().Contains("PrxDownload"))
                        NewUserRight.UserRight.PrxDownload = true;
                    if (row["permissions"].ToString().Contains("Archive"))
                        NewUserRight.UserRight.Archive = true;
                    if (row["permissions"].ToString().Contains("Broadcast"))
                        NewUserRight.UserRight.Broadcast = true;
                    UserRightsList.Add(NewUserRight);
                }
            }
        }
        private void GetUserRights()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("SELECT u.user_name,ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions FROM user u " +
                                    "left join user_roles ur on u.user_id = ur.user_id " +
                                    "left join user_group ug on ur.group_id = ug.group_id " +
                                    "left JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "left JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    "group by ug.group_id, u.user_name " +
                                    "order by ug.group_id, u.user_name; ");

            UserRightsList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                if (!string.IsNullOrEmpty(row["user_name"].ToString()))
                {
                    NewUserRight = new User();
                    NewUserRight.UserGroup = new UserGroup();
                    NewUserRight.UserRight = new Right();
                    NewUserRight.UserGroup.GroupName = row["group_name"].ToString();
                    NewUserRight.UserName = row["user_name"].ToString();
                    if (row["permissions"].ToString().Contains("Read"))
                        NewUserRight.UserRight.Read = true;
                    if (row["permissions"].ToString().Contains("Write"))
                        NewUserRight.UserRight.Write = true;
                    if (row["permissions"].ToString().Contains("Edit"))
                        NewUserRight.UserRight.Edit = true;
                    if (row["permissions"].ToString().Contains("Delete"))
                        NewUserRight.UserRight.Delete = true;
                    if (row["permissions"].ToString().Contains("OrgDownload"))
                        NewUserRight.UserRight.OrgDownload = true;
                    if (row["permissions"].ToString().Contains("PrxDownload"))
                        NewUserRight.UserRight.PrxDownload = true;
                    if (row["permissions"].ToString().Contains("Archive"))
                        NewUserRight.UserRight.Archive = true;
                    if (row["permissions"].ToString().Contains("Broadcast"))
                        NewUserRight.UserRight.Broadcast = true;
                    UserRightsList.Add(NewUserRight);
                }
            }
        }
        private async void SavePermissions()
        {
            int groupId = UserGroupRightsList[0].UserGroupId;
            ObservableCollection<UserGroup> dbUserGroupRightList = new ObservableCollection<UserGroup>();
            GetUserGroupRights(groupId, dbUserGroupRightList);
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            GlobalClass.Instance.Equals(UserGroupRightsList[0], dbUserGroupRightList[0], out propsList);
            await UpdatePermission(groupId, propsList);
            GetUserRights();
           
        }
        private async Task<List<MySqlParameter>> UpdatePermission(int groupId, Dictionary<string, object> props)
        {
           List<MySqlParameter> editedProps = new List<MySqlParameter>();
            int id = -1;
            foreach (KeyValuePair<string,object> permission in props)
            {
                editedProps.Add(new MySqlParameter("@PermissionName", permission.Key));
                    id = dataAccess.GetId($"select permission_id from permissions where permission_name=@PermissionName",editedProps);
                if (Convert.ToBoolean(permission.Value))
                   await InsertPermission(groupId, id,permission.Key);
                else
                   await DeletePermission(groupId, id,permission.Key);
            }
            return editedProps;
        }
        private async Task<int> InsertPermission(int groupId, int permissionId,string permissionName)
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
           var (affectedRows,lastInsertedId,errorCode)= await dataAccess.ExecuteNonQuery(query, parameters);
            string groupName = GetUserGroupName(groupId);
            if(affectedRows>0)
                await GlobalClass.Instance.AddtoHistoryAsync("Add user group right ", $"Added '{permissionName}' right for user group '{groupName}'");
            return lastInsertedId;
        }
        private async Task<int> DeletePermission(int groupId, int permissionId,string permissionName)
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
            string groupName = GetUserGroupName(groupId);
            if (affectedRows > 0)
                await GlobalClass.Instance.AddtoHistoryAsync("Delete user group right ", $"Deleted '{permissionName}' right for user group '{groupName}'");

            return affectedRows; // Return number of deleted rows
        }
        private void SaveUserGroupRights_Click(object sender, RoutedEventArgs e)
        {
        }
        
    }
    //public class UserGroupRight : ObservableObject
    //{
    //    private int groupId;
    //    private string groupName;
    //    private bool read;
    //    private bool write;
    //    private bool edit;
    //    private bool delete;
    //    private bool orgDownload;
    //    private bool prxDownload;
    //    private bool archive;
    //    private bool broadcast;
    //    public int GroupId
    //    {
    //        get => groupId;
    //        set => SetProperty(ref groupId, value);
    //    }
    //    public string GroupName
    //    {
    //        get => groupName;
    //        set => SetProperty(ref groupName, value);
    //    }
    //    public bool Read
    //    {
    //        get => read;
    //        set => SetProperty(ref read, value);
    //    }
    //    public bool Write
    //    {
    //        get => write;
    //        set => SetProperty(ref write, value);
    //    }
    //    public bool Edit
    //    {
    //        get => edit;
    //        set => SetProperty(ref edit, value);
    //    }
    //    public bool Delete
    //    {
    //        get => delete;
    //        set => SetProperty(ref delete, value);
    //    }
    //    public bool OrgDownload
    //    {
    //        get => orgDownload;
    //        set => SetProperty(ref orgDownload, value);
    //    }
    //    public bool PrxDownload
    //    {
    //        get => prxDownload;
    //        set => SetProperty(ref prxDownload, value);
    //    }
    //    public bool Archive
    //    {
    //        get => archive;
    //        set => SetProperty(ref archive, value);
    //    }
    //    public bool Broadcast
    //    {
    //        get => broadcast;
    //        set => SetProperty(ref broadcast, value);
    //    }
    //}
    public class Right : ObservableObject
    {
        private bool read;
        private bool write;
        private bool edit;
        private bool delete;
        private bool orgDownload;
        private bool prxDownload;
        private bool archive;
        private bool broadcast;
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
}
