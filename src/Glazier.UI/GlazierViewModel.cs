using Microsoft.Win32;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private ImageGlazier imageGlazier;
        private DelegateCommand browseForImageFile, saveImageData;

        #endregion

        public GlazierViewModel()
        {
            this.ReplacementColor = Colors.White;
            this.ImageGlazier = new();
            this.commonImageColors = [];
            this.colorSimilarity = 30;
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
                    this.GeneratePreviewImage();
                }
            }
        }

        public BitmapImage ImageData {
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
            set => this.SetPropertyValue(ref this.replacementColor, value, [nameof(this.ReplacementColor), nameof(this.SimilarityPreview)]);
        }

        public ImageGlazier ImageGlazier
        {
            get => this.imageGlazier;
            set => this.SetPropertyValue(ref this.imageGlazier, value, nameof(this.ImageGlazier));
        }

        public ObservableCollection<NamedColor> ImageColors {
            get => this.commonImageColors;
            set => this.SetPropertyValue(ref this.commonImageColors, value, nameof(this.ImageColors));
        }

        public BitmapSource SimilarityPreview
        {
            get
            {
                Rgba32 targetColor = new Rgba32(this.ReplacementColor.R, this.ReplacementColor.G, this.ReplacementColor.B, 255);

                BitmapSource colorRangeImage = ImageGlazier.GenerateColorRangeImageSource(targetColor, this.ColorSimilarityThreshold, 256, 256);
                return colorRangeImage;
            }
        }

        #region Visibility

        public bool IsImageLoaded => this.ImageData is not null;
        
        public bool IsImageNeeded => this.ImageData is null;

        #endregion

        #region Commands

        public ICommand BrowseForImageFileCommand => this.browseForImageFile ??= new(this.BrowseForImageFileImplementation);

        public ICommand SaveImageFileCommand => this.saveImageData ??= new(this.SaveImageImplementation);

        #endregion

        #endregion

        internal void GeneratePreviewImage()
        {
            var tempGlazier = new ImageGlazier
            {
                ImageData = this.ImageGlazier.ImageData.Clone(),
            };

            if (this.ColorSimilarityThreshold > 0)
            {
                tempGlazier.Glaze(this.ReplacementColor, this.ColorSimilarityThreshold);
            }

            this.PreviewImage = (BitmapImage)tempGlazier.ConvertToBitmapSource();
            this.OnPropertyChanged(nameof(this.ImageData));
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

            var color = this.ImageGlazier.GetMostCommonColors(1);
            this.ReplacementColor = new Color()
            {
                A = color.Keys.First().A,
                R = color.Keys.First().R,
                G = color.Keys.First().G,
                B = color.Keys.First().B
            };

            this.GetMostCommonColors();
            this.GeneratePreviewImage();
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

                this.ImageGlazier.Glaze(this.ReplacementColor, this.ColorSimilarityThreshold);
                this.ImageData = (BitmapImage)this.ImageGlazier.ConvertToBitmapSource();
                this.ImageGlazier.SaveImage(filename);

                this.GetMostCommonColors();

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
