#region Using Directives

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace CascadePass.Glazier.Core
{
    public class OnyxBackgroundRemover
    {
        #region Fields

        private readonly InferenceSession session;
        private Tensor<float> cachedTensor;
        private Bitmap cachedImage;

        #endregion

        #region Constructors

        public OnyxBackgroundRemover()
        {
            this.ModelExpectedHeight = 320;
            this.ModelExpectedWidth = 320;
            this.EdgeSmoothingKernelSize = 5;
        }

        public OnyxBackgroundRemover(string modelPath) : this()
        {
            if (!string.IsNullOrWhiteSpace(modelPath))
            {
                this.session = new InferenceSession(modelPath);
            }
        }

        #endregion

        #region Properties

        public int ModelExpectedWidth { get; set; }

        public int ModelExpectedHeight { get; set; }

        public bool EnhanceSaturation { get; set; }

        public bool SharpenEdges { get; set; }

        public int EdgeSmoothingKernelSize { get; set; }

        public OnyxProcessingMode ProcessingMode { get; set; }

        #endregion

        #region Methods

        #region Remove Background

        public async Task<Bitmap> RemoveBackgroundAsync(int tolerance, CancellationToken cancellationToken)
        {
            return await RemoveBackgroundInternal(tolerance, cancellationToken);
        }

        public Bitmap RemoveBackground(int tolerance, CancellationToken cancellationToken)
        {
            return RemoveBackgroundInternal(tolerance, cancellationToken).GetAwaiter().GetResult();
        }

        private async Task<Bitmap> RemoveBackgroundInternal(int tolerance, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            using Bitmap mask = ProcessOutput(this.cachedTensor, new(this.ModelExpectedWidth, this.ModelExpectedHeight), tolerance, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            using Bitmap scaledMask = this.UpscaleImage(mask, this.GetCachedImage().Size);
            return await this.ApplyMaskToImage(this.GetCachedImage(), scaledMask, tolerance, cancellationToken);
        }

        #endregion

        #region Image Processing

        #region Core Processing Methods

        internal DenseTensor<float> ImageToTensor(Bitmap image, CancellationToken cancellationToken)
        {
            var tensor = new DenseTensor<float>(new float[1 * 3 * ModelExpectedHeight * ModelExpectedWidth], [1, 3, ModelExpectedHeight, ModelExpectedWidth]);
            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                Parallel.For(0, ModelExpectedHeight * ModelExpectedWidth, (index, state) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }

                    int y = index / this.ModelExpectedWidth;
                    int x = index % this.ModelExpectedWidth;

                    if (x < this.ModelExpectedWidth && y < this.ModelExpectedHeight)
                    {
                        int pixelIndex = y * stride + x * 3;
                        tensor[0, 2, y, x] = ptr[pixelIndex] / 255f;      // Blue  
                        tensor[0, 1, y, x] = ptr[pixelIndex + 1] / 255f;  // Green  
                        tensor[0, 0, y, x] = ptr[pixelIndex + 2] / 255f;  // Red  
                    }
                });
            }

            image.UnlockBits(bmpData);
            return tensor;
        }

        internal Bitmap ProcessOutput(Tensor<float> outputTensor, Size size, int tolerance, CancellationToken cancellationToken)
        {
            Bitmap mask = new(size.Width, size.Height, PixelFormat.Format24bppRgb);
            mask = this.ApplyEdgeSmoothing(mask);

            float edgeCompensation = this.GetEdgeSharpness(mask);

            BitmapData bmpData = mask.LockBits(new Rectangle(0, 0, mask.Width, mask.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;
                int width = size.Width;
                int height = size.Height;

                Parallel.For(0, height, (y, state) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }

                    for (int x = 0; x < width; x++)
                    {
                        float probability = outputTensor[0, 0, y, x]; 
                        byte value = (byte)((probability * 255 > tolerance + edgeCompensation) ? 255 : 0);
                        int pixelIndex = y * stride + x * 3;

                        ptr[pixelIndex] = value;
                        ptr[pixelIndex + 1] = value;
                        ptr[pixelIndex + 2] = value;
                    }
                });
            }

            mask.UnlockBits(bmpData);
            return mask;
        }

        internal async Task<Bitmap> ApplyMaskToImage(Bitmap original, Bitmap mask, int tolerance, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                Bitmap result = new(original.Width, original.Height, PixelFormat.Format32bppArgb);

                BitmapData origData = original.LockBits(new Rectangle(0, 0, original.Width, original.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                BitmapData maskData = mask.LockBits(new Rectangle(0, 0, mask.Width, mask.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                BitmapData resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                unsafe
                {
                    byte* origPtr = (byte*)origData.Scan0;
                    byte* maskPtr = (byte*)maskData.Scan0;
                    byte* resultPtr = (byte*)resultData.Scan0;

                    int origStride = origData.Stride;
                    int maskStride = maskData.Stride;
                    int resultStride = resultData.Stride;

                    int width = original.Width;

                    Parallel.For(0, original.Height * width, (index, state) =>
                    {
                        if (cancellationToken.IsCancellationRequested) state.Stop();

                        int y = index / width;
                        int x = index % width;

                        int indexOrig = y * origStride + x * 3;
                        int indexMask = y * maskStride + x * 3;
                        int indexResult = y * resultStride + x * 4;

                        //byte alpha = maskPtr[indexMask];
                        byte edgeStrength = maskPtr[indexMask];
                        byte distanceToEdge = (byte)Math.Max(0, edgeStrength - 20);
                        byte alpha = (edgeStrength * 255 > tolerance) ? (byte)255 : (byte)0;

                        resultPtr[indexResult] = origPtr[indexOrig];
                        resultPtr[indexResult + 1] = origPtr[indexOrig + 1];
                        resultPtr[indexResult + 2] = origPtr[indexOrig + 2];
                        resultPtr[indexResult + 3] = alpha;
                    });
                }

                original.UnlockBits(origData);
                mask.UnlockBits(maskData);
                result.UnlockBits(resultData);

                result = this.ApplyGaussianBlur(result, 3);
                result = this.SharpenEdges ? this.ApplyEdgeSharpening(result) : result;
                result = this.EnhanceSaturation ? this.EnhanceColorSaturation(result) : result;

                return result;
            });
        }

        #endregion

        internal Bitmap ApplyEdgeSmoothing(Bitmap mask)
        {
            if (this.EdgeSmoothingKernelSize > 0)
            {
                float[,] gaussianKernel = this.GenerateGaussianKernel(this.EdgeSmoothingKernelSize);

                return this.ApplyConvolution(mask, gaussianKernel);
            }

            return mask;
        }

        internal int GetEdgeSharpness(Bitmap image)
        {
            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                ImageLockMode.ReadOnly,
                                                PixelFormat.Format24bppRgb);

            int sharpness = 0;
            int stride = bmpData.Stride;

            const int chunkSize = 8;
            int height = image.Height;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                Parallel.ForEach(Partitioner.Create(1, image.Width - 1, chunkSize), range =>
                {
                    int localSharpness = 0;

                    for (int x = range.Item1; x < range.Item2; x++)
                    {
                        for (int y = 1; y < height - 1; y += chunkSize)
                        {
                            int index = y * stride + x * 3;
                            int leftIndex = y * stride + (x - 1) * 3;
                            int rightIndex = y * stride + (x + 1) * 3;
                            int topIndex = (y - 1) * stride + x * 3;
                            int bottomIndex = (y + 1) * stride + x * 3;

                            int diff = Math.Abs(ptr[index] - ptr[leftIndex]) +
                                       Math.Abs(ptr[index] - ptr[rightIndex]) +
                                       Math.Abs(ptr[index] - ptr[topIndex]) +
                                       Math.Abs(ptr[index] - ptr[bottomIndex]);

                            localSharpness += diff;
                        }
                    }

                    Interlocked.Add(ref sharpness, localSharpness);
                });
            }

            image.UnlockBits(bmpData);
            return Math.Clamp((sharpness * 100) / (image.Width * image.Height), 1, 10);
        }

        internal Bitmap ApplyEdgeSharpening(Bitmap image)
        {
            float complexity = this.GetImageComplexity(image);
            float sharpeningAmount = Math.Clamp(complexity * 1.2f, 0.5f, 3.0f);

            // Adjusted center weight to compensate for surrounding pixel reduction
            float[,] sharpeningKernel =
            {
                {-1, -1, -1},
                {-1, sharpeningAmount + 8, -1}, 
                {-1, -1, -1}
            };

            Bitmap sharpenedImage = this.ApplyConvolution(image, sharpeningKernel);

            return sharpenedImage;
        }

        internal Bitmap EnhanceColorSaturation(Bitmap image, float saturationBoost = 1.2f)
        {
            saturationBoost = Math.Clamp(saturationBoost, 1.0f, 1.5f);

            Bitmap enhancedImage = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(enhancedImage);

            float lumR = 0.3086f * (1 - saturationBoost);
            float lumG = 0.6094f * (1 - saturationBoost) + 0.05f; // ✅ Added minor offset to stabilize green
            float lumB = 0.0820f * (1 - saturationBoost);

            float[][] saturationMatrix =
            [
                [lumR + saturationBoost, lumG, lumB, 0, 0],
                [lumR, lumG + saturationBoost, lumB, 0, 0],
                [lumR, lumG, lumB + saturationBoost, 0, 0],
                [0, 0, 0, 1, 0],
                [0, 0, 0, 0, 1]
            ];

            using ImageAttributes attributes = new();
            attributes.SetColorMatrix(new ColorMatrix(saturationMatrix));

            // ✅ Apply saturation globally
            g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                        0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);

            return enhancedImage;
        }

        internal float GetImageComplexity(Bitmap image)
        {
            float contrastLevel = GetImageContrast(image);
            int edgePixels = 0;
            int totalPixels = image.Width * image.Height;
            int width = image.Width;

            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            const int chunkSize = 8;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;
                int height = image.Height;

                Parallel.ForEach(Partitioner.Create(1, width - 1, chunkSize), range =>
                {
                    int localEdgePixels = 0;

                    int adaptiveThreshold = this.ProcessingMode switch
                    {
                        OnyxProcessingMode.Portrait => (int)(contrastLevel * 30),  // Softer edges, no over-sharpening
                        OnyxProcessingMode.Landscape => (int)(contrastLevel * 50), // High-detail sharpness for outdoor scenes
                        OnyxProcessingMode.LowLight => (int)(contrastLevel * 20),  // Increases sensitivity to low-contrast areas
                        _ => 40 // Default threshold
                    };

                    for (int x = range.Item1; x < range.Item2; x++)
                    {
                        for (int y = 1; y < height - 1; y += chunkSize)
                        {
                            int index = y * stride + x * 3;
                            int leftIndex = y * stride + (x - 1) * 3;
                            int rightIndex = y * stride + (x + 1) * 3;
                            int topIndex = (y - 1) * stride + x * 3;
                            int bottomIndex = (y + 1) * stride + x * 3;

                            int diff = Math.Abs(ptr[index] - ptr[leftIndex]) +
                                       Math.Abs(ptr[index] - ptr[rightIndex]) +
                                       Math.Abs(ptr[index] - ptr[topIndex]) +
                                       Math.Abs(ptr[index] - ptr[bottomIndex]);

                            if (diff > adaptiveThreshold) localEdgePixels++;
                        }
                    }

                    Interlocked.Add(ref edgePixels, localEdgePixels);
                });
            }

            image.UnlockBits(bmpData);
            return (float)edgePixels / totalPixels;
        }

        internal float GetAverageBrightness(Bitmap image)
        {
            long brightnessSum = 0;
            int totalPixels = image.Width * image.Height;
            int width = image.Width;

            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                ImageLockMode.ReadOnly,
                                                PixelFormat.Format24bppRgb);

            const int chunkSize = 8;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                Parallel.ForEach(Partitioner.Create(0, totalPixels, chunkSize), range =>
                {
                    long localBrightnessSum = 0;

                    for (int index = range.Item1; index < range.Item2; index++)
                    {
                        int x = index % width;
                        int y = index / width;

                        int pixelIndex = y * stride + x * 3;

                        // Apply human-perceived luminance weighting:
                        int brightness = (int)(ptr[pixelIndex] * 0.2126f +      // Red weight
                                               ptr[pixelIndex + 1] * 0.7152f +  // Green weight
                                               ptr[pixelIndex + 2] * 0.0722f);  // Blue weight

                        localBrightnessSum += brightness;
                    }

                    Interlocked.Add(ref brightnessSum, localBrightnessSum);
                });
            }

            image.UnlockBits(bmpData);
            return brightnessSum / (float)(totalPixels * 255);
        }

        internal float[] GetRegionalBrightness(Bitmap image, int regionCount = 4)
        {
            long[] brightnessSums = new long[regionCount * regionCount];
            int width = image.Width, height = image.Height;
            int regionWidth = width / regionCount, regionHeight = height / regionCount;
            int totalPixels = width * height;

            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, width, height),
                                                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                Parallel.ForEach(Partitioner.Create(0, width, regionWidth), range =>
                {
                    for (int x = range.Item1; x < range.Item2; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int regionX = x / regionWidth;
                            int regionY = y / regionHeight;
                            //int regionIndex = regionY * regionCount + regionX;
                            int regionIndex = Math.Min(regionY, regionCount - 1) * regionCount + Math.Min(regionX, regionCount - 1);

                            int pixelIndex = y * stride + x * 3;
                            float brightness = ptr[pixelIndex] * 0.2126f +
                                               ptr[pixelIndex + 1] * 0.7152f +
                                               ptr[pixelIndex + 2] * 0.0722f;

                            Interlocked.Add(ref brightnessSums[regionIndex], (long)brightness);
                        }
                    }
                });
            }

            image.UnlockBits(bmpData);

            float[] regionalBrightness = new float[regionCount * regionCount];
            int pixelsPerRegion = totalPixels / (regionCount * regionCount);

            for (int i = 0; i < brightnessSums.Length; i++)
            {
                regionalBrightness[i] = brightnessSums[i] / Math.Max(1, (float)(pixelsPerRegion * 255));
            }

            return regionalBrightness;
        }

        internal float AdjustBrightnessSensitivity(Bitmap image)
        {
            float brightness = this.GetAverageBrightness(image);

            // Apply clamping to prevent excessive scaling
            float adjustedBrightness = brightness switch
            {
                _ when brightness < 0.1f => brightness * 1.8f, // 🌑 Further boost for very dark images
                _ when brightness > 0.9f => brightness * 0.6f, // 🌞 Reduce intensity for extremely bright images
                _ => brightness
            };

            // Mode-based scaling
            return this.ProcessingMode switch
            {
                OnyxProcessingMode.LowLight => adjustedBrightness * 1.5f,
                OnyxProcessingMode.HighKey => adjustedBrightness * 0.7f,
                _ => adjustedBrightness
            };
        }

        internal Bitmap ApplyGaussianBlur(Bitmap image, int blurAmount = 3)
        {
            // Prevents excessive kernel sizes
            blurAmount = Math.Clamp(blurAmount, 1, 15);

            Bitmap blurredImage = new(image.Width, image.Height, PixelFormat.Format32bppArgb);

            float[,] kernel = this.GenerateGaussianKernel(blurAmount);

            Bitmap processedImage = this.ApplyConvolution(image, kernel);

            using Graphics g = Graphics.FromImage(blurredImage);
            g.DrawImage(processedImage, new Rectangle(0, 0, image.Width, image.Height));

            return blurredImage;
        }

        internal float[,] GenerateGaussianKernel(int size)
        {
            float[,] kernel = new float[size, size];
            float sigma = size / 2.0f;
            float sum = 0.0f;
            int center = size / 2;
            float twoSigmaSq = 2 * sigma * sigma;

            Parallel.For(0, size, x =>
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    kernel[x, y] = (float)(Math.Exp(-(dx * dx + dy * dy) / twoSigmaSq) / (Math.PI * twoSigmaSq));
                }
            });

            sum = kernel.Cast<float>().Sum();
            float scale = 1.0f / sum;

            Parallel.For(0, size, x =>
            {
                for (int y = 0; y < size; y++)
                {
                    kernel[x, y] *= scale;
                }
            });

            return kernel;
        }

        internal Bitmap ApplyConvolution(Bitmap image, float[,] kernel)
        {
            int width = image.Width;
            int height = image.Height;
            Bitmap blurredImage = new(width, height, PixelFormat.Format32bppArgb);

            BitmapData srcData = image.LockBits(new Rectangle(0, 0, width, height),
                                                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = blurredImage.LockBits(new Rectangle(0, 0, width, height),
                                                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int kernelSize = kernel.GetLength(0);
            int offset = kernelSize / 2;
            int stride = srcData.Stride;

            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0;
                byte* dstPtr = (byte*)dstData.Scan0;

                Parallel.For(offset, height - offset, y =>
                {
                    for (int x = offset; x < width - offset; x++)
                    {
                        float redSum = 0, greenSum = 0, blueSum = 0, alphaSum = 0, kernelTotal = 0;

                        for (int ky = -offset; ky <= offset; ky++)
                        {
                            for (int kx = -offset; kx <= offset; kx++)
                            {
                                int pixelIndex = ((y + ky) * stride) + ((x + kx) * 4);
                                float kernelValue = kernel[ky + offset, kx + offset];

                                if (srcPtr[pixelIndex + 3] > 0)
                                {
                                    redSum += srcPtr[pixelIndex] * kernelValue;
                                    greenSum += srcPtr[pixelIndex + 1] * kernelValue;
                                    blueSum += srcPtr[pixelIndex + 2] * kernelValue;
                                    alphaSum += srcPtr[pixelIndex + 3];
                                    kernelTotal += kernelValue;
                                }
                            }
                        }

                        int dstIndex = (y * stride) + (x * 4);

                        if (kernelTotal > 0)
                        {
                            dstPtr[dstIndex] = (byte)Math.Clamp(redSum / kernelTotal, 0, 255);
                            dstPtr[dstIndex + 1] = (byte)Math.Clamp(greenSum / kernelTotal, 0, 255);
                            dstPtr[dstIndex + 2] = (byte)Math.Clamp(blueSum / kernelTotal, 0, 255);
                        }
                        dstPtr[dstIndex + 3] = (byte)Math.Clamp(alphaSum / kernelTotal, 0, 255);
                    }
                });
            }

            image.UnlockBits(srcData);
            blurredImage.UnlockBits(dstData);
            return blurredImage;
        }

        internal float GetImageContrast(Bitmap image)
        {
            long contrastSum = 0;
            int totalPixels = image.Width * image.Height;
            int width = image.Width;
            int height = image.Height;

            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, width, height),
                                                ImageLockMode.ReadOnly,
                                                PixelFormat.Format24bppRgb);

            const int chunkSize = 8;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                Parallel.ForEach(Partitioner.Create(0, width, chunkSize), range =>
                {
                    long localContrastSum = 0;

                    for (int x = range.Item1; x < range.Item2; x++)
                    {
                        for (int y = 0; y < height; y += chunkSize)
                        {
                            int index = y * stride + x * 3;
                            int leftIndex = (x > 0) ? y * stride + (x - 1) * 3 : index;
                            int rightIndex = (x < width - 1) ? y * stride + (x + 1) * 3 : index;
                            int topIndex = (y > 0) ? (y - 1) * stride + x * 3 : index;
                            int bottomIndex = (y < height - 1) ? (y + 1) * stride + x * 3 : index;

                            float contrast = Math.Abs(ptr[index] - ptr[leftIndex]) * 0.2126f +
                                             Math.Abs(ptr[index + 1] - ptr[rightIndex]) * 0.7152f +
                                             Math.Abs(ptr[index + 2] - ptr[topIndex]) * 0.0722f;

                            localContrastSum += (long)contrast;
                        }
                    }

                    Interlocked.Add(ref contrastSum, localContrastSum);
                });
            }

            image.UnlockBits(bmpData);
            return contrastSum / (float)(totalPixels * 255);
        }

        internal OnyxProcessingMode AutoDetectMode(Bitmap image)
        {
            float[] brightnessRegions = this.GetRegionalBrightness(image);
            float avgBrightness = brightnessRegions.Average();

            float complexity = this.GetImageComplexity(image);
            float brightness = this.AdjustBrightnessSensitivity(image);
            float contrast = this.GetImageContrast(image);

            float highKeyScore = (avgBrightness * 0.6f) + (contrast * 0.4f);
            float lowLightScore = ((1.0f - avgBrightness) * 0.7f) + (complexity * 0.3f);
            float portraitScore = (complexity * 0.8f) + ((1.0f - contrast) * 0.2f);
            float landscapeScore = contrast * 0.9f + complexity * 0.1f;

            portraitScore += (complexity < 0.1f) ? 0.15f : 0f;
            landscapeScore += (contrast > 0.6f) ? 0.15f : 0f;
            lowLightScore += (contrast < 0.25f) ? 0.1f : 0f;
            highKeyScore += (avgBrightness > 0.8f && contrast > 0.3f) ? 0.1f : 0f;

            float maxScore = Math.Max(Math.Max(highKeyScore, lowLightScore), Math.Max(portraitScore, landscapeScore));

            if (maxScore == highKeyScore) return OnyxProcessingMode.HighKey;
            if (maxScore == lowLightScore) return OnyxProcessingMode.LowLight;
            if (maxScore == portraitScore) return OnyxProcessingMode.Portrait;

            return OnyxProcessingMode.Landscape;
        }

        internal Bitmap UpscaleImage(Bitmap image, Size size)
        {
            int newWidth = size.Width, newHeight = size.Height;

            Bitmap upscaledImage = new(newWidth, newHeight, image.PixelFormat);

            using Graphics g = Graphics.FromImage(upscaledImage);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, 0, 0, newWidth, newHeight);

            return upscaledImage;
        }

        #endregion

        #region Load Image

        public void LoadSourceImage(string filePath, CancellationToken cancellationToken)
        {
            this.DisposeCachedData();

            this.cachedImage = (Bitmap)Image.FromFile(filePath).Clone();

            using Bitmap resizedImage = new(this.cachedImage, new Size(ModelExpectedWidth, ModelExpectedHeight));
            var inputTensor = ImageToTensor(resizedImage, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var inputName = session.InputMetadata.Keys.First();
            var inputs = new NamedOnnxValue[] { NamedOnnxValue.CreateFromTensor<float>(inputName, inputTensor) };

            var results = session.Run(inputs).ToArray();

            string outputName = session.OutputMetadata.Keys.First();
            this.cachedTensor = results.First(r => r.Name == outputName).AsTensor<float>();
            this.ProcessingMode = this.AutoDetectMode(this.cachedImage);
        }

        #endregion

        #region Cached image and tensor

        internal Bitmap GetCachedImage()
        {
            if (this.cachedImage == null)
            {
                throw new InvalidOperationException("Cached image is not loaded. Ensure that an image is properly set before accessing.");
            }

            try
            {
                return (Bitmap)this.cachedImage.Clone();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to clone the cached image.", ex);
            }
        }

        internal void DisposeCachedData()
        {
            this.cachedImage?.Dispose();
            this.cachedImage = null;
            this.cachedTensor = null;
        }

        #endregion

        #endregion
    }
}