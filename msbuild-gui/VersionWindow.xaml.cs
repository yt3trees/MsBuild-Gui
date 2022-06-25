using System;
using System.Windows;
using System.Windows.Input;

namespace msbuild_gui
{
    /// <summary>
    /// VersionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class VersionWindow : Window
    {
        public VersionWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            try
            {
                VersionText.Text = "Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
