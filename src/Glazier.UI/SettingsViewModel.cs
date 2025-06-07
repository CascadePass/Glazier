using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CascadePass.Glazier.UI
{
    public class SettingsViewModel : ViewModel
    {
        #region Fields

        private bool isSettingsPageOpen;
        private Settings settings;

        private ObservableCollection<ThemeViewModel> availableThemes;

        private ThemeViewModel selectedTheme;
        private FontFamily selectedFont;
        private SelectableBrushViewModel selectedBrush;

        private DelegateCommand closeSettingsPageCommand, browseForOnxyModelFileCommand;

        private IFileDialogProvider fileDialogProvider;

        #endregion

        public EventHandler<GlazierTheme> ThemeChanged;

        #region Constructors

        public SettingsViewModel()
        {
            this.isSettingsPageOpen = true;

            this.AvailableFonts = [.. Fonts.SystemFontFamilies.OrderBy(f => f.Source)];
            this.AvailableBackgroundBrushes =
                [
                    new() { DisplayName = "Default Background Brush", Key = "ContrastyBackgroundBrush" },
                    new() { DisplayName = "Checkerboard", Key = "CrosshatchBrush" },
                ];

            this.AvailableSizes = [];

            this.fileDialogProvider = new FileDialogProvider();

            this.selectedFont = this.AvailableFonts.FirstOrDefault(f => f.Source == "Segoe UI");
            this.selectedBrush = this.AvailableBackgroundBrushes.FirstOrDefault();
            this.selectedTheme = this.AvailableThemes.FirstOrDefault(t => t.Theme == new ThemeDetector().GetTheme()) ?? this.AvailableThemes.FirstOrDefault();
        }

        public SettingsViewModel(Settings settingsObject) : this()
        {
            this.Settings = settingsObject;

            this.selectedFont = this.AvailableFonts.FirstOrDefault(f => f.Source == this.settings.FontFamily);
            this.selectedBrush = this.AvailableBackgroundBrushes.FirstOrDefault(b => b.Key == this.settings.BackgroundBrushKey);
            this.selectedTheme = this.AvailableThemes.FirstOrDefault(t => t.Theme == settings.Theme) ?? this.AvailableThemes.FirstOrDefault();

            if (this.settings.Theme == GlazierTheme.None)
            {
                this.selectedTheme = this.AvailableThemes.FirstOrDefault(t => t.Theme == new ThemeDetector().GetTheme()) ?? this.AvailableThemes.FirstOrDefault();
            }
        }

        #endregion

        #region Properties

        public bool IsSettingsPageOpen
        {
            get => isSettingsPageOpen;
            set => this.SetPropertyValue(ref this.isSettingsPageOpen, value, nameof(IsSettingsPageOpen));
        }

        public Settings Settings
        {
            get => this.settings;
            set
            {
                if (this.settings != value)
                {
                    if (this.settings is INotifyPropertyChanged oldSettings)
                    {
                        oldSettings.PropertyChanged -= Settings_PropertyChanged;

                        if(this.settings.ResizingOptions is not null)
                        {
                            this.settings.ResizingOptions.CollectionChanged -= this.Settings_ResizingOptions_CollectionChanged;
                        }
                    }

                    this.settings = value;
                    OnPropertyChanged(nameof(this.Settings));

                    if (this.settings is INotifyPropertyChanged newSettings)
                    {
                        newSettings.PropertyChanged += Settings_PropertyChanged;
                    }

                    if (settings.ResizingOptions is not null)
                    {
                        settings.ResizingOptions.CollectionChanged += this.Settings_ResizingOptions_CollectionChanged;
                    }

                    foreach (Size size in this.Settings.ResizingOptions)
                    {
                        this.AvailableSizes.Add(new(size));
                    }
                }
            }
        }

        public ThemeViewModel SelectedTheme
        {
            get => this.selectedTheme;
            set
            {
                var changed = this.SetPropertyValue(ref this.selectedTheme, value, nameof(this.SelectedTheme));

                if (changed)
                {
                    this.Settings.Theme = value.Theme;
                    this.OnThemeChanged(value.Theme);
                }
            }
        }

        public ObservableCollection<ThemeViewModel> AvailableThemes => this.availableThemes ??= ThemeViewModel.GetThemes();

        public ObservableCollection<FontFamily> AvailableFonts { get; }

        public ObservableCollection<SelectableBrushViewModel> AvailableBackgroundBrushes { get; set; }

        public ObservableCollection<SizeViewModel> AvailableSizes { get; set; }

        public FontFamily SelectedFont {
            get => this.selectedFont;
            set
            {
                var changed = this.SetPropertyValue(ref this.selectedFont, value, nameof(this.SelectedFont));
                if (changed)
                {
                    this.Settings.FontFamily = value.Source;
                }
            }
        }

        public SelectableBrushViewModel SelectedImageBackgroundBrush
        {
            get => this.selectedBrush;
            set
            {
                bool changed = this.SetPropertyValue(ref this.selectedBrush, value, nameof(this.SelectedImageBackgroundBrush));

                if (changed)
                {
                    this.Settings.BackgroundBrushKey = value.Key;
                }
            }
        }

        #endregion

        #region Commands

        public ICommand CloseSettingsCommand => this.closeSettingsPageCommand ??= new(this.CloseSettingsPageImplementation);

        public ICommand BrowseForOnyxModelCommand => this.browseForOnxyModelFileCommand ??= new(this.BrowseForOnyxModelFileImplementation);

        #endregion

        protected void CloseSettingsPageImplementation()
        {
            this.IsSettingsPageOpen = false;
        }

        protected void BrowseForOnyxModelFileImplementation()
        {
            var selected = this.fileDialogProvider.BrowseToOpenOnyxModelFile();

            if (!string.IsNullOrWhiteSpace(selected))
            {
                this.Settings.ModelFile = selected;
            }
        }

        public void UpdateFontSizes(double newMediumFontSize)
        {
            if (Application.Current?.Resources is not null)
            {
                double scaleFactor = newMediumFontSize / 12.0;
                var resourceDictionary = Application.Current.Resources;

                resourceDictionary["FontSize.Tiny"] = 9.0 * scaleFactor;
                resourceDictionary["FontSize.Small"] = 10.0 * scaleFactor;
                resourceDictionary["FontSize.Medium"] = newMediumFontSize;
                resourceDictionary["FontSize.Large"] = 16.0 * scaleFactor;
                resourceDictionary["FontSize.ExtraLarge"] = 18.0 * scaleFactor;
                resourceDictionary["FontSize.SubHeader"] = 20.0 * scaleFactor;
                resourceDictionary["FontSize.Header"] = 24.0 * scaleFactor;
                resourceDictionary["FontSize.Title"] = 32.0 * scaleFactor;
            }
        }

        #region Events

        protected void OnThemeChanged(GlazierTheme newTheme)
        {
            this.ThemeChanged?.Invoke(this, newTheme);
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not Settings settings)
            {
                return;
            }

            if(e.PropertyName == nameof(Settings.FontSize))
            {
                this.UpdateFontSizes(settings.FontSize);
            }

            // Bubble up property changes from Settings if needed
            this.OnPropertyChanged($"Settings.{e.PropertyName}");
        }

        private void Settings_ResizingOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
            {
                foreach (Size item in e.NewItems)
                {
                    this.AvailableSizes.Add(new(item));
                }
            }

            if (e.OldItems is not null)
            {
                foreach (Size item in e.OldItems)
                {
                    this.AvailableSizes.Remove(this.AvailableSizes.First(s => s.Size == item));
                }
            }
        }

        #endregion
    }
}