using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace msbuild_gui
{
    /// <summary>
    /// GitHub OAuth Device Flow による認証を行います。
    /// 事前に GitHub で OAuth App を登録し、Device Flow を有効にする必要があります。
    /// https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#device-flow
    /// </summary>
    internal static class GitHubDeviceAuth
    {
        // TODO: GitHub OAuth App を登録し、取得した Client ID に置き換えてください。
        // 登録先: https://github.com/settings/developers
        // 必要設定: "Enable Device Flow" にチェックを入れる
        // 必要スコープ: copilot (GitHub Copilot API アクセス用)
        private const string ClientId = "YOUR_GITHUB_OAUTH_APP_CLIENT_ID";

        // GitHub Copilot API にアクセスするために必要なスコープ
        private const string Scope = "copilot";

        private static readonly HttpClient _http = new HttpClient
        {
            DefaultRequestHeaders = { { "Accept", "application/json" } }
        };

        /// <summary>
        /// GitHub OAuth Device Flow でサインインし、アクセストークンを返します。
        /// </summary>
        /// <param name="onPrompt">ユーザーに表示するコード (userCode) と URL (verificationUri) を受け取るコールバック</param>
        /// <param name="cancellationToken">キャンセル用トークン</param>
        /// <returns>GitHub アクセストークン</returns>
        /// <param name="onPrompt">第1引数: user_code / 第2引数: verification_uri</param>
        public static async Task<string> SignInAsync(
            Action<string, string> onPrompt,
            CancellationToken cancellationToken = default)
        {
            // Step 1: device_code を取得
            var deviceCodeResponse = await RequestDeviceCodeAsync(cancellationToken);

            // Step 2: ユーザーにコードを表示
            onPrompt(deviceCodeResponse.UserCode, deviceCodeResponse.VerificationUri);

            // 検証 URL をブラウザで開く
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = deviceCodeResponse.VerificationUri,
                    UseShellExecute = true
                });
            }
            catch { /* ブラウザが開けなくても無視 */ }

            // Step 3: アクセストークンが発行されるまでポーリング
            int intervalSec = deviceCodeResponse.Interval > 0 ? deviceCodeResponse.Interval : 5;
            return await PollForTokenAsync(deviceCodeResponse.DeviceCode, intervalSec, cancellationToken);
        }

        private static async Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"]     = Scope
            });

            var response = await _http.PostAsync(
                "https://github.com/login/device/code", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>(cancellationToken: ct);
            if (result == null)
                throw new InvalidOperationException("GitHub から無効なレスポンスが返されました。");
            return result;
        }

        private static async Task<string> PollForTokenAsync(
            string deviceCode, int intervalSec, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSec), ct);

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]   = ClientId,
                    ["device_code"] = deviceCode,
                    ["grant_type"]  = "urn:ietf:params:oauth:grant-type:device_code"
                });

                var response = await _http.PostAsync(
                    "https://github.com/login/oauth/access_token", content, ct);

                if (!response.IsSuccessStatusCode) continue;

                var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
                if (result == null) continue;

                if (!string.IsNullOrEmpty(result.AccessToken))
                    return result.AccessToken;

                // ポーリング中のエラーを処理
                switch (result.Error)
                {
                    case "authorization_pending":
                        // まだ承認待ち — ポーリング継続
                        break;
                    case "slow_down":
                        // レート制限 — interval を増やす
                        intervalSec += 5;
                        break;
                    case "expired_token":
                        throw new InvalidOperationException("デバイスコードが期限切れです。もう一度サインインしてください。");
                    case "access_denied":
                        throw new InvalidOperationException("アクセスが拒否されました。");
                    default:
                        if (!string.IsNullOrEmpty(result.Error))
                            throw new InvalidOperationException($"認証エラー: {result.Error}");
                        break;
                }
            }

            throw new OperationCanceledException(ct);
        }

        // ---- JSON モデル ----

        private class DeviceCodeResponse
        {
            [JsonPropertyName("device_code")]
            public string DeviceCode { get; set; } = "";

            [JsonPropertyName("user_code")]
            public string UserCode { get; set; } = "";

            [JsonPropertyName("verification_uri")]
            public string VerificationUri { get; set; } = "";

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("interval")]
            public int Interval { get; set; }
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("scope")]
            public string? Scope { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("error_description")]
            public string? ErrorDescription { get; set; }
        }
    }
}
