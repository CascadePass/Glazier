using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace CascadePass.Glazier.UI
{
    public class WorkspaceViewModel : ViewModel
    {
        #region Fields

        private GlazierViewModel glazierViewModel;
        private Settings settings;
        private SettingsViewModel settingsViewModel;
        private bool isSettingsPageVisible;
        private FontFamily currentFont;

        private Brush imageBackgroundBrush;
        private GridLength originalImageColumnWidth;

        private IThemeListener themeListener;

        #endregion

        public WorkspaceViewModel()
        {
            this.Settings = this.GetSettings();
            this.SettingsViewModel = new(this.Settings);

            this.themeListener = new ThemeListener();
            this.themeListener.ApplyTheme(this.settings.Theme);

            this.originalImageColumnWidth = new GridLength(1, GridUnitType.Star);

            this.glazierViewModel = new GlazierViewModel() { Settings = this.settings };

            if (!string.IsNullOrWhiteSpace(this.Settings.ModelFile) && File.Exists(this.Settings.ModelFile))
            {
                this.glazierViewModel.LoadOnyxModel(this.Settings.ModelFile);
            }
            else
            {
                MessageBox.Show(
                    $"Can't load configured model '{this.Settings?.ModelFile}'",
                    "No Onxy Model",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                    );
            }
        }

        public WorkspaceViewModel(Settings settings)
        {
            this.Settings = settings;

            this.themeListener = new ThemeListener();
            this.themeListener.ApplyTheme(this.settings.Theme);

            this.originalImageColumnWidth = new GridLength(1, GridUnitType.Star);

            this.glazierViewModel = new GlazierViewModel() { Settings = this.settings };

            if (!string.IsNullOrWhiteSpace(this.Settings.ModelFile) && File.Exists(this.Settings.ModelFile))
            {
                this.glazierViewModel.LoadOnyxModel(this.Settings.ModelFile);
            }
        }

        public WorkspaceViewModel(Settings settings, IThemeListener themeListener)
        {
            this.Settings = settings;
            this.themeListener = themeListener;

            this.originalImageColumnWidth = new GridLength(1, GridUnitType.Star);

            this.glazierViewModel = new GlazierViewModel() { Settings = this.settings };

            if (!string.IsNullOrWhiteSpace(this.Settings.ModelFile) && File.Exists(this.Settings.ModelFile))
            {
                this.glazierViewModel.LoadOnyxModel(this.Settings.ModelFile);
            }
        }


        #region Properties

        public GlazierViewModel GlazierViewModel
        {
            get => this.glazierViewModel;
            set => this.SetPropertyValue(ref this.glazierViewModel, value, nameof(this.GlazierViewModel));
        }

        public Settings Settings
        {
            get => this.settings;
            set
            {
                if (value == null)
                {
                    throw new System.ArgumentNullException(nameof(value), "Settings cannot be null.");
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

        public SettingsViewModel SettingsViewModel
        {
            get => this.settingsViewModel/* ??= new(this.settings)*/;
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

        public string SettingsFilename { get; set; }

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
                ) as Brush;

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

        #region Event Handlers

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

        #endregion

        #endregion
    }
}
