using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM.UserControls
{
    public class CustomSlider : Slider
    {
        public Thumb? SliderThumb { get; private set; }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Try to find the thumb
            SliderThumb = GetTemplateChild("HorizontalThumb") as Thumb;
            if (SliderThumb != null)
            {
                SliderThumb.DragStarted += Thumb_DragStarted;
                SliderThumb.DragCompleted += Thumb_DragCompleted;
            }
        }

        public bool IsUserDragging { get; private set; } = false;

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            IsUserDragging = true;
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsUserDragging = false;
        }
    }
}
