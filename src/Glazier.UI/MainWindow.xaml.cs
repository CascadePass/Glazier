using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemeListener
    {
        public MainWindow()
        {
            base.ApplyTheme();
            this.InitializeComponent();

            var vm = this.DataContext as ViewModel;

            vm.PropertyChanged += this.ViewModel_PropertyChanged;
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

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlazierViewModel.ImageData))
            {
                BindingExpression binding = BindingOperations.GetBindingExpression(this.DisplayImage, Image.SourceProperty);
                binding?.UpdateTarget();
            }
            else if (e.PropertyName == nameof(GlazierViewModel.PreviewImage))
            {
                BindingExpression binding = BindingOperations.GetBindingExpression(this.PreviewImage, Image.SourceProperty);
                binding?.UpdateTarget();
            }
            else if (e.PropertyName == nameof(GlazierViewModel.SourceFilename))
            {
                BindingExpression binding = BindingOperations.GetBindingExpression(this.InputFile, CommandTextBox.UserTextProperty);
                binding?.UpdateTarget();
            }
            else if (e.PropertyName == nameof(GlazierViewModel.ReplacementColor))
            {
                BindingExpression imageBinding = BindingOperations.GetBindingExpression(this.DisplayImage, Image.SourceProperty);
                imageBinding?.UpdateTarget();

                BindingExpression colorPickerBinding = BindingOperations.GetBindingExpression(this.ColorPicker, ColorPicker.SelectedColorProperty);
                colorPickerBinding?.UpdateTarget();

                BindingExpression colorPickerBackgroundBinding = BindingOperations.GetBindingExpression(this.ColorPicker, ColorPicker.BackgroundProperty);
                colorPickerBackgroundBinding?.UpdateTarget();
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageLoaded))
            {
                BindingExpression binding = BindingOperations.GetBindingExpression(this.DestinationFile, CommandTextBox.VisibilityProperty);
                binding?.UpdateTarget();

                BindingExpression labelBinding = BindingOperations.GetBindingExpression(this.DestinationFileLabel, TextBlock.VisibilityProperty);
                labelBinding?.UpdateTarget();
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageNeeded))
            {
                BindingExpression binding = BindingOperations.GetBindingExpression(this.InputFile, CommandTextBox.VisibilityProperty);
                binding?.UpdateTarget();

                BindingExpression labelBinding = BindingOperations.GetBindingExpression(this.InputFileLabel, TextBlock.VisibilityProperty);
                labelBinding?.UpdateTarget();
            }
        }

        private void DisplayImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && this.DataContext is GlazierViewModel glazier)
            {
                // Get the click position relative to the image
                Point clickPosition = e.GetPosition(image);

                // Extract pixel color at the click position
                Color pixelColor = this.GetPixelColor(this.DisplayImage, (int)clickPosition.X, (int)clickPosition.Y);
                glazier.ReplacementColor = pixelColor;

                //var binding = BindingOperations.GetBindingExpression(this.ColorPicker, ColorPicker.SelectedColorProperty);
                //binding?.UpdateTarget();
            }
        }

    }
}