using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace msbuild_gui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string AppSettingsFile = "appsettings.json";
        
        // 多重起動を禁止する
        private System.Threading.Mutex mutex = new System.Threading.Mutex(false, "ApplicationName");
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // ミューテックスの所有権を要求
            if (!mutex.WaitOne(0, false))
            {
                // 既に起動しているため終了させる
                MessageBox.Show("MsBuild_Guiは既に起動しています。", "二重起動防止", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                mutex.Close();
                mutex = null;
                this.Shutdown();
            }
            DispatcherUnhandledException += (s, args) => HandleException(args.Exception);
            TaskScheduler.UnobservedTaskException += (s, args) => HandleException(args.Exception?.InnerException);
            AppDomain.CurrentDomain.UnhandledException += (s, args) => HandleException(args.ExceptionObject as Exception);

        }
        private void HandleException(Exception e)
        {
            if (e == null) return;

            ShowCustomErrorDialog("An error has occurred.\n" + e.ToString());
            Environment.Exit(1);
        }
        private void ShowCustomErrorDialog(string message)
        {
            Window errorWindow = new Window
            {
                Title = "Abnormal termination",
                Width = 500,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            TextBox textBox = new TextBox
            {
                Text = message,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            errorWindow.Content = textBox;
            errorWindow.ShowDialog();
            Environment.Exit(1);
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
            }
        }

    }
}