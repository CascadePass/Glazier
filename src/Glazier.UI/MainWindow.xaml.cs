using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] supportedExtensions;

        public MainWindow()
        {
            this.InitializeComponent();

            this.supportedExtensions = [".png", ".jpg", ".bmp", ".tiff", ".tif"];

            if (this.DataContext is WorkspaceViewModel wvm)
            {
                wvm.PropertyChanged += WorkspaceViewModel_PropertyChanged;
                wvm.GlazierViewModel.PropertyChanged += this.GlazierViewModel_PropertyChanged;
            }
        }

        protected WorkspaceViewModel WorkspaceViewModel => this.DataContext as WorkspaceViewModel;

        protected bool UseAnimation => this.WorkspaceViewModel?.Settings?.UseAnimation ?? true;

        #region Drag and Drop

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string file = files.FirstOrDefault(f => this.supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

                if (file is not null)
                {
                    if(this.DataContext is WorkspaceViewModel vm)
                    {
                        vm.GlazierViewModel.SourceFilename = file;
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


        private void WorkspaceViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WorkspaceViewModel.IsSettingsPageVisible))
            {
                if (this.WorkspaceViewModel.IsSettingsPageVisible)
                {
                    Animator.ShowSettingsPanel(this.SettingsEditor, this, this.UseAnimation);
                }
                else
                {
                    Animator.HideSettingsPanel(this.SettingsEditor, this, this.UseAnimation);
                }
            }
            else if (e.PropertyName == nameof(WorkspaceViewModel.CurrentFont))
            {
                this.FontFamily = this.WorkspaceViewModel.CurrentFont;
            }
        }

        private void GlazierViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlazierViewModel.SourceFilename))
            {
                //BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.InputFile, CommandTextBox.UserTextProperty));
            }
            else if (e.PropertyName == nameof(GlazierViewModel.ReplacementColor))
            {
            }
            else if (e.PropertyName == nameof(GlazierViewModel.IsImageNeeded))
            {
                //BindingUtility.UpdateBinding(BindingOperations.GetBindingExpression(this.ColorTolerance, Slider.VisibilityProperty));

                Animator.HideInputForm(this.InputFormBorder);
            }
        }
    }
}