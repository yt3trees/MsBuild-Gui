using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModernWpf.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            [JsonProperty("VisualStudioVersion")]
            public string? VisualStudioVersion { get; set; }
        }
        #endregion

        #region constructors
        public ProjectSettings(string projTexts)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            projText = projTexts;
        }
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            ProjSettingCombo.Items.Clear();
            foreach (var item in MainWindow.Projects.ProjectsList)
            {
                ProjSettingCombo.Items.Add(item.Value.ProjectName);
            }
            if (ProjSettingCombo.SelectedItem != null)
            {
                SetParameter((string)ProjSettingCombo.SelectedItem);
            }
            ProjSettingCombo.Text = projText;
        }
        #endregion

        #region ui events
        /// <summary>
        /// 閉じるボタン押下時
        /// </summary>
        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ProjSettingCombo.SelectedItem == null)
            {
                return;
            }
            // 保存せず閉じてよいか確認
            string? projectName = ProjSettingCombo.SelectedItem as string;
            if (MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.SourceFolder).FirstOrDefault() != ProjFolderPath.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.OutputFolder).FirstOrDefault() != OutputFolderPath.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.MsBuild).FirstOrDefault() != MsBuildPath.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Target).FirstOrDefault() != TargetCombo.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.AssemblySearchPaths).FirstOrDefault() != (AssemblySearchPath1.Text == "" ? "" : AssemblySearchPath1.Text + ";")
                                                                                                                           + (AssemblySearchPath2.Text == "" ? "" : AssemblySearchPath2.Text + ";")
                                                                                                                           + (AssemblySearchPath3.Text == "" ? "" : AssemblySearchPath3.Text + ";")
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Configuration).FirstOrDefault() != ConfigurationCombo.Text
                | MainWindow.Projects.ProjectsList
                .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.VisualStudioVersion).FirstOrDefault() != VisualStudioVersionText.Text
                )
            {
                var result = ModernWpf.MessageBox.Show(Properties.Resources.CloseTheWindowWithoutSaving1
                    + "\n" + Properties.Resources.CloseTheWindowWithoutSaving2, Properties.Resources.Confirmation, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ProjFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var cofd = new CommonOpenFileDialog()
                {
                    Title = Properties.Resources.SelectFolder,
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
                    Title = Properties.Resources.SelectOutputDestinationFolder,
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
                    Title = Properties.Resources.SelectMsBuild,
                    InitialDirectory = MsBuildPath.Text == "" ? "" : MsBuildPath.Text.Substring(0, MsBuildPath.Text.LastIndexOf("\\")),
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
                if (ProjSettingCombo.Text != "")
                {
                    string? projectName = ProjSettingCombo.SelectedItem as string;
                    string? SourceFolder = ProjFolderPath.Text;
                    string? OutputFolder = OutputFolderPath.Text;
                    string? MsBuild = MsBuildPath.Text;
                    string? Target = TargetCombo.Text;
                    string? AssemblySearchPaths = (AssemblySearchPath1.Text == "" ? "" : AssemblySearchPath1.Text + ";")
                                                    + (AssemblySearchPath2.Text == "" ? "" : AssemblySearchPath2.Text + ";")
                                                    + (AssemblySearchPath3.Text == "" ? "" : AssemblySearchPath3.Text + ";");
                    string? Configuration = ConfigurationCombo.Text;
                    string? VisualStudioVersion = VisualStudioVersionText.Text;

                    int key = MainWindow.Projects.ProjectsList
                            .Where(x => x.Value.ProjectName == projectName).Select(x => x.Key).FirstOrDefault();
                    MainWindow.Projects.ProjectsList[key].SourceFolder = SourceFolder;
                    MainWindow.Projects.ProjectsList[key].OutputFolder = OutputFolder;
                    MainWindow.Projects.ProjectsList[key].MsBuild = MsBuild;
                    MainWindow.Projects.ProjectsList[key].Target = Target;
                    MainWindow.Projects.ProjectsList[key].AssemblySearchPaths = AssemblySearchPaths;
                    MainWindow.Projects.ProjectsList[key].Configuration = Configuration;
                    MainWindow.Projects.ProjectsList[key].VisualStudioVersion = VisualStudioVersion;
                }
                // 設定ファイルに保存
                ((MainWindow)this.Owner).saveJson();
                // MainWindowのプロジェクトドロップダウンをクリアして再セット
                ((MainWindow)this.Owner).ProjCombo.Items.Clear();
                MainWindow.Projects.ProjectsList.ToList().ForEach(x => ((MainWindow)this.Owner).ProjCombo.Items.Add(x.Value.ProjectName));
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (window.ShowDialog() == true)
            {
                // 入力したプロジェクト名重複している場合は登録できない(大文字小文字を区別しない)
                if (MainWindow.Projects.ProjectsList.Any(x => x.Value.ProjectName.ToLower() == window.Answer.ToLower()))
                {
                    ModernWpf.MessageBox.Show(Properties.Resources.Mb_TheNameOfAProjectThatAlreadyExists, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
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
                    VisualStudioVersion = IsProjContentsCopy == true ? VisualStudioVersionText.Text : "",
                    AssemblySearchPaths = IsProjContentsCopy == true ? (AssemblySearchPath1.Text == "" ? "" : AssemblySearchPath1.Text + ";")
                                                + (AssemblySearchPath2.Text == "" ? "" : AssemblySearchPath2.Text + ";")
                                                + (AssemblySearchPath3.Text == "" ? "" : AssemblySearchPath3.Text + ";") : ""
                });
                Window_SourceInitialized(sender, e);
                // 追加したプロジェクトをセット
                SetParameter(window.Answer);
            }

        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjSettingCombo.Items.Count == 1)
            {
                ModernWpf.MessageBox.Show(Properties.Resources.Mb_NotAllProjectsCanBeRemoved, Properties.Resources.Alert, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (ProjSettingCombo.SelectedItem == null)
            {
                ModernWpf.MessageBox.Show(Properties.Resources.Mb_PlsSelectTheTargetForDeletion, Properties.Resources.Alert, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = ModernWpf.MessageBox.Show($"{ProjSettingCombo.SelectedItem}"+ Properties.Resources.Mb_DoYouWantToRemove, Properties.Resources.Confirmation, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string? projectName = ProjSettingCombo.SelectedItem as string;
            int key = MainWindow.Projects.ProjectsList
                    .Where(x => x.Value.ProjectName == projectName).Select(x => x.Key).FirstOrDefault();
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
            ProjSettingCombo.SelectedIndex = 0;
        }
        /// <summary>
        /// 登録内容をエクスポート
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
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
                    VisualStudioVersion = proj.Value.VisualStudioVersion
                });
            }
            var jsonData = JsonConvert.SerializeObject(appsettings, Formatting.Indented);
            var now = DateTime.Now.ToString("yyyyMMdd");
            string fileName = now + "_msbuildgui.json";

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = Properties.Resources.SaveTheFile;
            saveFileDialog.InitialDirectory = @"C:\";
            saveFileDialog.FileName = fileName;
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = saveFileDialog.FileName;
                System.IO.File.WriteAllText(filePath, jsonData, Encoding.UTF8);
                ModernWpf.MessageBox.Show(Properties.Resources.Mb_ExportComplete + $"\n{filePath}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            // jsonファイルを選択する
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = Properties.Resources.OpenFile;
            openFileDialog.InitialDirectory = @"C:\";
            openFileDialog.FileName = "msbuildgui.json";
            openFileDialog.Filter = "jsonFile(*.json)|*.json";
            bool? result = openFileDialog.ShowDialog();
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
                        // 入力したプロジェクト名重複している場合は登録できない(大文字小文字を区別しない)
                        if (MainWindow.Projects.ProjectsList.Any(x => x.Value.ProjectName.ToLower() == proj.ProjectName.ToLower()))
                        {
                            ModernWpf.MessageBox.Show(Properties.Resources.ProjectNamesThatAlreadyExistCannotBeImported + $"\n{proj.ProjectName}", Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    // jsonファイルからデータを取得
                    foreach (var proj in appsettings.Project)
                    {
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
                            VisualStudioVersion = proj.VisualStudioVersion
                        });
                    }
                    // 設定ファイルに保存
                    ((MainWindow)this.Owner).saveJson();

                    string importProj = string.Join(", ", projects);
                    ModernWpf.MessageBox.Show(Properties.Resources.ImportComplete + $"\n{importProj}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    // コンボボックスを更新
                    Window_SourceInitialized(sender, e);
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
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveButton_Click(sender, e);
                ModernWpf.MessageBox.Show(Properties.Resources.Mb_Saved, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Windows.Input.Keyboard.ClearFocus();
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
                string? VisualStudioVersion = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.VisualStudioVersion).FirstOrDefault();

                ProjFolderPath.Text = (SourceFolder == null ? "" : SourceFolder).ToString();
                OutputFolderPath.Text = (OutputFolder == null ? "" : OutputFolder).ToString();
                MsBuildPath.Text = (MsBuild == null ? "" : MsBuild).ToString();
                TargetCombo.Text = (Target == null ? "" : Target).ToString();
                ConfigurationCombo.Text = (Configuration == null ? "" : Configuration).ToString();
                VisualStudioVersionText.Text = (VisualStudioVersion == null ? "" : VisualStudioVersion).ToString();

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
                ModernWpf.MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
