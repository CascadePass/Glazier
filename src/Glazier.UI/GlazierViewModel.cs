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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Bitmap = System.Drawing.Bitmap;

#endregion

namespace CascadePass.Glazier.UI
{
    public class GlazierViewModel : ViewModel
    {
        #region Fields

        private int colorSimilarity;
        private string sourceFilename;
        private BitmapImage image, previewImage;
        private Color replacementColor;
        private ObservableCollection<NamedColor> commonImageColors;
        private GlazeMethod glazeMethod;
        private ImageGlazier imageGlazier;
        private OnyxBackgroundRemover onyx;

        private DelegateCommand browseForImageFile, saveImageData;

        private CancellationTokenSource mlProcessingCancellationToken;
        private readonly DispatcherTimer debounceTimer;

        #endregion

        public GlazierViewModel()
        {
            this.ReplacementColor = Colors.White;
            this.ImageGlazier = new();
            this.commonImageColors = [];
            this.colorSimilarity = 30;

            this.onyx = new(@"C:\dev\u2net.onnx");
            this.GlazeMethod = GlazeMethod.MachineLearning;

            debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            debounceTimer.Tick += this.ApplyDebouncedThreshold;
        }

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
            set => this.SetPropertyValue(ref this.image, value, [nameof(this.ImageData), nameof(this.IsImageLoaded), nameof(this.IsImageNeeded)]);
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
            set => this.SetPropertyValue(ref this.glazeMethod, value, [nameof(this.GlazeMethod), nameof(this.IsColorNeeded)]);
        }

        public IEnumerable<GlazeMethodViewModel> GlazeMethods => GlazeMethodViewModel.GetMethods();

        #region Visibility

        public bool IsImageLoaded => this.ImageData is not null;

        public bool IsImageNeeded => this.ImageData is null;

        public bool IsColorNeeded => this.GlazeMethod == GlazeMethod.ColorReplacement;

        #endregion

        #region Commands

        public ICommand BrowseForImageFileCommand => this.browseForImageFile ??= new(this.BrowseForImageFileImplementation);

        public ICommand SaveImageFileCommand => this.saveImageData ??= new(this.SaveImageImplementation);

        #endregion

        #endregion

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

            this.OnPropertyChanged(nameof(this.ImageData));
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

        internal void LoadSourceImage()
        {
            if (string.IsNullOrEmpty(this.SourceFilename))
            {
                return;
            }

            if (!File.Exists(this.SourceFilename))
            {
                return;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(this.SourceFilename, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // Ensures it's usable across threads

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
        }

        private void ApplyDebouncedThreshold(object sender, EventArgs e)
        {
            debounceTimer.Stop();
            this.CancelPreviousProcessing();

            this.GeneratePreviewImage();
        }

        private void CancelPreviousProcessing()
        {
            if (mlProcessingCancellationToken != null)
            {
                mlProcessingCancellationToken.Cancel();
                mlProcessingCancellationToken.Dispose();
            }

            mlProcessingCancellationToken = new CancellationTokenSource();
        }

        internal void SaveColorReplacementImage(string filename)
        {
            this.ImageGlazier.Glaze(ColorBridge.GetRgba32FromColor(this.ReplacementColor), this.ColorSimilarityThreshold);
            this.ImageData = (BitmapImage)this.ImageGlazier.ConvertToBitmapSource();
            this.ImageGlazier.SaveImage(filename);
        }

        internal void SaveOnyxImage(string filename)
        {
            this.CancelPreviousProcessing();

            Task.Run(() =>
            {
                var processedImage = this.onyx.RemoveBackground(this.ColorSimilarityThreshold, mlProcessingCancellationToken.Token);

                if (processedImage != null && !this.mlProcessingCancellationToken.Token.IsCancellationRequested)
                {
                    processedImage.Save(filename, ImageFormat.Png);
                }
            });

            // Cancel the operation after 15 seconds
            Task.Delay(15000).ContinueWith(_ => mlProcessingCancellationToken.Cancel());
        }

        #region Command Implementations

        private void BrowseForImageFileImplementation()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select an Image File",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                Multiselect = false
            };

            var result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                this.SourceFilename = openFileDialog.FileName;
            }
        }

        private void SaveImageImplementation()
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Save Image File",
                Filter = "Image Files|*.png|All Files|*.*",
                AddExtension = true,
                DefaultExt = "*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                string filename = dialog.FileName;

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

        #endregion
    }
}
