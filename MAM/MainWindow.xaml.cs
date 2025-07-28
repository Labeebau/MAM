using MAM.Data;
using MAM.Views;
using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Org.BouncyCastle.Asn1.Crmf;
using MAM.UserControls;
using MAM.Views.AdminPanelViews;
using MAM.Utilities;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public Frame Mainframe => this.MainFrame;

        public bool ShowSystemAdminPanel = false;
        public StatusBarControl StatusBar => AppStatusBar;
        public bool IsLoggingOut { get;  set; } = false;

        public MainWindow()
        {
            File.AppendAllText("log.txt", "MainWindow initializing.\n");

            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;  // enable custom titlebar
            SetTitleBar(AppTitleBar);
            //MainFrame.Navigate(typeof(HomePage));
            MainFrame.Navigate(typeof(MainProjectPage));
            
            SizeChanged += MainWindow_SizeChanged;
            Closed += MainWindow_Closed;
            File.AppendAllText("log.txt", "MainWindow initialized.\n");
            //MainAppWindow.Loaded += MainAppWindow_Loaded;
        }

        //private void MainAppWindow_Loaded(object sender, RoutedEventArgs e)
        //{
        //    LoadServers();
        //}

        
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if(!IsLoggingOut) 
                Application.Current.Exit(); 
        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            MenuPanel.Width=args.Size.Width;
        }

        public string CurrentUser
        {
            get { return (string)BtnLogout.Content; }
            set {BtnLogout.Content="Logout ("+value+")" ; }
        }


        private void ListBox2_Loaded(object sender, RoutedEventArgs e)
        {
            //LsbProjects.SelectedIndex = 2;
        }

        private  void BtnLogOut_Click(object sender, RoutedEventArgs e)
        {
            IsLoggingOut = true;
            Logout();
        }
        private async void Logout()
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Activate();
            await GlobalClass.Instance.AddtoHistoryAsync("Logout", "User logged out", GlobalClass.Instance.CurrentUser.UserId);
            UIThreadHelper.RunOnUIThread(() => { AppStatusBar.ShowStatus("Logging out...", true); });
            this.Close();
           
        }
    }


}
