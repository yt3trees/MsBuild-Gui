using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace msbuild_gui
{
    internal class AskAI
    {
        /// <summary>
        /// Migrated from Semantic Kernel to Microsoft Agent Framework / Microsoft.Extensions.AI
        /// https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel/
        /// </summary>
        public static async Task<string> ExecutePlugin(string errorMessage)
        {
            var chatClient = CreateChatClientWithAuthentication();

            // Load prompt template
            string promptTemplate = File.ReadAllText(@"Plugins\SemanticPlugins\ErrorAnalysis\skprompt.txt");

            // Replace template variables
            string prompt = promptTemplate
                .Replace("{{$errorMessage}}", errorMessage)
                .Replace("{{$language}}", MainWindow.Projects.Language);

            // Create chat messages with system instructions and user prompt
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "You are an expert at analyzing C# error messages."),
                new ChatMessage(ChatRole.User, prompt)
            };

            // Check if model/deployment contains "gpt-5"
            string modelOrDeployment = Properties.Settings.Default.Provider == "Azure"
                ? Properties.Settings.Default.AzDeploymentId
                : Properties.Settings.Default.Model;

            bool isGpt5 = modelOrDeployment?.Contains("gpt-5", StringComparison.OrdinalIgnoreCase) ?? false;

            // Execute with chat options (skip Temperature and TopP for GPT-5 models)
            var chatOptions = new ChatOptions();
            if (!isGpt5)
            {
                chatOptions.Temperature = 0.0f;
                chatOptions.TopP = 0.0f;
            }

            var response = await chatClient.CompleteAsync(messages, chatOptions);

            return response.Message.Text ?? string.Empty;
        }

        private static IChatClient CreateChatClientWithAuthentication()
        {
            switch (Properties.Settings.Default.Provider)
            {
                case "Azure":
                    return new AzureOpenAIClient(
                        new Uri(Properties.Settings.Default.AzBaseDomain),
                        new AzureKeyCredential(Properties.Settings.Default.APIKey))
                        .AsChatClient(modelId: Properties.Settings.Default.AzDeploymentId);

                case "OpenAI":
                    return new global::OpenAI.OpenAIClient(Properties.Settings.Default.APIKey)
                        .AsChatClient(modelId: Properties.Settings.Default.Model);

                default:
                    throw new InvalidOperationException("Unsupported provider");
            }
        }
    }
}
