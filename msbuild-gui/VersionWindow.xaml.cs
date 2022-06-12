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
            // バージョン情報を取得
            VersionText.Text = "Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Escキーが押されたとき
            if (e.Key == Key.Escape)
            {
                // ウィンドウを閉じる
                this.Close();
            }
        }
    }
}
