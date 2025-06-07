using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for SettingsEditorView.xaml
    /// </summary>
    public partial class SettingsEditorView : UserControl
    {
        public SettingsEditorView()
        {
            this.InitializeComponent();

            this.Loaded += this.SettingsEditorView_Loaded;

        }

        private void SettingsEditorView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SettingsViewModel settingsViewModel)
            {
                settingsViewModel.PropertyChanged += this.SettingsViewModel_PropertyChanged;
                settingsViewModel.Settings.PropertyChanged += this.Settings_PropertyChanged;
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (this.VisualSettingsRoot is not null)
            {
                Animator.OpenExpander(this.VisualSettingsRoot);
            }
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (this.VisualSettingsRoot is not null)
            {
                Animator.CloseExpander(this.VisualSettingsRoot, this.VisualSettingsExpander);
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.ModelFile))
            {
                BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.ModelFileTextBox, CommandTextBox.UserTextProperty));
            }
        }

        private void SettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }
    }
}
