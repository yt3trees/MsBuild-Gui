using System;
using System.Diagnostics;
using System.Windows;

namespace msbuild_gui
{
    /// <summary>
    /// LanguageSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class LanguageSettings : Window
    {
        public LanguageSettings()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            try
            {
                string lang = ResourceService.Current.GetCurrentCulture();
                Debug.Print(lang);
                if (MainWindow.Projects.Language == "en")
                {
                    LanguageCombo.Text = "English";
                }
                else if (MainWindow.Projects.Language == "ja-JP")
                {
                    LanguageCombo.Text = "日本語";
                }
                else
                {
                    CancelButton.Visibility = Visibility.Hidden;
                    this.WindowStyle = WindowStyle.None;
                    if (lang == "en")
                    {
                        LanguageCombo.Text = "English";
                    }
                    else if (lang == "ja-JP")
                    {
                        LanguageCombo.Text = "日本語";
                    }
                    else
                    {
                        LanguageCombo.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LanguageCombo.Text == "English")
                {
                    ResourceService.Current.ChangeCulture("en");
                    MainWindow.Projects.Language = "en";
                }
                else if (LanguageCombo.Text == "日本語")
                {
                    ResourceService.Current.ChangeCulture("ja-JP");
                    MainWindow.Projects.Language = "ja-JP";
                }
                else
                {
                    ModernWpf.MessageBox.Show("Select your language.\n言語を選択してください。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            // 設定ファイルに保存
            ((MainWindow)this.Owner).saveJson();
                this.Close();
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 閉じるボタン押下時
        /// </summary>
        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MainWindow.Projects.Language == "")
            {
                ModernWpf.MessageBox.Show("Select your language.\n言語を選択してください。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
                return;
            }
        }
    }

}
