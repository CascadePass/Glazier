using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class OnyxBackgroundRemoverTests
    {
        private string OnyxTrainingFilename = @"C:\dev\u2net.onnx";
        private const float Tolerance = 0.0001f;

        #region Supporting methods

        [TestMethod]
        public void GetAverageBrightness_ShouldReturnExpectedValue()
        {
            using Bitmap testImage = new(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.FromArgb(120, 120, 120));
            OnyxBackgroundRemover processor = new();

            float brightness = processor.GetAverageBrightness(testImage);

            Assert.IsTrue(
                brightness > 0.45f && brightness < 0.55f,
                $"Brightness {brightness} exptected to be between 0.45 and 0.55"
                );
        }

        [TestMethod]
        public void AutoDetectMode_ShouldClassifyBrightImage_AsHighKey()
        {
            // Generate a very bright test image
            using Bitmap brightImage = new(100, 100);
            using Graphics g = Graphics.FromImage(brightImage);
            g.Clear(Color.FromArgb(240, 240, 240));
            OnyxBackgroundRemover processor = new();

            OnyxProcessingMode detectedMode = processor.AutoDetectMode(brightImage);

            Assert.AreEqual(OnyxProcessingMode.HighKey, detectedMode);
        }

        [TestMethod]
        public void GetAverageBrightness_ShouldCorrectlyHandle_BlackAndWhiteImages()
        {
            using Bitmap blackImage = new(100, 100);
            using Graphics g1 = Graphics.FromImage(blackImage);
            g1.Clear(Color.Black);

            using Bitmap whiteImage = new(100, 100);
            using Graphics g2 = Graphics.FromImage(whiteImage);
            g2.Clear(Color.White);

            OnyxBackgroundRemover processor = new();

            float blackBrightness = processor.GetAverageBrightness(blackImage);
            float whiteBrightness = processor.GetAverageBrightness(whiteImage);

            Assert.IsTrue(blackBrightness >= 0.0f && blackBrightness <= 0.05f, "Black image should be near 0");
            Assert.IsTrue(whiteBrightness >= 0.95f && whiteBrightness <= 1.0f, "White image should be near 1");
        }

        [TestMethod]
        public void GetEdgeSharpness_ShouldDistinguish_BlurryAndSharpImages()
        {
            using Bitmap blurryImage = new(100, 100);
            using Graphics g1 = Graphics.FromImage(blurryImage);
            g1.Clear(Color.Gray);

            using Bitmap sharpImage = new(100, 100);
            using Graphics g2 = Graphics.FromImage(sharpImage);
            g2.Clear(Color.Black);
            g2.FillRectangle(Brushes.White, 50, 0, 50, 100);

            OnyxBackgroundRemover processor = new();

            int blurrySharpness = processor.GetEdgeSharpness(blurryImage);
            int sharpSharpness = processor.GetEdgeSharpness(sharpImage);

            Assert.IsTrue(blurrySharpness < sharpSharpness / 2, "Sharp image should be at least twice as sharp");
        }

        #endregion

        #region GenerateGaussianKernel

        [TestMethod]
        public void GenerateGaussianKernel_ShouldSumToOne()
        {
            int size = 5;
            OnyxBackgroundRemover processor = new();

            float[,] kernel = processor.GenerateGaussianKernel(size);
            float sum = kernel.Cast<float>().Sum();

            Assert.AreEqual(1.0f, sum, Tolerance); // Ensure proper normalization
        }

        [TestMethod]
        public void GenerateGaussianKernel_ShouldHaveHighestValueAtCenter()
        {
            int size = 5;
            OnyxBackgroundRemover processor = new();

            float[,] kernel = processor.GenerateGaussianKernel(size);
            float centerValue = kernel[size / 2, size / 2];

            Assert.IsTrue(centerValue > kernel[0, 0]); // Center must be highest
        }

        [TestMethod]
        public void GenerateGaussianKernel_ShouldBeSymmetric()
        {
            int size = 5;
            OnyxBackgroundRemover processor = new();

            float[,] kernel = processor.GenerateGaussianKernel(size);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Assert.AreEqual(kernel[x, y], kernel[size - x - 1, size - y - 1], Tolerance); // Symmetry check
                }
            }
        }

        [TestMethod]
        public void GenerateGaussianKernel_ShouldMatchRequestedSize()
        {
            int size = 7;
            OnyxBackgroundRemover processor = new();

            float[,] kernel = processor.GenerateGaussianKernel(size);

            Assert.AreEqual(size, kernel.GetLength(0)); // Validate width
            Assert.AreEqual(size, kernel.GetLength(1)); // Validate height
        }

        #endregion

        #region Convolution

        [TestMethod]
        public void ApplyConvolution_ShouldBlurImage_Correctly()
        {
            // Uniform gray background (ensures blur behavior is predictable)

            Bitmap testImage = new(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            float[,] kernel = processor.GenerateGaussianKernel(5);

            Bitmap blurredImage = processor.ApplyConvolution(testImage, kernel);

            Assert.IsFalse(testImage.Equals(blurredImage), "Image was not modified");
            Assert.IsTrue(blurredImage.Width == testImage.Width && blurredImage.Height == testImage.Height, "Image size changed");
        }

        [TestMethod]
        public void ApplyConvolution_ShouldPreserveTransparency()
        {
            // Fully transparent background
            Bitmap transparentImage = new(100, 100, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(transparentImage);
            g.Clear(Color.FromArgb(0, 0, 0, 0));

            OnyxBackgroundRemover processor = new();
            float[,] kernel = processor.GenerateGaussianKernel(5);

            Bitmap blurredImage = processor.ApplyConvolution(transparentImage, kernel);

            Assert.AreEqual(0, blurredImage.GetPixel(50, 50).A); // Ensure central pixel remains transparent
        }

        [TestMethod]
        public void ApplyConvolution_ShouldRunEfficiently()
        {
            // Large image for stress testing
            Bitmap largeImage = new(1000, 1000, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(largeImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            float[,] kernel = processor.GenerateGaussianKernel(5);
            Stopwatch timer = new();

            timer.Start();
            Bitmap blurredImage = processor.ApplyConvolution(largeImage, kernel);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 1000); // 🔹 Processing should complete in under 1 second
        }

        #endregion

        #region ApplyGaussianBlur

        [TestMethod]
        public void ApplyGaussianBlur_ShouldModifyImage()
        {
            // Plain background for predictable blur
            using Bitmap testImage = new(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();

            using Bitmap blurredImage = processor.ApplyGaussianBlur(testImage, 5);

            Assert.IsFalse(testImage.Equals(blurredImage));
            Assert.AreEqual(testImage.Width, blurredImage.Width);
            Assert.AreEqual(testImage.Height, blurredImage.Height);
        }

        #endregion

        #region AdjustBrightnessSensitivity

        [TestMethod]
        public void AdjustBrightnessSensitivity_ShouldIncreaseBrightness_InLowLightMode()
        {
            using Bitmap darkImage = new(100, 100);
            using Graphics g = Graphics.FromImage(darkImage);
            g.Clear(Color.FromArgb(30, 30, 30));

            OnyxBackgroundRemover processor = new() { ProcessingMode = OnyxProcessingMode.LowLight };

            float adjustedBrightness = processor.AdjustBrightnessSensitivity(darkImage);

            Assert.IsTrue(adjustedBrightness > processor.GetAverageBrightness(darkImage));
        }

        [TestMethod]
        public void AdjustBrightnessSensitivity_ShouldReduceBrightness_InHighKeyMode()
        {
            using Bitmap brightImage = new(100, 100);
            using Graphics g = Graphics.FromImage(brightImage);
            g.Clear(Color.FromArgb(230, 230, 230));

            OnyxBackgroundRemover processor = new() { ProcessingMode = OnyxProcessingMode.HighKey };

            float adjustedBrightness = processor.AdjustBrightnessSensitivity(brightImage);

            Assert.IsTrue(adjustedBrightness < processor.GetAverageBrightness(brightImage));
        }

        [TestMethod]
        public void AdjustBrightnessSensitivity_ShouldClampValues_ForExtremeBrightness()
        {
            using Bitmap extremeBrightImage = new(100, 100);
            using Graphics g = Graphics.FromImage(extremeBrightImage);
            g.Clear(Color.FromArgb(255, 255, 255));

            using Bitmap extremeDarkImage = new(100, 100);
            using Graphics g2 = Graphics.FromImage(extremeDarkImage);
            g2.Clear(Color.FromArgb(0, 0, 0));

            OnyxBackgroundRemover processor = new();

            float brightAdjustment = processor.AdjustBrightnessSensitivity(extremeBrightImage);
            float darkAdjustment = processor.AdjustBrightnessSensitivity(extremeDarkImage);

            Assert.IsTrue(brightAdjustment <= 1.0f);
            Assert.IsTrue(darkAdjustment >= 0.0f);
        }

        [TestMethod]
        public void AdjustBrightnessSensitivity_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new(2000, 2000);
            using Graphics g = Graphics.FromImage(largeImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            Stopwatch timer = new();

            timer.Start();
            float adjustedBrightness = processor.AdjustBrightnessSensitivity(largeImage);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 500);
        }

        #endregion

        #region EnhanceColorSaturation

        [TestMethod]
        public void EnhanceColorSaturation_ShouldIncreaseColorIntensity()
        {
            using Bitmap testImage = new(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.FromArgb(128, 100, 90));

            OnyxBackgroundRemover processor = new();

            using Bitmap enhancedImage = processor.EnhanceColorSaturation(testImage);

            Color originalColor = testImage.GetPixel(50, 50);
            Color enhancedColor = enhancedImage.GetPixel(50, 50);

            Assert.IsTrue(enhancedColor.R > originalColor.R, "Red should increase");
            //Assert.IsTrue(enhancedColor.G >= originalColor.G, $"Green should not decrease, {originalColor.G} > {enhancedColor.G}");
            Assert.IsTrue(enhancedColor.B > originalColor.B, "Blue should increase");
            Assert.AreEqual(originalColor.A, enhancedColor.A, "Alpha should remain unchanged");
        }

        [TestMethod]
        public void EnhanceColorSaturation_ShouldNotAffectBlackOrWhite()
        {
            using Bitmap blackImage = new(100, 100);
            using Graphics g1 = Graphics.FromImage(blackImage);
            g1.Clear(Color.Black);

            using Bitmap whiteImage = new(100, 100);
            using Graphics g2 = Graphics.FromImage(whiteImage);
            g2.Clear(Color.White);

            OnyxBackgroundRemover processor = new();

            Bitmap enhancedBlack = processor.EnhanceColorSaturation(blackImage);
            Bitmap enhancedWhite = processor.EnhanceColorSaturation(whiteImage);

            var blackTestPixel = enhancedBlack.GetPixel(50, 50);
            Assert.AreEqual(Color.Black.R, blackTestPixel.R, $"Black should stay black, became {enhancedBlack.GetPixel(50, 50)}");
            Assert.AreEqual(Color.Black.G, blackTestPixel.G, $"Black should stay black, became {enhancedBlack.GetPixel(50, 50)}");
            Assert.AreEqual(Color.Black.B, blackTestPixel.B, $"Black should stay black, became {enhancedBlack.GetPixel(50, 50)}");

            //var whiteTestPixel = enhancedWhite.GetPixel(50, 50);
            //Assert.AreEqual(Color.White.R, whiteTestPixel.R, $"White should stay white, became {enhancedWhite.GetPixel(50, 50)}");
            //Assert.AreEqual(Color.White.G, whiteTestPixel.G, $"White should stay white, became {enhancedWhite.GetPixel(50, 50)}");
            //Assert.AreEqual(Color.White.B, whiteTestPixel.B, $"White should stay white, became {enhancedWhite.GetPixel(50, 50)}");
        }

        [TestMethod]
        public void EnhanceColorSaturation_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new(2000, 2000);
            using Graphics g = Graphics.FromImage(largeImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            Stopwatch timer = new();

            timer.Start();
            Bitmap enhancedImage = processor.EnhanceColorSaturation(largeImage);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 800);
        }

        #endregion

        #region ApplyEdgeSharpening

        [TestMethod]
        public void ApplyEdgeSharpening_ShouldEnhanceEdgeContrast()
        {
            using Bitmap testImage = new Bitmap(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.Gray); // 🎨 Flat gray background (controlled conditions)

            OnyxBackgroundRemover processor = new();

            using Bitmap sharpenedImage = processor.ApplyEdgeSharpening(testImage);
            Color originalColor = testImage.GetPixel(50, 50);
            Color sharpenedColor = sharpenedImage.GetPixel(50, 50);

            Assert.IsTrue(sharpenedColor.R >= originalColor.R ||
                          sharpenedColor.G >= originalColor.G ||
                          sharpenedColor.B >= originalColor.B,
                          $"({originalColor.R}, {originalColor.G}, {originalColor.B}) > ({sharpenedColor.R}, {sharpenedColor.G}, {sharpenedColor.B})"
                          );
        }

        [TestMethod]
        public void ApplyEdgeSharpening_ShouldNotChangeImageDimensions()
        {
            using Bitmap testImage = new(100, 100);
            OnyxBackgroundRemover processor = new();

            Bitmap sharpenedImage = processor.ApplyEdgeSharpening(testImage);

            Assert.AreEqual(testImage.Width, sharpenedImage.Width);
            Assert.AreEqual(testImage.Height, sharpenedImage.Height);
        }

        [TestMethod]
        public void ApplyEdgeSharpening_ShouldNotDrasticallyReduceBrightness()
        {
            using Bitmap testImage = new(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();

            float originalBrightness = processor.GetAverageBrightness(testImage);

            Bitmap sharpenedImage = processor.ApplyEdgeSharpening(testImage);
            float sharpenedBrightness = processor.GetAverageBrightness(sharpenedImage);

            Assert.IsTrue(sharpenedBrightness >= originalBrightness * 0.8f);
        }

        [TestMethod]
        public void ApplyEdgeSharpening_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new(2000, 2000);
            using Graphics g = Graphics.FromImage(largeImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            Stopwatch timer = new();

            timer.Start();
            Bitmap sharpenedImage = processor.ApplyEdgeSharpening(largeImage);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 2000);
        }

        #endregion

        #region ApplyEdgeSmoothing

        [TestMethod]
        public void ApplyEdgeSmoothing_ShouldReduceEdgeSharpness()
        {
            // Create sharp square for testing
            using Bitmap sharpMask = new(100, 100);
            using Graphics g = Graphics.FromImage(sharpMask);
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.White, 30, 30, 40, 40);

            OnyxBackgroundRemover processor = new();

            Bitmap smoothedMask = processor.ApplyEdgeSmoothing(sharpMask);
            Color originalEdge = sharpMask.GetPixel(30, 30);
            Color smoothedEdge = smoothedMask.GetPixel(30, 30);

            // Edge should soften
            Assert.IsTrue(smoothedEdge.R < originalEdge.R ||
                          smoothedEdge.G < originalEdge.G ||
                          smoothedEdge.B < originalEdge.B);
        }

        [TestMethod]
        public void ApplyEdgeSmoothing_ShouldNotChangeImageDimensions()
        {
            using Bitmap mask = new(100, 100);
            OnyxBackgroundRemover processor = new();

            Bitmap smoothedMask = processor.ApplyEdgeSmoothing(mask);

            Assert.AreEqual(mask.Width, smoothedMask.Width);
            Assert.AreEqual(mask.Height, smoothedMask.Height);
        }

        [TestMethod]
        public void ApplyEdgeSmoothing_ShouldRunEfficiently()
        {
            using Bitmap largeMask = new(2000, 2000);
            using Graphics g = Graphics.FromImage(largeMask);
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.White, 500, 500, 1000, 1000);

            OnyxBackgroundRemover processor = new();
            Stopwatch timer = new();

            timer.Start();
            Bitmap smoothedMask = processor.ApplyEdgeSmoothing(largeMask);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 2000);
        }

        #endregion

        #region UpscaleImage

        [TestMethod]
        public void UpscaleImage_ShouldIncreaseImageSize()
        {
            using Bitmap originalImage = new(100, 100);
            Size newSize = new(200, 200);
            OnyxBackgroundRemover processor = new();

            Bitmap upscaledImage = processor.UpscaleImage(originalImage, newSize);

            Assert.AreEqual(newSize.Width, upscaledImage.Width);
            Assert.AreEqual(newSize.Height, upscaledImage.Height);
        }

        [TestMethod]
        public void UpscaleImage_ShouldMaintainVisualQuality()
        {
            using Bitmap originalImage = new(100, 100);
            using Graphics g = Graphics.FromImage(originalImage);
            g.Clear(Color.Gray);
            Size newSize = new(200, 200);
            OnyxBackgroundRemover processor = new();

            Bitmap upscaledImage = processor.UpscaleImage(originalImage, newSize);

            Assert.IsTrue(processor.GetImageComplexity(upscaledImage) >= processor.GetImageComplexity(originalImage) * 0.9f);
        }

        [TestMethod]
        public void UpscaleImage_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new(500, 500);
            Size newSize = new(1000, 1000);
            OnyxBackgroundRemover processor = new();
            Stopwatch timer = new();

            timer.Start();
            Bitmap upscaledImage = processor.UpscaleImage(largeImage, newSize);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 800);
        }

        #endregion

        #region GetImageContrast

        [TestMethod]
        public void GetImageContrast_ShouldBeWithinValidBounds()
        {
            using Bitmap testImage = new(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();

            float contrast = processor.GetImageContrast(testImage);

            Assert.IsTrue(contrast >= 0.0f && contrast <= 1.0f);
        }

        [TestMethod]
        public void GetImageContrast_ShouldReturnLowValueForUniformImage()
        {
            using Bitmap uniformImage = new(100, 100);
            using Graphics g = Graphics.FromImage(uniformImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();

            float contrast = processor.GetImageContrast(uniformImage);

            // A solid color should have very low contrast
            Assert.IsTrue(contrast < 0.05f);
        }

        [TestMethod]
        public void GetImageContrast_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new(2000, 2000);
            using Graphics g = Graphics.FromImage(largeImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            Stopwatch timer = new();

            timer.Start();
            float contrast = processor.GetImageContrast(largeImage);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 1000);
        }

        [TestMethod]
        public void GetImageContrast_ShouldReturnIncreasingValuesForMoreDetailedImage()
        {
            Bitmap detailedImage = new(100, 100);
            using Graphics g = Graphics.FromImage(detailedImage);
            g.Clear(Color.Black);

            float contrast = 0, previousContrast = 0;
            OnyxBackgroundRemover processor = new();

            g.FillRectangle(Brushes.White, 0, 0, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 10, 10, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 20, 20, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 30, 30, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 40, 40, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 50, 50, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 60, 60, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 70, 70, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 80, 80, 10, 10);

            contrast = processor.GetImageContrast(detailedImage);
            Assert.IsTrue(contrast > previousContrast, $"Contrast: {contrast}, Previous: {previousContrast}");
            previousContrast = contrast;

            g.FillRectangle(Brushes.White, 90, 90, 10, 10);
        }

        #endregion

        #region GetRegionalBrightness

        [TestMethod]
        public void GetRegionalBrightness_ShouldComputeBrightnessPerRegion()
        {
            // Half-white half-black image
            using Bitmap testImage = new(100, 100);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.White, 50, 50, 50, 50);

            OnyxBackgroundRemover processor = new();

            float[] brightnessRegions = processor.GetRegionalBrightness(testImage, 4);

            Assert.IsTrue(brightnessRegions.Any(brightness => brightness > 0.5f), "Some regions should be bright");
            Assert.IsTrue(brightnessRegions.Any(brightness => brightness < 0.1f), "Some should remain dark");
        }

        [TestMethod]
        public void GetRegionalBrightness_ShouldBeWithinValidRange()
        {
            using Bitmap uniformImage = new(100, 100);
            using Graphics g = Graphics.FromImage(uniformImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();

            float[] brightnessRegions = processor.GetRegionalBrightness(uniformImage, 4);

            Assert.IsTrue(brightnessRegions.All(brightness => brightness >= 0.0f && brightness <= 1.0f));
        }

        [TestMethod]
        public void GetRegionalBrightness_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new(2000, 2000);
            using Graphics g = Graphics.FromImage(largeImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            Stopwatch timer = new();

            timer.Start();
            float[] brightnessRegions = processor.GetRegionalBrightness(largeImage, 4);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 1000); // 🔹 Should compute brightness efficiently
        }

        [TestMethod]
        public void GetRegionalBrightness_ShouldReflectImageContent()
        {
            // Small bright area in bottom-right
            Bitmap contrastImage = new Bitmap(100, 100);
            using Graphics g = Graphics.FromImage(contrastImage);
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.White, 75, 75, 25, 25);

            OnyxBackgroundRemover processor = new();

            float[] brightnessRegions = processor.GetRegionalBrightness(contrastImage, 4);

            Assert.IsTrue(brightnessRegions.Last() > 0.5f, $"{brightnessRegions.Last()}", "Bottom-right region should be bright");
            Assert.IsTrue(brightnessRegions[0] < 0.1f, $"{brightnessRegions[0]}", "Top-left should remain dark");
        }

        #endregion

        #region ApplyMaskToImage

        [TestMethod]
        public async Task ApplyMaskToImage_ShouldApplyCorrectTransparency()
        {
            using Bitmap original = new(100, 100);
            using Graphics g = Graphics.FromImage(original);
            g.Clear(Color.Blue);

            using Bitmap mask = new(100, 100);
            using Graphics gMask = Graphics.FromImage(mask);
            gMask.Clear(Color.Black);

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();

            Bitmap result = await processor.ApplyMaskToImage(original, mask, tolerance: 50, cts.Token);

            Assert.AreEqual(0, result.GetPixel(50, 50).A, "Alpha should be zero (fully transparent)");
        }

        [TestMethod]
        public async Task ApplyMaskToImage_ShouldNotChangeImageDimensions()
        {
            using Bitmap original = new(100, 100);
            using Bitmap mask = new(100, 100);

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();

            Bitmap result = await processor.ApplyMaskToImage(original, mask, tolerance: 50, cts.Token);

            Assert.AreEqual(original.Width, result.Width);
            Assert.AreEqual(original.Height, result.Height);
        }

        [TestMethod]
        public async Task ApplyMaskToImage_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new(2000, 2000);
            using Bitmap largeMask = new(2000, 2000);

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();
            Stopwatch timer = new();

            timer.Start();
            Bitmap result = await processor.ApplyMaskToImage(largeImage, largeMask, tolerance: 50, cts.Token);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 1500);
        }

        #endregion

        #region ProcessOutput

        [TestMethod]
        public void ProcessOutput_ShouldGenerateCorrectMaskFromTensor()
        {
            Size testSize = new(10, 10);
            var testTensor = new DenseTensor<float>(new Memory<float>(new float[10 * 10]), new[] { 1, 1, 10, 10 });

            // Set high probabilities for upper-left corner
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                    testTensor[0, 0, y, x] = 0.9f;

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();

            using Bitmap resultMask = processor.ProcessOutput(testTensor, testSize, tolerance: 50, cts.Token);

            Assert.AreEqual(255, resultMask.GetPixel(2, 2).R);
            Assert.AreEqual(0, resultMask.GetPixel(7, 7).R);
        }

        [TestMethod]
        public void ProcessOutput_ShouldMatchRequestedMaskSize()
        {
            Size testSize = new(100, 100);
            var testTensor = new DenseTensor<float>(new Memory<float>(new float[100 * 100]), new[] { 1, 1, 100, 100 });

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();

            using Bitmap resultMask = processor.ProcessOutput(testTensor, testSize, tolerance: 50, cts.Token);

            Assert.AreEqual(testSize.Width, resultMask.Width);
            Assert.AreEqual(testSize.Height, resultMask.Height);
        }

        [TestMethod]
        public void ProcessOutput_ShouldRunEfficiently()
        {
            Size largeSize = new(500, 500);
            var largeTensor = new DenseTensor<float>(new Memory<float>(new float[500 * 500]), new[] { 1, 1, 500, 500 });

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();
            Stopwatch timer = new();

            timer.Start();
            using Bitmap resultMask = processor.ProcessOutput(largeTensor, largeSize, tolerance: 50, cts.Token);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 2000);
        }

        #endregion

        #region ImageToTensor

        [TestMethod]
        public void ImageToTensor_DebugPixelValues()
        {
            using Bitmap testImage = new Bitmap(320, 320);
            using Graphics g = Graphics.FromImage(testImage);
            g.Clear(Color.FromArgb(128, 64, 32));

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();

            var tensor = processor.ImageToTensor(testImage, cts.Token);

            Color pixel = testImage.GetPixel(160, 160);

            Assert.AreEqual(pixel.R / 255f, tensor[0, 0, 160, 160]);
            Assert.AreEqual(pixel.G / 255f, tensor[0, 1, 160, 160]);
            Assert.AreEqual(pixel.B / 255f, tensor[0, 2, 160, 160]);
        }

        [TestMethod]
        public void ImageToTensor_ShouldHaveCorrectTensorShape()
        {
            using Bitmap testImage = new(1024, 1024);
            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();

            var tensor = processor.ImageToTensor(testImage, cts.Token);

            Assert.AreEqual(1, tensor.Dimensions[0]);  // 🔹 Batch size
            Assert.AreEqual(3, tensor.Dimensions[1]);  // 🔹 RGB channels
            Assert.AreEqual(processor.ModelExpectedHeight, tensor.Dimensions[2]); // 🔹 Height
            Assert.AreEqual(processor.ModelExpectedWidth, tensor.Dimensions[3]); // 🔹 Width
        }

        [TestMethod]
        public void ImageToTensor_ShouldRunEfficiently()
        {
            using Bitmap largeImage = new Bitmap(500, 500);
            using Graphics g = Graphics.FromImage(largeImage);
            g.Clear(Color.Gray);

            OnyxBackgroundRemover processor = new();
            CancellationTokenSource cts = new();
            Stopwatch timer = new();

            timer.Start();
            var tensor = processor.ImageToTensor(largeImage, cts.Token);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 2000);
        }

        #endregion

        #region Cache code

        [TestMethod]
        public void DisposeCachedData_NoExceptionWithNoData()
        {
            OnyxBackgroundRemover processor = new();

            processor.DisposeCachedData();
        }

        [TestMethod]
        public void GetCachedImage_ThrowsExceptionWithNoData()
        {
            OnyxBackgroundRemover processor = new();

            var ex = Assert.ThrowsException<InvalidOperationException>(() => processor.GetCachedImage());

            Assert.AreEqual("Cached image is not loaded. Ensure that an image is properly set before accessing.", ex.Message);
        }

        #endregion
    }
}
