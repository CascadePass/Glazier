using System;
using System.Globalization;
using System.Windows.Data;

namespace CascadePass.Glazier.UI
{
    public class GlazeMethodConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GlazeMethod selectedMethod && parameter is GlazeMethod radioMethod)
            {
                return selectedMethod == radioMethod;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is GlazeMethod radioMethod)
            {
                return radioMethod;
            }

            return Binding.DoNothing;
        }
    }
}