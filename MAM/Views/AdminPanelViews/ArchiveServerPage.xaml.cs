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
    public sealed partial class ArchiveServerPage : Page
	{
        public ObservableCollection<ArchiveServer> ArchiveServerList { get; set; }

        public ArchiveServerPage()
        {
            this.InitializeComponent();
            ArchiveServerList = new ObservableCollection<ArchiveServer>
            {
                new ArchiveServer { Type = "Network Storage",Name="Archive Test", ComputerName="Sims",UserName="Admin",Password="Admin",ArchivePath="D:/Backup", Active=true},
               // new ArchiveServer { Type = "Network Storage",Name="Archive Test", ComputerName="Sims",UserName="Admin",Password="Admin",ArchivePath="D:/Backup", Active=true},

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
       
        private void Add_Click(object sender, RoutedEventArgs e)
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
    public class ArchiveServer
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ArchivePath { get; set; }
        public string LTOServerId { get; set; }
        public string LTOServer { get; set; }
        public bool Active { get; set; }
    }
}
