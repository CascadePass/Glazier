using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class ColorSimilarityTests
    {
        #region ColorsAreClose

        [TestMethod]
        public void ColorsAreClose_ShouldReturnTrue_WhenColorsAreIdentical()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(255, 0, 0, 255);
            var color2 = new Rgba32(255, 0, 0, 255);

            var result = glazier.ColorsAreClose(color1, color2, 0);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnFalse_WhenColorsAreCompletelyDifferent()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(255, 0, 0, 255);
            var color2 = new Rgba32(0, 255, 0, 255);

            var result = glazier.ColorsAreClose(color1, color2, 0);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnTrue_WhenColorsAreWithinTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(105, 105, 105, 255);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnFalse_WhenColorsExceedTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(110, 110, 110, 255);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnTrue_WhenAlphaDifferenceIsWithinTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 250);
            var color2 = new Rgba32(100, 100, 100, 245);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnFalse_WhenAlphaDifferenceExceedsTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 250);
            var color2 = new Rgba32(100, 100, 100, 240);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnTrue_WhenOnlyOneChannelDiffersWithinTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(105, 100, 100, 255);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnFalse_WhenOnlyOneChannelDiffersExceedingTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(110, 100, 100, 255);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnTrue_WhenAllChannelsDifferWithinTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(105, 105, 105, 250);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnFalse_WhenAnyChannelExceedsTolerance()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(105, 105, 110, 255);

            var result = glazier.ColorsAreClose(color1, color2, 5);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnTrue_WhenToleranceIsHighEnough()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(200, 200, 200, 255);

            var result = glazier.ColorsAreClose(color1, color2, 150);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ColorsAreClose_ShouldReturnFalse_WhenToleranceIsZero()
        {
            var glazier = new ImageGlazier();
            var color1 = new Rgba32(100, 100, 100, 255);
            var color2 = new Rgba32(101, 100, 100, 255);

            var result = glazier.ColorsAreClose(color1, color2, 0);

            Assert.IsFalse(result);
        }

        #endregion
    }
}
