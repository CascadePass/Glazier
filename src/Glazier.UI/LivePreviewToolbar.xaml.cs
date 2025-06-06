using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for LivePreviewToolbar.xaml
    /// </summary>
    public partial class LivePreviewToolbar : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty GlazierViewModelProperty =
            DependencyProperty.Register("GlazierViewModel", typeof(GlazierViewModel), typeof(LivePreviewToolbar),
                new PropertyMetadata(null, null));

        public static readonly DependencyProperty WorkspaceViewModelProperty =
            DependencyProperty.Register("WorkspaceViewModel", typeof(WorkspaceViewModel), typeof(LivePreviewToolbar),
                new PropertyMetadata(null, OnWorkspaceViewModelChanged));

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
                new PropertyMetadata(GlazeMethod.Onyx_MachineLearning, OnBackgroundRemovalMethodChanged));

        public static readonly DependencyProperty AllowPreviewProperty =
            DependencyProperty.Register("AllowPreview", typeof(bool), typeof(LivePreviewToolbar),
                new PropertyMetadata(true, OnAllowPreviewChanged));

        #endregion

        public LivePreviewToolbar()
        {
            this.InitializeComponent();

            this.Loaded += this.ToolbarBorder_Loaded;
        }

        #region Properties

        internal DateTime PopupLastOpened { get; set; }

        #region Dependency Properties

        public GlazierViewModel GlazierViewModel
        {
            get => (GlazierViewModel)GetValue(GlazierViewModelProperty);
            set => SetValue(GlazierViewModelProperty, value);
        }

        public WorkspaceViewModel WorkspaceViewModel
        {
            get => (WorkspaceViewModel)GetValue(WorkspaceViewModelProperty);
            set => SetValue(WorkspaceViewModelProperty, value);
        }

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

        public bool AllowPreview
        {
            get => (bool)GetValue(AllowPreviewProperty);
            set => SetValue(AllowPreviewProperty, value);
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
            toolbar.GlazierViewModel.GlazeMethod = isColorReplacement ? GlazeMethod.Prism_ColorReplacement: GlazeMethod.Onyx_MachineLearning;
        }

        private static void OnIsUsingOnyxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LivePreviewToolbar toolbar = (LivePreviewToolbar)d;
            bool isOnyx = (bool)e.NewValue;

            toolbar.IsUsingColorReplacement = !isOnyx;
            toolbar.GlazierViewModel.GlazeMethod = isOnyx ? GlazeMethod.Onyx_MachineLearning : GlazeMethod.Prism_ColorReplacement;
        }

        private static void OnBackgroundRemovalMethodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LivePreviewToolbar toolbar = (LivePreviewToolbar)d;
            var glazeMethod = (GlazeMethod)e.NewValue;

            toolbar.IsUsingColorReplacement = glazeMethod == GlazeMethod.Prism_ColorReplacement;
            toolbar.IsUsingOnyx = glazeMethod == GlazeMethod.Onyx_MachineLearning;
        }

        private static void OnAllowPreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageEditor control)
            {
                return;
            }
        }

        private static void OnWorkspaceViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not LivePreviewToolbar control || control.WorkspaceViewModel is null)
            {
                return;
            }

            control.WorkspaceViewModel.PropertyChanged += control.WorkspaceViewModel_PropertyChanged;
            //control.WorkspaceViewModel.GlazierViewModel.PropertyChanged += control.GlazierViewModel_PropertyChanged;
        }

        #endregion

        internal void WorkspaceViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
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
            if (this.Parent is FrameworkElement frameworkElement && frameworkElement.DataContext is GlazierViewModel vm)
            {
                this.GlazierViewModel = vm;
                this.GlazierViewModel.PropertyChanged += this.OnGlazierViewModelPropertyChanged;
                this.BackgroundRemovalMethod = this.GlazierViewModel.GlazeMethod;

                DependencyPropertyChangedEventArgs args = new(BackgroundRemovalMethodProperty, null, this.BackgroundRemovalMethod);
                LivePreviewToolbar.OnBackgroundRemovalMethodChanged(this, args);
            }

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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if(Application.Current?.MainWindow is not null && Application.Current.MainWindow.DataContext is WorkspaceViewModel vm)
            {
                vm.IsSettingsPageVisible = true;
            }
        }
    }
}
