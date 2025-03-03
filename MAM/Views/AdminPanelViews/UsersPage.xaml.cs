using CommunityToolkit.WinUI.UI.Controls;
using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Reflection;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UsersPage : Page
    {
        Data.DataAccess dataAccess = new Data.DataAccess();
        private User EditingUser;
        public User NewUSer { get; set; }
        public ObservableCollection<User> UserList { get; set; }
        public ObservableCollection<User> FilteredUserList { get; set; } = new ObservableCollection<User>();


        public UsersPage()
        {
            this.InitializeComponent();
            UserList = new ObservableCollection<User>((IEnumerable<User>)dataAccess.GetUsers());
            FilterData();
            // Data binding for GridView
            DataContext = this;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        // Filtering logic
        private void FilterData()
        {
            string UserNameFilter = TxtUserName.Text?.ToLower();
            //  string TypeFilter = TypeSearchBox.Text?.ToLower();
            // DateTime DateFilter = Convert.ToDateTime(DateSearchBox.Date);

            // Clear previous filtered results
            if (FilteredUserList != null)
                FilteredUserList.Clear();

            // Filter the master list and add matching items to FilteredProcesss
            var filtered = UserList.Where(p =>
                (string.IsNullOrEmpty(UserNameFilter) || p.UserName.Contains(UserNameFilter, StringComparison.CurrentCultureIgnoreCase)));

            foreach (var user in filtered)
            {
                FilteredUserList.Add(user);
            }
        }
        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            NewUSer = new User
            {
                UserId = -1,
                FirstName = string.Empty,
                LastName = string.Empty,
                Email = string.Empty,
                UserName = string.Empty,
                Password = string.Empty,
                IsADUser = false,
                IsActive = false,
                IsAdmin = false,
                IsReadOnly = false
            };
            UserList.Add(NewUSer);
            FilteredUserList.Add(NewUSer);
            DgvUser.SelectedIndex = FilteredUserList.Count - 1;
            DgvUser.BeginEdit();
            if (DgvUser.ItemsSource is IList<User> items)
            {
                DgvUser.SelectedIndex = items.Count - 1;
                DgvUser.CurrentColumn = DgvUser.Columns[0];

                DgvUser.ScrollIntoView(items[DgvUser.SelectedIndex], DgvUser.Columns[0]);
            }
            //EditLastRow_Click(sender, e);
        }
        private void EditLastRow_Click(object sender, RoutedEventArgs e)
        {
            //// Get the index of the last item in the collection
            //int lastIndex = FilteredUserList.Count - 1;

            //// Check if there are any items in the DgvUser
            //if (lastIndex >= 0)
            //{
            //    // Select the last item in the DgvUser
            //    DgvUser.SelectedIndex = lastIndex;

            //    // Scroll to the last row to make it visible
            //    DgvUser.ScrollIntoView(DgvUser.SelectedItem, DgvUser.Columns[0]);

            //    // Set the current column (you can choose which column to focus on)
            //    DgvUser.CurrentColumn = DgvUser.Columns[0];  // Focus on the first column

            //    // Start editing the selected cell
            //    DgvUser.BeginEdit();
            //}
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ChbADGroup_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChbAdmin_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void chbActive_Checked(object sender, RoutedEventArgs e)
        {

        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Confirmation",
                Content = "Are you sure you want to delete this User?",
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
                var user = button?.Tag as User;
                if (user != null)
                {
                    var userDict = new Dictionary<string, object>() { { "@UserId", user.UserId } };
                    int id = 0;
                    if(dataAccess.ExecuteNonQuery($"DELETE FROM user WHERE user_id=@UserId", userDict, out id)==1) 
                        FilteredUserList.Remove(user);
                }
                
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<User> dbUserList = new ObservableCollection<User>((IEnumerable<User>)dataAccess.GetUsers());
            Dictionary<PropertyInfo, string> DbFields = new Dictionary<PropertyInfo, string>();
            foreach (User user in UserList)
            {
                if (user.UserId == -1)
                {
                    user.UserId = InsertUser(user);
                    user.IsReadOnly = true;
                }
                else
                {
                    foreach (User dbUser in dbUserList)
                    {
                        if (user.UserId == dbUser.UserId)
                        {
                            Dictionary<string, object> propsList = new Dictionary<string, object>();
                            GlobalClass.Instance.Equals(user, dbUser, out propsList);
                            propsList = UpdateFieldNames(propsList);
                            if (propsList.Count > 0)
                                dataAccess.UpdateRecord("user", "user_id", user.UserId, propsList);
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
                if (key == "FirstName")
                    editedProps.Add("first_name", props[key]);
                else if (key == "LastName")
                    editedProps.Add("last_name", props[key]);
                else if (key == "Email")
                    editedProps.Add("email", props[key]);
                else if (key == "UserName")
                    editedProps.Add("user_name", props[key]);
                else if (key == "Password")
                    editedProps.Add("password", props[key]);
                else if (key == "IsADUser")
                    editedProps.Add("ad_user", props[key]);
                else if (key == "IsActive")
                    editedProps.Add("active", props[key]);
            }
            return editedProps;
        }
        private int InsertUser(User user)
        {

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = string.Empty;
            parameters.Add("@FirstName", user.FirstName);
            parameters.Add("@LastName", user.LastName);
            parameters.Add("@Email", user.Email);
            parameters.Add("@UserName", user.UserName);
            parameters.Add("@Password", user.Password);
            parameters.Add("@ADUser", user.IsADUser);
            parameters.Add("@Active", user.IsActive);

            query = $"insert into user(first_name,last_name,email,user_name,password,ad_user,active)" +
                $"values(@FirstName,@LastName,@Email,@UserName,@Password,@ADUser,@Active)";
            int newUserId = 0;
            dataAccess.ExecuteNonQuery(query, parameters,out newUserId);
            return newUserId;
        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UserList = new ObservableCollection<User>((IEnumerable<User>)dataAccess.GetUsers());
            FilterData();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {

            var button = sender as Button;
            var user = button?.Tag as User;
            if (user != null)
            {
                // Make sure all rows are not in edit mode
                foreach (var u in UserList)
                {
                    u.IsReadOnly = true;
                }
                // Set the current user row to edit mode
                user.IsReadOnly = false;
            }
            //if (user != null)
            //{
            //    // Toggle the IsEditable state
            //    user.IsReadOnly = !user.IsReadOnly;
            //}

            EditingUser = user;
            //var button =sender as Button;

            //// Get the user object from the Tag property (or directly use User ID)
            //if (button?.Tag is User userToEdit)
            //{
            //    // Find the user by ID in the collection (LINQ)
            //    var user = UserList.FirstOrDefault(u => u.UserId == userToEdit.UserId);

            //    if (user != null)
            //    {
            //        // Make sure all rows are not in edit mode
            //        foreach (var u in UserList)
            //        {
            //            u.IsEditing = false;
            //        }

            //        // Set the current user row to edit mode
            //        user.IsEditing = true;
            //    }

            //}
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void FirstNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void dg_Editing(object sender, DataGridBeginningEditEventArgs e)
        {

        }

        private void dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            //if(e.EditAction==DataGridEditAction.Commit)
            //{
            //    DgvUser.CommitEdit(DataGridEditingUnit.Row, true);
            //}
        }

        private void dg_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {

        }

        private void ContentGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            //  var q = GDVUsers.Items[0];

            var button = sender as Button;
            // Get the user object from the Tag property (or directly use User ID)
            if (button?.Tag is User userToEdit)
            {
                // Find the user by ID in the collection (LINQ)
                var user = UserList.FirstOrDefault(u => u.UserId == userToEdit.UserId);

                if (user != null)
                {
                    // Make sure all rows are not in edit mode
                    foreach (var u in UserList)
                    {
                        u.IsReadOnly = true;
                    }
                    // Set the current user row to edit mode
                    user.IsReadOnly = false;
                }

            }
        }

        private void ContentGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // string g = GDVUsers.Items[0].ToString();

        }

        private void DgvUSer_CurrentCellChanged(object sender, EventArgs e)
        {
            DgvUser.CommitEdit();
            //var a =UserList[0].UserName;
        }

        private void DgvUser_RowEditEnded(object sender, DataGridRowEditEndedEventArgs e)
        {
            //if (EditingUser != null)
            //    EditingUser.IsReadOnly = true;
        }

        private void DgvUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditingUser != null && e.RemovedItems.Count > 0 && EditingUser.UserId == ((User)e.RemovedItems[0]).UserId)
                EditingUser.IsReadOnly = true;

        }
    }
    public class User : ObservableObject
    {
        private int userId;
        private UserGroup userGroup;
        private string firstName;
        private string lastName;
        private string email;
        private string userName;
        private string password;
        private bool isADUser;
        private bool isAdmin;
        private bool isActive;
        private bool isReadOnly = true;
        private bool isEnabled = false;

        public int UserId
        {
            get => userId;
            set => SetProperty(ref userId, value);
        }
        public UserGroup UserGroup
        {
            get => userGroup;
            set => SetProperty(ref userGroup, value);
        }
        public string FirstName
        {
            get => firstName;
            set => SetProperty(ref firstName, value);
        }
        public string LastName
        {
            get => lastName;
            set => SetProperty(ref lastName, value);
        }
        public string Email
        {
            get => email; set => SetProperty(ref email, value);
        }
        public string UserName
        {
            get => userName; set => SetProperty(ref userName, value);
        }
        public string Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }
        public bool IsADUser
        {
            get => isADUser;
            set => SetProperty(ref isADUser, value);
        }
        public bool IsAdmin
        {
            get => isAdmin;
            set => SetProperty(ref isAdmin, value);
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
    }


    //    if (FirstName != other.FirstName)
    //    props.Add("@first_name",firstName);
    //if(LastName != other.LastName)
    //    props.Add("@last_name", LastName);
    //if(email != other.email)
    //    props.Add("@email", email);
    //if(userName != other.userName)
    //    props.Add("@user_name", userName);
    //if(password != other.password)
    //    props.Add("@password", password);
    //if (IsADUSer != other.IsADUSer)
    //    props.Add("@ad_user",IsADUSer);
    //if (IsAdmin != other.IsAdmin)
    //    props.Add("@IsAdmin", IsAdmin);
    //if(IsActive != other.IsActive)
    //    props.Add("@active",IsActive);
    //if (other == null) return false;
    //    return props.Count>0;

    //}
    //}

}
