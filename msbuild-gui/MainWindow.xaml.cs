using Microsoft.Extensions.Configuration;
using ModernWpf;
using ModernWpf.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI = ModernWpf;

namespace msbuild_gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region fields
        delegate void DelegateProcess(string a, string b);
        #endregion
 
        #region properties
        /// <summary>
        /// プロジェクトリスト
        /// </summary>
        public class Projects
        {
            public class ProjectData
            {
                public string? ProjectName { get; set; }
                public string? SourceFolder { get; set; }
                public string? OutputFolder { get; set; }
                public string? MsBuild { get; set; }
                public string? Target { get; set; }
                public string? AssemblySearchPaths { get; set; }
                public string? Configuration { get; set; }
            }
            public static Dictionary<int, ProjectData> ProjectsList = new Dictionary<int, ProjectData>();
            public static bool ShowLog = false;
        }
        /// <summary>
        /// 設定ファイル用プロパティ
        /// </summary>
        [JsonObject("Appsettings")]
        class Appsettings
        {
            [JsonProperty("Project")]
            public List<ProjectData>? Project { get; set; }
            [JsonProperty("ShowLog")]
            public string? ShowLog { get; set; }
            [JsonProperty("Theme")]
            public string? Theme { get; set; }
        }
        /// <summary>
        /// 設定ファイル用プロパティ
        /// </summary>
        [JsonObject("ProjectData")]
        public class ProjectData
        {
            [JsonProperty("Id")]
            public int Id { get; set; }
            [JsonProperty("ProjectName")]
            public string? ProjectName { get; set; }
            [JsonProperty("SourceFolder")]
            public string? SourceFolder { get; set; }
            [JsonProperty("OutputFolder")]
            public string? OutputFolder { get; set; }
            [JsonProperty("MsBuild")]
            public string? MsBuild { get; set; }
            [JsonProperty("Target")]
            public string? Target { get; set; }
            [JsonProperty("AssemblySearchPaths")]
            public string? AssemblySearchPaths { get; set; }
            [JsonProperty("Configuration")]
            public string? Configuration { get; set; }
        }
        /// <summary>
        /// SourceList検索制御用
        /// </summary>
        public class List
        {
            public static List<string> sourceList { get; set; } = new List<string>();
        }
        #endregion

        #region constructors
        public MainWindow()
        {
            InitializeComponent();
            ProgressBar.Visibility = Visibility.Hidden;
        }
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            try
            {
                LoadAppSettings();
                Projects.ProjectsList.ToList().ForEach(x => ProjCombo.Items.Add(x.Value.ProjectName));
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region ui events
        /// <summary>
        /// 閉じるボタン押下時
        /// </summary>
        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // エラー出力ファイルは終了時に削除する(ソフトウェアアンインストール時にファイルが残るため)
                if (File.Exists(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt"))
                {
                    File.Delete(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt");
                }
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// ビルド実行
        /// </summary>
        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            //PrepareBuild();
        }
        private void BuildButton2_Click(object sender, RoutedEventArgs e)
        {
            PrepareBuild();
            Flyout f = FlyoutService.GetFlyout(BuildButton) as Flyout;
            if (f != null)
            {
                f.Hide();
            }
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        /// <summary>
        /// ターゲットリスト追加ボタン
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string? sourceFolder = SourceList.SelectedItem as string;
            if (sourceFolder != null)
            {
                TargetList.Items.Add(sourceFolder);
            }
        }
        /// <summary>
        /// ターゲットリスト削除ボタン
        /// </summary>
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            string? targetFolder = TargetList.SelectedItem as string;
            TargetList.Items.Remove(targetFolder);
        }

        private void ProjCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            InputSourceList();
        }

        private void SourceList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddButton_Click(sender, e);
        }
        private void SourceList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                AddButton_Click(sender, e);
            }
        }
        private void TargetList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RemoveButton_Click(sender, e);
        }
        /// <summary>
        /// プロジェクト設定画面を開く
        /// </summary>
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            // ProjCombo.Textが空白の場合はProjComboの1つ目の値を取得
            string? pj = ProjCombo.Text;
            if (ProjCombo.Text == "")
            {
                pj = ProjCombo.Items[0] as string;
            }
            var window = new ProjectSettings(pj);
            window.Owner = this;
            window.ShowDialog();
        }
        /// <summary>
        /// ビルドログを表示チェックボックス更新時設定ファイルを更新する
        /// </summary>
        private void ShowLogCheck_Click(object sender, RoutedEventArgs e)
        {
            saveJson();
        }
        /// <summary>
        /// TargetListで選択した値を上と入れ替える
        /// </summary>
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (TargetList.SelectedItem == null)
            {
                return;
            }
            int index = TargetList.SelectedIndex;
            string? item = TargetList.SelectedItem.ToString();
            if (index == 0)
            {
                return;
            }
            TargetList.Items.RemoveAt(index);
            TargetList.Items.Insert(index - 1, item);
            TargetList.SelectedIndex = index - 1;
        }
        /// <summary>
        /// TargetListで選択した値を下と入れ替える
        /// </summary>
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (TargetList.SelectedItem == null)
            {
                return;
            }
            int index = TargetList.SelectedIndex;
            string? item = TargetList.SelectedItem.ToString();
            if (index == TargetList.Items.Count - 1)
            {
                return;
            }
            TargetList.Items.RemoveAt(index);
            TargetList.Items.Insert(index + 1, item);
            TargetList.SelectedIndex = index + 1;
        }
        /// <summary>
        /// プロジェクト設定を表示
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // ProjComboが空白の場合はProjComboの1つ目の値をセット
            string? pj = ProjCombo.Text;
            if (ProjCombo.Text == "")
            {
                pj = ProjCombo.Items[0] as string;
            }
            var window = new ProjectSettings(pj);
            window.Owner = this;
            window.ShowDialog();
        }
        /// <summary>
        /// バージョン情報を表示
        /// </summary>
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new VersionWindow();
                window.Owner = this;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 入力内容でSourceListをフィルタリング
        /// </summary>
        private void SearchTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceList.Items.Clear();
            foreach (string item in List.sourceList)
            {
                //大文字小文字を区別せず検索
                if (item.IndexOf(SearchTextbox.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    SourceList.Items.Add(item);
                }
            }
        }

        private void TargetList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                RemoveButton_Click(sender, e);
            }
            if (e.Key == Key.D)
            {
                RemoveButton_Click(sender, e);
            }
            if (e.Key == Key.K)
            {
                UpButton_Click(sender, e);
            }
            if (e.Key == Key.J)
            {
                DownButton_Click(sender, e);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                BuildButton2_Click(sender, e);
            }
        }
        private void ColorSetting_Click(object sender, RoutedEventArgs e)
        {
            var window = new ColorSettings();
            window.Owner = this;
            window.ShowDialog();
        }
        #endregion

        #region methods
        public void InputSourceList()
        {
            string? projectName = ProjCombo.SelectedItem as string;
            var proj = Projects.ProjectsList.FirstOrDefault(x => x.Value.ProjectName == projectName);

            SourceList.Items.Clear();
            TargetList.Items.Clear();

            if (projectName != null)
            {
                if (System.IO.Directory.Exists(proj.Value.SourceFolder))
                {
                    try
                    {
                        string[] subFolders = System.IO.Directory.GetDirectories(
                            path: proj.Value.SourceFolder, "*");

                        List.sourceList.Clear();

                        foreach (string subFolder in subFolders)
                        {
                            string[] files = System.IO.Directory.GetFiles(
                                path: subFolder, "*.csproj", SearchOption.AllDirectories);
                            foreach (string filepath in files)
                            {
                                // ファイルのフルパスからソースフォルダを削除してリストボックスに表示する
                                string filename = filepath.Replace(proj.Value.SourceFolder, "");
                                SourceList.Items.Add(filename);
                                List.sourceList.Add(filename);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    ModernWpf.MessageBox.Show("フォルダが存在しません。\n" + proj.Value.SourceFolder, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        /// <summary>
        /// ビルド前準備
        /// PrepareBuild(UIスレッド)→RunBuild(ワーカースレッド)→ShowResult(UIスレッド)の順で実行します
        /// </summary>
        private void PrepareBuild()
        {
            try
            {

                if (File.Exists(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt"))
                {
                    // エラーログ出力用ファイルをクリア
                    File.WriteAllText(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt", "");
                }
                List<string> targets = TargetList.Items.Cast<string>().ToList();
                if (targets.Count == 0)
                {
                    ModernWpf.MessageBox.Show("ビルド対象を選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                //if (ModernWpf.MessageBox.Show("ビルドを実行しますか？", "確認", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                //{
                //    return;
                //}

                ProgressRing.IsActive = true;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = targets.Count;
                BuildButton.IsEnabled = false;

                string? projectName = ProjCombo.SelectedItem as string;
                string? SourceFolder = Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.SourceFolder).FirstOrDefault();
                string? OutputFolder = Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.OutputFolder).FirstOrDefault();
                string? MsBuild = Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.MsBuild).FirstOrDefault();
                string? Target = Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Target).FirstOrDefault();
                string? AssemblySearchPaths = Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.AssemblySearchPaths).FirstOrDefault();
                string? Configuration = Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Configuration).FirstOrDefault();

                // 別スレッドでビルドを実行
                Task.Run(() => RunBuild(targets
                                        , SourceFolder
                                        , OutputFolder
                                        , MsBuild
                                        , Target
                                        , AssemblySearchPaths
                                        , Configuration
                                        ));
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        /// <summary>
        /// ビルド実行
        /// </summary>
        /// <param name="targets">ビルドターゲットファイルのリスト</param>
        /// <param name="SourceFolder">ソースフォルダのパス</param>
        /// <param name="OutputFolder">dll出力先フォルダのパス</param>
        /// <param name="MsBuild">MsBuildのパス</param>
        /// <param name="Target">MsBuildパラメータ:Target</param>
        /// <param name="AssemblySearchPaths">MsBuildパラメータ:AssemblySearchPaths</param>
        /// <param name="Configuration">MsBuildパラメータ:Configuration</param>
        private void RunBuild(List<string> targets ,string? SourceFolder,string? OutputFolder,string? MsBuild, string? Target,string? AssemblySearchPaths,string? Configuration)
        {
            try
            {
                string resultText = "";
                string cmdErrorText = "";

                int targetIndex = 0;

                string? asp = AssemblySearchPaths == "" ? "" : "/p:AssemblySearchPaths=\"" + AssemblySearchPaths + "\" ";
                
                foreach (var target in targets)
                {
                    string targetFilePath = SourceFolder + target;
                    Process? process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"" +
                        $"\"{MsBuild}\" " +
                        $"{targetFilePath} " +
                        $"/target:{Target} " +
                        $"/p:OutputPath={OutputFolder} /p:DebugType=None " +
                        asp +
                        $"/p:Configuration={Configuration} " +
                        $"/fileloggerparameters:LogFile=\"{Directory.GetCurrentDirectory()}\\BuildErrorLog.txt\";ErrorsOnly;Append=True",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    });

                    // 標準出力・標準エラー出力・終了コードを取得する
                    string? standardOutput = process?.StandardOutput.ReadToEnd();
                    string? standardError = process?.StandardError.ReadToEnd();
                    resultText += "------------------------------------------------------------------------------------------\n\n" +
                                  $"[{targetIndex+1}] {target}\n\n" +
                                  "------------------------------------------------------------------------------------------\n\n" +
                                  standardOutput + "\n\n\n\n\n\n";
                    cmdErrorText += standardError;

                    process?.Close();
                    
                    // ProgressBarを進める
                    Application.Current.Dispatcher.Invoke(() => {
                        targetIndex += 1;
                        ProgressBar.Visibility = Visibility.Visible;
                        ProgressBar.Value = targetIndex;
                    });
                }
                // UIスレッドでShowResultを実行
                this.Dispatcher.Invoke(new DelegateProcess(ShowResult), new object[] { resultText, cmdErrorText});
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }
        /// <summary>
        /// ビルド実行結果を表示
        /// </summary>
        /// <param name="resultText">実行結果</param>
        /// <param name="cmdErrorText">コマンドエラー結果</param>
        private void ShowResult(string resultText , string cmdErrorText)
        {
            try
            {
                ProgressRing.IsActive = false;
                
                bool errorFlg = false;
                if (File.Exists(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt") && File.ReadAllText(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt") != "" )
                {
                    errorFlg = true;
                }
                
                if (errorFlg == false)
                {
                    ModernWpf.MessageBox.Show("実行完了", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (ShowLogCheck.IsChecked == true)
                    {
                        var window = new Console("実行結果", TargetList.Items.Count);
                        window.CmdResult.Text = resultText;
                        window.Show();
                    }
                    if (cmdErrorText != "")
                    {
                        var window = new Console("エラー", TargetList.Items.Count);
                        window.CmdResult.Text = cmdErrorText;
                        window.Show();
                    }
                }

                if (errorFlg)
                {
                    // エラー出力ファイルの読み込み
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // Shift-JISを扱うためのおまじない
                    StreamReader resultErrorText = new StreamReader(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt"
                        , System.Text.Encoding.GetEncoding("shift_jis"));
                    // エラーウィンドウを表示
                    var errorWindow = new Console("エラーログ", TargetList.Items.Count);
                    errorWindow.CmdResult.Text = resultErrorText.ReadToEnd();
                    resultErrorText.Close();
                    
                    ModernWpf.MessageBox.Show("ビルドに失敗しました。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    if (ShowLogCheck.IsChecked == true)
                    {
                        var window = new Console("実行結果", TargetList.Items.Count);
                        window.CmdResult.Text = resultText;
                        window.Show();
                    }
                    if (cmdErrorText != "")
                    {
                        var window = new Console("エラー", TargetList.Items.Count);
                        window.CmdResult.Text = cmdErrorText;
                        window.Show();
                    }
                    errorWindow.Show();
                }
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Hidden;
                BuildButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// jsonファイルの読み込み
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void LoadAppSettings()
        {
            try
            {
                Projects.ProjectsList.Clear();

                IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(App.AppSettingsFile, optional: true, reloadOnChange: false)
                .Build();

                // 設定読み込み
                var projects = config.GetSection("Project").Get<ProjectData[]>();
                foreach (var proj in projects)
                {
                    Projects.ProjectsList.Add(proj.Id, new Projects.ProjectData
                    {
                        ProjectName = proj.ProjectName,
                        SourceFolder = proj.SourceFolder,
                        OutputFolder = proj.OutputFolder,
                        MsBuild = proj.MsBuild,
                        Target = proj.Target,
                        AssemblySearchPaths = proj.AssemblySearchPaths,
                        Configuration = proj.Configuration
                    });
                }

                Debug.Print("■取得プロジェクト一覧");
                foreach (var proj in Projects.ProjectsList)
                {
                    Debug.Print($"Id: {proj.Key}, ProjectName: {proj.Value.ProjectName}, SourceFolder: {proj.Value.SourceFolder}" +
                        $", OutputFolder: {proj.Value.OutputFolder}, MsBuild: {proj.Value.MsBuild}" +
                        $", Target: {proj.Value.Target}, AssemblySearchPaths: {proj.Value.AssemblySearchPaths}" +
                        $", Configuration: {proj.Value.Configuration}");
                }
                Projects.ShowLog = config.GetValue<bool>("ShowLog", false);
                ShowLogCheck.IsChecked = Projects.ShowLog;

                string theme = config.GetValue<string>("Theme");
                if (theme == "Dark")
                {
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                }
                else if (theme == "Light")
                {
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                }
                else
                {
                    ThemeManager.Current.ApplicationTheme = null;
                }
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// 設定ファイルを更新
        /// </summary>
        public void saveJson()
        {
            try
            {
                bool isChecked = ShowLogCheck.IsChecked ?? false;
                string? settingTheme;
                if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark)
                {
                    settingTheme = "Dark";
                }
                else if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light)
                {
                    settingTheme = "Light";
                }
                else
                {
                    settingTheme = "Default";
                }
                Appsettings appsettings = new Appsettings
                {
                    Project = new List<ProjectData>(),
                    ShowLog = isChecked.ToString(),
                    Theme = settingTheme
                };
                foreach (var proj in Projects.ProjectsList)
                {
                    appsettings.Project.Add(new ProjectData
                    {
                        Id = proj.Key,
                        ProjectName = proj.Value.ProjectName,
                        SourceFolder = proj.Value.SourceFolder,
                        OutputFolder = proj.Value.OutputFolder,
                        MsBuild = proj.Value.MsBuild,
                        Target = proj.Value.Target,
                        AssemblySearchPaths = proj.Value.AssemblySearchPaths,
                        Configuration = proj.Value.Configuration,
                    });
                }
                var jsonData = JsonConvert.SerializeObject(appsettings, Formatting.Indented);
                using (var sw = new StreamWriter($"{AppContext.BaseDirectory}/appsettings.json", false, System.Text.Encoding.UTF8))
                {
                    sw.Write(jsonData);
                }
                LoadAppSettings();
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        #endregion
    }
}
