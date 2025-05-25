using System.Collections.Generic;
using System.Windows.Media;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class NamedColorTests
    {
        [TestMethod]
        public void NamedColor_Properties_ShouldBeSetCorrectly()
        {
            var color = Colors.Red;
            var namedColor = new NamedColor { Name = "Red", Color = color };

            Assert.AreEqual("Red", namedColor.Name);
            Assert.AreEqual(color, namedColor.Color);
            Assert.AreEqual(color, namedColor.Brush.Color);
        }

        [TestMethod]
        public void NamedColor_Brush_ShouldBeValid()
        {
            var namedColor = new NamedColor { Color = Colors.Blue };

            Assert.IsNotNull(namedColor.Brush);
            Assert.AreEqual(Colors.Blue, namedColor.Brush.Color);
        }

        [TestMethod]
        public void NamedColor_ShouldHandleMultipleSystemColors()
        {
            NamedColor[] systemColors = [
                new() { Name = "Black", Color = Colors.Black },
                new() { Name = "White", Color = Colors.White },
                new() { Name = "Green", Color = Colors.Green }
            ];

            foreach (var color in systemColors)
            {
                Assert.IsNotNull(color.Brush);
                Assert.AreEqual(color.Color, color.Brush.Color);
            }
        }
    }
}
