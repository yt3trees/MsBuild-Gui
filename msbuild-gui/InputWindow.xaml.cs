﻿using System;
using System.Windows;
using System.Windows.Input;

namespace msbuild_gui
{
    /// <summary>
    /// InputWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class InputWindow : Window
    {
        public InputWindow(string defaultAnswer = "")
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            AnswerText.Text = defaultAnswer;
            var projSetting = ProjectSettings.ProjectNameTemp;
            CopyCheck.Content = projSetting + "の内容をコピーして作成";
        }
        public string Answer
        {
            get { return AnswerText.Text; }
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            AnswerText.SelectAll();
            AnswerText.Focus();
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSettings.IsProjContentsCopy = CopyCheck.IsChecked.Value;
            if (AnswerText.Text.Trim() == "")
            {
                ModernWpf.MessageBox.Show("プロジェクト名を入力してください。", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.DialogResult = true;
        }
    }
}
