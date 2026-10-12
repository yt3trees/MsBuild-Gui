using ModernWpf;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace msbuild_gui
{
    /// <summary>
    /// APISettings.xaml の相互作用ロジック
    /// </summary>
    public partial class APISettings : Window
    {
        public static bool OkFlg { get; set; }

        // デバイスフロー用キャンセルトークン
        private CancellationTokenSource? _deviceFlowCts;

        // Copilot 用モデル候補
        private static readonly string[] CopilotModels =
        {
            "gpt-5",
            "claude-sonnet-4.5",
            "gpt-4o",
        };

        public APISettings()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            OkFlg = false;

            ProviderComboBox.Text = Properties.Settings.Default.Provider;
            APIKeyPasswordbox.Password = Properties.Settings.Default.APIKey;
            DeploymentIdTextbox.Text = Properties.Settings.Default.AzDeploymentId;
            BaseDomainTextbox.Text = Properties.Settings.Default.AzBaseDomain;

            // モデルリストをプロバイダーに合わせて初期化
            UpdateModelList(Properties.Settings.Default.Provider);

            // 初期レイアウトをプロバイダーに合わせて適用
            ApplyProviderLayout(Properties.Settings.Default.Provider);

            // GitHubCopilot の場合、保存済みトークンがあればサインイン済みとして表示
            if (Properties.Settings.Default.Provider == "GitHubCopilot"
                && !string.IsNullOrWhiteSpace(Properties.Settings.Default.APIKey))
            {
                GitHubStatusText.Text = "✅ GitHub アカウントでサインイン済みです。";
                GitHubSignInButton.Content = "再サインイン";
            }
        }

        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _deviceFlowCts?.Cancel();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OkFlg = true;

            Properties.Settings.Default.Provider = ProviderComboBox.Text;

            // GitHubCopilot の場合は APIKey を変更しない (デバイスフローで更新済み)
            if (Properties.Settings.Default.Provider != "GitHubCopilot")
            {
                Properties.Settings.Default.APIKey = APIKeyPasswordbox.Password;
            }

            Properties.Settings.Default.AzDeploymentId = DeploymentIdTextbox.Text;
            Properties.Settings.Default.AzBaseDomain = BaseDomainTextbox.Text;

            if (Properties.Settings.Default.Provider == "GitHubCopilot")
            {
                Properties.Settings.Default.GitHubCopilotModel = ModelComboBox.Text;
            }
            else
            {
                Properties.Settings.Default.Model = ModelComboBox.Text;
            }

            Properties.Settings.Default.Save();
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem == null) return;

            string provider = (ProviderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
                              ?? ProviderComboBox.Text;

            UpdateModelList(provider);
            ApplyProviderLayout(provider);
        }

        /// <summary>
        /// プロバイダーに応じてモデルリストを更新します。
        /// </summary>
        private void UpdateModelList(string provider)
        {
            if (ModelComboBox == null) return;

            ModelComboBox.Items.Clear();

            if (provider == "GitHubCopilot")
            {
                foreach (string model in CopilotModels)
                    ModelComboBox.Items.Add(model);

                string savedCopilotModel = Properties.Settings.Default.GitHubCopilotModel;
                ModelComboBox.Text = string.IsNullOrEmpty(savedCopilotModel)
                    ? CopilotModels[0]
                    : savedCopilotModel;
            }
            else
            {
                string[] modelList = Properties.Settings.Default.ModelList.Split(',');
                foreach (string model in modelList)
                    ModelComboBox.Items.Add(model);

                ModelComboBox.Text = Properties.Settings.Default.Model;
            }
        }

        /// <summary>
        /// プロバイダーに応じて UI の表示/非表示を切り替えます。
        /// </summary>
        private void ApplyProviderLayout(string provider)
        {
            if (APIKeyLabel == null) return;

            if (provider == "GitHubCopilot")
            {
                // GitHubCopilot: API Key 欄を非表示、サインインパネルを表示
                APIKeyLabel.Visibility = Visibility.Collapsed;
                APIKeyPasswordbox.Visibility = Visibility.Collapsed;
                GitHubSignInPanel.Visibility = Visibility.Visible;
                DeploymentIdLabel.Visibility = Visibility.Collapsed;
                DeploymentIdTextbox.Visibility = Visibility.Collapsed;
                BaseDomainLabel.Visibility = Visibility.Collapsed;
                BaseDomainTextbox.Visibility = Visibility.Collapsed;
                ModelComboBox.IsEnabled = true;
            }
            else if (provider == "OpenAI")
            {
                APIKeyLabel.Visibility = Visibility.Visible;
                APIKeyPasswordbox.Visibility = Visibility.Visible;
                GitHubSignInPanel.Visibility = Visibility.Collapsed;
                DeviceCodePanel.Visibility = Visibility.Collapsed;
                DeploymentIdLabel.Visibility = Visibility.Collapsed;
                DeploymentIdTextbox.Visibility = Visibility.Collapsed;
                DeploymentIdTextbox.IsEnabled = false;
                BaseDomainLabel.Visibility = Visibility.Collapsed;
                BaseDomainTextbox.Visibility = Visibility.Collapsed;
                BaseDomainTextbox.IsEnabled = false;
                ModelComboBox.IsEnabled = true;
            }
            else // Azure
            {
                APIKeyLabel.Visibility = Visibility.Visible;
                APIKeyPasswordbox.Visibility = Visibility.Visible;
                GitHubSignInPanel.Visibility = Visibility.Collapsed;
                DeviceCodePanel.Visibility = Visibility.Collapsed;
                DeploymentIdLabel.Visibility = Visibility.Visible;
                DeploymentIdTextbox.Visibility = Visibility.Visible;
                DeploymentIdTextbox.IsEnabled = true;
                BaseDomainLabel.Visibility = Visibility.Visible;
                BaseDomainTextbox.Visibility = Visibility.Visible;
                BaseDomainTextbox.IsEnabled = true;
                ModelComboBox.IsEnabled = false;
            }
        }

        /// <summary>
        /// GitHub サインインボタン押下: OAuth デバイスフローを開始します。
        /// </summary>
        private async void GitHubSignInButton_Click(object sender, RoutedEventArgs e)
        {
            _deviceFlowCts?.Cancel();
            _deviceFlowCts = new CancellationTokenSource();

            GitHubSignInButton.IsEnabled = false;
            GitHubStatusText.Text = "GitHub に接続しています...";
            DeviceCodePanel.Visibility = Visibility.Collapsed;

            try
            {
                string token = await GitHubDeviceAuth.SignInAsync(
                    onPrompt: (userCode, verificationUri) =>
                    {
                        // コールバックは非 UI スレッドから呼ばれる可能性があるため Dispatcher 経由で更新
                        Dispatcher.Invoke(() =>
                        {
                            DeviceCodeText.Text = userCode;
                            DeviceCodePanel.Visibility = Visibility.Visible;
                            GitHubStatusText.Text =
                                $"ブラウザで {verificationUri} を開き、上のコードを入力してください。";
                        });
                    },
                    cancellationToken: _deviceFlowCts.Token);

                // トークンを保存 (APIKey に格納して既存ガード条件と共用)
                Properties.Settings.Default.APIKey = token;
                Properties.Settings.Default.Save();

                DeviceCodePanel.Visibility = Visibility.Collapsed;
                GitHubStatusText.Text = "✅ サインインが完了しました。";
                GitHubSignInButton.Content = "再サインイン";
            }
            catch (OperationCanceledException)
            {
                GitHubStatusText.Text = "サインインをキャンセルしました。";
            }
            catch (Exception ex)
            {
                DeviceCodePanel.Visibility = Visibility.Collapsed;
                GitHubStatusText.Text = $"エラー: {ex.Message}";
                ModernWpf.MessageBox.Show(ex.Message, Properties.Resources.Error);
            }
            finally
            {
                GitHubSignInButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// デバイスコードをクリップボードにコピーします。
        /// </summary>
        private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(DeviceCodeText.Text);
            }
            catch { /* クリップボードアクセス失敗は無視 */ }
        }
    }
}
