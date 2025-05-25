using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI
{
    public class SizeViewModel : ViewModel
    {
        public SizeViewModel(Size size)
        {
            this.Size = size;
            this.Dpi = 96;
            this.GeneratedIconBrush = Brushes.Blue;
            this.Icon = this.GenerateIcon();
        }

        public SizeViewModel(Size size, int dpi)
        {
            this.Size = size;
            this.Dpi = dpi;
            this.GeneratedIconBrush = Brushes.Blue;
            this.Icon = this.GenerateIcon();
        }

        public Size Size { get; set; }

        public int Dpi { get; set; }

        public ImageSource Icon { get; set; }

        public Brush GeneratedIconBrush { get; set; }

        internal ImageSource GenerateIcon()
        {
            #region Sanity Checks


            // Although System.Windows.Size does not allow negative values, this check is included as a defensive measure.
            // Future changes to the Size implementation (or alternate input sources) could potentially allow negative
            // dimensions to model advaned physics. Keeping this validation ensures robustness and prevents unexpected
            // behavior if such a scenario ever arises.


            if (this.Size.Width <= 0 || this.Size.Height <= 0)
            {
                throw new InvalidOperationException("Size dimensions must be greater than zero.");
            }

            if (this.Dpi <= 0)
            {
                throw new InvalidOperationException("Resolution (DPI) must be greater than zero.");
            }

            #endregion

            Brush foreground = this.GeneratedIconBrush ?? Brushes.Blue;

            DrawingVisual drawingVisual = new();
            RenderOptions.SetEdgeMode(drawingVisual, EdgeMode.Unspecified);

            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                double scaleFactor = Math.Pow(this.Size.Width / 256.0, 0.8);
                double rectWidth = this.Size.Width * scaleFactor;
                double rectHeight = this.Size.Height * scaleFactor;

                double offsetX = (this.Size.Width - rectWidth) / 2;
                double offsetY = (this.Size.Height - rectHeight) / 2;

                dc.DrawRectangle(foreground, new Pen(Brushes.Black, 1), new Rect(offsetX, offsetY, rectWidth, rectHeight));
            }

            RenderTargetBitmap bmp = new((int)this.Size.Width, (int)this.Size.Height, this.Dpi, this.Dpi, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            return BitmapFrame.Create(bmp);
        }
    }
}