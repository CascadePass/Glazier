using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CascadePass.Glazier.UI
{
    public class Settings : Observable
    {
        #region Fields

        private GlazierTheme theme;
        private GlazeMethod glazeMethod;
        private double fontSize;
        private bool showOriginalImage;
        private bool useAnimations;
        private bool autoSaveSettings;
        private string modelFile;
        private string fontFamily;
        private string backgroundBrushKey;
        private string settingsFilename;

        private ObservableCollection<Size> resizingOptions;

        private static SemaphoreSlim saveSemaphore;
        private CancellationTokenSource saveCancellationTokenSource;

        #endregion

        #region Constants

        public const string DEFAULT_FONT = "Seguoi UI";

        #endregion

        #region Constructors

        public Settings() {
            this.resizingOptions = [];

            this.saveCancellationTokenSource = new();
        }

        static Settings()
        {
            Settings.ValidBrushKeys = ["ContrastyBackgroundBrush", "CrosshatchBrush"];
            Settings.saveSemaphore = new(1, 1);
        }

        #endregion

        #region Properties

        public GlazierTheme Theme
        {
            get => this.theme;
            set => this.SetPropertyValue(ref this.theme, value, nameof(this.Theme));
        }

        public GlazeMethod GlazeMethod
        {
            get => this.glazeMethod;
            set => this.SetPropertyValue(ref this.glazeMethod, value, nameof(this.GlazeMethod));
        }

        public double FontSize
        {
            get => this.fontSize;
            set => this.SetPropertyValue(ref this.fontSize, this.ValidateFontSize(value), nameof(this.FontSize));
        }

        public string FontFamily
        {
            get => this.fontFamily;
            set => this.SetPropertyValue(ref this.fontFamily, this.ValidateFontFamily(value), nameof(this.FontFamily));
        }

        public bool ShowOriginalImage
        {
            get => this.showOriginalImage;
            set => this.SetPropertyValue(ref this.showOriginalImage, value, nameof(this.ShowOriginalImage));
        }

        public bool UseAnimation
        {
            get => this.useAnimations;
            set => this.SetPropertyValue(ref this.useAnimations, value, nameof(this.UseAnimation));
        }

        public string ModelFile
        {
            get => this.modelFile;
            set => this.SetPropertyValue(ref this.modelFile, this.ValidateModelFilename(value), nameof(this.ModelFile));
        }

        public string BackgroundBrushKey
        {
            get => this.backgroundBrushKey;
            set => this.SetPropertyValue(ref this.backgroundBrushKey, this.ValidateBackgroundBrushKey(value), nameof(this.BackgroundBrushKey));
        }

        [JsonIgnore]
        public bool AutoSaveSettings
        {
            get => this.autoSaveSettings;
            set => this.SetPropertyValue(ref this.autoSaveSettings, value, nameof(this.AutoSaveSettings));
        }

        [JsonIgnore]
        public string SettingsFilename
        {
            get => this.settingsFilename;
            set => this.SetPropertyValue(ref this.settingsFilename, value, nameof(this.SettingsFilename));
        }

        public ObservableCollection<Size> ResizingOptions
        {
            get => this.resizingOptions;
            set => this.SetPropertyValue(ref this.resizingOptions, this.ValidateSizeOptions(value), nameof(this.ResizingOptions));
        }

        #region Static Validation Properties

        public static List<string> ValidBrushKeys { get; private set; }

        #endregion

        #endregion

        #region Methods

        #region Property validation

        internal double ValidateFontSize(double fontSize) => Math.Clamp(fontSize, 7, 32);

        internal string ValidateFontFamily(string fontFamily)
        {
            // Should I verify the font family exists on the system?

            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                return Settings.DEFAULT_FONT;
            }

            return fontFamily;
        }

        internal string ValidateBackgroundBrushKey(string brushName)
        {
            if (!Settings.ValidBrushKeys.Contains(brushName))
            {
                return Settings.ValidBrushKeys.First();
            }

            return brushName;
        }

        internal string ValidateModelFilename(string filename)
        {
            if (File.Exists(filename))
            {
                return filename;
            }

            return null;
        }

        internal ObservableCollection<Size> ValidateSizeOptions(ObservableCollection<Size> sizes)
        {
            sizes ??= [];

            if (sizes.Count == 0)
            {
                foreach (var defaultSize in Settings.GetDefaultSizes())
                {
                    sizes.Add(defaultSize);
                }
            }

            // Eliminate duplicates

            return sizes;
        }

        #endregion

        #region Static Methods

        public static Settings GetDefault()
        {
            return new()
            {
                SettingsFilename = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Settings.GetApplicationName(),
                    "Settings.json"
                ),

                // None means Auto Detect, or follow system theme
                Theme = GlazierTheme.None,
                GlazeMethod = GlazeMethod.Prism_ColorReplacement,

                FontSize = 12,
                FontFamily = "Segoe UI",

                BackgroundBrushKey = "ContrastyBackgroundBrush",

                ShowOriginalImage = true,
                UseAnimation = true,

                AutoSaveSettings = true,

                ModelFile = "C:\\dev\\u2net.onnx",

                ResizingOptions = Settings.GetDefaultSizes()
            };
        }

        internal static string GetApplicationName() => Assembly.GetEntryAssembly()?.GetName()?.Name switch
        {
            null => "Glazier",
            "Glazier.UI" => "Glazier",
            "testhost" => "GlazierTestHost",
            var name => name
        };

        internal static ObservableCollection<Size> GetDefaultSizes() => [
            new(16, 16),
            new(32, 32),
            new(64, 64),
            new(128, 128),
            new(256, 256),
        ];

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes this Settings instance to a JSON string.
        /// </summary>
        public string ToJson(JsonSerializerOptions options)
        {
            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// Populates a new Settings instance from a JSON string.
        /// </summary>
        public static Settings FromJson(string json, JsonSerializerOptions options)
        {
            var obj = JsonSerializer.Deserialize<Settings>(json, options);

            return obj as Settings ?? throw new JsonException("Failed to deserialize Settings from JSON.");
        }

        /// <summary>
        /// Saves this Settings instance to a file as JSON.
        /// </summary>
        public void SaveToFile(string filePath, JsonSerializerOptions options)
        {
            var json = this.ToJson(options);
            this.EnsureValidFilePath(filePath);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads settings from a JSON file and updates this instance.
        /// </summary>
        public static Settings LoadFromFile(string filePath, JsonSerializerOptions options)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Settings file '{filePath}' not found.", filePath);
            }

            var json = File.ReadAllText(filePath);
            var settings = Settings.FromJson(json, options);

            settings.SettingsFilename = filePath;

            return settings;
        }

        #endregion

        #region File I/O

        public async Task SaveToFileAsync(string filePath, JsonSerializerOptions options)
        {
            var json = this.ToJson(options);
            this.EnsureValidFilePath(filePath);
            await File.WriteAllTextAsync(filePath, json);
        }

        public void Save()
        {
            this.SaveToFile(this.settingsFilename, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task SaveAsync()
        {
            // Cancel previous pending save request (if any)
            this.saveCancellationTokenSource.Cancel();
            this.saveCancellationTokenSource = new CancellationTokenSource();
            var token = this.saveCancellationTokenSource.Token;

            await Settings.saveSemaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested) return; // Skip if a newer request came in
                await this.SaveToFileAsync(this.settingsFilename, new JsonSerializerOptions { WriteIndented = true });
            }
            finally
            {
                Settings.saveSemaphore.Release();
            }
        }

        internal void EnsureValidFilePath(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                // Automatically creates all missing directories in the path
                Directory.CreateDirectory(directoryPath);
            }
        }

        #endregion

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if(this.autoSaveSettings && !string.IsNullOrEmpty(this.settingsFilename))
            {
                try
                {
                    _ = this.SaveAsync();
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion
    }
}
