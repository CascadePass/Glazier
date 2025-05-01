using SixLabors.ImageSharp.PixelFormats;

namespace CascadePass.Glazier.Core
{
    public static class ColorBridge
    {
        public static Rgba32 GetRgba32FromColor(System.Windows.Media.Color color)
        {
            return new Rgba32(color.R, color.G, color.B, color.A);
        }

        public static System.Windows.Media.Color GetColorFromRgba32(Rgba32 rgba)
        {
            return new System.Windows.Media.Color() { R = rgba.R, G = rgba.G, B = rgba.B, A = rgba.A };
        }
    }
}