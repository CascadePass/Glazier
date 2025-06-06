using System;

namespace CascadePass.Glazier.UI
{
    public interface IThemeListener
    {
        event EventHandler ThemeChanged;

        void ApplyTheme();
        void ApplyTheme(GlazierTheme theme);
    }
}