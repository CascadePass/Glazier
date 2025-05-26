using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.Core
{
    public class IconFileExporter
    {
        private BitmapImage source;

        public IconFileExporter()
        {
            this.IconSizes = [16, 32, 64, 128, 256];
        }

        public List<int> IconSizes { get; set; }

        public bool CompatibilityMode { get; set; }

        public BitmapImage SourceImage {
            get => this.source;
            set
            {
                if (this.source != value)
                {
                    this.source = value;
                    this.InferSizes();
                }
            }
        }

        public void Save(string outputPath)
        {
            if (this.SourceImage is null)
            {
                throw new InvalidOperationException();
            }

            using var stream = new FileStream(outputPath, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            // ICO file header
            writer.Write((short)0); // Reserved
            writer.Write((short)1); // ICO Type
            writer.Write((short)this.IconSizes.Count); // Number of images

            var imageDataOffsets = new List<long>();
            foreach (var size in this.IconSizes)
            {
                var resizedBitmap = this.DownsampleImage(size, size);
                var iconData = this.CompatibilityMode ? this.ConvertToBmp(resizedBitmap) : this.ConvertToPng(resizedBitmap);

                // Write icon directory entry
                writer.Write((byte)size); // Width
                writer.Write((byte)size); // Height
                writer.Write((byte)0); // Color Palette (0 = No palette)
                writer.Write((byte)0); // Reserved
                writer.Write((short)1); // Color planes
                writer.Write((short)32); // Bits per pixel
                writer.Write(iconData.Length); // Image data size
                imageDataOffsets.Add(writer.BaseStream.Position);
                writer.Write(0); // Placeholder for image data offset
            }

            // Write image data and update offsets
            for (int i = 0; i < this.IconSizes.Count(); i++)
            {
                var resizedBitmap = this.DownsampleImage(this.IconSizes.ElementAt(i), this.IconSizes.ElementAt(i));
                var iconData = this.CompatibilityMode ? this.ConvertToBmp(resizedBitmap) : this.ConvertToPng(resizedBitmap);

                long currentPosition = writer.BaseStream.Position;
                writer.Seek((int)imageDataOffsets[i], SeekOrigin.Begin);
                writer.Write((int)currentPosition);
                writer.Seek(0, SeekOrigin.End);

                writer.Write(iconData);
            }
        }

        internal BitmapSource DownsampleImage(int width, int height)
        {
            if (width > this.SourceImage.PixelWidth || height > this.SourceImage.PixelHeight)
            {
                return this.SourceImage;
            }

            if (width == this.SourceImage.PixelWidth && height == this.SourceImage.PixelHeight)
            {
                return this.SourceImage;
            }

            var transform = new ScaleTransform(width / (double)this.SourceImage.PixelWidth, height / (double)this.SourceImage.PixelHeight);
            var resized = new TransformedBitmap(this.SourceImage, transform);
            RenderOptions.SetBitmapScalingMode(resized, BitmapScalingMode.HighQuality);
            return resized;
        }

        internal void InferSizes()
        {
            var remove = this.IconSizes.Where(x => x > this.SourceImage.PixelWidth || x > this.SourceImage.PixelHeight).ToList();

            foreach (var size in remove)
            {
                this.IconSizes.Remove(size);
            }
        }

        internal byte[] ConvertToPng(BitmapSource bitmap)
        {
            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder { Interlace = PngInterlaceOption.On };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
            return stream.ToArray();
        }

        internal byte[] ConvertToBmp(BitmapSource bitmap)
        {
            using var stream = new MemoryStream();
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
            return stream.ToArray();
        }
    }
}