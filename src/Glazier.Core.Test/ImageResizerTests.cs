using System;
using System.Drawing;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class ImageResizerTests
    {
        private Bitmap CreateTestBitmap(int width, int height)
        {
            Bitmap bitmap = new(width, height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red); // Fill with a solid color for basic validation
            }

            return bitmap;
        }

        [TestMethod]
        public void ResizeBitmap_ResizesCorrectly()
        {
            Bitmap original = this.CreateTestBitmap(100, 100);
            Bitmap resized = ImageResizer.ResizeBitmap(original, 50, 50);

            Assert.IsNotNull(resized, "Resized bitmap should not be null.");
            Assert.AreEqual(50, resized.Width, "Width should be resized correctly.");
            Assert.AreEqual(50, resized.Height, "Height should be resized correctly.");
        }

        [TestMethod]
        public void ResizeBitmap_DrawingOverload_ResizesCorrectly()
        {
            System.Drawing.Size size = new(50, 50);
            Bitmap original = this.CreateTestBitmap(100, 100);
            Bitmap resized = ImageResizer.ResizeBitmap(original, size);

            Assert.IsNotNull(resized, "Resized bitmap should not be null.");
            Assert.AreEqual(50, resized.Width, "Width should be resized correctly.");
            Assert.AreEqual(50, resized.Height, "Height should be resized correctly.");
        }

        [TestMethod]
        public void ResizeBitmap_WindowsOverload_ResizesCorrectly()
        {
            System.Windows.Size size = new(50, 50);
            Bitmap original = this.CreateTestBitmap(100, 100);
            Bitmap resized = ImageResizer.ResizeBitmap(original, size);

            Assert.IsNotNull(resized, "Resized bitmap should not be null.");
            Assert.AreEqual(50, resized.Width, "Width should be resized correctly.");
            Assert.AreEqual(50, resized.Height, "Height should be resized correctly.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ResizeBitmap_InvalidDimensions_ThrowsException()
        {
            Bitmap original = this.CreateTestBitmap(100, 100);
            _ = ImageResizer.ResizeBitmap(original, 0, -10); // Invalid size
        }

        [TestMethod]
        public void ResizeBitmap_ResizedImageIsRed()
        {
            Bitmap original = this.CreateTestBitmap(100, 100);
            Bitmap resized = ImageResizer.ResizeBitmap(original, 50, 50);

            Color originalPixelColor = original.GetPixel(50, 50);

            Assert.IsNotNull(resized, "Resized bitmap should not be null.");
            Assert.AreEqual(50, resized.Width, "Width should be resized correctly.");
            Assert.AreEqual(50, resized.Height, "Height should be resized correctly.");


            for (int y = 0; y < resized.Height; y++)
            {
                for (int x = 0; x < resized.Width; x++)
                {
                    Color pixelColor = resized.GetPixel(x, y);

                    // Resizing uses high quality interopelation, which will adjust colors
                    // slightly (especially at the borders?).  Since an exact match doesn't
                    // work here, assert that red is similarly red, and blue and green are
                    // not present.

                    Assert.IsTrue(
                        originalPixelColor.R == pixelColor.R ||
                        (originalPixelColor.R > pixelColor.R && originalPixelColor.R <= pixelColor.R + 10),
                        $"Pixel at {x}x{y} is R={pixelColor.R}, should be {originalPixelColor.R}."
                    );
                    
                    Assert.IsTrue(originalPixelColor.B == pixelColor.B, $"Pixel at {x}x{y} is B={pixelColor.B}, should be {originalPixelColor.B}.");
                    Assert.IsTrue(originalPixelColor.G == pixelColor.G, $"Pixel at {x}x{y} is G={pixelColor.G}, should be {originalPixelColor.G}.");
                }
            }
        }
    }
}
