#region Using directives

using CascadePass.Glazier.Core;
using Microsoft.Win32;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Bitmap = System.Drawing.Bitmap;
using Size = System.Windows.Size;

#endregion

namespace CascadePass.Glazier.UI
{
    public class GlazierViewModel : ViewModel
    {
        #region Fields

        public bool isViewingMask;
        private int colorSimilarity;
        private string sourceFilename;
        private BitmapImage image, previewImage;
        private Color replacementColor;
        private ObservableCollection<NamedColor> commonImageColors;
        private ObservableCollection<SizeViewModel> imageOutputSizes;
        private SizeViewModel outputSize;

        private GlazeMethod glazeMethod;
        private ImageGlazier imageGlazier;
        private OnyxBackgroundRemover onyx;

        private IFileDialogProvider dialogProvider;

        private object threadKey;

        private DelegateCommand browseForImageFile, saveImageData, viewLargePreviewCommand, viewMaskCommand;

        private CancellationTokenSource mlProcessingCancellationToken;
        private readonly DispatcherTimer debounceTimer;

        #endregion

        #region Constructor

        public GlazierViewModel()
        {
            this.ReplacementColor = Colors.White;
            this.ImageGlazier = new();
            this.ImageColors = [];
            this.colorSimilarity = 30;

            this.Sizes = [];
            this.FileDialogProvider = new FileDialogProvider();

            this.threadKey = new();

            this.GlazeMethod = GlazeMethod.MachineLearning;

            this.debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            this.debounceTimer.Tick += this.ApplyDebouncedThreshold;

            BindingOperations.EnableCollectionSynchronization(this.imageOutputSizes, this.threadKey);



            //TODO: Read from config file
            this.Sizes.Add(new(new(16, 16)));
            this.Sizes.Add(new(new(32, 32)));
            this.Sizes.Add(new(new(64, 64)));
            this.Sizes.Add(new(new(128, 128)));
            this.Sizes.Add(new(new(256, 256)));

            //TODO: Read model location from config file?
            this.LoadOnyxModel();
        }

        #endregion

        #region Properties

        public string SourceFilename
        {
            get => this.sourceFilename;
            set
            {
                bool changed = this.SetPropertyValue(ref this.sourceFilename, value, nameof(this.SourceFilename));

                if (changed)
                {
                    this.LoadSourceImage();
                }
            }
        }

        public int ColorSimilarityThreshold
        {
            get => this.colorSimilarity;
            set
            {
                bool changed = this.SetPropertyValue(ref this.colorSimilarity, value, nameof(this.ColorSimilarityThreshold));

                if (changed && this.ImageData is not null)
                {
                    // Reset debounce timer
                    debounceTimer.Stop();
                    debounceTimer.Start();
                }
            }
        }

        public BitmapImage ImageData
        {
            get => this.image;
            set => this.SetPropertyValue(ref this.image, value, [nameof(this.ImageData), nameof(this.IsImageLoaded), nameof(this.IsImageNeeded), nameof(this.Sizes)]);
        }

        public BitmapImage PreviewImage
        {
            get => this.previewImage;
            set => this.SetPropertyValue(ref this.previewImage, value, nameof(this.PreviewImage));
        }

        public Color ReplacementColor
        {
            get => this.replacementColor;
            set
            {
                bool changed = this.SetPropertyValue(ref this.replacementColor, value, [nameof(this.ReplacementColor), nameof(this.SimilarityPreview)]);

                if (changed && this.ImageData is not null)
                {
                    this.GeneratePreviewImage();
                }
            }
        }

        public ImageGlazier ImageGlazier
        {
            get => this.imageGlazier;
            set => this.SetPropertyValue(ref this.imageGlazier, value, nameof(this.ImageGlazier));
        }

        public ObservableCollection<NamedColor> ImageColors
        {
            get => this.commonImageColors;
            set => this.SetPropertyValue(ref this.commonImageColors, value, nameof(this.ImageColors));
        }

        public BitmapSource SimilarityPreview
        {
            get
            {
                Rgba32 targetColor = new(this.ReplacementColor.R, this.ReplacementColor.G, this.ReplacementColor.B, 255);

                BitmapSource colorRangeImage = ImageGlazier.GenerateColorRangeImageSource(targetColor, this.ColorSimilarityThreshold, 256, 256);
                return colorRangeImage;
            }
        }

        public GlazeMethod GlazeMethod
        {
            get => this.glazeMethod;
            set
            {
                bool changed = this.SetPropertyValue(ref this.glazeMethod, value, [nameof(this.GlazeMethod), nameof(this.IsColorNeeded)]);

                if (changed)
                {
                    this.GeneratePreviewImage();
                }
            }
        }

        public IEnumerable<GlazeMethodViewModel> GlazeMethods => GlazeMethodViewModel.GetMethods();

        public ObservableCollection<SizeViewModel> Sizes
        {
            get => this.imageOutputSizes;
            set => this.SetPropertyValue(ref this.imageOutputSizes, value, nameof(this.Sizes));
        }

        public SizeViewModel SelectedSize
        {
            get => this.outputSize;
            set => this.SetPropertyValue(ref this.outputSize, value, nameof(this.SelectedSize));
        }

        public bool IsMaskVisible
        {
            get => this.isViewingMask;
            set => this.SetPropertyValue(ref this.isViewingMask, value, nameof(this.IsMaskVisible));
        }

        public bool IsSourceFilenameValid => !string.IsNullOrEmpty(this.SourceFilename) && File.Exists(this.SourceFilename);

        public IFileDialogProvider FileDialogProvider
        {
            get => this.dialogProvider;
            set => this.SetPropertyValue(ref this.dialogProvider, value, nameof(this.FileDialogProvider));
        }

        #region Visibility

        public bool IsImageLoaded => this.ImageData is not null;

        public bool IsImageNeeded => this.ImageData is null;

        public bool IsColorNeeded => this.GlazeMethod == GlazeMethod.ColorReplacement;

        #endregion

        #region Commands

        public ICommand BrowseForImageFileCommand => this.browseForImageFile ??= new(this.BrowseForImageFileImplementation);

        public ICommand SaveImageFileCommand => this.saveImageData ??= new(this.SaveImageImplementation);

        public ICommand ViewLargePreviewCommand => this.viewLargePreviewCommand ??= new(this.ViewLargePreviewImplementation);

        public ICommand ViewMaskCommand => this.viewMaskCommand ??= new(this.ViewMaskImplementation);

        #endregion

        #endregion

        #region Methods

        internal void GeneratePreviewImage()
        {
            if (this.GlazeMethod == GlazeMethod.ColorReplacement)
            {
                this.GenerateColorReplacementPreview();
            }
            else if (this.GlazeMethod == GlazeMethod.MachineLearning)
            {
                this.GenerateOnyxPreview();
            }
        }

        internal void GenerateColorReplacementPreview()
        {
            var tempGlazier = new ImageGlazier
            {
                ImageData = this.ImageGlazier.ImageData.Clone(),
            };

            if (this.ColorSimilarityThreshold > 0)
            {
                tempGlazier.Glaze(ColorBridge.GetRgba32FromColor(this.ReplacementColor), this.ColorSimilarityThreshold);
            }

            this.PreviewImage = (BitmapImage)tempGlazier.ConvertToBitmapSource();

        }

        internal void GenerateOnyxPreview()
        {
            this.CancelPreviousProcessing();

            if (this.onyx is null)
            {
                return;
            }

            Task.Run(() =>
            {
                var processedImage = this.onyx.RemoveBackground(
                    this.ColorSimilarityThreshold,
                    mlProcessingCancellationToken.Token
                );

                if (processedImage != null && !this.mlProcessingCancellationToken.Token.IsCancellationRequested)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        this.PreviewImage = ConvertBitmapToBitmapImage(processedImage);
                    });
                }
            });

            // Cancel the operation after 5 seconds
            Task.Delay(5000).ContinueWith(_ => mlProcessingCancellationToken.Cancel());
        }

        public static BitmapImage ConvertBitmapToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using MemoryStream memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Makes it usable across threads

            return bitmapImage;
        }

        public static Bitmap ConvertBitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            using var memoryStream = new MemoryStream();
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            memoryStream.Position = 0;

            return new Bitmap(memoryStream);
        }

        private void GetMostCommonColors()
        {
            this.commonImageColors.Clear();
            var colors = this.ImageGlazier.GetMostCommonColors(100);

            int i = 0;
            foreach (var color in colors)
            {
                this.commonImageColors.Add(new()
                {
                    Name = $"{color.Value.ToString("#,##0")} pixels ({i++}th most common color)",
                    Color = Color.FromArgb(color.Key.A, color.Key.R, color.Key.G, color.Key.B),
                });
            }
        }

        internal void LoadOnyxModel()
        {
            try
            {                
                this.onyx = new(@"C:\dev\u2net.onnx");
            }
            catch (Exception)
            {
            }
        }

        internal void LoadSourceImage()
        {
            if (!this.IsSourceFilenameValid)
            {
                return;
            }

            if (this.ImageData is not null)
            {
                Size imageSize = new(this.ImageData.PixelWidth, this.ImageData.PixelHeight);
                SizeViewModel existingSize = this.Sizes.FirstOrDefault(s => s.Size == imageSize);

                if (existingSize != null)
                {
                    this.Sizes.Remove(existingSize);
                }
            }

            var bitmap = new BitmapImage();

            try
            {
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(this.SourceFilename, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            catch (Exception ex)
            {
                //TODO: Improve this

                MessageBox.Show(ex.Message);
                return;
            }

            this.ImageData = bitmap;
            this.ImageGlazier.LoadImage(this.SourceFilename);
            this.onyx.LoadSourceImage(this.SourceFilename, new());

            var color = this.ImageGlazier.GetMostCommonColors(1);

            if (color is not null)
            {
                this.ReplacementColor = new Color()
                {
                    A = color.Keys.First().A,
                    R = color.Keys.First().R,
                    G = color.Keys.First().G,
                    B = color.Keys.First().B
                };
            }

            this.GetMostCommonColors();
            this.GeneratePreviewImage();

            if (this.ImageData is not null)
            {
                Size imageSize = new(this.ImageData.PixelWidth, this.ImageData.PixelHeight);
                SizeViewModel existingSize = this.Sizes.FirstOrDefault(s => s.Size == imageSize);

                if (existingSize != null)
                {
                    this.SelectedSize = existingSize;
                }
                else
                {
                    SizeViewModel imageSizeVM = new(imageSize);
                    this.Sizes.Add(imageSizeVM);
                    this.SelectedSize = imageSizeVM;
                }
            }
        }

        private void ApplyDebouncedThreshold(object sender, EventArgs e)
        {
            this.debounceTimer.Stop();
            this.CancelPreviousProcessing();

            this.GeneratePreviewImage();
        }

        private void CancelPreviousProcessing()
        {
            if (this.mlProcessingCancellationToken != null)
            {
                this.mlProcessingCancellationToken.Cancel();
                this.mlProcessingCancellationToken.Dispose();
            }

            this.mlProcessingCancellationToken = new CancellationTokenSource();
        }

        internal async void SaveIconFile(string filename)
        {
            IconFileExporter iconMaker = new();

            if (this.GlazeMethod == GlazeMethod.ColorReplacement)
            {
                var processedImage = this.ImageGlazier.ConvertToBitmapSource();

                if (processedImage != null)
                {
                    var resizedImage = this.ResizeBitmap(GlazierViewModel.ConvertBitmapSourceToBitmap(processedImage), this.outputSize.Size);

                    iconMaker.SourceImage = GlazierViewModel.ConvertBitmapToBitmapImage(resizedImage);
                }
            }
            else if (this.GlazeMethod == GlazeMethod.MachineLearning)
            {
                this.CancelPreviousProcessing();

                var processedImage = await this.onyx.RemoveBackgroundAsync(
                    this.ColorSimilarityThreshold,
                    mlProcessingCancellationToken.Token
                );

                if (processedImage != null && !this.mlProcessingCancellationToken.Token.IsCancellationRequested)
                {
                    var resizedImage = this.ResizeBitmap(processedImage, this.outputSize.Size);

                    iconMaker.SourceImage = GlazierViewModel.ConvertBitmapToBitmapImage(resizedImage);
                }
            }

            iconMaker.Save(filename);
        }

        internal void SaveColorReplacementImage(string filename)
        {
            this.ImageGlazier.Glaze(ColorBridge.GetRgba32FromColor(this.ReplacementColor), this.ColorSimilarityThreshold);
            this.ImageData = (BitmapImage)this.ImageGlazier.ConvertToBitmapSource();
            this.ImageGlazier.SaveImage(filename/*, this.outputSize.Size*/);
        }

        internal void SaveOnyxImage(string filename)
        {
            this.CancelPreviousProcessing();

            Task.Run(() =>
            {
                var processedImage = this.onyx.RemoveBackground(this.ColorSimilarityThreshold, mlProcessingCancellationToken.Token);

                if (processedImage != null && !this.mlProcessingCancellationToken.Token.IsCancellationRequested)
                {
                    Size currentSize = new(this.ImageData.PixelWidth, this.ImageData.PixelHeight);

                    if (currentSize == this.outputSize.Size)
                    {
                        processedImage.Save(filename, ImageFormat.Png);
                    }
                    else
                    {
                        var resizedImage = this.ResizeBitmap(processedImage, this.outputSize.Size);
                        resizedImage.Save(filename, ImageFormat.Png);
                    }
                }
            });

            // Cancel the operation after 15 seconds
            Task.Delay(15000).ContinueWith(_ => mlProcessingCancellationToken.Cancel());
        }

        public Bitmap ResizeBitmap(Bitmap original, Size newSize)
        {
            int newWidth = (int)newSize.Width, newHeight = (int)newSize.Height;
            Bitmap resizedBitmap = new(newWidth, newHeight);

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(resizedBitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                graphics.DrawImage(original, 0, 0, newWidth, newHeight);
            }

            return resizedBitmap;
        }

        #region Command Implementations

        internal void BrowseForImageFileImplementation()
        {
            var filename = this.FileDialogProvider.BrowseToOpenImageFile();

            if (!string.IsNullOrWhiteSpace(filename))
            {
                this.SourceFilename = filename;
            }
        }

        internal void SaveImageImplementation()
        {
            var filename = this.FileDialogProvider.BrowseToSaveImageFile();

            if (!string.IsNullOrWhiteSpace(filename))
            {
                if (filename.ToLower().EndsWith(".ico"))
                {
                    this.SaveIconFile(filename);
                    return;
                }

                if (this.GlazeMethod == GlazeMethod.ColorReplacement)
                {
                    this.SaveColorReplacementImage(filename);
                }
                else if (this.GlazeMethod == GlazeMethod.MachineLearning)
                {
                    this.SaveOnyxImage(filename);
                }

                try
                {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filename));
                }
                catch (Exception)
                {
                }
            }
        }

        internal void ViewMaskImplementation()
        {
            if (this.IsMaskVisible)
            {
                using MemoryStream memoryStream = new();
                this.onyx.Mask.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;

                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                this.PreviewImage = bitmapImage;
            }
            else
            {
                // User may have edited the mask.
                this.GeneratePreviewImage();
            }
        }

        internal void ViewLargePreviewImplementation()
        {
            var backgroundBrush = Application.Current?.Resources?["CrosshatchBrush"] as Brush;

            ImageEditor imageEditor = new()
            {
                Image = this.PreviewImage,
                Background = backgroundBrush,
                AllowPreview = false,
            };

            Window previewWindow = new()
            {
                Title = "Preview Image",
                Content = imageEditor,
                WindowState = WindowState.Maximized,
                Background = backgroundBrush,
                DataContext = this,
            };

            previewWindow.ShowDialog();
        }

        #endregion

        #endregion
    }
}
