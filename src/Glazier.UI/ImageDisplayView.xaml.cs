using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for ImageDisplayView.xaml
    /// </summary>
    public partial class ImageDisplayView : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty WorkspaceViewModelProperty =
            DependencyProperty.Register("WorkspaceViewModel", typeof(WorkspaceViewModel), typeof(ImageDisplayView),
                new PropertyMetadata(null, OnWorkspaceViewModelChanged));

        #endregion

        public ImageDisplayView()
        {
            this.InitializeComponent();
        }

        #region Dependency Properties

        public WorkspaceViewModel WorkspaceViewModel
        {
            get => (WorkspaceViewModel)GetValue(WorkspaceViewModelProperty);
            set => SetValue(WorkspaceViewModelProperty, value);
        }

        #endregion

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

        private static void OnWorkspaceViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageDisplayView control || control.WorkspaceViewModel is null)
            {
                return;
            }

            control.WorkspaceViewModel.PropertyChanged += control.WorkspaceViewModel_PropertyChanged;
            control.WorkspaceViewModel.GlazierViewModel.PropertyChanged += control.GlazierViewModel_PropertyChanged;
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

        private void WorkspaceViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void GlazierViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlazierViewModel.ImageData))
            {
                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.ImagePreviewSection, Border.VisibilityProperty));

                //BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.InkCanvasContainer, InkCanvas.VisibilityProperty));
                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.DisplayImage, Image.SourceProperty));
 
                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.PreviewImage, ImageEditor.GlazierViewModelProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageNeeded))
            {
                //BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.InkCanvasContainer, InkCanvas.VisibilityProperty));
                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.ImageSizeGridSplitter, InkCanvas.VisibilityProperty));

                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.PreviewImage, ImageEditor.GlazierViewModelProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageLoaded))
            {
                //BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.InkCanvasContainer, InkCanvas.VisibilityProperty));
                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.ImageSizeGridSplitter, InkCanvas.VisibilityProperty));

                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.PreviewImage, ImageEditor.GlazierViewModelProperty));
            }
        }
    }
}
