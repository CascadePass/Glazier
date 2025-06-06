using System.Windows;
using System.Windows.Media;

namespace CascadePass.Glazier.UI
{
    public class SelectableBrushViewModel : ViewModel
    {
        private string displayName, key;
        private Brush brush;

        public string Key
        {
            get => this.key;
            set => this.SetPropertyValue(ref this.key, value, nameof(this.Key));
        }

        public string DisplayName
        {
            get => this.displayName;
            set => this.SetPropertyValue(ref this.displayName, value, nameof(this.DisplayName));
        }

        public Brush Brush => this.brush ??= Application.Current?.FindResource(this.Key) as Brush;
    }
}