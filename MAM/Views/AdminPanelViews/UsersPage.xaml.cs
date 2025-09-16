using CommunityToolkit.WinUI.UI.Controls;
using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
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
            this.Loading += UsersPage_LoadingAsync;

            // Data binding for GridView
            DataContext = this;
        }

        private async void UsersPage_LoadingAsync(FrameworkElement sender, object args)
        {
            UserList = new ObservableCollection<User>((IEnumerable<User>)(await dataAccess.GetUsers()));
            FilterData();

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
            var button = sender as Button;
            var user = button?.Tag as User;
            ContentDialogResult result = await GlobalClass.Instance.ShowDialogAsync($"Are you sure you want to delete {user.UserName}?", XamlRoot, "Delete", "Cancel", "Delete Confirmation");
            if (result == ContentDialogResult.Primary)
            {
               
                if (user != null)
                {
                    var userDict = new List<MySqlParameter>{new MySqlParameter( "@UserId", user.UserId ) };
                    var (affectedRows, id, errorMessage) = await dataAccess.ExecuteNonQuery($"DELETE FROM user WHERE user_id=@UserId", userDict);
                    if (affectedRows == 1)
                    {
                        FilteredUserList.Remove(user);
                        await GlobalClass.Instance.AddtoHistoryAsync("Delete User", $"User '{user.UserName}' deleted.");
                    }
                    else
                        await GlobalClass.Instance.ShowDialogAsync($"Can't Delete {user.UserName}\n {errorMessage}", this.XamlRoot);
                }
                
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<User> dbUserList = new ObservableCollection<User>((IEnumerable<User>)dataAccess.GetUsers());
            Dictionary<PropertyInfo, string> DbFields = new Dictionary<PropertyInfo, string>();
            foreach (User user in UserList)
            {
                if (user.UserId == -1)
                {
                    user.UserId =await InsertUser(user);
                    user.IsReadOnly = true;
                }
                else
                {
                    foreach (User dbUser in dbUserList)
                    {
                        if (user.UserId == dbUser.UserId)
                        {
                            Dictionary<string, object> propsList = new Dictionary<string, object>();
                            GlobalClass.Instance.CompareProperties(user, dbUser, out propsList);
                            propsList = UpdateFieldNames(propsList);
                            if (propsList.Count > 0)
                               await dataAccess.UpdateRecord("user", "user_id", user.UserId, propsList);
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
        //private async Task<int> InsertUser(User user)
        //{
        //    var(hashedPassword,salt)= PasswordHelper.HashPassword(user.Password);
        //    List<MySqlParameter> parameters = new ();
        //    string query = string.Empty;
        //    parameters.Add(new MySqlParameter("@FirstName", user.FirstName));
        //    parameters.Add(new MySqlParameter("@LastName", user.LastName));
        //    parameters.Add(new MySqlParameter("@Email", user.Email));
        //    parameters.Add(new MySqlParameter("@UserName", user.UserName));
        //    parameters.Add(new MySqlParameter("@PasswordHash", hashedPassword));
        //    parameters.Add(new MySqlParameter("@PasswordSalt", salt));
        //    parameters.Add(new MySqlParameter("@ADUser", user.IsADUser));
        //    parameters.Add(new MySqlParameter("@Active", user.IsActive));
        //    query = $"insert into user(first_name,last_name,email,user_name,password_hash,password_salt,ad_user,active)" +
        //        $"values(@FirstName,@LastName,@Email,@UserName,@PasswordHash,@PasswordSalt,@ADUser,@Active)";
        //    var (affectedRows, newUserId, errorCode) = await dataAccess.ExecuteNonQuery(query, parameters);
        //    var params = new Dictionary<string, object>
        //            {
        //                { "@UserId", user.UserId },
        //                { "@Action", "Login" },
        //                { "@Description", "User logged in" }
        //            };
        //    dataAccess.ExecuteStoredProcedure("InsertUserAction", params);
        //    return affectedRows>0? newUserId:-1;
        //}

        private async Task<int> InsertUser(User user)
        {
            // Hash the password
            var (hashedPassword, salt) = PasswordHelper.HashPassword(user.Password);
            // Prepare parameters for INSERT
            List<MySqlParameter> parameters = new()
            {
                new MySqlParameter("@FirstName", user.FirstName),
                new MySqlParameter("@LastName", user.LastName),
                new MySqlParameter("@Email", user.Email),
                new MySqlParameter("@UserName", user.UserName),
                new MySqlParameter("@PasswordHash", hashedPassword),
                new MySqlParameter("@PasswordSalt", salt),
                new MySqlParameter("@ADUser", user.IsADUser),
                new MySqlParameter("@Active", user.IsActive)
            };

            string query = "INSERT INTO user (first_name, last_name, email, user_name, password_hash, password_salt, ad_user, active) " +
                           "VALUES (@FirstName, @LastName, @Email, @UserName, @PasswordHash, @PasswordSalt, @ADUser, @Active)";

            // Execute insert
            var (affectedRows, newUserId, errorCode) = await dataAccess.ExecuteNonQuery(query, parameters);

            if (affectedRows > 0)
            {
                // Update user object with new ID
                user.UserId = newUserId;
                await GlobalClass.Instance.AddtoHistoryAsync("Create User", $"New user '{user.UserName}' created.");
                return newUserId;
            }
            return -1;
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
            EditingUser = user;
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
        private Right userRighr;
        public int UserId
        {
            get => userId;
            set => SetProperty(ref userId, value);
        }
        //public UserGroup UserGroup
        //{
        //    get => userGroup;
        //    set => SetProperty(ref userGroup, value);
        //}
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
        public Right UserRight
        {
            get => userRighr;
            set => SetProperty(ref userRighr, value);
        }
    }


   

}
