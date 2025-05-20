#region Using directives

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#endregion

namespace CascadePass.Glazier.UI
{
    public class SizeViewModel : ViewModel
    {
        public SizeViewModel(Size size)
        {
            this.Size = size;
            this.Icon = SizeViewModel.GenerateIcon(size);
        }

        public Size Size { get; set; }

        public ImageSource Icon { get; set; }

        internal static ImageSource GenerateIcon(Size size)
        {
            DrawingVisual drawingVisual = new();

            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                // Use an exponential scaling factor to enhance size differentiation
                double scaleFactor = Math.Pow(size.Width / 256.0, 0.8); // Adjust exponent for stronger effect
                double rectWidth = size.Width * scaleFactor;
                double rectHeight = size.Height * scaleFactor;

                double offsetX = (size.Width - rectWidth) / 2;
                double offsetY = (size.Height - rectHeight) / 2;

                dc.DrawRectangle(Brushes.Blue, new Pen(Brushes.Black, 1), new Rect(offsetX, offsetY, rectWidth, rectHeight));
            }

            RenderTargetBitmap bmp = new((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            return BitmapFrame.Create(bmp);
        }
    }
}
