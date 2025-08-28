using CommunityToolkit.WinUI;
using MAM.Views.AdminPanelViews;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.Security.Policy;
using Windows.Graphics;
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
        //public string ConnectionString { get; set; }

        public bool IsFirstLaunch { get; set; }
        public User CurrentUser { get; set; }
        public UserGroup CurrentUserGroup { get; set; }
        public bool IsAdmin { get; set; } = false;

        public string AppTheme { get; set; }
        public string Language { get; set; }

        // You can add more properties as needed, like:
        public int SessionTimeout { get; set; }
        public bool IsLoggedIn { get; set; }
        public string MediaLibraryPath { get; set; }
        public string ArchivePath { get; set; }
        public string ProxyFolder { get; set; }
        public string ThumbnailFolder { get; set; }
        public string RecycleFolder { get; set; }
        public  FileServer FileServer { get; set; }

        public string DownloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public string ffmpegPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Assets", "ffmpeg", "ffmpeg.exe");
        public string ffprobePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Assets", "ffmpeg", "ffprobe.exe");



        //public List<string> SupportedFiles = new List<string> { ".avi", ".mpeg", ".mpg", ".mov", ".mkv", ".mp4", ".mp3", ".flac", ".wav", ".aac", ".ogg", ".jpeg", ".jpg", ".png", ".gif", ".tiff", ".bmp", ".svg" };
        public List<string> SupportedVideos = new List<string> { ".avi", ".mpeg", ".mpg", ".mov", ".mkv", ".mp4", ".wmv" };
        public List<string> SupportedAudios = new List<string> { ".mp3", ".flac", ".wma", ".wav", ".aac", ".ogg" };
        public List<string> SupportedImages = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".tiff", ".bmp", ".svg", ".webp" };
        public List<string> SupportedDocuments = new List<string> { ".txt", ".rtf", ".doc", ".docx", ".csv", ".pdf", ".xls", ".xlsx", ".html", ".ppt", ".pptx", ".odt" };
        public List<string> SupportedFiles => SupportedVideos
    .Concat(SupportedAudios)
    .Concat(SupportedImages)
    .Concat(SupportedDocuments)
    .ToList();
        public ObservableCollection<string> RecentFiles = new ObservableCollection<string>();
        #endregion Global Data
        #region Global Methods
        public async Task<User> GetUserRightsAsync(int userId)
        {
            var parameters = new List<MySqlParameter> { new("in_user_id", userId) };

            using var reader = await dataAccess.ExecuteReaderStoredProcedure("GetUserEffectivePermissions", parameters);

            if (reader != null)
            {
                var newUserRight = new User
                {
                    UserId = userId,
                    UserRight = new Right()
                };

                while (await reader.ReadAsync())
                {
                    string permission = reader["permission_name"].ToString();

                    if (permission.Contains("Read")) newUserRight.UserRight.Read = true;
                    if (permission.Contains("Write")) newUserRight.UserRight.Write = true;
                    if (permission.Contains("Edit")) newUserRight.UserRight.Edit = true;
                    if (permission.Contains("Delete")) newUserRight.UserRight.Delete = true;
                    if (permission.Contains("OriginalDownload")) newUserRight.UserRight.OriginalDownload = true;
                    if (permission.Contains("ProxyDownload")) newUserRight.UserRight.ProxyDownload = true;
                    if (permission.Contains("Archive")) newUserRight.UserRight.Archive = true;
                    if (permission.Contains("Broadcast")) newUserRight.UserRight.Broadcast = true;
                }
                return newUserRight;
            }
            return null;
        }
        //public User GetUserRights(int userId)
        //{
        //    DataTable dt = new DataTable();
        //    User NewUserWithRights;
        //    Dictionary<string, object> parameters = new Dictionary<string, object> { { "@UserId", userId } };
        //    dt = dataAccess.GetData("SELECT u.user_id, u.user_name,ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions FROM user u " +
        //                            "left join user_roles ur on u.user_id = ur.user_id " +
        //                            "left join user_group ug on ur.group_id = ug.group_id " +
        //                            "left JOIN group_permissions gp ON ug.group_id = gp.group_id " +
        //                            "left JOIN permissions p ON gp.permission_id = p.permission_id " +
        //                            "WHERE u.user_id = @UserId " +
        //                            "group by ug.group_id, u.user_name " +
        //                            "order by ug.group_id, u.user_name ", parameters);

        //    //foreach (DataRow row in dt.Rows)
        //    //{
        //    DataRow row = dt.Rows[0];
        //    if (row.ItemArray != null && !string.IsNullOrEmpty(row["user_name"].ToString()))
        //    {
        //        NewUserWithRights = new User();
        //        NewUserWithRights.UserRight = new Right();
        //        NewUserWithRights.UserId = Convert.ToInt32(row["user_id"]);
        //        NewUserWithRights.UserName = row["user_name"].ToString();
        //        //NewUserWithRights.UserGroup = new UserGroup();
        //        //NewUserWithRights.UserGroup.GroupName = row["group_name"].ToString();
        //        if (row["permissions"].ToString().Contains("Read"))
        //            NewUserWithRights.UserRight.Read = true;
        //        if (row["permissions"].ToString().Contains("Write"))
        //            NewUserWithRights.UserRight.Write = true;
        //        if (row["permissions"].ToString().Contains("Edit"))
        //            NewUserWithRights.UserRight.Edit = true;
        //        if (row["permissions"].ToString().Contains("Delete"))
        //            NewUserWithRights.UserRight.Delete = true;
        //        if (row["permissions"].ToString().Contains("OrgDownload"))
        //            NewUserWithRights.UserRight.OriginalDownload = true;
        //        if (row["permissions"].ToString().Contains("PrxDownload"))
        //            NewUserWithRights.UserRight.ProxyDownload = true;
        //        if (row["permissions"].ToString().Contains("Archive"))
        //            NewUserWithRights.UserRight.Archive = true;
        //        if (row["permissions"].ToString().Contains("Broadcast"))
        //            NewUserWithRights.UserRight.Broadcast = true;
        //        return NewUserWithRights;
        //    }
        //    else
        //        return null;
        //}
        //public UserGroup GetUserWithRights(int groupId)
        //{
        //    DataTable dt = new DataTable();
        //    UserGroup NewUserGroup;
        //    Dictionary<string, object> parameters = new Dictionary<string, object> { { "@GroupId", groupId } };

        //    dt = dataAccess.GetData("SELECT ug.group_id, ug.group_name,GROUP_CONCAT(p.permission_name ORDER BY p.permission_name SEPARATOR ', ') AS permissions " +
        //                            "FROM user_group ug " +
        //                            "LEFT JOIN group_permissions gp ON ug.group_id = gp.group_id " +
        //                            "LEFT JOIN permissions p ON gp.permission_id = p.permission_id " +
        //                            $"WHERE ug.group_id=@GroupId");

        //    DataRow row = dt.Rows[0];
        //    if (!string.IsNullOrEmpty(row["group_name"].ToString()))
        //    {
        //        NewUserGroup = new UserGroup();
        //        NewUserGroup.UserGroupId = Convert.ToInt32(row["group_id"]);
        //        NewUserGroup.GroupName = row["group_name"].ToString();
        //        if (row["permissions"].ToString().Contains("Read"))
        //            NewUserGroup.UserGroupRight.Read = true;
        //        if (row["permissions"].ToString().Contains("Write"))
        //            NewUserGroup.UserGroupRight.Write = true;
        //        if (row["permissions"].ToString().Contains("Edit"))
        //            NewUserGroup.UserGroupRight.Edit = true;
        //        if (row["permissions"].ToString().Contains("Delete"))
        //            NewUserGroup.UserGroupRight.Delete = true;
        //        if (row["permissions"].ToString().Contains("OrgDownload"))
        //            NewUserGroup.UserGroupRight.OriginalDownload = true;
        //        if (row["permissions"].ToString().Contains("PrxDownload"))
        //            NewUserGroup.UserGroupRight.ProxyDownload = true;
        //        if (row["permissions"].ToString().Contains("Archive"))
        //            NewUserGroup.UserGroupRight.Archive = true;
        //        if (row["permissions"].ToString().Contains("Broadcast"))
        //            NewUserGroup.UserGroupRight.Broadcast = true;
        //        return NewUserGroup;
        //    }
        //    else
        //        return null;
        //}
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
                    NewUserGroup.UserGroupRight.OriginalDownload = true;
                if (row["permissions"].ToString().Contains("PrxDownload"))
                    NewUserGroup.UserGroupRight.ProxyDownload = true;
                if (row["permissions"].ToString().Contains("Archive"))
                    NewUserGroup.UserGroupRight.Archive = true;
                if (row["permissions"].ToString().Contains("Broadcast"))
                    NewUserGroup.UserGroupRight.Broadcast = true;
                return NewUserGroup;
            }
            else
                return null;
        }
        // Helper methods for showing ContentDialog
        private static readonly SemaphoreSlim _dialogLock = new(1, 1);
        //It ensures that dialogs are queued one at a time(instead of multiple dialogs being opened simultaneously), avoiding the COMException.
        public async Task ShowDialogAsync(string message, XamlRoot xamlRoot)
        {
            await _dialogLock.WaitAsync();
            try
            {
                    var dialog = new ContentDialog
                    {
                        Content = message,
                        CloseButtonText = "OK",
                        XamlRoot = xamlRoot
                    };

                    await dialog.ShowAsync();
            }
            finally
            {
                _dialogLock.Release();
            }
        }
        public async Task ShowDialogAsync(string message, XamlRoot xamlRoot, string title = "Error!!!")
        {
            await _dialogLock.WaitAsync();
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = xamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                _dialogLock.Release();
            }
        }
        public async Task<ContentDialogResult> ShowDialogAsync(string message, XamlRoot xamlRoot, string okText = "OK", string cancelText = "Cancel", string title = "")
        {
            await _dialogLock.WaitAsync();
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = message,
                    PrimaryButtonText = okText,
                    CloseButtonText = cancelText,
                    XamlRoot = xamlRoot
                };
                return await dialog.ShowAsync();
            }
            finally
            {
                _dialogLock.Release();
            }
        }

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
        public void SetWindowSizeAndPosition(int width, int height, Window window)
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
        public bool CompareProperties(object obj1, object obj2, out Dictionary<string, object> props)
        {
            props = new Dictionary<string, object>();
            if (obj1 == null || obj2 == null)
                return obj1 == obj2;

            if (obj1.GetType() != obj2.GetType())
                return false;

            foreach (PropertyInfo prop in obj1.GetType().GetProperties())
            {
                if (prop.Name == "IsReadOnly" || prop.Name == "IsEnabled")
                    continue;

                var val1 = prop.GetValue(obj1);
                var val2 = prop.GetValue(obj2);

                if (!object.Equals(val1, val2))
                    props[prop.Name] = val1;
            }
            return props.Count > 0;
        }

        public async Task<FileServer> GetFileServer(XamlRoot xamlRoot)
        {
            FileServer newFileServer = new();
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select * from file_server where active=1; ");
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    newFileServer = new FileServer();
                    newFileServer.ServerId = Convert.ToInt32(row["server_Id"]);
                    newFileServer.ServerName = row["server_name"].ToString();
                    newFileServer.Domain = row["domain"].ToString();
                    newFileServer.UserName = row["user_name"].ToString();
                    newFileServer.Password = row["password"].ToString();
                    newFileServer.FileFolder = row["file_folder"].ToString();
                    newFileServer.ProxyFolder = row["proxy_folder"].ToString();
                    newFileServer.ThumbnailFolder = row["thumbnail_folder"].ToString();
                    newFileServer.RecycleFolder = row["recycle_folder"].ToString();
                    newFileServer.Active = Convert.ToBoolean(row["active"].ToString());
                }
                MediaLibraryPath = Path.Combine(newFileServer.ServerName, newFileServer.FileFolder);
                FileServer = newFileServer;
                ProxyFolder = newFileServer.ProxyFolder;
                ThumbnailFolder = newFileServer.ThumbnailFolder;
                RecycleFolder = newFileServer.RecycleFolder;
                return newFileServer;
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync("No active file server found.", xamlRoot);
                return null;
            }
        }

        public async Task<ArchiveServer> GetArchiveServer(XamlRoot xamlRoot)
        {
            ArchiveServer newArchiveServer = new();
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select * from archive_server where active=1; ");
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    newArchiveServer = new ArchiveServer();
                    newArchiveServer.ServerId = Convert.ToInt32(row["server_Id"]);
                    newArchiveServer.ServerType = row["server_type"].ToString();
                    newArchiveServer.ServerName = row["server_name"].ToString();
                    newArchiveServer.ComputerName = row["computer_name"].ToString();
                    newArchiveServer.UserName = row["user_name"].ToString();
                    newArchiveServer.Password = row["password"].ToString();
                    newArchiveServer.ArchivePath = row["archive_path"].ToString();
                    newArchiveServer.LTOServerId = Convert.ToInt32(row["lto_server_id"]);
                    newArchiveServer.LTOServer = row["lto_server"].ToString();
                    newArchiveServer.Active = Convert.ToBoolean(row["active"].ToString());
                }
                ArchivePath = Path.Combine(newArchiveServer.ServerName, newArchiveServer.ArchivePath);
                return newArchiveServer;
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync("No active archive server found.", xamlRoot);
                return null;

            }
        }
        public async Task AddtoHistoryAsync(string action, string description, int userId = 0)
        {
            if (userId == 0)
                userId = GlobalClass.Instance.CurrentUser.UserId;
            var parameters = new Dictionary<string, object>
                    {
                        { "user_id", userId },
                        { "action", action },
                        { "description", description }
                    };

            await dataAccess.ExecuteNonQueryStoredProcedure("InsertUserAction", parameters);
        }
        #endregion Global Methods
    }


}
