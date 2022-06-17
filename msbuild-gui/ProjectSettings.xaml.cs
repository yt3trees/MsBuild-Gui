using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace msbuild_gui
{
    /// <summary>
    /// ProjectSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class ProjectSettings : Window
    {
        #region fields
        public static string? projText { get; set; }
        public static string? ProjectNameTemp = "";
        public static bool IsProjContentsCopy = false;
        #endregion

        #region properties
        [JsonObject("Appsettings")]
        class Appsettings
        {
            [JsonProperty("Project")]
            public List<ProjectData>? Project { get; set; }
        }
        [JsonObject("ProjectData")]
        public class ProjectData
        {
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
        #endregion

        #region constructors
        public ProjectSettings(string projTexts)
        {
            InitializeComponent();
            projText = projTexts;
        }
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            ProjSettingCombo.Items.Clear();
            //Projects.ProjectsListをProjSettingComboに追加
            foreach (var item in MainWindow.Projects.ProjectsList)
            {
                ProjSettingCombo.Items.Add(item.Value.ProjectName);
            }
            //ProjSettingComboが選択されているならSetParamaterを実行
            if (ProjSettingCombo.SelectedItem != null)
            {
                SetParameter((string)ProjSettingCombo.SelectedItem);
            }
            // MainWindowからプロジェクト名を受け取って格納
            ProjSettingCombo.Text = projText;
        }
        #endregion

        #region ui events
        /// <summary>
        /// 閉じるボタン押下時
        /// </summary>
        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ProjSettingComboで何も選択されてないなら終了
            if (ProjSettingCombo.SelectedItem == null)
            {
                return;
            }
            // MainWindow.Projects.ProjectsListの内容と入力内容を比較して変更があった場合は閉じてよいか確認する
            string? projectName = ProjSettingCombo.SelectedItem as string;
            if (MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.SourceFolder).FirstOrDefault() != ProjFolderPath.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.OutputFolder).FirstOrDefault() != OutputFolderPath.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.MsBuild).FirstOrDefault() != MsBuildPath.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Target).FirstOrDefault() != TargetCombo.Text
                // TODO:*置換対策
                //| MainWindow.Projects.ProjectsList
                //.Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.AssemblySearchPaths).FirstOrDefault() != (AssemblySearchPath1.Text == "" ? "" : AssemblySearchPath1.Text + ";")
                //                                                                                                           + (AssemblySearchPath2.Text == "" ? "" : AssemblySearchPath2.Text + ";")
                //                                                                                                           + (AssemblySearchPath3.Text == "" ? "" : AssemblySearchPath3.Text + ";")                                                                                                      
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Configuration).FirstOrDefault() != ConfigurationCombo.Text
                )
            {
                var result = ModernWpf.MessageBox.Show("入力内容が変更されています。\n保存せず画面を閉じますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //ウィンドウを閉じる
            this.Close();
        }

        private void ProjFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var cofd = new CommonOpenFileDialog()
            {
                Title = "フォルダ選択",
                InitialDirectory = ProjFolderPath.Text,
                IsFolderPicker = true,
            }
                )
            {
                if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ProjFolderPath.Text = cofd.FileName;
                }
            }
        }

        private void OutputFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var cofd = new CommonOpenFileDialog()
            {
                Title = "dll出力先フォルダ選択",
                InitialDirectory = OutputFolderPath.Text,
                IsFolderPicker = true,
            }
                )
            {
                if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    OutputFolderPath.Text = cofd.FileName;
                }
            }
        }

        private void MsBuildFileSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var cofd = new CommonOpenFileDialog()
            {
                Title = "MsBuild.exe選択",
                InitialDirectory = MsBuildPath.Text.Substring(0, MsBuildPath.Text.LastIndexOf("\\")),
                IsFolderPicker = false,
            }
                )
            {
                if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    MsBuildPath.Text = cofd.FileName;
                }
            }
        }
        private void ProjSettingCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SetParameter((string)ProjSettingCombo.SelectedItem);
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = ModernWpf.MessageBox.Show("保存しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
                // ProjSettingComboが空白の場合は実行しない
                if (ProjSettingCombo.Text != "")
                {
                    // 入力した値を取得
                    string? projectName = ProjSettingCombo.SelectedItem as string;
                    string? SourceFolder = ProjFolderPath.Text;
                    string? OutputFolder = OutputFolderPath.Text;
                    string? MsBuild = MsBuildPath.Text;
                    string? Target = TargetCombo.Text;
                    string? AssemblySearchPaths = (AssemblySearchPath1.Text == "" ? "" : AssemblySearchPath1.Text + ";")
                                                    + (AssemblySearchPath2.Text == "" ? "" : AssemblySearchPath2.Text + ";")
                                                    + (AssemblySearchPath3.Text == "" ? "" : AssemblySearchPath3.Text + ";");
                    string? Configuration = ConfigurationCombo.Text;
                    //projectNameからProjectsのKeyを取得
                    int key = MainWindow.Projects.ProjectsList
                            .Where(x => x.Value.ProjectName == projectName).Select(x => x.Key).FirstOrDefault();

                    // 内容をjsonに保存
                    MainWindow.Projects.ProjectsList[key].SourceFolder = SourceFolder;
                    MainWindow.Projects.ProjectsList[key].OutputFolder = OutputFolder;
                    MainWindow.Projects.ProjectsList[key].MsBuild = MsBuild;
                    MainWindow.Projects.ProjectsList[key].Target = Target;
                    MainWindow.Projects.ProjectsList[key].AssemblySearchPaths = AssemblySearchPaths;
                    MainWindow.Projects.ProjectsList[key].Configuration = Configuration;
                }
                // 設定ファイルに保存
                ((MainWindow)this.Owner).saveJson();
                // MainWindowのプロジェクトドロップダウンをクリアして再セット
                ((MainWindow)this.Owner).ProjCombo.Items.Clear();
                MainWindow.Projects.ProjectsList.ToList().ForEach(x => ((MainWindow)this.Owner).ProjCombo.Items.Add(x.Value.ProjectName));
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ASPCopyButton1_Click(object sender, RoutedEventArgs e)
        {
            AssemblySearchPath1.Text = OutputFolderPath.Text;
        }

        private void ASPCopyButton2_Click(object sender, RoutedEventArgs e)
        {
            AssemblySearchPath2.Text = OutputFolderPath.Text;
        }

        private void ASPCopyButton3_Click(object sender, RoutedEventArgs e)
        {
            AssemblySearchPath3.Text = OutputFolderPath.Text;
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectNameTemp = ProjSettingCombo.Text;
            InputWindow window = new InputWindow();
            window.Owner = this;
            // window.AnswerをMainWindow.Projects.ProjectsListに追加
            if (window.ShowDialog() == true)
            {
                // window.Answerがすでに存在するなら登録できない(大文字小文字を区別しない)
                if (MainWindow.Projects.ProjectsList.Any(x => x.Value.ProjectName.ToLower() == window.Answer.ToLower()))
                {
                    ModernWpf.MessageBox.Show("すでに存在するプロジェクト名です。", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // MainWindow.Projects.ProjectsListのID+1を取得
                int key = MainWindow.Projects.ProjectsList.Keys.Max() + 1;
                MainWindow.Projects.ProjectsList.Add(key, new MainWindow.Projects.ProjectData
                {
                    // InputWindowのチェックボックスがオンの場合は入力内容をコピーする
                    ProjectName = window.Answer,
                    SourceFolder = IsProjContentsCopy == true ? ProjFolderPath.Text : "",
                    OutputFolder = IsProjContentsCopy == true ? OutputFolderPath.Text : "",
                    MsBuild = IsProjContentsCopy == true ? MsBuildPath.Text : "",
                    Target = IsProjContentsCopy == true ? TargetCombo.Text : "",
                    Configuration = IsProjContentsCopy == true ? ConfigurationCombo.Text : "",
                    AssemblySearchPaths = IsProjContentsCopy == true ? (AssemblySearchPath1.Text == "" ? "" : AssemblySearchPath1.Text + ";")
                                                + (AssemblySearchPath2.Text == "" ? "" : AssemblySearchPath2.Text + ";")
                                                + (AssemblySearchPath3.Text == "" ? "" : AssemblySearchPath3.Text + ";") : ""
                });
                Window_SourceInitialized(null, null);
                // 追加したプロジェクトをセット
                SetParameter(window.Answer);
            }

        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 1つしか存在しない場合は削除できないようにする
            if (ProjSettingCombo.Items.Count == 1)
            {
                ModernWpf.MessageBox.Show("全てのプロジェクトを削除することはできません。", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // 未選択なら削除できない
            if (ProjSettingCombo.SelectedItem == null)
            {
                ModernWpf.MessageBox.Show("削除対象を選択してください。", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 削除してよいですか？
            var result = ModernWpf.MessageBox.Show($"{ProjSettingCombo.SelectedItem}を削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            string? projectName = ProjSettingCombo.SelectedItem as string;
            // projectNameからProjectsのKeyを取得
            int key = MainWindow.Projects.ProjectsList
                    .Where(x => x.Value.ProjectName == projectName).Select(x => x.Key).FirstOrDefault();
            // 削除
            MainWindow.Projects.ProjectsList.Remove(key);

            // コンボボックスの内容をクリアして再セット
            ProjSettingCombo.Items.Clear();
            foreach (var item in MainWindow.Projects.ProjectsList)
            {
                ProjSettingCombo.Items.Add(item.Value.ProjectName);
            }
            ProjFolderPath.Text = "";
            OutputFolderPath.Text = "";
            MsBuildPath.Text = "";
            TargetCombo.Text = "";
            ConfigurationCombo.Text = "";
            AssemblySearchPath1.Text = "";
            AssemblySearchPath2.Text = "";
            AssemblySearchPath3.Text = "";

            // MainWindowのプロジェクトドロップダウンをクリアして再セット
            ((MainWindow)this.Owner).ProjCombo.Items.Clear();
            MainWindow.Projects.ProjectsList.ToList().ForEach(x => ((MainWindow)this.Owner).ProjCombo.Items.Add(x.Value.ProjectName));
            // 1つ目を選択
            ProjSettingCombo.SelectedIndex = 0;
        }
        /// <summary>
        /// 登録内容をエクスポート
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            //var ms = ModernWpf.MessageBox.Show("登録内容ををエクスポートします。", "確認", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            //if (ms == MessageBoxResult.Cancel)
            //{
            //    return;
            //}
            // MainWindow.ProjectDataのデータをjson出力
            //string? projectName = ProjSettingCombo.SelectedItem as string;
            //// projectNameからProjectsのKeyを取得
            //int key = MainWindow.Projects.ProjectsList
            //        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Key).FirstOrDefault();
            Appsettings appsettings = new Appsettings
            {
                Project = new List<ProjectData>(),
            };
            foreach (var proj in MainWindow.Projects.ProjectsList)
            {
                appsettings.Project.Add(new ProjectData
                {
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
            // jsonファイルに出力
            var now = DateTime.Now.ToString("yyyyMMdd");
            string fileName = now + "_msbuildgui.json";

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "ファイルを保存する"; // ダイアログタイトル
            saveFileDialog.InitialDirectory = @"C:\";    // 初期のディレクトリ
            saveFileDialog.FileName = fileName;       // デフォルトファイル名
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = saveFileDialog.FileName;
                System.IO.File.WriteAllText(filePath, jsonData, Encoding.UTF8);
                ModernWpf.MessageBox.Show($"エクスポート完了\n{filePath}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                //string filePath = System.IO.Path.Combine(System.I, fileName);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            //var ms = ModernWpf.MessageBox.Show("jsonファイルをインポートします。", "確認", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            //if (ms == MessageBoxResult.Cancel)
            //{
            //    return;
            //}
            // jsonファイルを選択する
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "ファイルを開く";               // ダイアログタイトル
            openFileDialog.InitialDirectory = @"C:\";              // 初期のディレクトリ
            openFileDialog.FileName = "msbuildgui.json";           // デフォルトファイル名
            openFileDialog.Filter = "jsonファイル(*.json)|*.json"; // jsonファイルのみ選択可
            bool? result = openFileDialog.ShowDialog();
            // 選択したファイルをインポート
            if (result == true)
            {
                string filePath = openFileDialog.FileName;
                string jsonData = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
                Appsettings appsettings = JsonConvert.DeserializeObject<Appsettings>(jsonData);
                string[] projects = { };
                try
                {
                    foreach (var proj in appsettings.Project)
                    {
                        // window.Answerがすでに存在するなら登録できない(大文字小文字を区別しない)
                        if (MainWindow.Projects.ProjectsList.Any(x => x.Value.ProjectName.ToLower() == proj.ProjectName.ToLower()))
                        {
                            ModernWpf.MessageBox.Show($"すでに存在するプロジェクト名はインポートできません。\n{proj.ProjectName}", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    // jsonファイルからデータを取得
                    foreach (var proj in appsettings.Project)
                    {
                        // projectsにproj.ProjectNameを格納
                        Array.Resize(ref projects, projects.Length + 1);
                        projects[projects.Length - 1] = proj.ProjectName;

                        // ProjectsListのId+1を取得
                        int id = MainWindow.Projects.ProjectsList.Count + 1;
                        // idがすでに存在するならさらに+1
                        while (MainWindow.Projects.ProjectsList.Any(x => x.Key == id))
                        {
                            id++;
                        }
                        MainWindow.Projects.ProjectsList.Add(id, new MainWindow.Projects.ProjectData
                        {
                            ProjectName = proj.ProjectName,
                            SourceFolder = proj.SourceFolder,
                            OutputFolder = proj.OutputFolder,
                            MsBuild = proj.MsBuild,
                            Target = proj.Target,
                            AssemblySearchPaths = proj.AssemblySearchPaths,
                            Configuration = proj.Configuration,
                        });
                    }
                    // 設定ファイルに保存
                    ((MainWindow)this.Owner).saveJson();
                    // 結果表示
                    string importProj = string.Join(", ", projects);
                    ModernWpf.MessageBox.Show($"インポート完了\n{importProj}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    // コンボボックスを更新
                    Window_SourceInitialized(null, null);
                    // MainWindowのプロジェクトドロップダウンをクリアして再セット
                    ((MainWindow)this.Owner).ProjCombo.Items.Clear();
                    MainWindow.Projects.ProjectsList.ToList().ForEach(x => ((MainWindow)this.Owner).ProjCombo.Items.Add(x.Value.ProjectName));
                }
                catch (Exception ex)
                {
                    ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Escキーが押されたとき
            if (e.Key == Key.Escape)
            {
                // ウィンドウを閉じる
                this.Close();
            }
            // Ctrl+Sキーが押されたとき
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // プロジェクト設定を保存
                SaveButton_Click(null, null);
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// ProjSettingComboに値をセット
        /// </summary>
        /// <param name="item"></param>
        public void SetParameter(string item)
        {
            try
            {
                // 選択した値を取得
                string? projectName = item;
                ProjSettingCombo.Text = projectName;

                string? SourceFolder = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.SourceFolder).FirstOrDefault();
                string? OutputFolder = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.OutputFolder).FirstOrDefault();
                string? MsBuild = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.MsBuild).FirstOrDefault();
                string? Target = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Target).FirstOrDefault();
                string? AssemblySearchPaths = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.AssemblySearchPaths).FirstOrDefault();
                string[] AssmblySearchPath123 = new string[3];
                AssmblySearchPath123 = (AssemblySearchPaths == null ? "" : AssemblySearchPaths).Split(';');
                string? Configuration = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Configuration).FirstOrDefault();

                ProjFolderPath.Text = (SourceFolder == null ? "" : SourceFolder).ToString();
                OutputFolderPath.Text = (OutputFolder == null ? "" : OutputFolder).ToString();
                MsBuildPath.Text = (MsBuild == null ? "" : MsBuild).ToString();
                TargetCombo.Text = (Target == null ? "" : Target).ToString();
                ConfigurationCombo.Text = (Configuration == null ? "" : Configuration).ToString();

                //配列数が存在するなら格納する
                AssemblySearchPath1.Text = "";
                AssemblySearchPath2.Text = "";
                AssemblySearchPath3.Text = "";
                if (AssmblySearchPath123.Length > 0)
                {
                    AssemblySearchPath1.Text = AssmblySearchPath123[0] == "*" ? (OutputFolder == null ? "" : OutputFolder).ToString() : AssmblySearchPath123[0];
                }
                if (AssmblySearchPath123.Length > 1)
                {
                    AssemblySearchPath2.Text = AssmblySearchPath123[1] == "*" ? (OutputFolder == null ? "" : OutputFolder).ToString() : AssmblySearchPath123[1];
                }
                if (AssmblySearchPath123.Length > 2)
                {
                    AssemblySearchPath3.Text = AssmblySearchPath123[2] == "*" ? (OutputFolder == null ? "" : OutputFolder).ToString() : AssmblySearchPath123[2];
                }
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
