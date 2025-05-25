namespace CascadePass.Glazier.UI
{
    public interface IThemeDetector
    {
        bool IsHighContrastEnabled { get; }

        bool IsInLightMode { get; }

        GlazierTheme GetTheme();

        string GetThemeName();
    }
}