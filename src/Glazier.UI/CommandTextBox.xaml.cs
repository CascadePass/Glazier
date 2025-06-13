using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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

        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register("Buttons",
                typeof(ObservableCollection<Button>), typeof(CommandTextBox), new PropertyMetadata(null)
            );

        #endregion

        public CommandTextBox()
        {
            this.InitializeComponent();
            this.Buttons = [];
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

        public ObservableCollection<Button> Buttons
        {
            get { return (ObservableCollection<Button>)GetValue(ButtonsProperty); }
            set
            {
                if (this.Buttons != null)
                {
                    this.Buttons.CollectionChanged -= this.Buttons_CollectionChanged;
                }

                SetValue(ButtonsProperty, value);

                if (value != null)
                {
                    value.CollectionChanged += this.Buttons_CollectionChanged;
                }

                this.UpdateContentPresenter();
            }
        }

        public bool IsSingleButton => this.Buttons is null || this.Buttons.Count == 0;

        private void Buttons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateContentPresenter();
        }

        private void UpdateContentPresenter()
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (var button in Buttons)
            {
                var parent = LogicalTreeHelper.GetParent(button) as Panel;
                parent?.Children.Remove(button);
                panel.Children.Add(button);
            }

            this.ButtonsPresenter.Content = panel;
            this.ButtonsPresenter.UpdateLayout();
        }

        #endregion
    }
}
