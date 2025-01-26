using ModernWpf;
using System;
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
        private string command;
        public Console(string result, int maxCount, string[,] list, string errorLog)
        {
            InitializeComponent();
            this.Width = 1000;
            AIResponseColumn.Width = new GridLength(0);
            SplitterColumn.MinWidth = 0;
            SplitterColumn.Width = new GridLength(0);
            searchIndex = 2;
            searchIndexMAX = maxCount;
            Title = result;

            if (String.IsNullOrEmpty(Properties.Settings.Default.APIKey))
            {
                AskAIButtonBorder.ToolTip = Properties.Resources.APIisnotconfigured;
                AskAIButton.IsEnabled = false;
            }
            // シングルビルドモード時は0のため1に置き換える
            if (searchIndexMAX == 0)
            {
                searchIndexMAX = 1;
            }

            if (errorLog == "")
            {
                ResultTabControl.Items.Remove(ErrorTab);
            }

            Brush black = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111111"));
            Brush gray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("LightGray"));
            Brush white = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            Brush black80 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC111111"));
            Brush white80 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCFFFFFF"));
            ErrorResult.Text = errorLog;

            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark
            || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
            {
                ButtonBorder.Background = black80;
            }
            else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light
                || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light)
            {
                ButtonBorder.Background = white80;
            }

            for (int count = 0; count < searchIndexMAX; count++)
            {
                TabItem item = new TabItem();
                TextBox text = new TextBox();

                text.TextWrapping = TextWrapping.Wrap;
                text.IsReadOnly = true;
                text.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                text.FontFamily = new FontFamily("BIZ UDGothic");
                text.FontSize = 14;
                text.FontWeight = FontWeights.Normal;
                Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111111"));
                text.BorderBrush = brush;
                text.BorderThickness = new Thickness(1, 1, 1, 1);
                text.Padding = new Thickness(5, 5, 15, 5);
                text.Margin = new Thickness(0, 0, 0, 5);
                text.KeyDown += CmdResult_KeyDown;

                if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark
                || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
                {
                    text.Background = black;
                    text.Foreground = gray;
                    AIResponseTextBox.Background = black;
                    AIResponseTextBox.Foreground = gray;
                }
                else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light
                    || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light)
                {
                    text.Background = white;
                    text.Foreground = black;
                    AIResponseTextBox.Background = white;
                    AIResponseTextBox.Foreground = black;
                }
                text.Text = list[count, 1];

                TextBlock textBlock = new TextBlock();
                textBlock.Text = list[count, 0];
                textBlock.FontFamily = new FontFamily("Yu Gothic UI");
                textBlock.FontSize = 20;
                item.Header = textBlock;

                item.Content = text;
                ResultTabControl.Items.Add(item);
                command += list[count, 2].ToString() + "\r\n\r\n";
            }

            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark
                || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
            {
                ErrorResult.Background = black;
                ErrorResult.Foreground = gray;
            }
            else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light
                || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light)
            {
                ErrorResult.Background = white;
                ErrorResult.Foreground = black;
            }
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
            // フォーカスしているテキストボックスを取得
            TextBox textBox = (TextBox)Keyboard.FocusedElement;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Add)
                {
                    textBox.FontSize += 1;
                }
                else if (e.Key == Key.Subtract)
                {
                    textBox.FontSize -= 1;
                }
            }
            if (e.Key == Key.Enter)
            {
                if (textBox.CaretIndex == textBox.Text.Length)
                {
                    textBox.CaretIndex = 0;
                }
                else
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }
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
        private void CommandButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new Messagebox("Execution command", command);
            window.Owner = this;
            window.ShowDialog();
        }
        private void ShowTabList(object sender, RoutedEventArgs e)
        {
            TabList.Items.Clear();
            foreach (TabItem tabItem in ResultTabControl.Items)
            {
                if (tabItem.Header is TextBlock textBlock)
                {
                    string text = textBlock.Text;
                    TreeViewItem item = new TreeViewItem();
                    item.Header = text;
                    TabList.Items.Add(item);
                }
                else
                {
                    string text = tabItem.Header.ToString();
                    TreeViewItem item = new TreeViewItem();
                    item.Header = text;
                    TabList.Items.Add(item);
                }
            }
            TabList.Visibility = Visibility.Visible;
        }
        private void TabList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = TabList.SelectedIndex;
            if (selectedIndex != -1)
            {
                ResultTabControl.SelectedIndex = selectedIndex;
                TabList.Visibility = Visibility.Collapsed;
            }
        }
        private async void AskAIButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI(false);

            try
            {
                string errorMessage = ErrorResult.Text;
                string result = await System.Threading.Tasks.Task.Run(() => AskAI.ExecutePlugin(errorMessage));
                ShowResponse(result);
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, Properties.Resources.Error);
            }
            finally
            {
                ToggleUI(true);
            }
        }
        private void ShowResponse(string response)
        {
            AIResponseTextBox.Markdown = response;
            this.Width = 1500;
            AIResponseColumn.Width = new GridLength(500);
            SplitterColumn.MinWidth = 5;
            SplitterColumn.Width = GridLength.Auto;
        }
        private void ToggleUI(bool isEnabled)
        {
            AskAIButton.IsEnabled = isEnabled;
            AskAIButton.Content = isEnabled ? "Ask AI" : "Analyzing...";
            ProgressRing.IsActive = !isEnabled;
        }
        private void AskAIButtonBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(String.IsNullOrEmpty(Properties.Settings.Default.APIKey)))
            {
                AskAIButtonBorder.ToolTip = "";
                if (ProgressRing.IsActive == false)
                {
                    AskAIButton.IsEnabled = true;
                }
            }
            else
            {
                ModernWpf.MessageBox.Show(Properties.Resources.APIisnotconfigured, Properties.Resources.Error);
            }
        }
    }
}
