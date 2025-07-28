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
    public sealed partial class MailSettingsPage : Page
	{
		public ObservableCollection<EmailList> Emails { get; set; }
		public MailSettingsPage()
		{
			this.InitializeComponent();
			// Sample data for GridView
			Emails = new ObservableCollection<EmailList>
			{
				new EmailList { Email = "sample1@example.com" },
				new EmailList { Email = "sample2@example.com"},
				new EmailList { Email = "sample3@example.com" },
				new EmailList { Email = "sample4@example.com" },

                new EmailList { Email = "sample1@example.com" },
                new EmailList { Email = "sample2@example.com"},
                new EmailList { Email = "sample3@example.com" },
                new EmailList { Email = "sample4@example.com" }

            };

			// Data binding for GridView
			DataContext = this;

		}
		private void RevealModeCheckbox_Changed(object sender, RoutedEventArgs e)
		{
			if (revealModeCheckBox.IsChecked == true)
			{
				passworBoxWithRevealmode.PasswordRevealMode = PasswordRevealMode.Visible;
			}
			else
			{
				passworBoxWithRevealmode.PasswordRevealMode = PasswordRevealMode.Hidden;
			}
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
		// Event handler for Delete button click
		private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			if (sender is Button deleteButton && deleteButton.Tag is EmailList itemToDelete)
			{
				// Remove the item from the collection
				Emails.Remove(itemToDelete);
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{

		}

        private void AddEmail_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    // Item model class
    public class EmailList
	{
		public string Email { get; set; }
	//	public string ImagePath { get; set; }
	}
}
