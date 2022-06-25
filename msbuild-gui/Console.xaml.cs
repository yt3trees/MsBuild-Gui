using ModernWpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace msbuild_gui
{
    /// <summary>
    /// Console.xaml の相互作用ロジック
    /// </summary>
    public partial class Console : Window
    {
        private int searchIndex { get; set; }
        private int searchIndexMAX { get; set; }
        public Console(string result , int maxCount, string[,] list, string errorLog)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            searchIndex = 2;
            searchIndexMAX = maxCount;
            Title = result;

            if (errorLog == "")
            {
                ResultTabControl.Items.Remove(ErrorTab);
            }
            
            Brush black = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111111"));
            Brush gray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("LightGray"));
            Brush white = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            ErrorResult.Text = errorLog;

            for (int count = 0; count < maxCount; count++)
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
                text.Margin = new Thickness(12, 0, 12, 5);
                text.KeyDown += CmdResult_KeyDown;

                if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark
                || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
                {
                    text.Background = black;
                    text.Foreground = gray;
                }
                else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light
                    || ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light)
                {
                    text.Background = white;
                    text.Foreground = black;
                }
                text.Text = list[count, 1];

                TextBlock textBlock = new TextBlock();
                textBlock.Text = list[count, 0];
                textBlock.FontFamily = new FontFamily("Yu Gothic UI");
                textBlock.FontSize = 20;
                item.Header = textBlock;

                item.Content = text;
                ResultTabControl.Items.Add(item);
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
            if (e.Key == Key.Enter)
            {
                if (ErrorResult.CaretIndex == ErrorResult.Text.Length)
                {
                    ErrorResult.CaretIndex = 0;
                }
                else
                {
                    ErrorResult.CaretIndex = ErrorResult.Text.Length;
                }
            }
        }
    }
}
