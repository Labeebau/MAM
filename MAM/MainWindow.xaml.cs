using MAM.Data;
using MAM.Views;
using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;  // enable custom titlebar
            SetTitleBar(AppTitleBar);
            //MainFrame.Navigate(typeof(HomePage));
            MainFrame.Navigate(typeof(MainProjectPage));

            //if(ShowSystemAdminPanel)
            //{
            //    ProjectPanel .Visibility = Visibility.Collapsed;
            //}
            SizeChanged += MainWindow_SizeChanged;
            
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
             Logout();
        }
        private async void Logout()
        { 
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Activate();
            this.Close();
            await GlobalClass.Instance.AddtoHistoryAsync("Logout", "User logged out", GlobalClass.Instance.CurrentUser.UserId);
        }
    }


}
