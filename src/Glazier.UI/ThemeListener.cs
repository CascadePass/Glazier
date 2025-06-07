using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Xaml;

namespace CascadePass.Glazier.UI
{
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

        #region ApplyTheme

        public void ApplyTheme()
        {
            var detectedTheme = this.ThemeDetector.GetTheme();
            this.ApplyTheme(detectedTheme);
        }

        public void ApplyTheme(GlazierTheme theme)
        {
            if (theme == GlazierTheme.None)
            {
                theme = this.ThemeDetector.GetTheme();
            }

            string themeName = theme.ToString();

            this.ApplyTheme(themeName);
        }

        internal void ApplyTheme(string themeName)
        {
            if (!Enum.TryParse(themeName, out GlazierTheme selectedTheme))
            {
                throw new ArgumentException($"Invalid theme name: {themeName}", nameof(themeName));
            }

            if (Application.Current?.Resources is null)
            {
                return;
            }

            if (string.Equals(themeName, this.ThemeDetector.GetThemeName(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                ResourceDictionary
                    themeDictionary = new() { Source = new Uri($"/Themes/{themeName}.xaml", UriKind.Relative) },
                    universalDictionary = new() { Source = new Uri($"/Themes/Universal.xaml", UriKind.Relative) };

                if (themeDictionary is not null && universalDictionary is not null)
                {
                    Application.Current.Resources.MergedDictionaries.Clear();

                    Application.Current.Resources.MergedDictionaries.Add(themeDictionary);
                    Application.Current.Resources.MergedDictionaries.Add(universalDictionary);

                    this.ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (UriFormatException)
            {
            }
            catch (XamlParseException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        #endregion

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