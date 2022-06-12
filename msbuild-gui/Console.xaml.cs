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
        public Console(string result , int maxCount)
        {
            InitializeComponent();
            searchIndex = 2;
            searchIndexMAX = maxCount;
            Title = result;
            // ボーダーカラーを変更する
            if (result == "エラーログ")
            {
                Border.BorderBrush = new SolidColorBrush(Colors.LightSalmon);
            }
            // result == "エラーログ"の場合は少し右下にずらして表示、それ以外の場合は中心
            if (result == "エラーログ")
            {
                this.Left = System.Windows.SystemParameters.PrimaryScreenWidth / 2 - ((this.Width / 2) - 20);
                this.Top = System.Windows.SystemParameters.PrimaryScreenHeight / 2 - ((this.Height / 2) - 20);
                //NextButton.Visibility = Visibility.Hidden;
            }
            else
            {
                this.Left = System.Windows.SystemParameters.PrimaryScreenWidth / 2 - this.Width / 2;
                this.Top = System.Windows.SystemParameters.PrimaryScreenHeight / 2 - this.Height / 2;
            }
        }
        private void CmdResult_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Ctrl+("+"or"-")で文字の大きさを調整する
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
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            FocusNext();
        }
        /// <summary>
        /// 次のビルド実行結果に移動する
        /// </summary>
        private void FocusNext()
        {
            // TextBoxにフォーカスする
            CmdResult.Focus();
            // カーソルをTextBoxのsearchTextの値に移動
            string searchText = "[" + searchIndex + "]";
            //最後のindexにfocusする
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
            // Escキーで終了
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
