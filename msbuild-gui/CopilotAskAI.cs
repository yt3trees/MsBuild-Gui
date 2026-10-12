using GitHub.Copilot;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msbuild_gui
{
    /// <summary>
    /// GitHub Copilot SDK を使ったエージェント型エラー解析クラス。
    /// セッションを維持してマルチターン会話とツール呼び出しに対応します。
    /// ランタイム (Copilot CLI) はビルド時に NuGet パッケージが自動ダウンロードして
    /// 出力ディレクトリ (runtimes\win-x64\native\copilot.exe) に配置します。
    /// </summary>
    internal sealed class CopilotAskAI : IAsyncDisposable
    {
        private CopilotClient? _client;

        // セッションを IAsyncDisposable で保持 (DisposeAsync 呼び出し用)
        private IAsyncDisposable? _sessionDisposable;

        // SendAsync を型なしで呼び出すためのデリゲート (型パラメータの公開を避けるため)
        private Func<string, Task>? _sendFunc;

        // 各 SendAsync の完了を待つための TCS
        private TaskCompletionSource? _idleTcs;

        // ストリーミング中のチャンク配送コールバック
        private Action<string>? _currentOnChunk;

        // 最終レスポンス全文の組み立てバッファ
        private readonly StringBuilder _responseBuffer = new StringBuilder();

        // ソースフォルダ (ツール呼び出し時のサンドボックス境界)
        private string? _sourceFolder;

        /// <summary>
        /// Copilot クライアントとセッションを初期化します。
        /// </summary>
        /// <param name="gitHubToken">GitHub アクセストークン (null の場合はログイン済みユーザーを使用)</param>
        /// <param name="model">使用するモデル ("gpt-5", "claude-sonnet-4.5" など)</param>
        /// <param name="sourceFolder">ツールでアクセスを許可するソースフォルダのルートパス</param>
        public async Task InitializeAsync(string? gitHubToken, string model, string? sourceFolder)
        {
            _sourceFolder = sourceFolder;

            // クライアント生成
            var clientOptions = new CopilotClientOptions();
            if (!string.IsNullOrWhiteSpace(gitHubToken))
            {
                clientOptions.GitHubToken = gitHubToken;
            }
            // else: UseLoggedInUser のデフォルト (true) を利用

            _client = new CopilotClient(clientOptions);
            await _client.StartAsync();

            // 言語設定
            string lang = MainWindow.Projects.Language == "ja-JP" ? "Japanese" : "English";

            // ---- ツール定義 (SourceFolder 内のファイルに限定した読み取り専用操作) ----
            var tools = new List<AIFunctionDeclaration>();

            if (!string.IsNullOrEmpty(sourceFolder) && Directory.Exists(sourceFolder))
            {
                // ファイル読み取りツール (SkipPermission で毎回の確認プロンプトを省略)
                tools.Add(CopilotTool.DefineTool(
                    ([Description("SourceFolder からの相対パス (例: \\MyProject\\MyProject.csproj)")] string relativePath) =>
                    {
                        string fullPath = Path.GetFullPath(
                            Path.Combine(sourceFolder, relativePath.TrimStart('\\', '/')));

                        // SourceFolder 外へのパストラバーサルを防ぐ
                        if (!fullPath.StartsWith(sourceFolder, StringComparison.OrdinalIgnoreCase))
                            return "アクセス拒否: SourceFolder 外のパスは読み取れません。";

                        if (!File.Exists(fullPath))
                            return $"ファイルが見つかりません: {relativePath}";

                        // 対象拡張子を .cs / .csproj / .sln / .props / .targets に限定
                        string ext = Path.GetExtension(fullPath).ToLowerInvariant();
                        if (ext != ".cs" && ext != ".csproj" && ext != ".sln"
                            && ext != ".props" && ext != ".targets")
                            return $"対応していないファイル形式です: {ext}";

                        try
                        {
                            return File.ReadAllText(fullPath, Encoding.UTF8);
                        }
                        catch (Exception ex)
                        {
                            return $"読み取りエラー: {ex.Message}";
                        }
                    },
                    toolOptions: new CopilotToolOptions { SkipPermission = true },
                    factoryOptions: new AIFunctionFactoryOptions
                    {
                        Name = "read_file",
                        Description = "SourceFolder 内の .cs / .csproj / .sln / .props / .targets ファイルを読み取ります。" +
                                      "ビルドエラーの原因調査に使用してください。"
                    }));

                // ファイル一覧ツール
                tools.Add(CopilotTool.DefineTool(
                    ([Description("検索パターン (例: *.csproj, *.cs)。省略時はすべての .csproj を列挙します。")] string pattern = "*.csproj") =>
                    {
                        try
                        {
                            var files = Directory.GetFiles(sourceFolder, pattern, SearchOption.AllDirectories)
                                .Select(f => f.Replace(sourceFolder, "").TrimStart('\\', '/'))
                                .Take(100)
                                .ToList();

                            return files.Count > 0
                                ? string.Join("\n", files)
                                : "一致するファイルが見つかりませんでした。";
                        }
                        catch (Exception ex)
                        {
                            return $"ファイル列挙エラー: {ex.Message}";
                        }
                    },
                    toolOptions: new CopilotToolOptions { SkipPermission = true },
                    factoryOptions: new AIFunctionFactoryOptions
                    {
                        Name = "list_files",
                        Description = "SourceFolder 内のファイルを列挙します。プロジェクト構造の確認に使用してください。"
                    }));
            }

            // ---- セッション作成 ----
            string systemPrompt =
                $"You are an expert MSBuild and C# build engineer. " +
                $"When the user reports a build error, analyze it thoroughly. " +
                $"Use the read_file tool to examine relevant .cs and .csproj files, " +
                $"and use list_files to discover the project structure when needed. " +
                $"Provide concrete, actionable fixes with code examples or .csproj diff snippets. " +
                $"Respond in {lang}.";

            var session = await _client.CreateSessionAsync(new SessionConfig
            {
                Model = model,
                Streaming = true,
                SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Append,
                    Content = systemPrompt
                },
                Tools = tools.Count > 0 ? tools.ToArray() : null,
                OnPermissionRequest = PermissionHandler.ApproveAll
            });

            // IAsyncDisposable として保持
            _sessionDisposable = session;

            // SendAsync デリゲートをクロージャで保持 (型なし参照を避けるため)
            _sendFunc = async (prompt) =>
                await session.SendAsync(new MessageOptions { Prompt = prompt });

            // ストリーミングイベントを購読
            session.On<SessionEvent>(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent delta:
                        string? chunk = delta.Data?.DeltaContent;
                        if (chunk != null)
                        {
                            _responseBuffer.Append(chunk);
                            _currentOnChunk?.Invoke(chunk);
                        }
                        break;

                    case SessionIdleEvent:
                        // SendAsync の完了シグナル
                        _idleTcs?.TrySetResult();
                        break;
                }
            });
        }

        /// <summary>
        /// ビルドエラーを解析します (初回呼び出し)。
        /// </summary>
        /// <param name="errorMessage">エラーログ全文</param>
        /// <param name="onChunk">ストリーミング中に各チャンクを受け取るコールバック</param>
        /// <returns>AI の応答全文</returns>
        public async Task<string> AnalyzeAsync(string errorMessage, Action<string>? onChunk = null)
        {
            EnsureInitialized();

            _responseBuffer.Clear();
            _currentOnChunk = onChunk;
            _idleTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // プロンプトテンプレートを読み込んでエラーメッセージを埋め込む
            string prompt;
            string templatePath = @"Plugins\SemanticPlugins\ErrorAnalysis\skprompt.txt";
            if (File.Exists(templatePath))
            {
                string template = File.ReadAllText(templatePath);
                prompt = template
                    .Replace("{{$errorMessage}}", errorMessage)
                    .Replace("{{$language}}", MainWindow.Projects.Language);
            }
            else
            {
                prompt = $"Please analyze the following MSBuild/C# build error and suggest a fix:\n\n{errorMessage}";
            }

            await _sendFunc!(prompt);
            await _idleTcs.Task;

            return _responseBuffer.ToString();
        }

        /// <summary>
        /// フォローアップ質問を送信します (同一セッションで文脈を継続)。
        /// </summary>
        /// <param name="followUpQuestion">追加質問</param>
        /// <param name="onChunk">ストリーミング中に各チャンクを受け取るコールバック</param>
        /// <returns>AI の応答全文</returns>
        public async Task<string> FollowUpAsync(string followUpQuestion, Action<string>? onChunk = null)
        {
            EnsureInitialized();

            _responseBuffer.Clear();
            _currentOnChunk = onChunk;
            _idleTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await _sendFunc!(followUpQuestion);
            await _idleTcs.Task;

            return _responseBuffer.ToString();
        }

        private void EnsureInitialized()
        {
            if (_sendFunc == null)
                throw new InvalidOperationException(
                    "InitializeAsync を先に呼び出してください。");
        }

        public async ValueTask DisposeAsync()
        {
            if (_sessionDisposable != null)
            {
                await _sessionDisposable.DisposeAsync();
                _sessionDisposable = null;
            }

            if (_client != null)
            {
                await _client.DisposeAsync();
                _client = null;
            }
        }
    }
}
