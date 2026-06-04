using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TrackStop.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLiked = (bool)value;

            // Яркий чистый красный без прозрачности
            return isLiked
                ? new SolidColorBrush(Color.FromRgb(255, 0, 0)) // 🔴 чистый красный
                : new SolidColorBrush(Color.FromRgb(180, 180, 180)); // Серый для неактивного
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
