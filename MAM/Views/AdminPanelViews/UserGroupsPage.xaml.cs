using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using Windows.System;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class UserGroupsPage : Page
    {
        DataAccess dataAccess = new DataAccess();

        public ObservableCollection<UserGroup> UserGroupsList { get; set; } = new ObservableCollection<UserGroup>();
        public ObservableCollection<User> UserList { get; set; } = new ObservableCollection<User>();
        // public ObservableCollection<UserForGrouping> Users { get; set; } = new ObservableCollection<UserForGrouping>();
        // public CollectionViewSource GroupedUsers { get; set; } = new CollectionViewSource();
        public ObservableCollection<UserGroupForGrouping> GroupedUsersList { get; set; } = new();



        User NewUser { get; set; }
        UserGroup NewUserGroup { get; set; }

        private UserGroup EditingUser;
        public UserGroupsPage()
        {
            this.InitializeComponent();
            UserGroupsList = GetUserGroup();
            GetGroupedData();
            GetUserInUserGroup();
            DataContext = this;
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
        private ObservableCollection<UserGroup> GetUserGroup()
        {
            ObservableCollection<UserGroup> userGroupsList = new ObservableCollection<UserGroup>();
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select group_id,group_name,ad_group,active from user_group");
            foreach (DataRow row in dt.Rows)
            {
                NewUserGroup = new UserGroup();
                NewUserGroup.UserGroupId = Convert.ToInt32(row[0]);
                NewUserGroup.GroupName = row[1].ToString();
                NewUserGroup.IsAdGroup = Convert.ToBoolean(row[2].ToString());
                NewUserGroup.IsActive = Convert.ToBoolean(row[3].ToString());
                userGroupsList.Add(NewUserGroup);
            }
            return userGroupsList;
        }
        private void GetGroupedData()
        {
            // Clear existing items
            GroupedUsersList.Clear();

            // SQL to include all user groups even if no users are assigned
            DataTable dt = dataAccess.GetData(
                "SELECT ug.group_id, ug.group_name, u.user_id, u.user_name " +
                "FROM user_group ug " +
                "LEFT JOIN user_roles ur ON ug.group_id = ur.group_id " +
                "LEFT JOIN user u ON ur.user_id = u.user_id " +
                "ORDER BY ug.group_name, u.user_name;");

            // Group by group_id (int is a stable dictionary key)
            var groups = new Dictionary<int, UserGroupForGrouping>();

            foreach (DataRow row in dt.Rows)
            {
                // Extract group info
                int groupId = Convert.ToInt32(row["group_id"]);
                string groupName = row["group_name"].ToString();

                // Get or create the group
                if (!groups.TryGetValue(groupId, out var groupObj))
                {
                    groupObj = new UserGroupForGrouping
                    {
                        Group = new UserGroup
                        {
                            UserGroupId = groupId,
                            GroupName = groupName
                        }
                    };
                    groups[groupId] = groupObj;
                }

                // Extract user if present
                if (row["user_id"] != DBNull.Value && row["user_name"] != DBNull.Value)
                {
                    var user = new User
                    {
                        UserId = Convert.ToInt32(row["user_id"]),
                        UserName = row["user_name"].ToString()
                    };

                    var userForGrouping = new UserForGrouping
                    {
                        User = user,
                        ParentGroup = groupObj // ✅ link back to parent
                    };

                    groupObj.Users.Add(userForGrouping);
                }
            }

            // Add to ObservableCollection (bound to UI)
            foreach (var group in groups.Values)
            {
                GroupedUsersList.Add(group);
            }
        }

        private async  void DeleteUserFromUserGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserForGrouping userToRemove)
            {
               await DeleteFromUserGroupAsync(userToRemove);
            }
        }
        private async Task DeleteFromUserGroupAsync(UserForGrouping userForGrouping)
        {
            var user = userForGrouping.User;
            var group = userForGrouping.ParentGroup;
            ContentDialogResult result = await GlobalClass.Instance.ShowDialogAsync($"Are you sure you want to delete {userForGrouping.User.UserName} from {userForGrouping.ParentGroup.Group.GroupName}?", XamlRoot, "Delete", "Cancel", "Delete Confirmation");
            if (result == ContentDialogResult.Primary)
            {
                if (userForGrouping != null && userForGrouping.ParentGroup!= null)
                {
                    var userGrpDict = new List<MySqlParameter>
        {
            new("@UserId", user.UserId),
            new("@GroupId", group.Group.UserGroupId)
        };

                    var (affectedRows, _, errorMessage) = await dataAccess.ExecuteNonQuery($"DELETE FROM user_roles WHERE user_id=@UserId and group_id=@GroupId", userGrpDict);
                    if (affectedRows == 1)
                    {
                        group.Users.Remove(userForGrouping);
                        await GlobalClass.Instance.AddtoHistoryAsync("Delete User Group", $"User group '{group.Group.GroupName}' deleted.");
                    }
                    else
                    {
                        await GlobalClass.Instance.ShowDialogAsync($"Unable to delete user group '{group.Group.GroupName}'\n {errorMessage}", this.XamlRoot);
                    }
                }
            }
        }



        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
        private void DeleteGroup_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button deleteButton && deleteButton.Tag is UserGroup itemToDelete)
            {
                // Remove the item from the collection
                UserGroupsList.Remove(itemToDelete);
                // GlobalClass.Instance.AddtoHistoryAsync("Delete User", $"User '{itemToDelete.GroupName}' deleted.");

            }
        }

        private void ADGroup_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Acive_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void AddUserGroup_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            NewUserGroup = new UserGroup();
            NewUserGroup.UserGroupId = -1;
            NewUserGroup.IsReadOnly = false;
            UserGroupsList.Add(NewUserGroup);

            DgvGroups.SelectedIndex = UserGroupsList.Count - 1;
            DgvGroups.BeginEdit();
            if (DgvGroups.ItemsSource is IList<UserGroup> items)
            {
                DgvGroups.SelectedIndex = items.Count - 1;
                DgvGroups.CurrentColumn = DgvGroups.Columns[0];

                DgvGroups.ScrollIntoView(items[DgvGroups.SelectedIndex], DgvGroups.Columns[0]);
            }
        }
        private void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SaveUserGroup();
        }
        private async void SaveUserGroup()
        {
            ObservableCollection<UserGroup> dbUserGroupList = GetUserGroup();
            Dictionary<PropertyInfo, string> DbFields = new Dictionary<PropertyInfo, string>();
            foreach (UserGroup userGroup in UserGroupsList)
            {
                if (userGroup.UserGroupId == -1)
                {
                    userGroup.UserGroupId = await InsertUserGroup(userGroup);
                    userGroup.IsReadOnly = true;
                }
                else
                {
                    foreach (UserGroup dbUserGroup in dbUserGroupList)
                    {
                        if (userGroup.UserGroupId == dbUserGroup.UserGroupId)
                        {
                            Dictionary<string, object> propsList = new Dictionary<string, object>();
                            GlobalClass.Instance.CompareProperties(userGroup, dbUserGroup, out propsList);
                            propsList = UpdateFieldNames(propsList);

                            if (propsList.Count > 0)
                                await dataAccess.UpdateRecord("user_group", "group_id", userGroup.UserGroupId, propsList);
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
                if (key == "UserGroupName")
                    editedProps.Add("group_name", props[key]);
                else if (key == "IsAdGroup")
                    editedProps.Add("ad_group", props[key]);
                else if (key == "IsActive")
                    editedProps.Add("active", props[key]);
            }
            return editedProps;
        }
        private void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            GetUserGroup();
        }
        private async void DeleteUserGroup_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var userGroup = button?.Tag as UserGroup;
            if (userGroup.GroupName != "Admin")
            {
                ContentDialogResult result = await GlobalClass.Instance.ShowDialogAsync($"Are you sure you want to delete {userGroup.GroupName}?", XamlRoot, "Delete", "Cancel", "Delete Confirmation");
                if (result == ContentDialogResult.Primary)
                {
                    if (userGroup != null)
                    {
                        var userGrpDict = new List<MySql.Data.MySqlClient.MySqlParameter>() { new MySql.Data.MySqlClient.MySqlParameter("@GroupId", userGroup.UserGroupId) };
                        var (affectedRows, _, errorMessage) = await dataAccess.ExecuteNonQuery($"DELETE FROM user_group WHERE group_id=@GroupId", userGrpDict);
                        if (affectedRows == 1)
                        {
                            UserGroupsList.Remove(userGroup);
                            await GlobalClass.Instance.AddtoHistoryAsync("Delete User Group", $"User group '{userGroup.GroupName}' deleted.");
                        }
                        else
                        {
                            await GlobalClass.Instance.ShowDialogAsync($"Unable to delete user group '{userGroup.GroupName}'\n {errorMessage}", this.XamlRoot);
                        }
                    }
                }
            }
            else
               await GlobalClass.Instance.ShowDialogAsync($"Action blocked: 'Admin' group is protected from deletion.", XamlRoot, "Action Not Allowed!");
        }
        private async Task<int> InsertUserGroup(UserGroup userGroup)
        {
            List<MySqlParameter> parameters = new();
            string query = string.Empty;

            parameters.Add(new MySqlParameter("@UserGroupName", userGroup.GroupName));
            parameters.Add(new MySqlParameter("@IsAdGroup", userGroup.IsAdGroup));
            parameters.Add(new MySqlParameter("@IsActive", userGroup.IsActive));

            query = $"insert into user_group(group_name,ad_group,active)" +
                $"values(@UserGroupName,@IsAdGroup,@IsActive)";
            var (affectedRows, newUserGroupId, errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);
            await GlobalClass.Instance.AddtoHistoryAsync("Create User Group", $"New user group '{userGroup.GroupName}' created.");
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.ShowDialogAsync("Unable to insert user group '{userGroup.GroupName}'\n {errorMessage}", this.XamlRoot);
                return -1;
            }
            else
                return newUserGroupId;
        }

        private void AddUSer_Click(object sender, RoutedEventArgs e)
        {

        }



        private void DgvGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditingUser != null && e.RemovedItems.Count > 0 && EditingUser.UserGroupId == ((UserGroup)e.RemovedItems[0]).UserGroupId)
                EditingUser.IsReadOnly = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void UserGroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
        private void GetUserInUserGroup()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData($"select user_id,user_name FROM user;");
            // dt = dataAccess.GetData($"select u.user_id,u.user_name FROM user u inner join user_roles ur on u.user_id = ur.user_id where ur.group_id={groupId};");

            foreach (DataRow row in dt.Rows)
            {
                NewUser = new User();
                NewUser.UserId = Convert.ToInt32(row[0]);
                NewUser.UserName = row[1].ToString();
                UserList.Add(NewUser);
            }

        }

        private async void AddUSerToGroup_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser != null && SelectedUserGroup != null)
            {
                int result = await InsertUserToGroup();
                if (result == 1)
                {
                    GetGroupedData();
                    //Right right= GetUserGroupRights(SelectedUserGroup.UserGroupId);
                    //await UpdatePermission(SelectedUserGroup.UserGroupId, right);

                }
                //if (result == -1)
                //{
                //    await GlobalClass.Instance.ShowErrorDialogAsync("User already exists in the Group", this.XamlRoot);
                //}
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync("Select User and User Group", XamlRoot, "Warning");
            }
        }
        private async Task<int> InsertUserToGroup()
        {
            List<MySqlParameter> parameters = new();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@UserId", SelectedUser.UserId));
            parameters.Add(new MySqlParameter("@GroupId", SelectedUserGroup.UserGroupId));
            query = $"insert into user_roles(user_id,group_id) values(@UserId,@GroupId)";
            var (affectedRows, newUserGroupId, errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);
            await GlobalClass.Instance.AddtoHistoryAsync("Add user to group", $"User '{SelectedUser.UserName}' is added to User Group '{SelectedUserGroup.GroupName}'");
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.ShowDialogAsync($"Unable to add '{SelectedUser.UserName}' to group '{SelectedUserGroup.GroupName}'\n {errorMessage}", this.XamlRoot);
                return -1;
            }
            else
                return affectedRows;

        }
        //private Right GetUserGroupRights(int groupId)
        //{
        //    DataTable dt = new DataTable();
        //    dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,ad_group,active,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
        //                            "FROM user_group ug " +
        //                            "LEFT JOIN group_permissions gp ON ug.group_id = gp.group_id " +
        //                            "LEFT JOIN permissions p ON gp.permission_id = p.permission_id " +
        //                            $"WHERE ug.group_id={groupId}");

        //    DataRow row = dt.Rows[0];
        //    if (!string.IsNullOrEmpty(row["group_name"].ToString()))
        //    {
        //        Right rights = new Right();
        //        if (row["permissions"].ToString().Contains("Read"))
        //            rights.Read = true;
        //        if (row["permissions"].ToString().Contains("Write"))
        //            rights.Write = true;
        //        if (row["permissions"].ToString().Contains("Edit"))
        //            rights.Edit = true;
        //        if (row["permissions"].ToString().Contains("Delete"))
        //            rights.Delete = true;
        //        if (row["permissions"].ToString().Contains("OrgDownload"))
        //            rights.OrgDownload = true;
        //        if (row["permissions"].ToString().Contains("PrxDownload"))
        //            rights.PrxDownload = true;
        //        if (row["permissions"].ToString().Contains("Archive"))
        //            rights.Archive = true;
        //        if (row["permissions"].ToString().Contains("Broadcast"))
        //            rights.Broadcast = true;
        //        return rights;
        //    }
        //    return null;
        //}
        //private async Task<List<MySqlParameter>> UpdatePermission(int groupId, Right rights)
        //{
        //    List<MySqlParameter> allParamsUsed = new List<MySqlParameter>();
        //    foreach (PropertyInfo prop in rights.GetType().GetProperties())
        //    {
        //        var paramList = new List<MySqlParameter>
        //{
        //    new MySqlParameter("@PermissionName", prop.Name)
        //};
        //        //allParamsUsed.AddRange(paramList);

        //        int id = dataAccess.GetId("SELECT permission_id FROM permissions WHERE permission_name = @PermissionName", paramList);

        //        if (Convert.ToBoolean(prop.GetValue(rights)))
        //            await InsertPermission(groupId, id, prop.Name);
        //        //else
        //        //    await DeletePermission(groupId, id, permission.Key);
        //    }

        //    return allParamsUsed;
        //}
        //private async Task<int> InsertPermission(int groupId, int permissionId, string permissionName)
        //{
        //    List<MySqlParameter> parameters = new();
        //    string query = string.Empty;
        //    parameters.Add(new MySqlParameter("@GroupId", groupId));
        //    parameters.Add(new MySqlParameter("@PermissionId", permissionId));
        //    query = "INSERT INTO group_permissions (group_id,permission_id) " +
        //            $"SELECT {groupId},{permissionId} " +
        //            "WHERE NOT EXISTS(" +
        //            "SELECT 1 " +
        //            "FROM group_permissions " +
        //            $"WHERE group_id = {groupId} AND permission_id = {permissionId})";
        //    // int newUserGroupId = 0,errorCode = 0;
        //    var (affectedRows, lastInsertedId, errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);
        //    //string groupName = GetUserGroupName(groupId);
        //    if (affectedRows > 0)
        //    {
        //        //await GlobalClass.Instance.AddtoHistoryAsync("Add user group right ", $"Added '{permissionName}' right for user group '{groupName}'");
        //        return lastInsertedId;
        //    }
        //    else
        //    {
        //        //await GlobalClass.Instance.ShowErrorDialogAsync($"Unable to add permission '{permissionName}'\n {errorMessage}", this.XamlRoot);
        //        return -1;
        //    }
        //}
        private void RefreshGroups_Click(object sender, RoutedEventArgs e)
        {
            UserGroupsList.Clear();
            UserGroupsList = GetUserGroup();
        }

        private void RefreshGroupedUsers_Click(object sender, RoutedEventArgs e)
        {
            GetGroupedData();
        }

        private void DgvGroup_CurrentCellChanged(object sender, EventArgs e)
        {
            DgvGroups.CommitEdit();
        }

        private void dg_loadingRowGroup(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowGroupHeaderEventArgs e)
        {
            //ICollectionViewGroup group = e.RowGroupHeader.CollectionViewGroup;
            //UserGroupForGrouping item = group.GroupItems[0] as UserGroupForGrouping;
            //e.RowGroupHeader.PropertyValue = item.Range;
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    // Item model class
    public class UserGroup : ObservableObject
    {
        private int userGroupId;
        private string groupName;
        private bool isAdGroup;
        private bool isActive;
        private bool isReadOnly = true;
        private bool isEnabled = false;
        private Right userGroupRight;
        public int UserGroupId
        {
            get => userGroupId;
            set => SetProperty(ref userGroupId, value);
        }
        public string GroupName
        {
            get => groupName;
            set => SetProperty(ref groupName, value);
        }

        public bool IsAdGroup
        {
            get => isAdGroup;
            set => SetProperty(ref isAdGroup, value);
        }

        public bool IsActive
        {
            get => isActive;
            set => SetProperty(ref isActive, value);
        }
        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                SetProperty(ref isReadOnly, value);
                IsEnabled = !value;
            }
        }
        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }
        public Right UserGroupRight
        {
            get => userGroupRight;
            set => SetProperty(ref userGroupRight, value);
        }

    }
    public class UserForGrouping:ObservableObject
    {
        private User user;
        private UserGroupForGrouping parentGroup;

        public User User
        {
            get => user;
            set => SetProperty(ref user, value);
        }
        public UserGroupForGrouping ParentGroup
        {
            get => parentGroup;
            set => SetProperty(ref parentGroup, value);
        }
        public UserForGrouping Self => this;

    }

    public class UserGroupForGrouping:ObservableObject
    {
        private UserGroup group;
        private ObservableCollection<UserForGrouping> users=new();

        public UserGroup Group
        {
            get => group;
            set => SetProperty(ref group, value);
        }
        public ObservableCollection<UserForGrouping> Users
        {
            get => users;
            set => SetProperty(ref users, value);
        }
        public int UserCount => Users?.Count ?? 0;

    }

}








