using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class ImageGlazierTests
    {
        #region LoadImage

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void LoadImage_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {
            var glazier = new ImageGlazier();
            string invalidFilePath = $"{Guid.NewGuid()}_nonexistent.png";

            glazier.LoadImage(invalidFilePath);
        }

        [TestMethod]
        public void LoadImage_ShouldLoadImage_WhenFileExists()
        {
            var glazier = new ImageGlazier();
            string testImagePath = $"test {Guid.NewGuid().ToString()}.png";

            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Save(testImagePath);
            }

            glazier.LoadImage(testImagePath);

            Assert.IsNotNull(glazier.ImageData);
            Assert.AreEqual(100, glazier.ImageData.Width);
            Assert.AreEqual(100, glazier.ImageData.Height);

            File.Delete(testImagePath);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task LoadImageAsync_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {
            ImageGlazier imageGlazier = new ImageGlazier();
            string testImagePath = Guid.NewGuid().ToString();

            await imageGlazier.LoadImageAsync(testImagePath);
        }

        [TestMethod]
        public async Task LoadImageAsync_Should_Set_ImageData()
        {
            ImageGlazier imageGlazier = new ImageGlazier();
            string testImagePath = $"test {Guid.NewGuid().ToString()}.png";

            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Save(testImagePath);
            }

            await imageGlazier.LoadImageAsync(testImagePath);

            Assert.IsNotNull(imageGlazier.ImageData);
            Assert.AreEqual(100, imageGlazier.ImageData.Width);
            Assert.AreEqual(100, imageGlazier.ImageData.Height);

            File.Delete(testImagePath);
        }

        #endregion

        #region SaveImage

        [TestMethod]
        public void SaveImage_ShouldSaveImageToFile()
        {
            var glazier = new ImageGlazier();
            string outputPath = "output.png";

            glazier.ImageData = new Image<Rgba32>(100, 100);

            glazier.SaveImage(outputPath);

            Assert.IsTrue(File.Exists(outputPath));

            File.Delete(outputPath);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SaveImage_ShouldThrowInvalidOperationException_WhenImageDataIsNull()
        {
            string outputPath = "output.png";
            var glazier = new ImageGlazier();

            glazier.SaveImage(outputPath);
        }

        #endregion

        #region Dispose

        [TestMethod]
        public void Dispose_ShouldReleaseImageData()
        {
            var glazier = new ImageGlazier();
            glazier.ImageData = new Image<Rgba32>(100, 100);

            glazier.Dispose();

            Assert.IsNull(glazier.ImageData);
        }

        [TestMethod]
        public void Dispose_ShouldNotThrowOnSecondCall()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(100, 100)
            };

            glazier.Dispose();
            glazier.Dispose();

            Assert.IsNull(glazier.ImageData, "ImageData should be null after disposal.");
        }

        #endregion

        [TestMethod]
        public void GetMostCommonColors_ShouldReturnCorrectColors()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            glazier.ImageData[0, 0] = new Rgba32(255, 0, 0);
            glazier.ImageData[0, 1] = new Rgba32(255, 0, 0);
            glazier.ImageData[1, 0] = new Rgba32(0, 255, 0);
            glazier.ImageData[1, 1] = new Rgba32(0, 0, 255);

            var result = glazier.GetMostCommonColors(2);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(new Rgba32(255, 0, 0)));
            Assert.IsTrue(result.ContainsKey(new Rgba32(0, 255, 0)));
        }

        #region Glaze

        [TestMethod]
        public void Glaze_ShouldReplaceMatchingColors()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            glazier.ImageData[0, 0] = new Rgba32(255, 0, 0);
            glazier.ImageData[0, 1] = new Rgba32(255, 0, 0);
            glazier.ImageData[1, 0] = new Rgba32(0, 255, 0);
            glazier.ImageData[1, 1] = new Rgba32(0, 0, 255);

            var matchColor = new Rgba32(255, 0, 0);

            glazier.Glaze(matchColor, 0);

            // First pixel, which was red, should now be transparent
            Assert.AreEqual(matchColor.R, glazier.ImageData[0, 0].R, $"R: {matchColor.R} vs {glazier.ImageData[0, 0].R}");
            Assert.AreEqual(matchColor.G, glazier.ImageData[0, 0].G, $"G: {matchColor.G} vs {glazier.ImageData[0, 0].G}");
            Assert.AreEqual(matchColor.B, glazier.ImageData[0, 0].B, $"B: {matchColor.B} vs {glazier.ImageData[0, 0].B}");
            Assert.AreNotEqual(matchColor.A, glazier.ImageData[0, 0].A, $"B: {matchColor.A} vs {glazier.ImageData[0, 0].A}");

            // Second pixel, which was also red, should now be transparent
            Assert.AreEqual(matchColor.R, glazier.ImageData[0, 1].R, $"R: {matchColor.R} vs {glazier.ImageData[0, 1].R}");
            Assert.AreEqual(matchColor.G, glazier.ImageData[0, 1].G, $"G: {matchColor.G} vs {glazier.ImageData[0, 1].G}");
            Assert.AreEqual(matchColor.B, glazier.ImageData[0, 1].B, $"B: {matchColor.B} vs {glazier.ImageData[0, 1].B}");
            Assert.AreNotEqual(matchColor.A, glazier.ImageData[0, 0].A, $"B: {matchColor.A} vs {glazier.ImageData[0, 1].A}");

            // Other pixels,green and blue, should remain unchanged
            Assert.AreNotEqual(matchColor, glazier.ImageData[1, 0]);
            Assert.AreNotEqual(matchColor, glazier.ImageData[1, 1]);
        }

        #region Exceptions

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Glaze_ShouldThrowWhenImageDataIsNull()
        {
            var glazier = new ImageGlazier();

            glazier.Glaze(new Rgba32(255, 0, 0), 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Glaze_ShouldThrowWhenMaskIsNull()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            glazier.Glaze(new Rgba32(255, 0, 0), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Glaze_ShouldThrowWhenMaskIsWrongSize()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            var mask = new Image<Rgba32>(3, 3);

            glazier.Glaze(new Rgba32(255, 0, 0), mask);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Glaze_ShouldThrowWhenToleranceIsNegative()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            glazier.Glaze(new Rgba32(255, 0, 0), -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Glaze_ShouldThrowWhenToleranceIsTooHigh()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            glazier.Glaze(new Rgba32(255, 0, 0), 257);
        }

        #endregion

        #endregion

        #region Mask Functionality

        [TestMethod]
        public void GenerateMask_CorrectlyIdentifiesBackgroundPixels()
        {
            int width = 3, height = 3;
            Image<Rgba32> testImage = new(width, height);

            // Set up test pixels (middle pixel different)
            testImage[0, 0] = new Rgba32(255, 255, 255);
            testImage[1, 0] = new Rgba32(255, 255, 255);
            testImage[2, 0] = new Rgba32(255, 0, 0); // Different color
            testImage[0, 1] = new Rgba32(255, 255, 255);
            testImage[1, 1] = new Rgba32(255, 255, 255);
            testImage[2, 1] = new Rgba32(255, 255, 255);
            testImage[0, 2] = new Rgba32(255, 255, 255);
            testImage[1, 2] = new Rgba32(255, 255, 255);
            testImage[2, 2] = new Rgba32(255, 255, 255);

            var backgroundColor = new Rgba32(255, 255, 255);
            int tolerance = 10;

            var glazier = new ImageGlazier
            {
                ImageData = testImage
            };

            var maskImage = glazier.GenerateMask(backgroundColor, tolerance);

            // Verify expected mask values
            Assert.AreEqual(new Rgba32(0, 0, 0, 0), maskImage[0, 0]);
            Assert.AreEqual(new Rgba32(0, 0, 0, 0), maskImage[1, 0]);
            Assert.AreEqual(new Rgba32(255, 255, 255, 255), maskImage[2, 0]); // Should be kept
        }

        [TestMethod]
        public void ApplyMask_CorrectlySetsAlphaToZero()
        {
            int width = 3, height = 3;
            Image<Rgba32> testImage = new(width, height);
            Image<Rgba32> mask = new(width, height);

            // Initialize test image with solid colors
            testImage[0, 0] = new Rgba32(255, 0, 0, 255); // Red
            testImage[1, 0] = new Rgba32(0, 255, 0, 255); // Green
            testImage[2, 0] = new Rgba32(0, 0, 255, 255); // Blue

            // Initialize mask (only one pixel should remain visible)
            mask[0, 0] = new Rgba32(0, 0, 0, 0); // Should be transparent
            mask[1, 0] = new Rgba32(0, 0, 0, 0); // Should be transparent
            mask[2, 0] = new Rgba32(255, 255, 255, 255); // Should remain visible

            // Apply mask to image
            ImageGlazier instance = new() { ImageData = testImage };
            instance.ApplyMask(mask);

            // Validate alpha values
            Assert.AreEqual(0, testImage[0, 0].A); // Should be fully transparent
            Assert.AreEqual(0, testImage[1, 0].A); // Should be fully transparent
            Assert.AreEqual(255, testImage[2, 0].A); // Should remain visible
        }

        #endregion
    }
}
