using Microsoft.Extensions.Configuration;
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
        delegate void DelegateProcess(string a, string b); // デリゲート宣言
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
                // jsonファイル読み込み
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
            PrepareBuild();
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
            // SourceListの選択項目を取得
            string? sourceFolder = SourceList.SelectedItem as string;
            // TargetListに格納
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
            // TargetListの選択項目を取得
            string? targetFolder = TargetList.SelectedItem as string;
            // TargetListから削除
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
            // Spaceキー押下時
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
            string pj = ProjCombo.Text;
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
            // 何も選択されていない場合は処理を終了
            if (TargetList.SelectedItem == null)
            {
                return;
            }
            int index = TargetList.SelectedIndex;
            string? item = TargetList.SelectedItem.ToString();
            // 一番上の場合は処理を終了
            if (index == 0)
            {
                return;
            }
            TargetList.Items.RemoveAt(index);
            TargetList.Items.Insert(index - 1, item);
            // 選択状態にする
            TargetList.SelectedIndex = index - 1;
        }
        /// <summary>
        /// TargetListで選択した値を下と入れ替える
        /// </summary>
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            // 何も選択されていない場合は処理を終了
            if (TargetList.SelectedItem == null)
            {
                return;
            }
            int index = TargetList.SelectedIndex;
            string? item = TargetList.SelectedItem.ToString();
            // 一番下の場合は処理を終了
            if (index == TargetList.Items.Count - 1)
            {
                return;
            }
            TargetList.Items.RemoveAt(index);
            TargetList.Items.Insert(index + 1, item);
            //　選択状態にする
            TargetList.SelectedIndex = index + 1;
        }
        /// <summary>
        /// プロジェクト設定を表示
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // ProjCombo.Textが空白の場合はProjComboの1つ目の値を取得
            string pj = ProjCombo.Text;
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
            // Deleteキーを押したとき
            if (e.Key == Key.Delete)
            {
                RemoveButton_Click(sender, e);
            }
            // Dキーを押したとき
            if (e.Key == Key.D)
            {
                RemoveButton_Click(sender, e);
            }
            // Kキーを押したとき
            if (e.Key == Key.K)
            {
                UpButton_Click(sender, e);
            }
            // Jキーを押したとき
            if (e.Key == Key.J)
            {
                DownButton_Click(sender, e);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // F5が押されたとき
            if (e.Key == Key.F5)
            {
                BuildButton_Click(sender, e);
            }
        }
        #endregion

        #region methods
        public void InputSourceList()
        {
            // 選択した値を取得
            string? projectName = ProjCombo.SelectedItem as string;
            // プロジェクトリストからプロジェクト名を検索
            var proj = Projects.ProjectsList.FirstOrDefault(x => x.Value.ProjectName == projectName);

            // 選択したソースをクリア
            SourceList.Items.Clear();
            TargetList.Items.Clear();

            // フォルダが存在するかチェック
            if (projectName != null)
            {
                if (System.IO.Directory.Exists(proj.Value.SourceFolder))
                {
                    try
                    {
                        string[] subFolders = System.IO.Directory.GetDirectories(
                            path: proj.Value.SourceFolder, "*");

                        // プロパティを初期化(検索用)
                        List.sourceList.Clear();

                        foreach (string subFolder in subFolders)
                        {
                            // フォルダ名を取得
                            //.csprojファイルを取得
                            string[] files = System.IO.Directory.GetFiles(
                                path: subFolder, "*.csproj", SearchOption.AllDirectories);
                            foreach (string filepath in files)
                            {
                                // filesからproj.Value.SourceFolderを削除
                                string filename = filepath.Replace(proj.Value.SourceFolder, "");
                                // フォルダ名をComboBoxに追加
                                SourceList.Items.Add(filename);
                                // プロパティList.sourceListに追加(検索用)
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
                //TargetListの内容を変数に格納
                List<string> targets = TargetList.Items.Cast<string>().ToList();
                if (targets.Count == 0)
                {
                    ModernWpf.MessageBox.Show("ビルド対象を選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                // Build実行するか確認
                if (ModernWpf.MessageBox.Show("ビルドを実行しますか？", "確認", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
                // ぐるぐるを有効化
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

                //targetsの数を格納
                int targetCount = targets.Count;
                int targetIndex = 0;
               
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
                        $"/fileloggerparameters:LogFile=\"{Directory.GetCurrentDirectory()}\\BuildErrorLog.txt\";ErrorsOnly;Append=True "+
                        $"/p:OutputPath={OutputFolder} /p:DebugType=None " +
                        $"/p:AssemblySearchPaths=\"{AssemblySearchPaths}\"\" " +
                        $"/p:Configuration={Configuration}",
                        CreateNoWindow = true, // ウィンドウを表示しない
                        UseShellExecute = false,
                        RedirectStandardOutput = true, // 標準出力を取得できるようにする
                        RedirectStandardError = true // 標準エラー出力を取得できるようにする
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
                    
                    // ProgressBarを表示
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
                // ぐるぐるを無効化
                ProgressRing.IsActive = false;
                
                bool errorFlg = false;
                if (File.Exists(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt") && File.ReadAllText(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt") != "" )
                {
                    errorFlg = true;
                }
                
                // 実行結果を表示
                if (errorFlg == false)
                {
                    ModernWpf.MessageBox.Show("実行完了", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (ShowLogCheck.IsChecked == true)
                    {
                        var window = new Console("実行結果", TargetList.Items.Count);
                        window.CmdResult.Text = resultText;
                        window.Show();
                    }

                }
                // コマンドエラーを表示
                if (cmdErrorText != "" && errorFlg == false)
                {
                    var window = new Console("エラー", TargetList.Items.Count);
                    window.CmdResult.Text = cmdErrorText;
                    window.Show();
                }
                // Msbuildエラーを表示
                if (errorFlg)
                {
                    // エラー出力ファイルの読み込み
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // Shift-JISを扱うためのおまじない
                    StreamReader resultErrorText = new StreamReader(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt"
                        , System.Text.Encoding.GetEncoding("shift_jis"));
                    // エラーウィンドウを表示
                    var errorWindow = new Console("エラーログ", TargetList.Items.Count);
                    errorWindow.CmdResult.Text = resultErrorText.ReadToEnd();
                    // StreamReaderを終了する
                    resultErrorText.Close();
                    
                    ModernWpf.MessageBox.Show("ビルドに失敗しました。", "アラート", MessageBoxButton.OK, MessageBoxImage.Error);

                    // 実行結果を表示
                    if (ShowLogCheck.IsChecked == true)
                    {
                        var window = new Console("実行結果", TargetList.Items.Count);
                        window.CmdResult.Text = resultText;
                        window.Show();
                    }
                    // コマンドエラーを表示
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
                    //Projects.ProjectsList.Add(proj.Id, );
                    //追加(ProjectDataを使用)
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
                // プロジェクトリストを表示
                Debug.Print("■取得プロジェクト一覧");
                foreach (var proj in Projects.ProjectsList)
                {
                    Debug.Print($"Id: {proj.Key}, ProjectName: {proj.Value.ProjectName}, SourceFolder: {proj.Value.SourceFolder}" +
                        $", OutputFolder: {proj.Value.OutputFolder}, MsBuild: {proj.Value.MsBuild}" +
                        $", Target: {proj.Value.Target}, AssemblySearchPaths: {proj.Value.AssemblySearchPaths}" +
                        $", Configuration: {proj.Value.Configuration}");
                }
                // ShowLogを元にShowLogCheckのチェックを制御する
                Projects.ShowLog = config.GetValue<bool>("ShowLog", false);
                ShowLogCheck.IsChecked = Projects.ShowLog;
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
                // "ビルドログを表示"チェックボックスのチェック状態を取得
                bool isChecked = ShowLogCheck.IsChecked ?? false;
                Appsettings appsettings = new Appsettings
                {
                    Project = new List<ProjectData>(),
                    ShowLog = isChecked.ToString(),
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
                    // JSON データをファイルに書き込み
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
