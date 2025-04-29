using MAM.Data;
using MAM.Views.AdminPanelViews;
using MAM.Windows;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
//using MAM.Windows;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        /// 
        public static Microsoft.UI.Dispatching.DispatcherQueue UIDispatcherQueue { get; private set; }

        public App()
		{
			this.InitializeComponent();
			SetThemeBasedOnSystem();
#if DEBUG
            this.DebugSettings.BindingFailed += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine(args.Message);
            };
#endif
            UIDispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        }
        public static MainWindow MainAppWindow { get;  set; }
        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{
            //m_window = new TestWindow();
            ////m_window = new MainWindow();
            ////m_window = new ProjectWindow();
            //m_window.Activate();


            // Store reference to MainWindow
            //MainAppWindow = new MainWindow();
            //MainAppWindow.Activate();
            bool isFirstLaunch = SettingsService.Get<bool>("IsFirstLaunch", defaultValue: true);

            if (isFirstLaunch)
            {
                MainAppWindow = new MainWindow();
                MainAppWindow.Activate();
                // Show DB setup page
                MainAppWindow.Mainframe.Navigate(typeof(GeneralPage), args.Arguments);
                //SettingsService.Set("IsFirstLaunch", false);
            }
            else
            {
                var connectionString = SettingsService.Get<string>("ConnectionString");
                DataAccess.ConnectionString = EncryptionHelper.Decrypt(connectionString);

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Activate();
            }
		}

		private Window m_window;
		private Page m_page;
		private void SetThemeBasedOnSystem()
		{
			//var uiSettings = new Windows.UI.ViewManagement.UISettings();
			//var background = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);

			//this.RequestedTheme = background == Colors.White ? ApplicationTheme.Light : ApplicationTheme.Dark;
		}

	}
}
