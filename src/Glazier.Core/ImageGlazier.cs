using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

public class ImageGlazier
{
    private Image<Rgba32> _imageData;

    public Image<Rgba32> ImageData
    {
        get => _imageData;
        set => _imageData = value;
    }

    public void LoadImage(string filePath)
    {
        try
        {
            ImageData = Image.Load<Rgba32>(filePath);
            Console.WriteLine($"Image loaded successfully: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image: {ex.Message}");
        }
    }

    public void SaveImage(string outputPath)
    {
        if (_imageData != null)
        {
            _imageData.Save(outputPath);
            Console.WriteLine($"Image saved to: {outputPath}");
        }
        else
        {
            Console.WriteLine("No image data to save.");
        }
    }

    public Dictionary<Rgba32, int> GetMostCommonColors(int topColorsCount = 5)
    {
        var colorCounts = new Dictionary<Rgba32, int>();

        {
            for (int y = 0; y < this.ImageData.Height; y++)
            {
                for (int x = 0; x < this.ImageData.Width; x++)
                {
                    Rgba32 pixelColor = this.ImageData[x, y];

                    if (colorCounts.TryGetValue(pixelColor, out int value))
                        colorCounts[pixelColor] = ++value;
                    else
                        colorCounts[pixelColor] = 1;
                }
            }
        }

        // Sort colors by frequency and return the top results
        return colorCounts
            .OrderByDescending(kv => kv.Value)
            .Take(topColorsCount)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private bool ColorsAreClose(Rgba32 color1, Rgba32 color2, int tolerance)
    {
        return Math.Abs(color1.R - color2.R) <= tolerance &&
               Math.Abs(color1.G - color2.G) <= tolerance &&
               Math.Abs(color1.B - color2.B) <= tolerance &&
               Math.Abs(color1.A - color2.A) <= tolerance;
    }

    public void Glaze(System.Windows.Media.Color targetColor, int tolerance)
    {
        if (this.ImageData == null)
            return;

        // Convert System.Windows.Media.Color to ImageSharp's Rgba32
        Rgba32 targetRgba = new Rgba32(targetColor.R, targetColor.G, targetColor.B, targetColor.A);

        for (int y = 0; y < this.ImageData.Height; y++)
        {
            for (int x = 0; x < this.ImageData.Width; x++)
            {
                if (ColorsAreClose(this.ImageData[x, y], targetRgba, tolerance))
                {
                    this.ImageData[x, y] = new Rgba32(0, 0, 0, 0); // Fully transparent
                }
            }
        }
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
        return this.ImageData.Clone();
    }
}