using ModernWpf;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace msbuild_gui
{
    /// <summary>
    /// ColorSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorSettings : Window
    {
        public static IReadOnlyList<string> KnownColorNames { get; } =
            typeof(Colors)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(info => (info.Name))
            .ToArray();

        public ColorSettings()
        {
            InitializeComponent();

            AccentColorList.ItemsSource = KnownColorNames;
            
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

            if (ThemeManager.Current.AccentColor == null)
            {
                AccentColorSystem.IsChecked = true;
            }
            else
            {
                AccentColorSet.IsChecked = true;
                AccentColorList.SelectedItem = ThemeManager.Current.AccentColor.ToString();
            }
        }

        private void ThemeRadio_Click(object sender, RoutedEventArgs e)
        {
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

        private void AccentColorSystem_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.AccentColor = null;
            AccentColorList.IsEnabled = false;
        }

        private void AccentColorSet_Checked(object sender, RoutedEventArgs e)
        {
            AccentColorList.IsEnabled = true;
        }

        private void AccentColorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var color = (Color)ColorConverter.ConvertFromString(AccentColorList.SelectedValue.ToString());
            ThemeManager.Current.AccentColor = color;
        }
    }
}
