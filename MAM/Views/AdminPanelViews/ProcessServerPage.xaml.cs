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
    public sealed partial class ProcessServerPage : Page
    {
        public ObservableCollection<ProcessServer> ProcessServerList { get; set; }

        public ProcessServerPage()
        {
            this.InitializeComponent();
            ProcessServerList = new ObservableCollection<ProcessServer>
            {
                new ProcessServer { ServerType = "Proxy Server",ComputerName="Sims",MaxThreadCount=5,Perfomance="High",LastAccess=DateTime.Now,CPUUsage=0,JobCount=0,Active=true},
            };

            //Data binding for GridView
            DataContext = this;
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
    public class ProcessServer
    {
        public string ServerType { get; set; }
        public string ComputerName { get; set; }
        public int MaxThreadCount { get; set; }
        public string Perfomance { get; set; }
        public DateTime LastAccess { get; set; }
        public int CPUUsage { get; set; }
        public int JobCount { get; set; }
        public bool Active { get; set; }
    }
}
