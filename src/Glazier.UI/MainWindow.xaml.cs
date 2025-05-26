using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] supportedExtensions;
        private IThemeListener themeListener;

        public MainWindow()
        {
            this.themeListener = new ThemeListener();
            this.InitializeComponent();

            this.supportedExtensions = [".png", ".jpg", ".bmp", ".tiff", ".tif"];

            var vm = this.DataContext as ViewModel;

            vm.PropertyChanged += this.ViewModel_PropertyChanged;
        }


        private Color GetPixelColor(Image image, int x, int y)
        {
            if (image.Source is BitmapSource bitmapSource)
            {
                // There are 4 bytes per pixel (BGRA format)
                int stride = bitmapSource.PixelWidth * 4;
                byte[] pixels = new byte[stride * bitmapSource.PixelHeight];

                bitmapSource.CopyPixels(pixels, stride, 0);

                int index = (y * stride) + (x * 4); // Calculate pixel index
                if (index + 3 < pixels.Length) // Ensure index is valid
                {
                    return Color.FromArgb(255, pixels[index + 2], pixels[index + 1], pixels[index]);
                }
            }

            return Colors.Transparent;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlazierViewModel.ImageData))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.DisplayImage, Image.SourceProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.PreviewImage))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.PreviewImage, ImageEditor.ImageProperty));
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
            //else if (e.PropertyName == nameof(GlazierViewModel.IsImageLoaded))
            //{
            //    this.UpdateBinding(BindingOperations.GetBindingExpression(this.DestinationFile, CommandTextBox.VisibilityProperty));
            //    this.UpdateBinding(BindingOperations.GetBindingExpression(this.DestinationFileLabel, TextBlock.VisibilityProperty));
            //}
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageNeeded))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ImagePreviewSection, TextBlock.VisibilityProperty));

                this.HideInputForm();

            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsColorNeeded))
            {
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ColorPicker, TextBlock.VisibilityProperty));
                this.UpdateBinding(BindingOperations.GetBindingExpression(this.ColorLabel, TextBlock.VisibilityProperty));
            }
        }

        private void HideInputForm()
        {
            var animation = new DoubleAnimation
            {
                From = this.InputFormBorder.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(animation, this.InputFormBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));

            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void ShowInputForm(double originalHeight)
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = originalHeight,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(animation, this.InputFormBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));

            storyboard.Children.Add(animation);
            storyboard.Begin();
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

        #region Drag and Drop

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string file = files.FirstOrDefault(f => this.supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

                if (file is not null)
                {
                    if(this.DataContext is GlazierViewModel vm)
                    {
                        vm.SourceFilename = file;
                    }
                }
            }

            Mouse.OverrideCursor = null;
            e.Effects = DragDropEffects.None;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool isValid = files.Any(f => this.supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

                e.Effects = isValid ? DragDropEffects.Copy : DragDropEffects.None;
                Mouse.OverrideCursor = isValid ? Cursors.Arrow : Cursors.No; // Updates dynamically
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    string fileExtension = Path.GetExtension(files[0]).ToLower();

                    if (this.supportedExtensions.Contains(fileExtension))
                    {
                        Mouse.OverrideCursor = Cursors.Arrow; // Normal cursor
                        e.Effects = DragDropEffects.Copy; // Allow copy operation
                    }
                    else
                    {
                        Mouse.OverrideCursor = Cursors.No; // "No" symbol cursor
                        e.Effects = DragDropEffects.None; // Disallow drop
                    }
                }
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            Mouse.OverrideCursor = null;
            e.Effects = DragDropEffects.None;
        }

        #endregion
    }
}