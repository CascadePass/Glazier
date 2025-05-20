using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for LivePreviewToolbar.xaml
    /// </summary>
    public partial class LivePreviewToolbar : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty IsSettingsExpandedProperty =
            DependencyProperty.Register("IsSettingsExpanded", typeof(bool), typeof(LivePreviewToolbar),
                new PropertyMetadata(false, OnPopupOpenChanged));

        public static readonly DependencyProperty IsSizeExpandedProperty =
            DependencyProperty.Register("IsSizeExpanded", typeof(bool), typeof(LivePreviewToolbar),
                new PropertyMetadata(false, OnPopupOpenChanged));

        public static readonly DependencyProperty IsUsingColorReplacementProperty =
            DependencyProperty.Register("IsUsingColorReplacement", typeof(bool), typeof(LivePreviewToolbar),
                new PropertyMetadata(false, OnIsUsingColorReplacementChanged));

        public static readonly DependencyProperty IsUsingOnyxProperty =
            DependencyProperty.Register("IsUsingOnyx", typeof(bool), typeof(LivePreviewToolbar),
                new PropertyMetadata(false, OnIsUsingOnyxChanged));

        public static readonly DependencyProperty BackgroundRemovalMethodProperty =
            DependencyProperty.Register("BackgroundRemovalMethod", typeof(GlazeMethod), typeof(LivePreviewToolbar),
                new PropertyMetadata(GlazeMethod.MachineLearning, OnBackgroundRemovalMethodChanged));

        #endregion

        public LivePreviewToolbar()
        {
            this.InitializeComponent();
        }

        #region Properties

        internal GlazierViewModel GlazierViewModel { get; set; }

        internal DateTime PopupLastOpened { get; set; }

        #region Dependency Properties

        public bool IsSettingsExpanded
        {
            get => (bool)GetValue(IsSettingsExpandedProperty);
            set => SetValue(IsSettingsExpandedProperty, value);
        }

        public bool IsSizeExpanded
        {
            get => (bool)GetValue(IsSizeExpandedProperty);
            set => SetValue(IsSizeExpandedProperty, value);
        }

        public bool IsUsingColorReplacement
        {
            get => (bool)GetValue(IsUsingColorReplacementProperty);
            set => SetValue(IsUsingColorReplacementProperty, value);
        }

        public bool IsUsingOnyx
        {
            get => (bool)GetValue(IsUsingOnyxProperty);
            set => SetValue(IsUsingOnyxProperty, value);
        }

        public GlazeMethod BackgroundRemovalMethod
        {
            get => (GlazeMethod)GetValue(BackgroundRemovalMethodProperty);
            set => SetValue(BackgroundRemovalMethodProperty, value);
        }

        #endregion

        #endregion

        #region Methods

        internal void ClosePopups()
        {
            this.IsSettingsExpanded = false;
            this.IsSizeExpanded = false;
        }

        #region Dependency Property Callbacks

        private static void OnPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LivePreviewToolbar toolbar = (LivePreviewToolbar)d;
            bool isOpen = (bool)e.NewValue;

            if (isOpen)
            {
                toolbar.PopupLastOpened = DateTime.Now;
            }
        }

        private static void OnIsUsingColorReplacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LivePreviewToolbar toolbar = (LivePreviewToolbar)d;
            bool isColorReplacement = (bool)e.NewValue;

            toolbar.IsUsingOnyx = !isColorReplacement;
            toolbar.GlazierViewModel.GlazeMethod = isColorReplacement ? GlazeMethod.ColorReplacement: GlazeMethod.MachineLearning;
        }

        private static void OnIsUsingOnyxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LivePreviewToolbar toolbar = (LivePreviewToolbar)d;
            bool isOnyx = (bool)e.NewValue;

            toolbar.IsUsingColorReplacement = !isOnyx;
            toolbar.GlazierViewModel.GlazeMethod = isOnyx ? GlazeMethod.MachineLearning : GlazeMethod.ColorReplacement;
        }

        private static void OnBackgroundRemovalMethodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LivePreviewToolbar toolbar = (LivePreviewToolbar)d;
            var glazeMethod = (GlazeMethod)e.NewValue;

            toolbar.IsUsingColorReplacement = glazeMethod == GlazeMethod.ColorReplacement;
            toolbar.IsUsingOnyx = glazeMethod == GlazeMethod.MachineLearning;
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.Parent is FrameworkElement frameworkElement && frameworkElement.DataContext is GlazierViewModel vm)
            {
                this.GlazierViewModel = vm;
                this.GlazierViewModel.PropertyChanged += this.OnGlazierViewModelPropertyChanged;
                this.BackgroundRemovalMethod = this.GlazierViewModel.GlazeMethod;

                DependencyPropertyChangedEventArgs args = new(BackgroundRemovalMethodProperty, null, this.BackgroundRemovalMethod);
                LivePreviewToolbar.OnBackgroundRemovalMethodChanged(this, args);
            }
        }

        private void OnGlazierViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlazierViewModel.GlazeMethod))
            {
                this.BackgroundRemovalMethod = this.GlazierViewModel.GlazeMethod;
                this.ColorPicker.SelectedColor = this.GlazierViewModel.ReplacementColor;
            }
            else if (e.PropertyName == nameof(GlazierViewModel.ReplacementColor))
            {
                this.ColorPicker.SelectedColor = this.GlazierViewModel.ReplacementColor;
            }
        }

        private void OnOpacityChanged(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - this.PopupLastOpened;

            if (this.ToolbarBorder.Opacity < 0.7 && elapsed.TotalSeconds > 5)
            {
                this.IsSettingsExpanded = false;
                this.IsSizeExpanded = false;
            }
        }

        private void ToolbarBorder_Loaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor
                .FromProperty(Border.OpacityProperty, typeof(Border))
                .AddValueChanged(ToolbarBorder, OnOpacityChanged)
            ;
        }

        private void ListBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.ClosePopups();
        }

        #endregion
    }
}
