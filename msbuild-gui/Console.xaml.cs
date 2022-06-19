using ModernWpf;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace msbuild_gui
{
    /// <summary>
    /// Console.xaml の相互作用ロジック
    /// </summary>
    public partial class Console : Window
    {
        private int searchIndex { get; set; }
        private int searchIndexMAX { get; set; }
        public Console(string result , int maxCount, string resultLog, string errorLog)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            searchIndex = 2;
            searchIndexMAX = maxCount;
            Title = result;

            if (errorLog == "")
            {
                ErrorTab.Visibility = Visibility.Hidden;
            }

            CmdResult.Text = resultLog;
            ErrorResult.Text = errorLog;

            Brush black = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111111"));
            Brush gray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("LightGray"));
            Brush white = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark 
                || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
            {
                CmdResult.Background = black;
                CmdResult.Foreground = gray;
                ErrorResult.Background = black;
                ErrorResult.Foreground = gray;
            }
            else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light
                || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light)
            {
                CmdResult.Background = white;
                CmdResult.Foreground = black;
                ErrorResult.Background = white;
                ErrorResult.Foreground = black;
            }
        }
        
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            FocusNext();
        }
        /// <summary>
        /// 次のビルド実行結果に移動する
        /// </summary>
        private void FocusNext()
        {
            CmdResult.Focus();
            string searchText = "[" + searchIndex + "]";
            CmdResult.CaretIndex = CmdResult.Text.Length;

            // searchIndexがsearchIndexMAXと同じ値ならIndexを0にする
            if (searchIndex >= searchIndexMAX)
            {
                searchIndex = 0;
            }
            int index = CmdResult.Text.IndexOf(searchText);
            if (index != -1)
            {
                CmdResult.CaretIndex = index - 10;
            }
            searchIndex++;
            Debug.WriteLine(searchIndex.ToString());
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
        private void CmdResult_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Add)
                {
                    CmdResult.FontSize += 1;
                }
                else if (e.Key == Key.Subtract)
                {
                    CmdResult.FontSize -= 1;
                }

            }
            if (e.Key == Key.Enter)
            {
                FocusNext();
            }
        }
        private void ErrorResult_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Add)
                {
                    ErrorResult.FontSize += 1;
                }
                else if (e.Key == Key.Subtract)
                {
                    ErrorResult.FontSize -= 1;
                }

            }
        }
    }
}
