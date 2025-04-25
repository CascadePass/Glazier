using System.Windows.Media;

namespace CascadePass.Glazier.UI
{
    public class NamedColor
    {
        public string Name { get; set; }

        public Color Color { get; set; }

        public SolidColorBrush Brush => new(this.Color);
    }
}
