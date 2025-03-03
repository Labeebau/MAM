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
    public sealed partial class RSSListPage : Page
	{
        public ObservableCollection<Rss> RssList { get; set; }
        public RSSListPage()
        {
            this.InitializeComponent();
            RssList = new ObservableCollection<Rss>
            {
                new Rss { Title = "DHA RSS",Description="DHA Haber",Link="http://192.168.1.193.9000" },
                new Rss { Title = "DHA RSS",Description="DHA Haber",Link="http://192.168.1.193.9000" },
                new Rss { Title = "DHA RSS",Description="DHA Haber",Link="http://192.168.1.193.9000" },
                new Rss { Title = "DHA RSS",Description="DHA Haber",Link="http://192.168.1.193.9000" },
                new Rss { Title = "DHA RSS",Description="DHA Haber",Link="http://192.168.1.193.9000" }

            };

            // Data binding for GridView
            DataContext = this;
        }
        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is EmailList clickedItem)
            {
                // Handle item click
                var itemName = clickedItem.Email;
                // Do something with the itemName
            }
        }
        private void DeleteGroup_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button deleteButton && deleteButton.Tag is Rss itemToDelete)
            {
                // Remove the item from the collection
                RssList.Remove(itemToDelete);
            }
        }

        private void ADGroup_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Acive_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void Add_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
        private void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
        private void AddUSer_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
        private void SaveButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
        private void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }

        private void Copy_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
        private void Edit_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            RightGrid.Visibility = Visibility.Visible;

        }
        private void Delete_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
        private void DeleteUserGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton && deleteButton.Tag is Rss itemToDelete)
            {
                // Remove the item from the collection
                RssList.Remove(itemToDelete);
            }
        }
    }
    // Item model class
    public class Rss
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }

    }
  
}
