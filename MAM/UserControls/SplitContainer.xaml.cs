using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.UserControls
{
    public sealed partial class SplitContainer : UserControl
    {
        private bool _isDragging;
        private double _originalSplitterPosition;
        public SplitContainer()
        {
            this.InitializeComponent();
        }
        private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Adjust RightPanel width based on the splitter's position
            RightPanel.Width = MainCanvas.ActualWidth - Canvas.GetLeft(VerticalSplitter) - VerticalSplitter.Width;
        }

        private void Splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Start dragging
            _isDragging = true;
            _originalSplitterPosition = e.GetCurrentPoint(MainCanvas).Position.X;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
        }

        private void Splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                // Calculate the new position of the splitter
                double currentPosition = e.GetCurrentPoint(MainCanvas).Position.X;
                double delta = currentPosition - _originalSplitterPosition;

                // Get the new left position for the splitter
                double newLeft = Canvas.GetLeft(VerticalSplitter) + delta;

                // Set the splitter within bounds (ensuring left and right panels have a minimum width)
                if (newLeft > 100 && newLeft < MainCanvas.ActualWidth - 100)
                {
                    Canvas.SetLeft(VerticalSplitter, newLeft);
                    Canvas.SetLeft(RightPanel, newLeft + VerticalSplitter.Width);
                    LeftPanel.Width = newLeft;
                    RightPanel.Width = MainCanvas.ActualWidth - newLeft - VerticalSplitter.Width;

                    _originalSplitterPosition = currentPosition;
                }
            }
        }

        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // Stop dragging
            _isDragging = false;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);

        }
    }
}
