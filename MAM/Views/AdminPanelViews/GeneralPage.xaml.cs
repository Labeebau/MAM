using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeneralPage : Page
	{
		public GeneralPage()
		{
			this.InitializeComponent();
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

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
