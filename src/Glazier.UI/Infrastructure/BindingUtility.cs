using System.Windows;
using System.Windows.Data;

namespace CascadePass.Glazier.UI
{
    public static class BindingUtility
    {
        public static void UpdateBinding(BindingExpression binding)
        {
            if (binding?.Target is DependencyObject target)
            {
                var dispatcher = target.Dispatcher;

                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(() => binding.UpdateTarget());
                }
                else
                {
                    binding.UpdateTarget();
                }
            }
        }
    }
}
