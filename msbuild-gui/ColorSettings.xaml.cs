using ModernWpf;
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
using System.Windows.Shapes;

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
