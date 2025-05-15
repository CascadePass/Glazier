using System.Threading;
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
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.DisplayImage, Image.SourceProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.PreviewImage))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.PreviewImage, Image.SourceProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.SourceFilename))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.InputFile, CommandTextBox.UserTextProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.ReplacementColor))
            {
                this.ColorPicker.SelectedColor = ((GlazierViewModel)this.DataContext).ReplacementColor;

                this.UpdateBinding(BindingOperations.GetBindingExpression(this.DisplayImage, Image.SourceProperty));
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ColorPicker, ColorPicker.SelectedColorProperty));
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ColorPicker, ColorPicker.BackgroundProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageLoaded))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.DestinationFile, CommandTextBox.VisibilityProperty));
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.DestinationFileLabel, TextBlock.VisibilityProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageNeeded))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ImagePreviewSection, TextBlock.VisibilityProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsColorNeeded))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ColorPicker, TextBlock.VisibilityProperty));
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ColorLabel, TextBlock.VisibilityProperty));
            }
        }

        private void UpdateBinding(BindingExpression binding)
        {
            if (binding?.Target is DependencyObject target)
            {
                var dispatcher = Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(() => binding.UpdateTarget());
                }
                else
                {
                    binding.UpdateTarget();
                }
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