using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for CommandTextBox.xaml
    /// </summary>
    public partial class CommandTextBox : UserControl
    {
        #region Dependency Properties (Declarations)

        public static readonly DependencyProperty InsetTextProperty =
            DependencyProperty.Register("InsetText",
                typeof(string), typeof(CommandTextBox), new PropertyMetadata(string.Empty)
            );

        public static readonly DependencyProperty InsetIconProperty =
            DependencyProperty.Register("InsetIcon",
                typeof(ImageSource), typeof(CommandTextBox), new PropertyMetadata(null)
            );

        public static readonly DependencyProperty CommandIconProperty =
            DependencyProperty.Register("CommandIcon",
                typeof(ImageSource), typeof(CommandTextBox), new PropertyMetadata(null)
            );

        public static readonly DependencyProperty UserTextProperty =
            DependencyProperty.Register("UserText",
                typeof(string), typeof(CommandTextBox), new PropertyMetadata(string.Empty)
            );


        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command",
                typeof(ICommand), typeof(CommandTextBox), new PropertyMetadata(null)
            );

        #endregion

        public CommandTextBox()
        {
            InitializeComponent();
        }

        #region Dependency Properties (Getters/Setters)

        public string UserText
        {
            get { return (string)GetValue(UserTextProperty); }
            set { SetValue(UserTextProperty, value); }
        }

        public string InsetText
        {
            get { return (string)GetValue(InsetTextProperty); }
            set { SetValue(InsetTextProperty, value); }
        }

        public ImageSource InsetIcon
        {
            get { return (ImageSource)GetValue(InsetIconProperty); }
            set { SetValue(InsetIconProperty, value); }
        }

        public ImageSource CommandIcon
        {
            get { return (ImageSource)GetValue(CommandIconProperty); }
            set { SetValue(CommandIconProperty, value); }
        }

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        #endregion
    }
}
