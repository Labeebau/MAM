using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM.Utilities.Converters
{
    public class PageIndexToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int selectedPage && parameter is int thisPage)
            {
                return selectedPage + 1 == thisPage
                    ? new SolidColorBrush(Colors.Blue)
                    : new SolidColorBrush(Colors.Black);
            }

            return new SolidColorBrush(Colors.Gray); // Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }



}


