using System;

namespace CascadePass.Glazier.UI
{
    public interface IThemeListener
    {
        event EventHandler ThemeChanged;
    }
}