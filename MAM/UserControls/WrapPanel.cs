using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System;
using Windows.Foundation;
namespace MAM.UserControls
{
    [ContentProperty(Name = nameof(Children))]
    public class WrapPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double lineWidth = 0;
            double lineHeight = 0;
            double totalWidth = 0;
            double totalHeight = 0;

            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
                Size desiredSize = child.DesiredSize;

                if (lineWidth + desiredSize.Width > availableSize.Width)
                {
                    totalWidth = Math.Max(lineWidth, totalWidth);
                    totalHeight += lineHeight;

                    lineWidth = desiredSize.Width;
                    lineHeight = desiredSize.Height;
                }
                else
                {
                    lineWidth += desiredSize.Width;
                    lineHeight = Math.Max(lineHeight, desiredSize.Height);
                }
            }

            totalWidth = Math.Max(lineWidth, totalWidth);
            totalHeight += lineHeight;

            return new Size(totalWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double x = 0;
            double y = 0;
            double lineHeight = 0;

            foreach (UIElement child in Children)
            {
                Size desiredSize = child.DesiredSize;

                if (x + desiredSize.Width > finalSize.Width)
                {
                    x = 0;
                    y += lineHeight;
                    lineHeight = 0;
                }

                child.Arrange(new Rect(new Point(x, y), desiredSize));

                x += desiredSize.Width;
                lineHeight = Math.Max(lineHeight, desiredSize.Height);
            }

            return finalSize;
        }
    }
}
