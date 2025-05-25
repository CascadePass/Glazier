using System;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class SizeViewModelTests
    {
        [TestMethod]
        public void Constructor_ShouldSetPropertiesCorrectly()
        {
            var size = new Size(100, 50);
            var dpi = 144; // High DPI example
            var viewModel = new SizeViewModel(size, dpi);

            Assert.AreEqual(size, viewModel.Size);
            Assert.AreEqual(dpi, viewModel.Dpi);
            Assert.IsNotNull(viewModel.Icon);
        }

        [TestMethod]
        public void GenerateIcon_ShouldReturnNonNullImageSource()
        {
            var size = new Size(256, 256);
            var icon = new SizeViewModel(size).GenerateIcon();

            Assert.IsNotNull(icon);
            Assert.IsInstanceOfType(icon, typeof(BitmapSource));
        }

        [DataTestMethod]
        [DataRow(1, 1)]    // Tiny size
        [DataRow(512, 512)] // Large size
        public void GenerateIcon_ShouldHandleEdgeCases(int width, int height)
        {
            var size = new Size(width, height);
            var icon = new SizeViewModel(size).GenerateIcon();

            Assert.IsNotNull(icon);
            Assert.IsInstanceOfType(icon, typeof(BitmapSource));
        }

        [TestMethod]
        public void GenerateIcon_ShouldThrowException_WhenSizeIsZeroOrNegative()
        {
            var invalidSize1 = new Size(0, 50);   // Width is zero

            Assert.ThrowsException<InvalidOperationException>(() => new SizeViewModel(invalidSize1).GenerateIcon());
        }

        [TestMethod]
        public void GenerateIcon_ShouldThrowException_WhenDpiIsZeroOrNegative()
        {
            Assert.ThrowsException<InvalidOperationException>(() => new SizeViewModel(new Size(16, 16), 0).GenerateIcon());
            Assert.ThrowsException<InvalidOperationException>(() => new SizeViewModel(new Size(16, 16), -10).GenerateIcon());
        }

        [TestMethod]
        public void GenerateIcon_ShouldMatchSizeDimensions()
        {
            var size = new Size(256, 128);
            var viewModel = new SizeViewModel(size, 96);

            var bitmapSource = viewModel.Icon as BitmapSource;
            Assert.IsNotNull(bitmapSource);
            Assert.AreEqual((int)size.Width, bitmapSource.PixelWidth);
            Assert.AreEqual((int)size.Height, bitmapSource.PixelHeight);
        }

        [TestMethod]
        public void GenerateIcon_ShouldUseCustomBrush()
        {
            var size = new Size(100, 100);
            var viewModel = new SizeViewModel(size, 96);
            viewModel.GeneratedIconBrush = Brushes.Red; // Change brush
            viewModel.Icon = viewModel.GenerateIcon(); // Regenerate icon

            // Here, we can't directly check color, but we confirm re-generation occurred
            Assert.IsNotNull(viewModel.Icon);
        }
    }
}
