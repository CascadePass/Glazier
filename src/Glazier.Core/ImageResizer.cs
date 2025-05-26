using System;
using System.Drawing;

namespace CascadePass.Glazier.Core
{
    public class ImageResizer
    {
        public static Bitmap ResizeBitmap(Bitmap original, System.Windows.Size newSize)
        {
            return ImageResizer.ResizeBitmap(original, (int)newSize.Width, (int)newSize.Height);
        }

        public static Bitmap ResizeBitmap(Bitmap original, System.Drawing.Size newSize)
        {
            return ImageResizer.ResizeBitmap(original, (int)newSize.Width, (int)newSize.Height);
        }

        public static Bitmap ResizeBitmap(Bitmap original, int newWidth, int newHeight)
        {
            if (newWidth <= 0 || newHeight <= 0)
            {
                throw new ArgumentException("Invalid dimensions for resizing.");
            }

            Bitmap resizedBitmap = new(newWidth, newHeight);

            using Graphics graphics = Graphics.FromImage(resizedBitmap);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            graphics.DrawImage(original, 0, 0, newWidth, newHeight);

            return resizedBitmap;
        }
    }
}
