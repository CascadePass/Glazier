using Microsoft.Win32;
using System.Runtime.InteropServices;
using System;
using System.Windows;
using System.Windows.Interop;

namespace CascadePass.Glazier.UI
{
    public interface IThemeDetector
    {
        bool IsHighContrastEnabled { get; }
        bool IsInLightMode { get; }

        GlazierTheme GetTheme();

        string GetThemeName();
    }

    public class ThemeDetector : IThemeDetector
    {
        public bool IsInLightMode
        {
            get
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
                );

                return key?.GetValue("AppsUseLightTheme")?.Equals(1) ?? true;
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

    public class ThemeListener : Window
    {
        public ThemeListener()
        {
            this.ThemeDetector = new ThemeDetector();
        }

        protected IThemeDetector ThemeDetector { get; set; }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x001A) // WM_SETTINGCHANGE
            {
                if (Marshal.PtrToStringUni(lParam) == "WindowsThemeElement")
                {
                    // Theme changed, trigger an update!
                    this.ApplyTheme();
                }
            }
            return IntPtr.Zero;
        }

        protected void ApplyTheme()
        {
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
    }
}