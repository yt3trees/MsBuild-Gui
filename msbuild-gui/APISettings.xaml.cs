using ModernWpf;
using System;
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
    public partial class APISettings : Window
    {
        public static bool OkFlg { get; set; }

        public APISettings()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            OkFlg = false;

            ProviderComboBox.Text = Properties.Settings.Default.Provider;
            APIKeyPasswordbox.Password = Properties.Settings.Default.APIKey;
            DeploymentIdTextbox.Text = Properties.Settings.Default.AzDeploymentId;
            BaseDomainTextbox.Text = Properties.Settings.Default.AzBaseDomain;
            string[] modelList = Properties.Settings.Default.ModelList.Split(',');
            foreach(string model in modelList)
            {
                ModelComboBox.Items.Add(model);
            }
            ModelComboBox.Text = Properties.Settings.Default.Model;
        }
        /// <summary>
        /// 閉じるボタン押下時
        /// </summary>
        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (OkFlg == false)
            {
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OkFlg = true;

            Properties.Settings.Default.Provider = ProviderComboBox.Text;
            Properties.Settings.Default.APIKey = APIKeyPasswordbox.Password;
            Properties.Settings.Default.Model = ModelComboBox.Text;
            Properties.Settings.Default.AzDeploymentId = DeploymentIdTextbox.Text;
            Properties.Settings.Default.AzBaseDomain = BaseDomainTextbox.Text;
            Properties.Settings.Default.Save();
            this.Close();
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem == null) return;
            if (ProviderComboBox.SelectedItem.ToString() == "System.Windows.Controls.ComboBoxItem: OpenAI")
            {
                ModelComboBox.IsEnabled = true;
                DeploymentIdTextbox.IsEnabled = false;
                BaseDomainTextbox.IsEnabled = false;
            }
            else
            {
                ModelComboBox.IsEnabled = false;
                DeploymentIdTextbox.IsEnabled = true;
                BaseDomainTextbox.IsEnabled = true;
            }
        }
    }
}
