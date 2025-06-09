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
    /// Interaction logic for LoadImageForm.xaml
    /// </summary>
    public partial class LoadImageForm : UserControl
    {
        public LoadImageForm()
        {
            InitializeComponent();
        }

        public void Hide()
        {
            Animator.HideInputForm(this.InputFormBorder);
        }
    }
}
