using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for ImageEditor.xaml
    /// </summary>
    public partial class ImageEditor : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(ImageEditor),
                new PropertyMetadata(null, OnImageChanged));

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register("Mask", typeof(ImageSource), typeof(ImageEditor),
                new PropertyMetadata(null, OnMaskChanged));

        public static readonly DependencyProperty AllowPreviewProperty =
            DependencyProperty.Register("AllowPreview", typeof(bool), typeof(ImageEditor),
                new PropertyMetadata(true, OnAllowPreviewChanged));

        #endregion

        public ImageEditor()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public ImageSource Image
        {
            get => (ImageSource)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public ImageSource Mask
        {
            get => (ImageSource)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        public bool AllowPreview
        {
            get => (bool)GetValue(AllowPreviewProperty);
            set => SetValue(AllowPreviewProperty, value);
        }

        #endregion

        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageEditor control)
            {
                return;
            }
        }

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageEditor control)
            {
                return;
            }
        }

        private static void OnAllowPreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageEditor control)
            {
                return;
            }
        }
    }
}
