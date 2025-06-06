using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for ImageEditor.xaml
    /// </summary>
    public partial class ImageEditor : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty GlazierViewModelProperty =
            DependencyProperty.Register("GlazierViewModel", typeof(GlazierViewModel), typeof(ImageEditor),
                new PropertyMetadata(null, OnGlazierViewModelChanged));

        public static readonly DependencyProperty AllowPreviewProperty =
            DependencyProperty.Register("AllowPreview", typeof(bool), typeof(ImageEditor),
                new PropertyMetadata(true, OnAllowPreviewChanged));

        #endregion

        public ImageEditor()
        {
            this.InitializeComponent();
        }

        #region Dependency Properties

        public GlazierViewModel GlazierViewModel
        {
            get => (GlazierViewModel)GetValue(GlazierViewModelProperty);
            set => SetValue(GlazierViewModelProperty, value);
        }

        public bool AllowPreview
        {
            get => (bool)GetValue(AllowPreviewProperty);
            set => SetValue(AllowPreviewProperty, value);
        }

        #endregion

        private static void OnAllowPreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageEditor control)
            {
                return;
            }
        }

        private static void OnGlazierViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageEditor control || control.GlazierViewModel is null)
            {
                return;
            }

            control.GlazierViewModel.PropertyChanged += control.GlazierViewModel_PropertyChanged;
        }

        private void GlazierViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(GlazierViewModel.PreviewImage)))
            {
                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.PreviewImage, Image.SourceProperty));
            }
        }
    }
}
