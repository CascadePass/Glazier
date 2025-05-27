using System;
using System.Globalization;
using System.Windows;

namespace CascadePass.Glazier.UI
{
    public class GlazeMethodToBoolConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GlazeMethod glazeMethod && parameter is GlazeMethod compareMethod)
            {
                return glazeMethod == compareMethod;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GlazeMethod glazeMethod && parameter is GlazeMethod compareMethod)
            {
                return glazeMethod == compareMethod;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
