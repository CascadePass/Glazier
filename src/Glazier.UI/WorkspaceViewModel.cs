using CascadePass.Glazier.Core;
using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI
{
    public class WorkspaceViewModel : ViewModel
    {
        #region Fields

        private GlazierViewModel glazierViewModel;
        private Settings settings;
        private SettingsViewModel settingsViewModel;
        private bool isSettingsPageVisible;
        private bool isOnyxModelLoaded;
        private bool isViewingMask;
        private FontFamily currentFont;
        private string sourceFilename;

        private Brush imageBackgroundBrush;
        private GridLength originalImageColumnWidth;

        private ObservableCollection<GlazeMethodViewModel> availableGlazeMethods;

        private IThemeListener themeListener;
        private IFileDialogProvider dialogProvider;

        private DelegateCommand browseForImageFile, saveImageData, viewLargePreviewCommand, viewMaskCommand, editSettingsCommand;

        #endregion

        #region Constructors

        public WorkspaceViewModel()
        {
            this.Settings = this.GetSettings();

            this.CreateVisualProperties(new ThemeListener());

            this.SettingsViewModel = new(this.Settings);
            this.AvailableGlazeMethods = this.CreateGlazeMethodViewModels();
            this.GlazierViewModel = new GlazierViewModel() { Settings = this.settings };

            this.SelectedGlazeMethod = this.AvailableGlazeMethods.FirstOrDefault(m => m.Method == GlazeMethod.Prism_ColorReplacement);
            this.LoadOnyxModel(this.Settings.ModelFile);
        }

        public WorkspaceViewModel(Settings settings)
        {
            this.Settings = settings;

            this.CreateVisualProperties(new ThemeListener());

            this.SettingsViewModel = new(this.Settings);
            this.AvailableGlazeMethods = this.CreateGlazeMethodViewModels();
            this.GlazierViewModel = new GlazierViewModel() { Settings = this.settings };

            this.SelectedGlazeMethod = this.AvailableGlazeMethods.FirstOrDefault(m => m.Method == GlazeMethod.Prism_ColorReplacement);
            this.LoadOnyxModel(this.Settings.ModelFile);
        }

        public WorkspaceViewModel(Settings settings, IThemeListener themeListener)
        {
            this.Settings = settings;

            this.CreateVisualProperties(themeListener);

            this.SettingsViewModel = new(this.Settings);
            this.AvailableGlazeMethods = this.CreateGlazeMethodViewModels();
            this.GlazierViewModel = new GlazierViewModel() { Settings = this.settings };

            this.SelectedGlazeMethod = this.AvailableGlazeMethods.FirstOrDefault(m => m.Method == GlazeMethod.Prism_ColorReplacement);
            this.LoadOnyxModel(this.Settings.ModelFile);
        }

        #endregion

        #region Properties


        #region Settings

        public string SettingsFilename { get; set; }

        /// <summary>
        /// Gets or sets the application settings.
        /// </summary>
        /// <remarks>When the settings are updated, the property ensures that event handlers for property
        /// changes are properly managed. Assigning a new value will detach event handlers from the old settings
        /// instance (if any) and attach them to the new instance.</remarks>
        /// <exception cref="ArgumentNullException">Thrown when the value is set to null.</exception>"
        public Settings Settings
        {
            get => this.settings;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "Settings cannot be null.");
                }

                var old = this.settings;
                bool changed = this.SetPropertyValue(ref this.settings, value, nameof(this.Settings));

                if (changed)
                {
                    if (old is not null)
                    {
                        old.PropertyChanged -= this.Settings_PropertyChanged;
                    }

                    this.Settings.PropertyChanged += this.Settings_PropertyChanged;
                }
            }
        }

        #endregion

        #region GlazierViewModel

        /// <summary>
        /// Gets or sets the view model for the glazier component.
        /// </summary>
        /// <remarks>When the property value changes, event handlers are updated to reflect the new
        /// instance.  Ensure that the new value is properly initialized before setting this property to avoid
        /// unexpected behavior.</remarks>
        public GlazierViewModel GlazierViewModel
        {
            get => this.glazierViewModel;
            set
            {
                var old = this.glazierViewModel;
                var changed = this.SetPropertyValue(ref this.glazierViewModel, value, nameof(this.GlazierViewModel));

                if (changed)
                {
                    if (old is not null)
                    {
                        old.PropertyChanged -= this.GlazierViewModel_PropertyChanged;
                    }
                    if (value is not null)
                    {
                        value.PropertyChanged += this.GlazierViewModel_PropertyChanged;
                    }
                }
            }
        }

        #endregion

        #region SettingsViewModel

        public SettingsViewModel SettingsViewModel
        {
            get => this.settingsViewModel;
            set
            {
                var old = this.settingsViewModel;
                bool changed = this.SetPropertyValue(ref this.settingsViewModel, value, nameof(this.SettingsViewModel));

                if (changed)
                {
                    if (old is not null)
                    {
                        old.PropertyChanged -= this.SettingsViewModel_PropertyChanged;
                        old.ThemeChanged -= this.SettingsViewModel_ThemeChanged;
                    }
                    if (value is not null)
                    {
                        value.PropertyChanged += this.SettingsViewModel_PropertyChanged;
                        value.ThemeChanged += this.SettingsViewModel_ThemeChanged;
                    }
                }
            }
        }

        #endregion

        public ObservableCollection<GlazeMethodViewModel> AvailableGlazeMethods
        {
            get => this.availableGlazeMethods;
            set => this.SetPropertyValue(ref this.availableGlazeMethods, value, nameof(this.AvailableGlazeMethods));
        }

        public bool IsSettingsPageVisible
        {
            get => this.isSettingsPageVisible;
            set
            {
                bool changed = this.SetPropertyValue(ref this.isSettingsPageVisible, value, nameof(this.IsSettingsPageVisible));

                if (changed)
                {
                    this.SettingsViewModel.IsSettingsPageOpen = value;
                }
            }
        }

        public GridLength OriginalImageColumnWidth
        {
            get => this.originalImageColumnWidth;
            set
            {
                var changed = this.SetPropertyValue(ref this.originalImageColumnWidth, value, nameof(this.OriginalImageColumnWidth));
                if (changed)
                {
                    this.IsSettingsPageVisible = this.originalImageColumnWidth.Value > 5;
                }
            }
        }

        public FontFamily CurrentFont
        {
            get => this.currentFont ??= new FontFamily("Segoe UI");
            set => this.SetPropertyValue(ref this.currentFont, value, nameof(this.CurrentFont));
        }

        public Brush ImageBackgroundBrush =>
            this.imageBackgroundBrush ??=
                Application.Current?.FindResource(
                    this.Settings.BackgroundBrushKey ??
                    "ContrastyBackgroundBrush"
                )
            as Brush;

        public GlazeMethodViewModel SelectedGlazeMethod
        {
            get => this.AvailableGlazeMethods.FirstOrDefault(m => m.IsSelected);
            set
            {
                if (value is not null)
                {
                    foreach (var method in this.AvailableGlazeMethods)
                    {
                        method.IsSelected = (method == value) && (method.IsEnabled);
                    }

                    this.GlazierViewModel.GlazeMethod = value.Method;
                }
            }
        }

        public IFileDialogProvider FileDialogProvider
        {
            get => this.dialogProvider;
            set => this.SetPropertyValue(ref this.dialogProvider, value, nameof(this.FileDialogProvider));
        }

        public string SourceFilename
        {
            get => this.sourceFilename;
            set
            {
                bool changed = this.SetPropertyValue(ref this.sourceFilename, value, [nameof(this.SourceFilename), nameof(this.IsSourceFilenameValid)]);

                if (changed)
                {
                    this.LoadSourceImage();
                }
            }
        }

        public bool IsSourceFilenameValid => !string.IsNullOrEmpty(this.SourceFilename) && File.Exists(this.SourceFilename);

        public bool IsMaskVisible
        {
            get => this.isViewingMask;
            set => this.SetPropertyValue(ref this.isViewingMask, value, nameof(this.IsMaskVisible));
        }


        #region Visibility

        public bool IsImageLoaded => this.GlazierViewModel.ImageData is not null;

        public bool IsImageNeeded => this.GlazierViewModel.ImageData is null;

        public bool IsColorNeeded => this.GlazierViewModel.GlazeMethod == GlazeMethod.Prism_ColorReplacement;

        #endregion

        #region Commands

        public ICommand BrowseForImageFileCommand => this.browseForImageFile ??= new(this.BrowseForImageFileImplementation);

        public ICommand SaveImageFileCommand => this.saveImageData ??= new(this.SaveImageImplementation);

        public ICommand ViewLargePreviewCommand => this.viewLargePreviewCommand ??= new(this.ViewLargePreviewImplementation);

        public ICommand ViewMaskCommand => this.viewMaskCommand ??= new(this.ViewMaskImplementation);

        public ICommand EditSettingsCommand => this.editSettingsCommand ??= new(this.EditSettingsImplementation);

        #endregion


        #endregion

        #region Methods

        internal Settings GetSettings()
        {

            if (this.settings is not null)
            {
                return this.settings;
            }

            this.SettingsFilename ??= Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Glazier",
                    "Settings.json"
                );

            if (File.Exists(this.SettingsFilename))
            {
                this.settings = Settings.LoadFromFile(this.SettingsFilename, new());
            }

            this.settings ??= Settings.GetDefault();
            this.settings.SettingsFilename = this.SettingsFilename;
            this.settings.AutoSaveSettings = true;

            return this.settings;
        }

        internal void LoadOnyxModel(string modelFile)
        {
            var onyxMethodVM = this.availableGlazeMethods.FirstOrDefault(m => m.Method == GlazeMethod.Onyx_MachineLearning);

            if (!string.IsNullOrWhiteSpace(modelFile) && File.Exists(modelFile))
            {
                this.glazierViewModel.LoadOnyxModel(modelFile);
                this.isOnyxModelLoaded = true;

                if (onyxMethodVM is not null)
                {
                    onyxMethodVM.IsEnabled = true;
                    onyxMethodVM.CurrentStatus = string.Empty;

                    this.SelectedGlazeMethod = onyxMethodVM;
                }
            }
            else
            {
                if(!string.IsNullOrWhiteSpace(modelFile))
                {
                    if (onyxMethodVM is not null)
                    {
                        onyxMethodVM.CurrentStatus = $"Model file '{modelFile}' not found.";
                    }
                }
                else
                {
                    if (onyxMethodVM is not null)
                    {
                        onyxMethodVM.CurrentStatus = "No model file configured.";
                    }
                }

#if RELEASE
                MessageBox.Show(
                    $"Can't load configured model '{modelFile}'",
                    "No Onyx Model",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
#endif
            }
        }

        internal void LoadSourceImage()
        {
            if (!this.IsSourceFilenameValid)
            {
                return;
            }

            this.GlazierViewModel.LoadSourceImage(this.SourceFilename);
        }

        internal ObservableCollection<GlazeMethodViewModel> CreateGlazeMethodViewModels()
        {
            ObservableCollection<GlazeMethodViewModel> result = [.. GlazeMethodViewModel.GetMethods()];

            foreach (var item in result)
            {
                if (item.Method == GlazeMethod.Onyx_MachineLearning)
                {
                    if (!string.IsNullOrWhiteSpace(this.Settings?.ModelFile))
                    {
                        item.ModelPath = this.Settings.ModelFile;
                    }

                    if (!this.isOnyxModelLoaded)
                    {
                        item.IsEnabled = false;
                        item.CurrentStatus = "Model not loaded.";
                    }
                }

                item.PropertyChanged += this.GlazeMethodViewModel_PropertyChanged;
            }

            return result;
        }

        internal void CreateVisualProperties(IThemeListener themeListener)
        {
            this.FileDialogProvider = new FileDialogProvider();

            this.themeListener = themeListener;
            this.themeListener.ApplyTheme(this.settings.Theme);

            this.originalImageColumnWidth = new GridLength(1, GridUnitType.Star);
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
                    this.GlazierViewModel.SaveIconFile(filename);
                    return;
                }

                if (this.GlazierViewModel.GlazeMethod == GlazeMethod.Prism_ColorReplacement)
                {
                    this.GlazierViewModel.SaveColorReplacementImage(filename);
                }
                else if (this.GlazierViewModel.GlazeMethod == GlazeMethod.Onyx_MachineLearning)
                {
                    this.GlazierViewModel.SaveOnyxImage(filename);
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
                if (this.GlazierViewModel.GlazeMethod == GlazeMethod.Onyx_MachineLearning)
                {
                    this.GlazierViewModel.GetOnyxMask();
                }
                else if (this.GlazierViewModel.GlazeMethod == GlazeMethod.Prism_ColorReplacement && this.GlazierViewModel.ImageGlazier?.ImageData is not null)
                {
                    var mask = this.GlazierViewModel.ImageGlazier.Mask ?? this.GlazierViewModel.ImageGlazier.GenerateMask(ColorBridge.GetRgba32FromColor(this.GlazierViewModel.ReplacementColor), this.GlazierViewModel.ColorSimilarityThreshold); ;

                    this.GlazierViewModel.PreviewImage = ImageFormatBridge.ToBitmapImage(mask);
                }
            }
            else
            {
                // User may have edited the mask.
                this.GlazierViewModel.GeneratePreviewImage();
            }
        }

        internal void ViewLargePreviewImplementation()
        {
            var backgroundBrush = Application.Current?.Resources?["CrosshatchBrush"] as Brush;

            ImageEditor imageEditor = new()
            {
                GlazierViewModel = this.GlazierViewModel,
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

        internal void EditSettingsImplementation()
        {
            this.IsSettingsPageVisible = true;
            this.SettingsViewModel.IsSettingsPageOpen = true;
        }

        #endregion

        #region Event Handlers

        private void GlazierViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(GlazierViewModel.ImageData), StringComparison.Ordinal))
            {
                this.OnPropertyChanged(nameof(this.IsImageLoaded));
                this.OnPropertyChanged(nameof(this.IsImageNeeded));
            }
            //else if (string.Equals(e.PropertyName, nameof(GlazierViewModel.PreviewImage), StringComparison.Ordinal))
            //{
            //    this.OnPropertyChanged(nameof(this.GlazierViewModel.PreviewImage));
            //}
            //else if (string.Equals(e.PropertyName, nameof(GlazierViewModel.ImageGlazier), StringComparison.Ordinal))
            //{
            //    this.OnPropertyChanged(nameof(this.GlazierViewModel.ImageGlazier));
            //}
            else if (string.Equals(e.PropertyName, nameof(GlazierViewModel.GlazeMethod), StringComparison.Ordinal))
            {
                this.SelectedGlazeMethod = this.AvailableGlazeMethods.FirstOrDefault(m => m.Method == this.GlazierViewModel.GlazeMethod);
                this.OnPropertyChanged(nameof(this.IsColorNeeded));
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.FontFamily))
            {
                this.CurrentFont = new FontFamily(this.Settings.FontFamily);
            }
            else if (e.PropertyName == nameof(Settings.ModelFile))
            {
                this.glazierViewModel.LoadOnyxModel(this.Settings.ModelFile);
            }
        }

        private void SettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.IsSettingsPageOpen))
            {
                this.IsSettingsPageVisible = this.SettingsViewModel.IsSettingsPageOpen;
            } else if (e.PropertyName == nameof(SettingsViewModel.SelectedImageBackgroundBrush))
            {
                this.imageBackgroundBrush = null;


                // This line shouldn't be necessary, but it is.
                // Probably removing backing fields from SettingsViewModel and using Settings directly will fix it.

                this.Settings.BackgroundBrushKey = this.SettingsViewModel.SelectedImageBackgroundBrush.Key;


                this.OnPropertyChanged(nameof(this.ImageBackgroundBrush));
            }
        }

        private void SettingsViewModel_ThemeChanged(object sender, GlazierTheme e)
        {
            this.themeListener?.ApplyTheme(this.Settings.Theme);
        }

        private void GlazeMethodViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlazeMethodViewModel.IsSelected))
            {
                var method = sender as GlazeMethodViewModel;
                if (method?.IsSelected == true)
                {
                    this.SelectedGlazeMethod = method;
                }
            }
        }

        #endregion

        #endregion
    }
}
