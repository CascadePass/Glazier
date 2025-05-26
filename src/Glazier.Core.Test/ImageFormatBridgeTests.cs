using CascadePass.Glazier.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class ImageFormatBridgeTests
    {
        [TestMethod]
        public void TestBitmapToBitmapImage()
        {
            Bitmap bmp = new Bitmap(100, 100); // Simple blank bitmap
            BitmapImage result = ImageFormatBridge.ToBitmapImage(bmp);

            Assert.IsNotNull(result);
            Assert.AreEqual(100, result.PixelWidth);
            Assert.AreEqual(100, result.PixelHeight);
        }

        [TestMethod]
        public void TestImageSharpToBitmapImage()
        {
            using Image<Rgba32> image = new(100, 100);
            BitmapImage result = ImageFormatBridge.ToBitmapImage(image);

            Assert.IsNotNull(result);
            Assert.AreEqual(100, result.PixelWidth);
            Assert.AreEqual(100, result.PixelHeight);
        }

        [TestMethod]
        public void TestBitmapSourceToBitmapImage()
        {
            BitmapSource bitmapSource = BitmapSource.Create(100, 100, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, new byte[100 * 100 * 4], 100 * 4);
            Bitmap result = ImageFormatBridge.ToBitmap(bitmapSource);

            Assert.IsNotNull(result);
            Assert.AreEqual(100, result.Width);
            Assert.AreEqual(100, result.Height);
        }
    }
}