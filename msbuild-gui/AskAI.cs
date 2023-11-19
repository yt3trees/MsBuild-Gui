using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace msbuild_gui
{
    internal class AskAI
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/semantic-kernel/prompt-engineering/your-first-prompt
        /// </summary>
        public static async Task<string> ExecutePlugin(string errorMessage)
        {
            var kernel = CreateKernelWithAuthentication();

            IDictionary<string, ISKFunction> plugins = kernel.ImportSemanticFunctionsFromDirectory(@"Plugins", "SemanticPlugins");

            ContextVariables variables = new ContextVariables();
            variables.Set("errorMessage", errorMessage);
            variables.Set("language", MainWindow.Projects.Language);

            var context = await kernel.RunAsync(variables, plugins["ErrorAnalysis"]);
            return context.ToString();
        }
        private static IKernel CreateKernelWithAuthentication()
        {
            var builder = new KernelBuilder();

            switch (Properties.Settings.Default.Provider)
            {
                case "Azure":
                    return builder.WithAzureOpenAIChatCompletionService(
                       apiKey: Properties.Settings.Default.APIKey,
                       deploymentName: Properties.Settings.Default.AzDeploymentId,
                       endpoint: Properties.Settings.Default.AzBaseDomain
                       ).Build();
                case "OpenAI":
                    return builder.WithOpenAIChatCompletionService(
                        apiKey: Properties.Settings.Default.APIKey,
                        modelId: Properties.Settings.Default.Model
                       ).Build();
                default:
                    throw new InvalidOperationException("Unsupported provider");
            }
        }
    }
}
