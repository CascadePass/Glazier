using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class ColorToBrushConverterTests
    {
        [TestMethod]
        public void Convert_ShouldReturnSolidColorBrush_WhenInputIsColor()
        {
            var color = Color.FromArgb(255, 128, 64, 32);

            var result = new ColorToBrushConverter().Convert(color, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            Assert.IsInstanceOfType(result, typeof(SolidColorBrush));
            var brush = (SolidColorBrush)result;
            Assert.AreEqual(color, brush.Color);
        }

        [TestMethod]
        public void Convert_ShouldReturnUnsetValue_WhenInputIsNotColor()
        {
            var invalidInput = "NotAColor";

            var result = new ColorToBrushConverter().Convert(invalidInput, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

            Assert.AreEqual(DependencyProperty.UnsetValue, result);
        }

        [TestMethod]
        public void ConvertBack_ShouldReturnColor_WhenInputIsSolidColorBrush()
        {
            var brush = new SolidColorBrush(Color.FromArgb(255, 128, 64, 32));

            var result = new ColorToBrushConverter().ConvertBack(brush, typeof(Color), null, CultureInfo.InvariantCulture);

            Assert.IsInstanceOfType(result, typeof(Color));
            var color = (Color)result;
            Assert.AreEqual(brush.Color, color);
        }

        [TestMethod]
        public void ConvertBack_ShouldReturnUnsetValue_WhenInputIsNotSolidColorBrush()
        {
            var invalidInput = "NotABrush";

            var result = new ColorToBrushConverter().ConvertBack(invalidInput, typeof(Color), null, CultureInfo.InvariantCulture);

            Assert.AreEqual(DependencyProperty.UnsetValue, result);
        }
    }
}
