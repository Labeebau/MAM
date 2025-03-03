using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileServerPage : Page
	{
        public ObservableCollection<FileServer> FileServerList { get; set; }

        public FileServerPage()
        {
            this.InitializeComponent();
            FileServerList = new ObservableCollection<FileServer>
            {
                new FileServer { Name = "FS1",ComputerName="Sims",Domain=".",UserName="Labeeba",Password="123",FileFolder="D:\backup",ProxyFolder="D:\backup",ThumbnailFolder="D:\backup",Active=true},
            };

            //Data binding for GridView
            DataContext = this;
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ChbACtive_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void BtnProxyServer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnThumbnailServer_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnQCServer_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnFileServer_Click(object sender, RoutedEventArgs e)
        {

        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

    }
    public class FileServer
    {
        public string Name { get; set; }
        public string ComputerName { get; set; }
        public string Domain { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FileFolder { get; set; }
        public string ProxyFolder { get; set; }
        public string ThumbnailFolder { get; set; }
        public bool Active { get; set; }
    }
}
