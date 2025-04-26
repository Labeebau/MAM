using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using MySql.Data.MySqlClient;
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
        public ObservableCollection<UserGroupForGrouping> GroupedUsersList { get; set; } = new ObservableCollection<UserGroupForGrouping>();



        User NewUser { get; set; }
        UserGroup NewUserGroup { get; set; }

        private UserGroup EditingUser;
        public UserGroupsPage()
        {
            this.InitializeComponent();
            GetUserGroup(UserGroupsList);
            GetGroupedData();
            GetUserInUserGroup();
            // Data binding for GridView
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
        private void GetUserGroup(ObservableCollection<UserGroup> userGroupsList=null)
        {

            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select group_id,group_name,ad_group,active from user_group");
            UserGroupsList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewUserGroup = new UserGroup();
                NewUserGroup.UserGroupId = Convert.ToInt32(row[0]);
                NewUserGroup.GroupName = row[1].ToString();
                NewUserGroup.IsAdGroup = Convert.ToBoolean(row[2].ToString());
                NewUserGroup.IsActive = Convert.ToBoolean(row[3].ToString());
                if (userGroupsList == null)
                {
                    UserGroupsList.Add(NewUserGroup);
                }
                else
                    userGroupsList.Add(NewUserGroup);
            }
        }
        private void GetGroupedData()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("SELECT ug.group_name, u.user_name " +
                "FROM user u " +
                "JOIN user_roles ur ON u.user_id = ur.user_id " +
                "JOIN user_group ug ON ur.group_id = ug.group_id " +
                "GROUP BY ug.group_name, u.user_name " +
                "ORDER BY ug.group_name, u.user_name;");
            //foreach (DataRow row in dt.Rows)
            //{
            //    NewGroupedUser = new UsersInGroup();
            //    NewGroupedUser.GroupName = row[0].ToString();
            //    NewGroupedUser.UserName = row[1].ToString();
            //    UserInGroupList.Add(NewGroupedUser);
            //}
            var groups = new Dictionary<string, UserGroupForGrouping>();


            foreach (DataRow row in dt.Rows)
            {
                string groupName = row["group_name"].ToString();
                string userName = row["user_name"].ToString();

                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new UserGroupForGrouping { GroupName = groupName };
                }

                groups[groupName].Users.Add(new UserForGrouping { UserName = userName,GroupName=groupName });
            }

            // Populate GroupedUsers with the values from the dictionary
            foreach (var group in groups.Values)
            {
                GroupedUsersList.Add(group);
            }

            //Create the CollectionViewSource  and set to grouped collection
            CollectionViewSource groupedItems = new CollectionViewSource();
            groupedItems.IsSourceGrouped = true;
            groupedItems.Source = GroupedUsersList;
            DgvUserGroup.ItemsSource=groupedItems.View;
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
            ObservableCollection<UserGroup> dbUserGroupList = new ObservableCollection<UserGroup>();
            GetUserGroup(dbUserGroupList);
            Dictionary<PropertyInfo, string> DbFields = new Dictionary<PropertyInfo, string>();
            foreach (UserGroup userGroup in UserGroupsList)
            {
                if (userGroup.UserGroupId == -1)
                {
                    userGroup.UserGroupId =await InsertUserGroup(userGroup);
                    userGroup.IsReadOnly = true;
                }
                else
                {
                    foreach (UserGroup dbUserGroup in dbUserGroupList)
                    {
                        if (userGroup.UserGroupId == dbUserGroup.UserGroupId)
                        {
                            Dictionary<string, object> propsList = new Dictionary<string, object>();
                            GlobalClass.Instance.Equals(userGroup, dbUserGroup, out propsList);
                            propsList = UpdateFieldNames(propsList);

                            if (propsList.Count > 0)
                                dataAccess.UpdateRecord("user_group", "group_id", userGroup.UserGroupId, propsList);
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
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Confirmation",
                Content = "Are you sure you want to delete this User Group?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot // Required in WinUI 3 for displaying dialogs
            };

            // Show the dialog
            ContentDialogResult result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var button = sender as Button;
                var userGroup = button?.Tag as UserGroup;
                if (userGroup != null)
                {
                    var userGrpDict = new List<MySql.Data.MySqlClient.MySqlParameter>() { new MySql.Data.MySqlClient.MySqlParameter("@GroupId", userGroup.UserGroupId) };
                    var (affectedRows, _, erroeCode) = await dataAccess.ExecuteNonQuery($"DELETE FROM user_group WHERE group_id=@GroupId", userGrpDict);
                    if (affectedRows == 1)
                    {
                        UserGroupsList.Remove(userGroup);
                        await GlobalClass.Instance.AddtoHistoryAsync("Delete User Group", $"User group '{userGroup.GroupName}' deleted.");
                    }
                }
            }
        }
        private async Task<int> InsertUserGroup(UserGroup userGroup)
        {
            List<MySqlParameter> parameters = new ();
            string query = string.Empty;

            parameters.Add(new MySqlParameter( "@UserGroupName", userGroup.GroupName));
            parameters.Add(new MySqlParameter("@IsAdGroup", userGroup.IsAdGroup));
            parameters.Add(new MySqlParameter("@IsActive", userGroup.IsActive));

            query = $"insert into user_group(group_name,ad_group,active)" +
                $"values(@UserGroupName,@IsAdGroup,@IsActive)";
            var(affectedRows, newUserGroupId ,errorCode)=await dataAccess.ExecuteNonQuery(query, parameters);
            await GlobalClass.Instance.AddtoHistoryAsync("Create User Group", $"New user group '{userGroup.GroupName}' created.");
            return affectedRows >0? newUserGroupId:-1;
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

        int result =await InsertUserToGroup();
            if (result == -1)
            {
                ContentDialog duplicateDialog = new ContentDialog
                {
                    // Title = "Warning!!!",
                    Content = "User already exists in the Group",
                    PrimaryButtonText = "OK",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot // Required in WinUI 3 for displaying dialogs
                };
                await duplicateDialog.ShowAsync();
            }

        }
        private async Task<int> InsertUserToGroup()
        {
            List<MySqlParameter> parameters = new ();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@UserId", SelectedUser.UserId));
            parameters.Add(new MySqlParameter("@GroupId", SelectedUserGroup.UserGroupId));
            query = $"insert into user_roles(user_id,group_id) values(@UserId,@GroupId)";
            var(affectedRows,newUserGroupId,errorCode)=await dataAccess.ExecuteNonQuery(query, parameters);
            await GlobalClass.Instance.AddtoHistoryAsync("Add user to group", $"User '{SelectedUser.UserName}' is added to User Group '{SelectedUser.UserGroup}");
            return affectedRows >0? newUserGroupId:-1;
        }

        private void RefreshGroups_Click(object sender, RoutedEventArgs e)
        {
            UserGroupsList.Clear();
            GetUserGroup(UserGroupsList);
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
    public class UserForGrouping
    {
        public string UserName { get; set; }
        public string GroupName { get; set; }
    }

    public class UserGroupForGrouping
    {
        public string GroupName { get; set; }
        public ObservableCollection<UserForGrouping> Users { get; set; } = new ObservableCollection<UserForGrouping>();
    }


}


