using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace msbuild_gui
{
    /// <summary>
    /// SingleBuild.xaml の相互作用ロジック
    /// </summary>
    public partial class SingleBuild : Window
    {
        public static string? targetProj { get; set; }
        public SingleBuild(string args)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            try
            {
                targetProj = args;
                MainWindow.Projects.ProjectsList.ToList().ForEach(x => ProjCombo.Items.Add(x.Value.ProjectName));
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                string? projectName = ProjCombo.SelectedItem as string;
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
                string? Configuration = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.Configuration).FirstOrDefault();
                string? VisualStudioVersion = MainWindow.Projects.ProjectsList
                        .Where(x => x.Value.ProjectName == projectName).Select(x => x.Value.VisualStudioVersion).FirstOrDefault();

                string targetFileName = targetProj.Replace(SourceFolder, "");
                targetFileName.Remove(1);
                List<string?> target = new List<string?>();
                target.Add(targetFileName);

                ((MainWindow)this.Owner).RunBuild(target
                                            , SourceFolder
                                            , OutputFolder
                                            , MsBuild
                                            , Target
                                            , AssemblySearchPaths
                                            , Configuration
                                            , VisualStudioVersion
                                            , false
                                            , Environment.ProcessorCount
                                            , true
                                            , CancellationToken.None
                                            );
                Application.Current.Shutdown();
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
            try
            {
                if (File.Exists(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt"))
                {
                    File.Delete(Directory.GetCurrentDirectory() + "\\BuildErrorLog.txt");
                }
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Application.Current.Shutdown();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}
