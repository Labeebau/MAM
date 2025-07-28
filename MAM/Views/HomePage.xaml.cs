using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
        }
        private List<Tuple<string, FontFamily>> _fonts = new List<Tuple<string, FontFamily>>()
        {
            new Tuple<string, FontFamily>("Test1", new FontFamily("Arial")),
            new Tuple<string, FontFamily>("Test2", new FontFamily("Comic Sans MS")),
            new Tuple<string, FontFamily>("Test3", new FontFamily("Courier New")),
            new Tuple<string, FontFamily>("Test4", new FontFamily("Segoe UI")),
        };

        public List<Tuple<string, FontFamily>> Fonts
        {
            get { return _fonts; }
        }
        private void SystmAdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow.ShowWindow();
        }

        private void LsbProjects_ItemClick(object sender, ItemClickEventArgs e)
        {
            App.MainAppWindow.Mainframe.Navigate(typeof(MainProjectPage));  // Navigate to HomePage
        }

    }
}
