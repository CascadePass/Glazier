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
        public Image<Rgba32> ImageData { get; set; }

        public void LoadImage(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file {filePath} does not exist.", filePath);
            }

            try
            {
                ImageData = Image.Load<Rgba32>(filePath);
            }
            catch (Exception)
            {
            }
        }

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

        public async Task LoadImageAsync(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                this.ImageData = await Image.LoadAsync<Rgba32>(stream);
            }
            catch (Exception)
            {
            }
        }

        public Dictionary<Rgba32, int> GetMostCommonColors(int topColorsCount = 5)
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
            return Math.Abs(color1.R - color2.R) <= tolerance &&
                   Math.Abs(color1.G - color2.G) <= tolerance &&
                   Math.Abs(color1.B - color2.B) <= tolerance &&
                   Math.Abs(color1.A - color2.A) <= tolerance;
        }

        public void Glaze(Rgba32 targetRgba, int tolerance)
        {
            this.Glaze(targetRgba, new Rgba32(0, 0, 0, 0), tolerance);
        }

        public void Glaze(Rgba32 matchColor, Rgba32 replacementColor, int tolerance)
        {
            if (this.ImageData == null)
            {
                return;
            }

            if (this.ImageData.Height < 100 && this.ImageData.Width < 100)
            {
                this.GlazeSmallImage(matchColor, replacementColor, tolerance);
                return;
            }

            this.GlazeLargeImage(matchColor, replacementColor, tolerance);
        }

        internal void GlazeSmallImage(Rgba32 matchColor, Rgba32 replacementColor, int tolerance)
        {
            for (int y = 0; y < this.ImageData.Height; y++)
            {
                for (int x = 0; x < this.ImageData.Width; x++)
                {
                    if (ColorsAreClose(this.ImageData[x, y], matchColor, tolerance))
                    {
                        this.ImageData[x, y] = replacementColor;
                    }
                }
            }
        }

        internal void GlazeLargeImage(Rgba32 matchColor, Rgba32 replacementColor, int tolerance)
        {
            Parallel.For(0, this.ImageData.Height, y =>
            {
                for (int x = 0; x < this.ImageData.Width; x++)
                {
                    if (ColorsAreClose(this.ImageData[x, y], matchColor, tolerance))
                    {
                        this.ImageData[x, y] = replacementColor;
                    }
                }
            });
        }

        public BitmapSource ConvertToBitmapSource() => ImageGlazier.ConvertToBitmapSource(this.ImageData);

        public static BitmapSource ConvertToBitmapSource(Image<Rgba32> image)
        {
            using MemoryStream memoryStream = new();
            image.SaveAsPng(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = memoryStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // Makes it UI-thread safe

            return bitmap;
        }

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

            return ConvertToBitmapSource(image);
        }

        public Image<Rgba32> Clone()
        {
            if (this.ImageData is null)
            {
                throw new InvalidOperationException($"ImageData is null.");
            }

            return this.ImageData.Clone();
        }

        public void Dispose()
        {
            this.ImageData?.Dispose();
            this.ImageData = null;
        }
    }
}