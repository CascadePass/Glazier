using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for LoadImageForm.xaml
    /// </summary>
    public partial class LoadImageForm : UserControl
    {
        public LoadImageForm()
        {
            this.InitializeComponent();
        }

        public void Hide()
        {
            Animator.HideInputForm(this.InputFormBorder);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton selectedButton)
            {
                Animator.SelectAlgorithmRadioButton(selectedButton, this.GlazeMethodSelector, this.HighlightTransform, this.SelectionHighlight);
            }
        }

        private void RadioButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { IsChecked: true })
            {
                this.RadioButton_Checked(sender, e);
            }
        }
    }
}
