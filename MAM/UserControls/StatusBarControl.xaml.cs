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

namespace MAM.UserControls
{
    public sealed partial class StatusBarControl : UserControl
    {
        public StatusBarControl()
        {
            this.InitializeComponent();
        }
        public void ShowStatus(string message, bool showProgress = false)
        {
            StatusText.Text = message;
            StatusRing.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
            StatusRing.IsActive = showProgress;
            this.Visibility = Visibility.Visible;
        }
        //public void ShowStatus(string message, bool showProgress=false)
        //{
        //    if (!DispatcherQueue.HasThreadAccess)
        //    {
        //        DispatcherQueue.TryEnqueue(() => ShowStatus(message, showProgress));
        //        return;
        //    }

        //    StatusText.Text = message;
        //    StatusRing.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
        //}


        public void HideStatus()
        {
            StatusRing.IsActive = false;
            this.Visibility = Visibility.Collapsed;
        }

    }
}
