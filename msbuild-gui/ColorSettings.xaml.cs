using ModernWpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace msbuild_gui
{
    /// <summary>
    /// ColorSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorSettings : Window
    {
        public ColorSettings()
        {
            InitializeComponent();
            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark)
            {
                ThemeDark.IsChecked = true;
            }
            else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light)
            {
                ThemeLight.IsChecked = true;
            }
            else
            {
                ThemeSystem.IsChecked = true;
            }
        }

        private void ThemeRadio_Click(object sender, RoutedEventArgs e)
        {
            //ThemeManager.Current.AccentColor = null;
            var ctrl = sender as Control;
            if (ctrl == ThemeLight)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            }
            else if (ctrl == ThemeDark)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
            else
            {
                ThemeManager.Current.ApplicationTheme = null;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 設定ファイルに保存
            ((MainWindow)this.Owner).saveJson();
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
