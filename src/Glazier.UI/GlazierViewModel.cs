﻿#region Using directives

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
        private SizeViewModel outputSize;

        private GlazeMethod glazeMethod;
        private ImageGlazier imageGlazier;
        private OnyxBackgroundRemover onyx;

        private IFileDialogProvider dialogProvider;

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

            this.FileDialogProvider = new FileDialogProvider();

            this.GlazeMethod = GlazeMethod.Onyx_MachineLearning;

            this.debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            this.debounceTimer.Tick += this.ApplyDebouncedThreshold;
        }

        #endregion

        #region Properties

        public Settings Settings { get; set; }

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

        public bool IsColorNeeded => this.GlazeMethod == GlazeMethod.Prism_ColorReplacement;

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
            if (this.GlazeMethod == GlazeMethod.Prism_ColorReplacement)
            {
                this.GeneratePrismPreview();
            }
            else if (this.GlazeMethod == GlazeMethod.Onyx_MachineLearning)
            {
                this.GenerateOnyxPreview();
            }
        }

        internal void GeneratePrismPreview()
        {
            if (this.ImageGlazier?.ImageData is null)
            {
                // The image hasn't been loaded yet, so we can't generate a preview.

                return;
            }

            var tempGlazier = new ImageGlazier
            {
                ImageData = this.ImageGlazier.ImageData.Clone(),
            };

            if (this.ColorSimilarityThreshold > 0)
            {
                tempGlazier.Glaze(ColorBridge.GetRgba32FromColor(this.ReplacementColor), this.ColorSimilarityThreshold);
            }

            this.PreviewImage = ImageFormatBridge.ToBitmapImage(tempGlazier.ImageData);
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
                        this.PreviewImage = ImageFormatBridge.ToBitmapImage(processedImage);
                    });
                }
            });

            // Cancel the operation after 5 seconds
            Task.Delay(5000).ContinueWith(_ => mlProcessingCancellationToken.Cancel());
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

        public void LoadOnyxModel(string filename)
        {
            ArgumentNullException.ThrowIfNull(filename, nameof(filename));

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Model file path cannot be null or empty.", nameof(filename));
            }

            if(!File.Exists(filename))
            {
                throw new FileNotFoundException("Model file not found.", filename);
            }

            try
            {                
                this.onyx = new(filename);
            }
            catch (Exception)
            {
                this.onyx = null;
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
                //SizeViewModel existingSize = this.Sizes.FirstOrDefault(s => s.Size == imageSize);

                //if (existingSize != null)
                //{
                //    this.Sizes.Remove(existingSize);
                //}
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
                //SizeViewModel existingSize = this.Sizes.FirstOrDefault(s => s.Size == imageSize);

                //if (existingSize != null)
                //{
                //    this.SelectedSize = existingSize;
                //}
                //else
                //{
                //    SizeViewModel imageSizeVM = new(imageSize);
                //    this.Sizes.Add(imageSizeVM);
                //    this.SelectedSize = imageSizeVM;
                //}
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

            if (this.GlazeMethod == GlazeMethod.Prism_ColorReplacement)
            {
                var processedImage = ImageFormatBridge.ToBitmapImage(this.ImageGlazier.ImageData);

                if (processedImage != null)
                {
                    var resizedImage = ImageResizer.ResizeBitmap(ImageFormatBridge.ToBitmap(processedImage), this.outputSize.Size);

                    iconMaker.SourceImage = ImageFormatBridge.ToBitmapImage(resizedImage);
                }
            }
            else if (this.GlazeMethod == GlazeMethod.Onyx_MachineLearning)
            {
                this.CancelPreviousProcessing();

                var processedImage = await this.onyx.RemoveBackgroundAsync(
                    this.ColorSimilarityThreshold,
                    mlProcessingCancellationToken.Token
                );

                if (processedImage != null && !this.mlProcessingCancellationToken.Token.IsCancellationRequested)
                {
                    var resizedImage = ImageResizer.ResizeBitmap(processedImage, this.outputSize.Size);

                    iconMaker.SourceImage = ImageFormatBridge.ToBitmapImage(resizedImage);
                }
            }

            iconMaker.Save(filename);
        }

        internal void SaveColorReplacementImage(string filename)
        {
            this.ImageGlazier.Glaze(ColorBridge.GetRgba32FromColor(this.ReplacementColor), this.ColorSimilarityThreshold);
            this.ImageData = ImageFormatBridge.ToBitmapImage(this.ImageGlazier.ImageData);
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
                        var resizedImage = ImageResizer.ResizeBitmap(processedImage, this.outputSize.Size);
                        resizedImage.Save(filename, ImageFormat.Png);
                    }
                }
            });

            // Cancel the operation after 15 seconds
            Task.Delay(15000).ContinueWith(_ => mlProcessingCancellationToken.Cancel());
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

                if (this.GlazeMethod == GlazeMethod.Prism_ColorReplacement)
                {
                    this.SaveColorReplacementImage(filename);
                }
                else if (this.GlazeMethod == GlazeMethod.Onyx_MachineLearning)
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
                if (this.GlazeMethod == GlazeMethod.Onyx_MachineLearning && this.onyx is not null)
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
                else if(this.GlazeMethod == GlazeMethod.Prism_ColorReplacement && this.ImageGlazier?.ImageData is not null)
                {
                    var mask = this.ImageGlazier.Mask ?? this.ImageGlazier.GenerateMask(ColorBridge.GetRgba32FromColor(this.ReplacementColor), this.ColorSimilarityThreshold);;

                    this.PreviewImage = ImageFormatBridge.ToBitmapImage(mask);
                }
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
                GlazierViewModel = this,
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
