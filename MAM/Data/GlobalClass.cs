using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.Marshalling;
using MAM.Views.AdminPanelViews;
using System.Data;
using System.Text.RegularExpressions;
using Windows.System;
using User = MAM.Views.AdminPanelViews.User;

namespace MAM.Data
{
   
    public class GlobalClass
    {
        #region Global Data
        private DataAccess dataAccess = new DataAccess();
        // Step 1: Private static instance of the same class
        private static GlobalClass _instance = null;

        // Lock synchronization object for thread safety
        private static readonly object _lock = new object();

        // Step 2: Private constructor to prevent direct instantiation
        private GlobalClass()
        {
            // Initialize default values
           // CurrentUserName = "admin";
            AppTheme = "Dark";
        }

        // Step 3: Public static property to provide global access to the instance
        public static GlobalClass Instance
        {
            get
            {
                // Ensure thread-safety
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GlobalClass();
                    }
                    return _instance;
                }
            }
        }

        // Step 4: Define properties for global data
        //public string CurrentUserName { get; set; } = string.Empty;
        //public int CurrentUserId { get; set; } = 0;
        //public int CurrentUserGroupId { get; set; } =0 ;
        //public string CurrentUserGroupName { get; set; } = string.Empty;
        public User CurrentUser { get; set; }
        public UserGroup CurrentUserGroup{ get; set; }
        public bool IsAdmin { get; set; } = false;

        public string AppTheme { get; set; }
        public string Language { get; set; }

        // You can add more properties as needed, like:
        public int SessionTimeout { get; set; }
        public bool IsLoggedIn { get; set; }
        public string MediaLibraryPath { get; set; } = @"F:\MAM\Data";
        public string ArchivePath { get; set; } = @"F:\MAM\Archive";
        public string DownloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

        public string ffmpegPath = @"C:\Program Files\ffmpeg\ffmpeg-2024-12-04-git-2f95bc3cb3-full_build\bin\ffmpeg.exe";
        public string ffprobePath = @"C:\Program Files\ffmpeg\ffmpeg-2024-12-04-git-2f95bc3cb3-full_build\bin\ffprobe.exe";


        //public List<string> SupportedFiles = new List<string> { ".avi", ".mpeg", ".mpg", ".mov", ".mkv", ".mp4", ".mp3", ".flac", ".wav", ".aac", ".ogg", ".jpeg", ".jpg", ".png", ".gif", ".tiff", ".bmp", ".svg" };
        public List<string> SupportedVideos = new List<string> { ".avi", ".mpeg", ".mpg", ".mov", ".mkv", ".mp4" ,".wmv"};
        public List<string> SupportedAudios = new List<string> { ".mp3", ".flac",".wma", ".wav", ".aac", ".ogg" };
        public List<string> SupportedImages = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".tiff", ".bmp", ".svg",".webp" };
        public List<string> SupportedDocuments = new List<string> { ".txt", ".rtf", ".doc", ".docx", ".csv", ".pdf", ".xls", ".xlsx",".html",".ppt", ".pptx",".odt" };
        public List<string> SupportedFiles => SupportedVideos
    .Concat(SupportedAudios)
    .Concat(SupportedImages)
    .Concat(SupportedDocuments)
    .ToList();
        public ObservableCollection<string> RecentFiles = new ObservableCollection<string>();
        #endregion Global Data
        #region Global Methods
        public User GetUserRights(int userId)
        {
            DataTable dt = new DataTable();
            User NewUserWithRights;
            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@UserId", userId } };
            dt = dataAccess.GetData("SELECT u.user_id, u.user_name,ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions FROM user u " +
                                    "left join user_roles ur on u.user_id = ur.user_id " +
                                    "left join user_group ug on ur.group_id = ug.group_id " +
                                    "left JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "left JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    "WHERE u.user_id = @UserId "+
                                    "group by ug.group_id, u.user_name " +
                                    "order by ug.group_id, u.user_name ",parameters);

            //foreach (DataRow row in dt.Rows)
            //{
                DataRow row= dt.Rows[0];
                if (row.ItemArray!=null&& !string.IsNullOrEmpty(row["user_name"].ToString()))
                {
                    NewUserWithRights = new User();
                    NewUserWithRights.UserRight = new Right();
                    NewUserWithRights.UserId = Convert.ToInt32(row["user_id"]);
                    NewUserWithRights.UserName =row["user_name"].ToString();
                    NewUserWithRights.UserGroup = new UserGroup();
                    NewUserWithRights.UserGroup.GroupName = row["group_name"].ToString();
                    if (row["permissions"].ToString().Contains("Read"))
                        NewUserWithRights.UserRight.Read = true;
                    if (row["permissions"].ToString().Contains("Write"))
                        NewUserWithRights.UserRight.Write = true;
                    if (row["permissions"].ToString().Contains("Edit"))
                        NewUserWithRights.UserRight.Edit = true;
                    if (row["permissions"].ToString().Contains("Delete"))
                        NewUserWithRights.UserRight.Delete = true;
                    if (row["permissions"].ToString().Contains("OrgDownload"))
                        NewUserWithRights.UserRight.OrgDownload = true;
                    if (row["permissions"].ToString().Contains("PrxDownload"))
                        NewUserWithRights.UserRight.PrxDownload = true;
                    if (row["permissions"].ToString().Contains("Archive"))
                        NewUserWithRights.UserRight.Archive = true;
                    if (row["permissions"].ToString().Contains("Broadcast"))
                        NewUserWithRights.UserRight.Broadcast = true;
                    return NewUserWithRights;
                }
                else
                     return null; 
        }
        public UserGroup GetUserWithRights(int groupId)
        {
            DataTable dt = new DataTable();
            UserGroup NewUserGroup;
            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@GroupId", groupId } };

            dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
                                    "FROM user_group ug " +
                                    "LEFT JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "LEFT JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    $"WHERE ug.group_id=@GroupId");

            DataRow row = dt.Rows[0];
            if (!string.IsNullOrEmpty(row["group_name"].ToString()))
            {
                NewUserGroup = new UserGroup();
                NewUserGroup.UserGroupId = Convert.ToInt32(row["group_id"]);
                NewUserGroup.GroupName = row["group_name"].ToString();
                if (row["permissions"].ToString().Contains("Read"))
                    NewUserGroup.UserGroupRight.Read = true;
                if (row["permissions"].ToString().Contains("Write"))
                    NewUserGroup.UserGroupRight.Write = true;
                if (row["permissions"].ToString().Contains("Edit"))
                    NewUserGroup.UserGroupRight.Edit = true;
                if (row["permissions"].ToString().Contains("Delete"))
                    NewUserGroup.UserGroupRight.Delete = true;
                if (row["permissions"].ToString().Contains("OrgDownload"))
                    NewUserGroup.UserGroupRight.OrgDownload = true;
                if (row["permissions"].ToString().Contains("PrxDownload"))
                    NewUserGroup.UserGroupRight.PrxDownload = true;
                if (row["permissions"].ToString().Contains("Archive"))
                    NewUserGroup.UserGroupRight.Archive = true;
                if (row["permissions"].ToString().Contains("Broadcast"))
                    NewUserGroup.UserGroupRight.Broadcast = true;
                return NewUserGroup;
            }
            else
                return null;
        }
        public UserGroup GetUserGroupWithRights(int groupId)
        {
            DataTable dt = new DataTable();
            UserGroup NewUserGroup;
            dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
                                    "FROM user_group ug " +
                                    "LEFT JOIN group_permissions gp ON ug.group_id = gp.group_id " +
                                    "LEFT JOIN permissions p ON gp.permission_id = p.permission_id " +
                                    $"WHERE ug.group_id={groupId}");

            DataRow row = dt.Rows[0];
            if (!string.IsNullOrEmpty(row["group_name"].ToString()))
            {
                NewUserGroup = new UserGroup();
                NewUserGroup.UserGroupRight = new Right();
                NewUserGroup.UserGroupId = Convert.ToInt32(row["group_id"]);
                NewUserGroup.GroupName = row["group_name"].ToString();
                if (row["permissions"].ToString().Contains("Read"))
                    NewUserGroup.UserGroupRight.Read = true;
                if (row["permissions"].ToString().Contains("Write"))
                    NewUserGroup.UserGroupRight.Write = true;
                if (row["permissions"].ToString().Contains("Edit"))
                    NewUserGroup.UserGroupRight.Edit = true;
                if (row["permissions"].ToString().Contains("Delete"))
                    NewUserGroup.UserGroupRight.Delete = true;
                if (row["permissions"].ToString().Contains("OrgDownload"))
                    NewUserGroup.UserGroupRight.OrgDownload = true;
                if (row["permissions"].ToString().Contains("PrxDownload"))
                    NewUserGroup.UserGroupRight.PrxDownload = true;
                if (row["permissions"].ToString().Contains("Archive"))
                    NewUserGroup.UserGroupRight.Archive = true;
                if (row["permissions"].ToString().Contains("Broadcast"))
                    NewUserGroup.UserGroupRight.Broadcast = true;
                return NewUserGroup;
            }
            else
                return null;
        }
        // Helper method for showing ContentDialog
        public async Task ShowErrorDialogAsync(string message, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }
        //public void ShowErrorDialog(string message,XamlRoot xamlRoot)
        //{
        //    ContentDialog errorDialog = new ContentDialog
        //    {
        //        Title = "Error",
        //        Content = message,
        //        CloseButtonText = "OK",
        //        XamlRoot = xamlRoot // Set the XamlRoot to ensure proper display
        //    };

        //    errorDialog.ShowAsync();
        //}
        public void DisableMaximizeButton(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false; // Disable maximize button
            }
        }
        public void SetWindowSizeAndPosition(int width, int height,Window window)
        {
            // Get the native window handle of the current window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Get the window ID from the handle
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            // Retrieve the AppWindow using the static method GetFromWindowId
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // Resize the window to the specified size
                appWindow.Resize(new SizeInt32(width, height));

                // Get the screen size
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                var workArea = displayArea.WorkArea;

                // Calculate the center position
                int centerX = (workArea.Width - width) / 2;
                int centerY = (workArea.Height - height) / 2;

                // Move the window to the center of the screen
                appWindow.Move(new PointInt32(centerX, centerY));
            }
        }
        public bool Equals(object obj1,object obj2, out Dictionary<string, object> props)
        {
            props = new Dictionary<string, object>();
            if (obj1 == null || obj2 == null)
                return obj1 == obj2;

            if (obj1.GetType() != obj2.GetType())
                return false;
           
            foreach (PropertyInfo userProperty in obj1.GetType().GetProperties())
            {
                if ((userProperty.Name== "IsReadOnly") || (userProperty.Name == "IsEnabled"))
                    continue;
                var value1 = userProperty.GetValue(obj1);
                var value2 = userProperty.GetValue(obj2);
                if (!object.Equals(value1, value2))
                    props.Add($"{userProperty.Name}", value1);
            }
            return props.Count > 0;
        }
        public async Task AddtoHistoryAsync(string action, string description,int userId=0)
        { 
            if(userId==0)
             userId = GlobalClass.Instance.CurrentUser.UserId;
            var parameters = new Dictionary<string, object>
                    {
                        { "user_id", userId },
                        { "action", action },
                        { "description", description }
                    };

            await dataAccess.ExecuteStoredProcedure("InsertUserAction", parameters);
        }
        #endregion Global Methods
    }


}
