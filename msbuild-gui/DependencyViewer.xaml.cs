using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace msbuild_gui
{
    /// <summary>
    /// Interaction logic for DependencyViewer.xaml
    /// </summary>
    public partial class DependencyViewer : Window
    {
        private string _allContent = "";

        public DependencyViewer(
            List<string> orderedTargets,
            List<List<string>> parallelGroups,
            Dictionary<string, HashSet<string>> dependencyInfo)
        {
            InitializeComponent();

            // Sequential Build Order
            var sequentialText = "Build order (dependencies first):\n\n";
            for (int i = 0; i < orderedTargets.Count; i++)
            {
                sequentialText += $"{i + 1,3}. {orderedTargets[i]}\n";
            }
            SequentialTextBox.Text = sequentialText;

            // Parallel Build Groups
            var parallelText = "Projects can be built in parallel within each group.\n";
            parallelText += "Groups are executed sequentially.\n\n";
            for (int i = 0; i < parallelGroups.Count; i++)
            {
                parallelText += $"Group {i + 1} ({parallelGroups[i].Count} project{(parallelGroups[i].Count > 1 ? "s" : "")}):\n";
                foreach (var project in parallelGroups[i])
                {
                    parallelText += $"  • {project}\n";
                }
                parallelText += "\n";
            }
            ParallelTextBox.Text = parallelText;

            // Dependencies
            var depsText = "Project dependencies:\n\n";
            var hasAnyDeps = false;
            foreach (var kvp in dependencyInfo.OrderBy(x => x.Key))
            {
                if (kvp.Value.Count > 0)
                {
                    hasAnyDeps = true;
                    depsText += $"{kvp.Key}\n";
                    depsText += $"  depends on:\n";
                    foreach (var dep in kvp.Value.OrderBy(x => x))
                    {
                        depsText += $"    → {dep}\n";
                    }
                    depsText += "\n";
                }
            }

            if (!hasAnyDeps)
            {
                depsText += "No dependencies found among selected projects.\n";
                depsText += "All projects can be built independently.\n";
            }

            DependenciesTextBox.Text = depsText;

            // Store all content for copy functionality
            _allContent = "=== Sequential Build Order ===\n\n" + sequentialText +
                         "\n=== Parallel Build Groups ===\n\n" + parallelText +
                         "\n=== Dependencies ===\n\n" + depsText;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_allContent);
                ModernWpf.MessageBox.Show(
                    "Dependency information copied to clipboard.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ModernWpf.MessageBox.Show(
                    $"Failed to copy to clipboard:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
