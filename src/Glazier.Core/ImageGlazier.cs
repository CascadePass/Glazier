using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.Core
{
    public class ImageGlazier : IDisposable
    {
        #region Properties

        public Image<Rgba32> ImageData { get; set; }

        public Image<Rgba32> Mask { get; set; }

        #endregion

        #region Methods

        #region LoadImage

        public void LoadImage(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file {filePath} does not exist.", filePath);
            }

            try
            {
                this.ImageData = Image.Load<Rgba32>(filePath);
            }
            catch (Exception)
            {
            }
        }

        public async Task LoadImageAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file {filePath} does not exist.", filePath);
            }

            try
            {
                using var stream = File.OpenRead(filePath);
                this.ImageData = await Image.LoadAsync<Rgba32>(stream);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region SaveImage

        public void SaveImage(string outputPath)
        {
            if (this.ImageData is null)
            {
                throw new InvalidOperationException("There is no image data to save.");
            }

            try
            {
                this.ImageData.Save(outputPath);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        public Dictionary<Rgba32, int> GetMostCommonColors(int topColorsCount)
        {
            var colorCounts = new ConcurrentDictionary<Rgba32, int>();

            Parallel.For(0, this.ImageData.Height, y =>
            {
                for (int x = 0; x < this.ImageData.Width; x++)
                {
                    Rgba32 pixelColor = this.ImageData[x, y];
                    colorCounts.AddOrUpdate(pixelColor, 1, (_, count) => count + 1);
                }
            });

            return colorCounts
                .OrderByDescending(kv => kv.Value)
                .Take(topColorsCount)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public bool ColorsAreClose(Rgba32 color1, Rgba32 color2, int tolerance)
        {
            int diffR = color1.R - color2.R;
            int diffG = color1.G - color2.G;
            int diffB = color1.B - color2.B;
            int diffA = color1.A - color2.A;

            return (diffR * diffR + diffG * diffG + diffB * diffB + diffA * diffA) <= (tolerance * tolerance);
        }

        #region Glaze

        public void Glaze(Rgba32 targetRgba, int tolerance)
        {
            #region Sanity Checks

            if (this.ImageData is null)
            {
                throw new InvalidOperationException("There is no image data to glaze.");
            }

            if (tolerance < 0 || tolerance > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 255.");
            }

            #endregion

            this.Mask = this.GenerateMask(targetRgba, tolerance);
            this.Glaze(targetRgba, this.Mask);
        }

        public void Glaze(Rgba32 targetRgba, Image<Rgba32> mask)
        {
            #region Sanity Checks

            if (this.ImageData is null)
            {
                throw new InvalidOperationException("There is no image data to glaze.");
            }

            if (mask is null)
            {
                throw new ArgumentException("The provided mask is null.");
            }

            if (mask.Width != this.ImageData.Width || mask.Height != this.ImageData.Height)
            {
                throw new ArgumentException("The provided mask must have the same dimensions as the image data.");
            }

            #endregion

            this.ApplyMask(mask);
        }

        #endregion

        #region Mask

        public Image<Rgba32> GenerateMask(Rgba32 backgroundColor, int tolerance)
        {
            Image<Rgba32> mask = new(this.ImageData.Width, this.ImageData.Height);

            for (int y = 0; y < ImageData.Height; y++)
            {
                for (int x = 0; x < ImageData.Width; x++)
                {
                    bool isBackground = this.ColorsAreClose(this.ImageData[x, y], backgroundColor, tolerance);
                    mask[x, y] = isBackground ? new Rgba32(0, 0, 0, 0) : new Rgba32(255, 255, 255, 255);
                }
            }

            return mask;
        }

        public void ApplyMask(Image<Rgba32> mask)
        {
            if (this.ImageData == null)
            {
                return;
            }

            for (int y = 0; y < ImageData.Height; y++)
            {
                for (int x = 0; x < ImageData.Width; x++)
                {
                    if (mask[x, y].PackedValue == 0)
                    {
                        // Preserve original color data, but turn alpha chanel fully transparent.
                        // This process is reversible, just set alpha to 255.

                        var old = this.ImageData[x, y];
                        this.ImageData[x, y] = new Rgba32(old.R, old.G, old.B, 0);
                    }
                }
            }
        }

        #endregion

        public static BitmapSource GenerateColorRangeImageSource(Rgba32 targetColor, int tolerance, int imageWidth = 400, int imageHeight = 100)
        {
            using Image<Rgba32> image = new(imageWidth, imageHeight);
            int steps = imageWidth / (tolerance * 2 + 1);

            for (int i = 0; i < tolerance * 2 + 1; i++)
            {
                int r = Math.Clamp(targetColor.R + (i - tolerance), 0, 255);
                int g = Math.Clamp(targetColor.G + (i - tolerance), 0, 255);
                int b = Math.Clamp(targetColor.B + (i - tolerance), 0, 255);
                Rgba32 variationColor = new((byte)r, (byte)g, (byte)b, 255);

                image.Mutate(ctx => ctx.Fill(variationColor, new Rectangle(i * steps, 0, steps, imageHeight)));
            }

            return ImageFormatBridge.ToBitmapImage(image);
        }

        public void Dispose()
        {
            if (this.ImageData is not null)
            {
                this.ImageData.Dispose();
                this.ImageData = null;
            }
        }

        #endregion
    }
}