using System;
using System.Windows;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Initialize application settings or resources here if needed

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Perform any necessary cleanup before the application exits
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}{Environment.NewLine}{Environment.NewLine}{e.Exception.InnerException?.Message}{Environment.NewLine}{e.Exception.InnerException?.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
