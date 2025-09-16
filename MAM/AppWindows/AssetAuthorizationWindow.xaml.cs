using MAM.Data;
using MAM.Views.AdminPanelViews;
using MAM.Views.MediaBinViews;
using MAM.Windows;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.AppWindows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AssetAuthorizationWindow : Window
    {
        Data.DataAccess dataAccess = new Data.DataAccess();
        public ObservableCollection<UserGroup> UserGroupRightsList { get; set; } = new ObservableCollection<UserGroup>();
        public ObservableCollection<User> UserRightsList { get; set; } = new ObservableCollection<User>();
        public ObservableCollection<User> UserList { get; set; }
        public ObservableCollection<UserGroup> UserGroupList { get; set; } = new ObservableCollection<UserGroup>();

        public List<string> UsersList = new List<string>();
        public ObservableCollection<UserGroup> AllUserGroups { get; } = new();

        //  public List<string> UserGroupList = new List<string>();
        public UserGroup NewUserGroup { get; set; }
        public UserGroup NewUserGroupRight { get; set; }
        private bool isInitialized = false;
        private static AssetAuthorizationWindow _instance;

        public XamlRoot xamlRoot { get; private set; }

        public AssetAuthorizationWindow()
        {
            this.InitializeComponent();
            // Customize the title bar
            var titleBar = AppWindow.TitleBar;
            // Set the background colors for active and inactive states
            titleBar.BackgroundColor = Colors.Black;
            titleBar.InactiveBackgroundColor = Colors.DarkGray;
            // Set the foreground colors (text/icons) for active and inactive states
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveForegroundColor = Colors.Gray;
            GlobalClass.Instance.DisableMaximizeButton(this);
            GlobalClass.Instance.SetWindowSizeAndPosition(1000, 600, this);
            MainGrid.DataContext = this;
            this.Activated += AssetAuthorizationWindow_Activated;
            isInitialized = true;
            xamlRoot = (App.MainAppWindow.Content as FrameworkElement)?.XamlRoot;
        }
        public static void ShowWindow()
        {
            if (_instance == null)
            {
                _instance = new AssetAuthorizationWindow();
                // When the window closes, clear the instance so it can be opened again
                _instance.Closed += (s, e) => _instance = null;
            }
            _instance.Activate();
          
        }
        private bool _isLoaded = false; // ensure it runs only once
        private async void AssetAuthorizationWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_isLoaded) return; // run only the first time
            _isLoaded = true;

            // Your async code
            UserList = new ObservableCollection<User>((IEnumerable<User>)(await dataAccess.GetUsers()));
            await LoadUserGroups();
        }

        private async Task LoadUserGroups()
        {
            await GetAllUserGroups();
            AllUserGroups.Clear();
            // Add "All" option
            AllUserGroups.Add(new UserGroup
            {
                UserGroupId = -1,  // Special ID for "All"
                GroupName = "All",
                UserGroupRight = new Right()
            });
            // Add real groups
            foreach (var group in UserGroupList)
            {
                AllUserGroups.Add(group);
            }
            // Optionally, set default selection
            SelectedUserGroup = AllUserGroups.FirstOrDefault();
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
            set { _selectedUserGroup = value; OnPropertyChanged(); }
        }
        private User _selectedUser;


        public User SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }
        public User NewUserRight { get; private set; }

        private async Task GetAllUserGroups()
        {
            UserGroupList.Clear();
            DataTable dt = await dataAccess.GetDataAsync("select group_id, group_name, ad_group, active from user_group");

            foreach (DataRow row in dt.Rows)
            {
                var newUserGroup = new UserGroup
                {
                    UserGroupId = Convert.ToInt32(row["group_id"]),
                    GroupName = row["group_name"].ToString(),
                    IsAdGroup = Convert.ToBoolean(row["ad_group"]),
                    IsActive = Convert.ToBoolean(row["active"])
                };

                UserGroupList.Add(newUserGroup);
            }
        }

        private async Task<string> GetUserGroupName(int userGroupId)
        {
            DataTable dt = new DataTable();
            string groupName = string.Empty;
            List<MySqlParameter> parameters = new List<MySqlParameter> { new MySqlParameter("@group_id", userGroupId) };
            dt = await dataAccess.GetDataAsync("select group_name from user_group where group_id=@group_id", parameters);
            foreach (DataRow row in dt.Rows)
            {
                groupName = row[0].ToString();
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
            //SavePermissions();
        }
        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //UserList = new ObservableCollection<User>((IEnumerable<User>)dataAccess.GetUsers());
            //await LoadUserGroups();
            //GetAllUserGroupRights();
        }
        private void ComboBoxUSer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void ComboBoxUserGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized) return;
            if (SelectedUserGroup != null)
            {
                if (SelectedUserGroup.UserGroupId <= 1)
                {
                    //await GetAllUserGroups();
                    GetAllUserGroupRights();
                    GetAllUserRights();

                }
                else
                {
                    UserGroupRightsList.Clear();
                    UserGroupRightsList.Add(await GetUserGroupRights(SelectedUserGroup.UserGroupId));
                    GetUserRightList(SelectedUserGroup.UserGroupId);
                }
            }
        }
        private async void GetAllUserGroupRights()
        {
            UserGroupRightsList.Clear();
            DataTable dt = new DataTable();
            dt = await dataAccess.GetDataAsync(
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
                    if (row["permissions"].ToString().Contains("OriginalDownload"))
                        NewUserGroupRight.UserGroupRight.OriginalDownload = true;
                    if (row["permissions"].ToString().Contains("ProxyDownload"))
                        NewUserGroupRight.UserGroupRight.ProxyDownload = true;
                    if (row["permissions"].ToString().Contains("Archive"))
                        NewUserGroupRight.UserGroupRight.Archive = true;
                    if (row["permissions"].ToString().Contains("Broadcast"))
                        NewUserGroupRight.UserGroupRight.Broadcast = true;
                    UserGroupRightsList.Add(NewUserGroupRight);

                }
            }
        }
        private async Task<UserGroup> GetUserGroupRights(int groupId)
        {
            DataTable dt = new DataTable();
            dt = await dataAccess.GetDataAsync("SELECT ug.group_id, ug.group_name,ad_group,active,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
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
                if (row["permissions"].ToString().Contains("OriginalDownload"))
                    NewUserGroupRight.UserGroupRight.OriginalDownload = true;
                if (row["permissions"].ToString().Contains("ProxyDownload"))
                    NewUserGroupRight.UserGroupRight.ProxyDownload = true;
                if (row["permissions"].ToString().Contains("Archive"))
                    NewUserGroupRight.UserGroupRight.Archive = true;
                if (row["permissions"].ToString().Contains("Broadcast"))
                    NewUserGroupRight.UserGroupRight.Broadcast = true;
                return NewUserGroupRight;
            }
            return null;
        }
        private async void GetUserRightList(int groupId)
        {
            UserRightsList.Clear();
            List<MySqlParameter> param = new();
            param.Add(new MySqlParameter("@GroupId", groupId));
            DataTable dt = await dataAccess.GetDataAsync($"SELECT u.user_id, u.user_name FROM user u INNER JOIN user_roles ur ON u.user_id = ur.user_id WHERE ur.group_id = @GroupId", param);

            foreach (DataRow row in dt.Rows)
            {
                int userId = Convert.ToInt32(row["user_id"]);
                string userName = row["user_name"].ToString();

                var parameters = new List<MySqlParameter> { new("in_user_id", userId) };
                User user = new User();
                user = await GlobalClass.Instance.GetUserRightsAsync(userId);
                user.UserName = userName;
                if (user != null)
                    UserRightsList.Add(user);

                //using var reader = await dataAccess.ExecuteReaderStoredProcedure("GetUserEffectivePermissions", parameters);

                //if (reader != null)
                //{
                //    var newUserRight = new User
                //    {
                //        UserId=userId,
                //        UserName = userName,
                //        UserRight = new Right()
                //    };

                //    while (await reader.ReadAsync())
                //    {
                //        string permission = reader["permission_name"].ToString();

                //        if (permission.Contains("Read")) newUserRight.UserRight.Read = true;
                //        if (permission.Contains("Write")) newUserRight.UserRight.Write = true;
                //        if (permission.Contains("Edit")) newUserRight.UserRight.Edit = true;
                //        if (permission.Contains("Delete")) newUserRight.UserRight.Delete = true;
                //        if (permission.Contains("OriginalDownload")) newUserRight.UserRight.OriginalDownload = true;
                //        if (permission.Contains("ProxyDownload")) newUserRight.UserRight.ProxyDownload = true;
                //        if (permission.Contains("Archive")) newUserRight.UserRight.Archive = true;
                //        if (permission.Contains("Broadcast")) newUserRight.UserRight.Broadcast = true;
                //    }
                //    UserRightsList.Add(newUserRight);
                //}
            }
        }
        private async void GetAllUserRights()
        {
            UserRightsList.Clear();
            DataTable dt = await dataAccess.GetDataAsync($"SELECT u.user_id, u.user_name FROM user u ");
            foreach (DataRow row in dt.Rows)
            {
                int userId = Convert.ToInt32(row["user_id"]);
                string userName = row["user_name"].ToString();

                var parameters = new List<MySqlParameter> { new("in_user_id", userId) };
                User user = new User();
                user = await GlobalClass.Instance.GetUserRightsAsync(userId);
                user.UserName = userName;
                if (user != null)
                    UserRightsList.Add(user);
                //using var reader = await dataAccess.ExecuteReaderStoredProcedure("GetUserEffectivePermissions", parameters);

                //if (reader != null)
                //{
                //    var newUserRight = new User
                //    {
                //        UserId = userId,
                //        UserName = userName,
                //        UserRight = new Right()
                //    };

                //    while (await reader.ReadAsync())
                //    {
                //        string permission = reader["permission_name"].ToString();

                //        if (permission.Contains("Read")) newUserRight.UserRight.Read = true;
                //        if (permission.Contains("Write")) newUserRight.UserRight.Write = true;
                //        if (permission.Contains("Edit")) newUserRight.UserRight.Edit = true;
                //        if (permission.Contains("Delete")) newUserRight.UserRight.Delete = true;
                //        if (permission.Contains("OriginalDownload")) newUserRight.UserRight.OriginalDownload = true;
                //        if (permission.Contains("ProxyDownload")) newUserRight.UserRight.ProxyDownload = true;
                //        if (permission.Contains("Archive")) newUserRight.UserRight.Archive = true;
                //        if (permission.Contains("Broadcast")) newUserRight.UserRight.Broadcast = true;
                //    }
                //    UserRightsList.Add(newUserRight);
                //}
            }
        }



        //private void GetUserRights()
        //{
        //    DataTable dt = new DataTable();
        //    dt = dataAccess.GetData("SELECT u.user_name,ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions FROM user u " +
        //                            "left join user_roles ur on u.user_id = ur.user_id " +
        //                            "left join user_group ug on ur.group_id = ug.group_id " +
        //                            "left JOIN group_permissions gp ON ug.group_id = gp.group_id " +
        //                            "left JOIN permissions p ON gp.permission_id = p.permission_id " +
        //                            "group by ug.group_id, u.user_name " +
        //                            "order by ug.group_id, u.user_name; ");

        //    UserRightsList.Clear();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        if (!string.IsNullOrEmpty(row["user_name"].ToString()))
        //        {
        //            NewUserRight = new User();
        //            NewUserRight.UserRight = new Right();
        //            NewUserRight.UserName = row["user_name"].ToString();
        //            if (row["permissions"].ToString().Contains("Read"))
        //                NewUserRight.UserRight.Read = true;
        //            if (row["permissions"].ToString().Contains("Write"))
        //                NewUserRight.UserRight.Write = true;
        //            if (row["permissions"].ToString().Contains("Edit"))
        //                NewUserRight.UserRight.Edit = true;
        //            if (row["permissions"].ToString().Contains("Delete"))
        //                NewUserRight.UserRight.Delete = true;
        //            if (row["permissions"].ToString().Contains("OriginalDownload"))
        //                NewUserRight.UserRight.OriginalDownload = true;
        //            if (row["permissions"].ToString().Contains("ProxyDownload"))
        //                NewUserRight.UserRight.ProxyDownload = true;
        //            if (row["permissions"].ToString().Contains("Archive"))
        //                NewUserRight.UserRight.Archive = true;
        //            if (row["permissions"].ToString().Contains("Broadcast"))
        //                NewUserRight.UserRight.Broadcast = true;
        //            UserRightsList.Add(NewUserRight);
        //        }
        //    }
        //}
        private async void SavePermissions()
        {
            foreach (var groupRight in UserGroupRightsList)
            {
                int groupId = groupRight.UserGroupId;
                UserGroup dbUserGroupRight = new UserGroup();
                dbUserGroupRight = await GetUserGroupRights(groupId);
                Dictionary<string, object> propsList = new Dictionary<string, object>();
                GlobalClass.Instance.CompareProperties(groupRight.UserGroupRight, dbUserGroupRight.UserGroupRight, out propsList);
                if (propsList.Count > 0)
                    await UpdateGroupPermission(groupId, propsList);
            }
            foreach (var userRight in UserRightsList)
            {

                int userId = userRight.UserId;
                User dbUserRightList = new User();
                dbUserRightList = await GlobalClass.Instance.GetUserRightsAsync(userId);
                Dictionary<string, object> propsList = new Dictionary<string, object>();
                GlobalClass.Instance.CompareProperties(userRight.UserRight, dbUserRightList.UserRight, out propsList);
                if (propsList.Count > 0)
                    await UpdateUserPermission(userId, propsList);
            }
            //GetUserRights();

        }
        private async Task<List<MySqlParameter>> UpdateGroupPermission(int groupId, Dictionary<string, object> props)
        {
            List<MySqlParameter> allParamsUsed = new List<MySqlParameter>();

            foreach (var permission in props)
            {
                var paramList = new List<MySqlParameter>
        {
            new MySqlParameter("@PermissionName", permission.Key)
        };
                allParamsUsed.AddRange(paramList);

                int id = await dataAccess.GetId("SELECT permission_id FROM permissions WHERE permission_name = @PermissionName", paramList);

                if (Convert.ToBoolean(permission.Value))
                    await InsertPermission(groupId, id, permission.Key);
                else
                    await DeletePermission(groupId, id, permission.Key);
            }

            return allParamsUsed;
        }
        private async Task<List<MySqlParameter>> UpdateUserPermission(int userId, Dictionary<string, object> props)
        {
            List<MySqlParameter> allParamsUsed = new List<MySqlParameter>();

            foreach (var permission in props)
            {
                var paramList = new List<MySqlParameter>
                {
                    new MySqlParameter("@PermissionName", permission.Key)
                };
                allParamsUsed.AddRange(paramList);

                int id = await dataAccess.GetId("SELECT permission_id FROM permissions WHERE permission_name = @PermissionName", paramList);

                var parameters = new List<MySqlParameter>
                    {
                        {new MySqlParameter("in_user_id", userId) },
                        { new MySqlParameter("in_permission_id", id)},
                        {new MySqlParameter("in_allowed", Convert.ToBoolean(permission.Value))},
                    };
                await dataAccess.ExecuteNonQueryStoredProcedure("InsertOrUpdateUserPermission", parameters);
            }
            return allParamsUsed;
        }

        private async Task<int> InsertPermission(int groupId, int permissionId, string permissionName)
        {
            List<MySqlParameter> parameters = new();
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
            var (affectedRows, lastInsertedId, errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);
            string groupName = await GetUserGroupName(groupId);
            if (affectedRows > 0)
            {
                await GlobalClass.Instance.AddtoHistoryAsync("Add user group right ", $"Added '{permissionName}' right for user group '{groupName}'");
                return lastInsertedId;
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync($"Unable to add permission '{permissionName}'\n {errorMessage}", xamlRoot);
                return -1;
            }
        }
        private async Task<int> DeletePermission(int groupId, int permissionId, string permissionName)
        {
            List<MySqlParameter> parameters = new();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@GroupId", groupId));
            parameters.Add(new MySqlParameter("@PermissionId", permissionId));
            query = $"DELETE FROM group_permissions where group_id=@GroupId AND permission_id=@PermissionId ";
            // Execute query and retrieve affected rows
            var (affectedRows, _, errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);

            string groupName = await GetUserGroupName(groupId);
            if (affectedRows > 0 && string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.AddtoHistoryAsync("Delete user group right ", $"Deleted '{permissionName}' right for user group '{groupName}'");
                return affectedRows; // Return number of deleted rows
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync($"Failed to delete permission. \n {errorMessage}", xamlRoot);
                return 0;
            }
        }
        private void SaveUserGroupRights_Click(object sender, RoutedEventArgs e)
        {
        }

        private void EditGroupRight_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var userGroup = button?.Tag as UserGroup;
            if (userGroup != null)
            {
                // Make sure all rows are not in edit mode
                foreach (var u in UserGroupRightsList)
                {
                    u.IsEnabled = false;
                }
            }
            SelectedUserGroup = userGroup;
            SelectedUserGroup.IsEnabled = true;
        }

        private void EditUserRight_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var user = button?.Tag as User;
            if (user != null)
            {
                // Make sure all rows are not in edit mode
                foreach (var u in UserRightsList)
                {
                    u.IsEnabled = false;
                }
            }
            SelectedUser = user;
            SelectedUser.IsEnabled = true;
        }
    }
}
