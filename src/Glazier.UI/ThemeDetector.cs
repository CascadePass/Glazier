using Microsoft.Win32;
using System;
using System.Threading.Tasks;
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

    public class ThemeListener : IThemeListener
    {
        public event EventHandler ThemeChanged;

        public ThemeListener()
        {
            this.ThemeDetector = new ThemeDetector();
            SystemEvents.UserPreferenceChanged += this.OnUserPreferenceChanged;

            this.ApplyTheme();
        }

        protected IThemeDetector ThemeDetector { get; set; }

        internal void ApplyTheme()
        {
            if (Application.Current is null)
            {
                return;
            }

            Application.Current.Resources.MergedDictionaries.Clear();

            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"Themes/{this.ThemeDetector.GetThemeName()}", UriKind.Relative)
            });


            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"Themes/Universal.xaml", UriKind.Relative)
            });
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                this.OnThemeChanged(sender, e);
            }
        }

        protected void OnThemeChanged(object sender, EventArgs e)
        {
            Task.Delay(100).ContinueWith(_ => this.ApplyTheme());
        }
    }
}