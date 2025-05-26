using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.Core
{
    public static class ImageFormatBridge
    {
        public static Bitmap ToBitmap(BitmapSource bitmapSource)
        {
            using var memoryStream = new MemoryStream();
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            memoryStream.Position = 0;

            return new Bitmap(memoryStream);
        }

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using MemoryStream ms = new();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Makes it usable across threads

            return bitmapImage;
        }

        public static BitmapImage ToBitmapImage(Image<Rgba32> image)
        {
            using MemoryStream ms = new();

            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder
            {
                CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.BestCompression
            };

            image.Save(ms, encoder);

            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Makes it usable across threads

            return bitmapImage;
        }
    }
}
