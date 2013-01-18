using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace AudioPlay.Converter
{
    class ConvertColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetColor((float)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private SolidColorBrush GetColor(float value)
        {
            Color color = new Color();
            color.A = 200;
            color.R = (byte)(value * 255);
            color.B = (byte)((1 - value) * 255);

            return new SolidColorBrush(color);
        }
    }
}
