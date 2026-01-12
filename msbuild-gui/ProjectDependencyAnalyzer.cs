using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace msbuild_gui
{
    /// <summary>
    /// csprojファイルからプロジェクト依存関係を解析するクラス
    /// </summary>
    public class ProjectDependencyAnalyzer
    {
        /// <summary>
        /// csprojファイルから依存するプロジェクトのパスを抽出
        /// </summary>
        /// <param name="csprojPath">csprojファイルのフルパス</param>
        /// <returns>依存するプロジェクトのフルパスのリスト</returns>
        public static List<string> GetProjectReferences(string csprojPath)
        {
            var dependencies = new List<string>();

            try
            {
                if (!File.Exists(csprojPath))
                {
                    return dependencies;
                }

                var doc = XDocument.Load(csprojPath);
                var projectDir = Path.GetDirectoryName(csprojPath);

                // ProjectReference要素を取得
                var projectReferences = doc.Descendants()
                    .Where(e => e.Name.LocalName == "ProjectReference")
                    .Select(e => e.Attribute("Include")?.Value)
                    .Where(v => !string.IsNullOrEmpty(v));

                foreach (var reference in projectReferences)
                {
                    // 相対パスを絶対パスに変換
                    var fullPath = Path.GetFullPath(Path.Combine(projectDir!, reference!));
                    dependencies.Add(fullPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing {csprojPath}: {ex.Message}");
            }

            return dependencies;
        }

        /// <summary>
        /// 複数のプロジェクトの依存関係マップを作成
        /// </summary>
        /// <param name="projectPaths">プロジェクトファイルのフルパスのリスト</param>
        /// <returns>プロジェクトパスをキー、依存プロジェクトパスのリストを値とする辞書</returns>
        public static Dictionary<string, List<string>> BuildDependencyMap(List<string> projectPaths)
        {
            var dependencyMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var projectPath in projectPaths)
            {
                var dependencies = GetProjectReferences(projectPath);
                // 依存先が対象プロジェクト内に含まれるもののみフィルタリング
                var filteredDependencies = dependencies
                    .Where(dep => projectPaths.Any(p =>
                        string.Equals(p, dep, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                dependencyMap[projectPath] = filteredDependencies;
            }

            return dependencyMap;
        }
    }
}
