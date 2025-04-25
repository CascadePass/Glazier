using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPicker),
                new PropertyMetadata(Colors.White, OnSelectedColorChanged));

        public static readonly DependencyProperty HexCodeProperty =
            DependencyProperty.Register("HexCode", typeof(string), typeof(ColorPicker),
                new PropertyMetadata("#FFFFFF", OnColorChannelChanged));

        public static readonly DependencyProperty RedProperty =
            DependencyProperty.Register("Red", typeof(byte), typeof(ColorPicker),
                new PropertyMetadata((byte)255, OnColorChannelChanged));

        public static readonly DependencyProperty GreenProperty =
            DependencyProperty.Register("Green", typeof(byte), typeof(ColorPicker),
                new PropertyMetadata((byte)255, OnColorChannelChanged));

        public static readonly DependencyProperty BlueProperty =
            DependencyProperty.Register("Blue", typeof(byte), typeof(ColorPicker),
                new PropertyMetadata((byte)255, OnColorChannelChanged));

        public static readonly DependencyProperty SuggestedColorsProperty =
            DependencyProperty.Register("SuggestedColors", typeof(ObservableCollection<NamedColor>), typeof(ColorPicker),
                new PropertyMetadata(null, null));

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(ColorPicker),
                new PropertyMetadata(false, null));

        #endregion

        public ColorPicker()
        {
            this.InitializeComponent();
            this.AvailableColors = [];
            this.WindowsColors = [];

            var namedColors = typeof(Colors)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => new NamedColor { Name = p.Name, Color = (Color)p.GetValue(null) })
                .ToList();

            foreach (var color in namedColors)
            {
                this.AvailableColors.Add(color);
            }

            var systemColors = typeof(SystemColors)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => new NamedColor { Name = p.Name, Color = (Color)p.GetValue(null) })
                .ToList();

            foreach (var color in systemColors)
            {
                this.WindowsColors.Add(color);
            }
        }

        #region Properties

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public string HexCode
        {
            get => (string)GetValue(HexCodeProperty);
            set => SetValue(HexCodeProperty, value);
        }

        public byte Red
        {
            get => (byte)GetValue(RedProperty);
            set => SetValue(RedProperty, value);
        }

        public byte Green
        {
            get => (byte)GetValue(GreenProperty);
            set => SetValue(GreenProperty, value);
        }

        public byte Blue
        {
            get => (byte)GetValue(BlueProperty);
            set => SetValue(BlueProperty, value);
        }

        public ObservableCollection<NamedColor> SuggestedColors
        {
            get => (ObservableCollection<NamedColor>)GetValue(SuggestedColorsProperty);
            set => SetValue(SuggestedColorsProperty, value);
        }
        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        #endregion

        public ObservableCollection<NamedColor> AvailableColors { get; set; }

        public ObservableCollection<NamedColor> WindowsColors { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.UpdateSelectedColor();
        }

        protected void ClosePopup()
        {
            this.IsExpanded = false;
            //this.ToggleButton.IsChecked = false;
        }

        private static void OnColorChannelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ColorPicker;
            control?.UpdateSelectedColor();
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorPicker control) return;

            var color = (Color)e.NewValue;
            control.Red = color.R;
            control.Green = color.G;
            control.Blue = color.B;
        }

        private void UpdateSelectedColor()
        {
            SelectedColor = Color.FromRgb(Red, Green, Blue);
            HexCode = $"#{Red.ToString("X2")}{Green.ToString("X2")}{Blue.ToString("X2")}";
            this.ToggleButton.Background = new SolidColorBrush(SelectedColor);

            this.GenerateGradient(Red, Green, Blue);
        }

        private void GenerateGradient(int redValue, int greenValue, int blueValue)
        {
            int width = 256;
            int height = 256;

            if (width < 256) width = 256;

            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

            // Lock the bitmap for writing
            bitmap.Lock();

            unsafe
            {
                // Get a pointer to the back buffer
                int* backBuffer = (int*)bitmap.BackBuffer;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Decide which channels to blend:
                        byte dynamicGreen = (byte)x; // Blend Green horizontally
                        byte dynamicBlue = (byte)y; // Blend Blue vertically

                        // Use provided Red as the fixed value
                        int color = (redValue << 16) | (dynamicGreen << 8) | dynamicBlue;

                        // Write the pixel color
                        backBuffer[y * width + x] = color;
                    }
                }
            }

            // Unlock and refresh the bitmap
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();

            // Display the bitmap in an Image control
            this.GradientImage.Source = bitmap;
        }

        private Color GetPixelColor(Image image, int x, int y)
        {
            if (image.Source is BitmapSource bitmapSource)
            {
                int stride = bitmapSource.PixelWidth * 4; // 4 bytes per pixel (BGRA format)
                byte[] pixels = new byte[stride * bitmapSource.PixelHeight];

                bitmapSource.CopyPixels(pixels, stride, 0);

                int index = (y * stride) + (x * 4); // Calculate pixel index
                if (index + 3 < pixels.Length) // Ensure index is valid
                {
                    return Color.FromArgb(255, pixels[index + 2], pixels[index + 1], pixels[index]);
                }
            }

            return Colors.Transparent; // If something goes wrong, return transparent
        }

        private void ColorPickerArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                // Get the click position relative to the image
                Point clickPosition = e.GetPosition(image);

                // Extract pixel color at the click position
                Color pixelColor = this.GetPixelColor(this.GradientImage, (int)clickPosition.X, (int)clickPosition.Y);

                this.SelectedColor = pixelColor;
                this.ClosePopup();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (button.Tag is not Color color)
            {
                return;
            }

            this.SelectedColor = color;
            this.ClosePopup();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border)
            {
                return;
            }

            if (border.Tag is not Color color)
            {
                return;
            }

            this.SelectedColor = color;
            this.ClosePopup();
        }
    }
}
