using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Linq;
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

                // ModernWpf.MessageBoxで確認(入力内容が変更されています。\n保存せず画面を閉じますか？)
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
            // jsonファイルに保存
            ((MainWindow)this.Owner).saveJson();
            // MainWindowのプロジェクトドロップダウンをクリアして再セット
            ((MainWindow)this.Owner).ProjCombo.Items.Clear();
            MainWindow.Projects.ProjectsList.ToList().ForEach(x => ((MainWindow)this.Owner).ProjCombo.Items.Add(x.Value.ProjectName));
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
                    ModernWpf.MessageBox.Show("すでに存在するプロジェクト名です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var result = ModernWpf.MessageBox.Show("削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
        /// フォルダダイアログを開く
        /// </summary>
        /// <param name="FolderPicker">フォルダ選択true/ファイル選択false</param>
        /// <returns>選択したパス</returns>
        private string OpenFolderDialog(bool FolderPicker, string FolderPath)
        {
            var browser = new CommonOpenFileDialog();
            if (FolderPath == "")
            {
                browser.InitialDirectory = @"C:\";
            }
            else
            {
                browser.InitialDirectory = FolderPath;
            }
            browser.Title = "フォルダを選択してください";
            browser.IsFolderPicker = FolderPicker;
            if (browser.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = browser.FileName;
                return path;
            }
            return FolderPath;
        }
        /// <summary>
        /// ProjSettingComboに値をセット
        /// </summary>
        /// <param name="item"></param>
        public void SetParameter(string item)
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
        #endregion
    }
}
