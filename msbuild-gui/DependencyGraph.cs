using System;
using System.Collections.Generic;
using System.Linq;

namespace msbuild_gui
{
    /// <summary>
    /// プロジェクト依存関係グラフを管理し、ビルド順序を計算するクラス
    /// </summary>
    public class DependencyGraph
    {
        /// <summary>
        /// プロジェクトパスと依存先リストのマップ
        /// </summary>
        private Dictionary<string, List<string>> _adjacencyList;

        public DependencyGraph()
        {
            _adjacencyList = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依存関係マップからグラフを構築
        /// </summary>
        public void BuildGraph(Dictionary<string, List<string>> dependencyMap)
        {
            _adjacencyList = new Dictionary<string, List<string>>(dependencyMap, StringComparer.OrdinalIgnoreCase);

            // すべてのプロジェクトをノードとして追加（依存先として参照されているが自身がキーに存在しない場合のため）
            var allProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in dependencyMap)
            {
                allProjects.Add(kvp.Key);
                foreach (var dep in kvp.Value)
                {
                    allProjects.Add(dep);
                }
            }

            foreach (var project in allProjects)
            {
                if (!_adjacencyList.ContainsKey(project))
                {
                    _adjacencyList[project] = new List<string>();
                }
            }
        }

        /// <summary>
        /// トポロジカルソートを使用してビルド順序を計算
        /// </summary>
        /// <param name="projects">ビルド対象のプロジェクトパス</param>
        /// <returns>ビルド順序でソートされたプロジェクトパスのリスト</returns>
        /// <exception cref="InvalidOperationException">循環依存が検出された場合</exception>
        public List<string> GetBuildOrder(List<string> projects)
        {
            var buildOrder = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 対象プロジェクトのみを処理
            var projectSet = new HashSet<string>(projects, StringComparer.OrdinalIgnoreCase);

            foreach (var project in projects)
            {
                if (!visited.Contains(project))
                {
                    TopologicalSortVisit(project, visited, visiting, buildOrder, projectSet);
                }
            }

            return buildOrder;
        }

        /// <summary>
        /// 深さ優先探索でトポロジカルソート
        /// </summary>
        private void TopologicalSortVisit(
            string project,
            HashSet<string> visited,
            HashSet<string> visiting,
            List<string> buildOrder,
            HashSet<string> projectSet)
        {
            if (visiting.Contains(project))
            {
                throw new InvalidOperationException($"Circular dependency detected involving project: {project}");
            }

            if (visited.Contains(project))
            {
                return;
            }

            visiting.Add(project);

            // 依存先を先に訪問（対象プロジェクト内の依存のみ）
            if (_adjacencyList.ContainsKey(project))
            {
                foreach (var dependency in _adjacencyList[project])
                {
                    // 対象プロジェクトに含まれる依存のみ処理
                    if (projectSet.Contains(dependency))
                    {
                        TopologicalSortVisit(dependency, visited, visiting, buildOrder, projectSet);
                    }
                }
            }

            visiting.Remove(project);
            visited.Add(project);
            buildOrder.Add(project);
        }

        /// <summary>
        /// 並列ビルド可能なプロジェクトグループを計算
        /// </summary>
        /// <param name="projects">ビルド対象のプロジェクトパス</param>
        /// <returns>並列実行可能なプロジェクトのグループのリスト（各グループは同時実行可能）</returns>
        public List<List<string>> GetParallelBuildGroups(List<string> projects)
        {
            var buildGroups = new List<List<string>>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var projectSet = new HashSet<string>(projects, StringComparer.OrdinalIgnoreCase);
            var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 各プロジェクトの入次数を計算（依存されている数）
            foreach (var project in projects)
            {
                inDegree[project] = 0;
            }

            foreach (var project in projects)
            {
                if (_adjacencyList.ContainsKey(project))
                {
                    foreach (var dependency in _adjacencyList[project])
                    {
                        if (projectSet.Contains(dependency))
                        {
                            if (!inDegree.ContainsKey(project))
                            {
                                inDegree[project] = 0;
                            }
                            inDegree[project]++;
                        }
                    }
                }
            }

            // Kahn's アルゴリズムで並列グループを計算
            var queue = new Queue<string>();

            // 入次数0のプロジェクトをキューに追加
            foreach (var project in projects)
            {
                if (inDegree[project] == 0)
                {
                    queue.Enqueue(project);
                }
            }

            while (queue.Count > 0)
            {
                // 現在のキュー内のすべてのプロジェクトは並列実行可能
                var currentGroup = new List<string>();
                int groupSize = queue.Count;

                for (int i = 0; i < groupSize; i++)
                {
                    var project = queue.Dequeue();
                    currentGroup.Add(project);
                    visited.Add(project);

                    // このプロジェクトに依存するプロジェクトの入次数を減らす
                    foreach (var otherProject in projects)
                    {
                        if (_adjacencyList.ContainsKey(otherProject) &&
                            _adjacencyList[otherProject].Any(dep =>
                                string.Equals(dep, project, StringComparison.OrdinalIgnoreCase)))
                        {
                            inDegree[otherProject]--;
                            if (inDegree[otherProject] == 0 && !visited.Contains(otherProject))
                            {
                                queue.Enqueue(otherProject);
                            }
                        }
                    }
                }

                if (currentGroup.Count > 0)
                {
                    buildGroups.Add(currentGroup);
                }
            }

            // すべてのプロジェクトが処理されたか確認
            if (visited.Count != projects.Count)
            {
                throw new InvalidOperationException("Circular dependency detected in project references.");
            }

            return buildGroups;
        }

        /// <summary>
        /// 特定のプロジェクトが依存するすべてのプロジェクトを取得
        /// </summary>
        public HashSet<string> GetAllDependencies(string project)
        {
            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            GetAllDependenciesRecursive(project, dependencies);
            dependencies.Remove(project); // 自身は含めない
            return dependencies;
        }

        private void GetAllDependenciesRecursive(string project, HashSet<string> dependencies)
        {
            if (dependencies.Contains(project))
            {
                return;
            }

            dependencies.Add(project);

            if (_adjacencyList.ContainsKey(project))
            {
                foreach (var dependency in _adjacencyList[project])
                {
                    GetAllDependenciesRecursive(dependency, dependencies);
                }
            }
        }
    }
}
