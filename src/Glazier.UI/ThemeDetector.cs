using System.Windows;

namespace CascadePass.Glazier.UI
{
    public class ThemeDetector : IThemeDetector
    {
        private readonly IRegistryProvider registryProvider;

        #region Constructors

        public ThemeDetector()
        {
            this.registryProvider = new RegistryProvider();
        }

        public ThemeDetector(IRegistryProvider registryProviderToUse)
        {
            this.registryProvider = registryProviderToUse;
        }

        #endregion

        public IRegistryProvider RegistryProvider => this.registryProvider;

        public bool IsInLightMode
        {
            get
            {
                return
                    this.RegistryProvider.GetValue(
                        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "AppsUseLightTheme"
                        )
                    ?.Equals(1) ?? true;
            }
        }

        public bool IsHighContrastEnabled
        {
            get
            {
                return SystemParameters.HighContrast;
            }
        }

        public GlazierTheme GetTheme()
        {
            if (this.IsHighContrastEnabled)
            {
                return GlazierTheme.HighContrast;
            }

            if (this.IsInLightMode)
            {
                return GlazierTheme.Light;
            }

            return GlazierTheme.Dark;
        }

        public string GetThemeName()
        {
            if (this.IsHighContrastEnabled)
            {
                return "HighContrast.xaml";
            }

            if (this.IsInLightMode)
            {
                return "Light.xaml";
            }

            return "Dark.xaml";
        }
    }
}