using System.Drawing;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class IconFileExporterTests
    {
        [TestMethod]
        public void DownsampleImage_Should_Resize_Correctly()
        {
            IconFileExporter exporter = new();
            exporter.SourceImage = GenerateTestImage(256, 256);

            BitmapSource resized = exporter.DownsampleImage(32, 32);

            Assert.AreEqual(32, resized.PixelWidth);
            Assert.AreEqual(32, resized.PixelHeight);
        }

        [TestMethod]
        public void ConvertToBmp_Should_Create_Valid_BMP()
        {
            IconFileExporter exporter = new();
            exporter.SourceImage = GenerateTestImage(64, 64);

            byte[] bmpData = exporter.ConvertToBmp(exporter.SourceImage);

            Assert.IsNotNull(bmpData);
            Assert.IsTrue(bmpData.Length > 0);
        }

        [TestMethod]
        public void ConvertToPng_Should_Create_Valid_PNG()
        {
            IconFileExporter exporter = new();
            exporter.SourceImage = GenerateTestImage(64, 64);

            byte[] pngData = exporter.ConvertToPng(exporter.SourceImage);

            Assert.IsNotNull(pngData);
            Assert.IsTrue(pngData.Length > 0);
        }

        private static BitmapImage GenerateTestImage(int width, int height)
        {
            using Bitmap bitmap = new(width, height);
            BitmapImage image = ImageFormatBridge.ToBitmapImage(bitmap);

            return image;
        }
    }
}
