﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CascadePass.Glazier.UI
{
    public class IsNullOrWhitespaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
