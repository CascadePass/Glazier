using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CascadePass.Glazier.UI
{
    public class ThemeViewModel : ViewModel
    {
        private GlazierTheme theme;
        private string themeName, iconPath;

        #region Constructors

        public ThemeViewModel()
        {
        }

        public ThemeViewModel(GlazierTheme glazierTheme, string name)
        {
            this.theme = glazierTheme;
            this.themeName = name;
        }

        public ThemeViewModel(GlazierTheme glazierTheme, string name, string iconUriPath)
        {
            this.theme = glazierTheme;
            this.themeName = name;
            this.iconPath = iconUriPath;
        }

        #endregion

        #region Properties

        public GlazierTheme Theme
        {
            get => this.theme;
            set => this.SetPropertyValue(ref this.theme, value, nameof(this.Theme));
        }

        public string Name
        {
            get => this.themeName;
            set => this.SetPropertyValue(ref this.themeName, value, nameof(this.Name));
        }

        public string IconPath
        {
            get => this.iconPath;
            set => this.SetPropertyValue(ref this.iconPath, value, nameof(this.IconPath));
        }

        #endregion

        #region Methods

        public static ObservableCollection<ThemeViewModel> GetThemes()
        {
            ObservableCollection<ThemeViewModel> themes =
            [
                new(GlazierTheme.None, Resources.Theme_None, "pack://application:,,,/Images/Themed/InferMode.png"),
                new(GlazierTheme.Light, Resources.Theme_Light, "pack://application:,,,/Images/Themed/LightMode.png"),
                new(GlazierTheme.Dark, Resources.Theme_Dark, "pack://application:,,,/Images/Themed/DarkMode.png"),
                new(GlazierTheme.HighContrast, Resources.Theme_HighContrast, "pack://application:,,,/Images/Themed/HighContrastMode.png")
            ];

#if DEBUG

            // If an enum value is added, it needs to also be added to the list above.
            // This check will make it obvious if that is not done, in debug builds.

            foreach (GlazierTheme glazierTheme in Enum.GetValues<GlazierTheme>())
            {
                if (!themes.Any(t => t.Theme == glazierTheme))
                {
                    throw new InvalidOperationException($"Theme {glazierTheme} is missing from ThemeViewModel.GetThemes()");
                }
            }
#endif

            return themes;
        }

        public override string ToString()
        {
            return this.themeName ?? this.theme.ToString();
        }

        #endregion
    }
}