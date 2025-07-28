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
    public sealed partial class CustomThumb : UserControl
    {
        public CustomThumb()
        {
            this.InitializeComponent();
            this.ManipulationMode = ManipulationModes.TranslateX; // Allow horizontal dragging
            this.ManipulationDelta += OnManipulationDelta;
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Get the parent container
            var parent = this.Parent as FrameworkElement;
            if (parent == null) return;

            // Calculate the new position
            double newLeft = Canvas.GetLeft(this) + e.Delta.Translation.X;

            // Constrain the thumb within the parent bounds
            if (newLeft >= 0 && newLeft + this.Width <= parent.ActualWidth)
            {
                Canvas.SetLeft(this, newLeft);
            }
        }
    }
}
