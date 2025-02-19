using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class TargetLocationsPage : Page
	{
        public ObservableCollection<TargetLocations> TargetLocationsList { get; set; }

        public TargetLocationsPage()
        {
            this.InitializeComponent();
            TargetLocationsList = new ObservableCollection<TargetLocations>
            {
                new TargetLocations { Name="Archive Test", ComputerName="Sims",NetworkPath="D:/Backup",LocalPath="D:/Backup" },
               // new TargetLocations { Type = "Network Storage",Name="Archive Test", ComputerName="Sims",UserName="Admin",Password="Admin",ArchivePath="D:/Backup", Active=true},

            };

            //Data binding for GridView
            DataContext = this;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {

        }
        private void AuthorizeButton_Click(object sender, RoutedEventArgs e)
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
        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnAddUserGroup_Click(object sender, RoutedEventArgs e)
        {

        }
        
    }
    public class TargetLocations
    {
        public string ComputerName { get; set; }
        public string Name { get; set; }
        public string NetworkPath { get; set; }
        public string LocalPath { get; set; }
    }
}
