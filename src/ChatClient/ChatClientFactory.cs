using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;

public static class ChatClientFactory
{
    public static IChatClient CreateAzureOpenAIChatClientWithApiKey()
    {
        var deployment = EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? throw new EnvVarSettingException("AZURE_OPENAI_CHAT_DEPLOYMENT is not set.");
        var endpoint = EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_ENDPOINT") ?? throw new EnvVarSettingException("AZURE_OPENAI_ENDPOINT is not set.");
        var apiKey = EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_API_KEY") ?? throw new EnvVarSettingException("AZURE_OPENAI_API_KEY is not set.");

        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey), InitAzureOpenAIClientOptions());
        var chatClient = client.GetChatClient(deployment);

        ConsoleHelpers.WriteDebugLine("Using Azure OpenAI API key for authentication");
        return chatClient.AsChatClient();
    }

    public static IChatClient CreateOpenAIChatClientWithApiKey()
    {
        var model = EnvironmentHelpers.FindEnvVar("OPENAI_CHAT_MODEL_NAME") ?? "gpt-4o";
        var apiKey = EnvironmentHelpers.FindEnvVar("OPENAI_API_KEY") ?? throw new EnvVarSettingException("OPENAI_API_KEY is not set.");

        var chatClient = new ChatClient(model, new ApiKeyCredential(apiKey), InitOpenAIClientOptions());
        
        ConsoleHelpers.WriteDebugLine("Using OpenAI API key for authentication");
        return chatClient.AsChatClient();
    }

    public static IChatClient CreateCopilotChatClientWithGitHubToken()
    {
        var model = EnvironmentHelpers.FindEnvVar("COPILOT_MODEL_NAME") ?? "claude-3.7-sonnet";
        var endpoint = EnvironmentHelpers.FindEnvVar("COPILOT_API_ENDPOINT") ?? "https://api.githubcopilot.com";
        var githubToken = EnvironmentHelpers.FindEnvVar("GITHUB_TOKEN") ?? throw new EnvVarSettingException("GITHUB_TOKEN is not set. Run 'chatx github login' to authenticate with GitHub Copilot.");
        var integrationId = EnvironmentHelpers.FindEnvVar("COPILOT_INTEGRATION_ID") ?? string.Empty;
        var editorVersion = EnvironmentHelpers.FindEnvVar("COPILOT_EDITOR_VERSION") ?? "vscode/1.80.1";

        // Get the Copilot token using the GitHub token
        var helper = new GitHubCopilotHelper();
        var tokenDetails = helper.GetCopilotTokenDetailsSync(githubToken);
        
        if (string.IsNullOrEmpty(tokenDetails.token))
        {
            throw new EnvVarSettingException("Failed to get a valid Copilot token from GitHub. Please run 'chatx github login' to authenticate.");
        }

        // Create options with the initial auth header
        var options = InitOpenAIClientOptions(endpoint, $"Bearer {tokenDetails.token}");
        
        // Create the refresh policy with automatic token refresh capability
        var refreshPolicy = CopilotTokenRefreshPolicy.CreateWithAutoRefresh(
            tokenDetails.token!, 
            tokenDetails.expires_at!.Value, 
            githubToken,
            helper);
        
        // Add the refresh policy to the pipeline
        options.AddPolicy(refreshPolicy, PipelinePosition.BeforeTransport);

        var integrationIdOk = !string.IsNullOrEmpty(integrationId);
        if (integrationIdOk) options.AddPolicy(new CustomHeaderPolicy("Copilot-Integration-Id", integrationId!), PipelinePosition.BeforeTransport);

        var impersonateVsCodeEditor = !integrationIdOk;
        if (impersonateVsCodeEditor) options.AddPolicy(new CustomHeaderPolicy("Editor-Version", editorVersion), PipelinePosition.BeforeTransport);

        var chatClient = new ChatClient(model, new ApiKeyCredential(" "), options);
        
        ConsoleHelpers.WriteDebugLine("Using GitHub Copilot token for authentication (with auto-refresh)");
        return chatClient.AsChatClient();
    }

    public static IChatClient CreateCopilotChatClientWithHmacKey()
    {
        var model = EnvironmentHelpers.FindEnvVar("COPILOT_MODEL_NAME") ?? "claude-3.7-sonnet";
        var endpoint = EnvironmentHelpers.FindEnvVar("COPILOT_API_ENDPOINT") ?? "https://api.githubcopilot.com";
        var integrationId = EnvironmentHelpers.FindEnvVar("COPILOT_INTEGRATION_ID") ?? throw new EnvVarSettingException("COPILOT_INTEGRATION_ID is not set.");
        var hmacKey = EnvironmentHelpers.FindEnvVar("COPILOT_HMAC_KEY") ?? throw new EnvVarSettingException("COPILOT_HMAC_KEY is not set.");

        var options = InitOpenAIClientOptions(endpoint, "");
        options.AddPolicy(new CustomHeaderPolicy("Request-HMAC", HMACHelper.Encode(hmacKey)), PipelinePosition.BeforeTransport);
        options.AddPolicy(new CustomHeaderPolicy("Copilot-Integration-Id", integrationId), PipelinePosition.BeforeTransport);

        var chatClient = new ChatClient(model, new ApiKeyCredential(" "), options);
        
        ConsoleHelpers.WriteDebugLine("Using HMAC for Copilot authentication");
        return chatClient.AsChatClient();
    }

    private static IChatClient? TryCreateChatClientFromPreferredProvider()
    {
        // Check for explicit provider preference from configuration or environment variables
        var preferredProvider = ConfigStore.Instance.GetFromAnyScope(KnownSettings.AppPreferredProvider).AsString()?.ToLowerInvariant();
        
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            ConsoleHelpers.WriteDebugLine($"Using preferred provider: {preferredProvider}");
            
            // Try to create client based on preference
            if ((preferredProvider == "copilot-github" || preferredProvider == "copilot") && !string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("GITHUB_TOKEN")))
            {
                return CreateCopilotChatClientWithGitHubToken();
            }
            else if ((preferredProvider == "copilot-hmac" || preferredProvider == "copilot") && !string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("COPILOT_HMAC_KEY")))
            {
                return CreateCopilotChatClientWithHmacKey();
            }
            else if ((preferredProvider == "azure-openai" || preferredProvider == "azure") && !string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_API_KEY")))
            {
                return CreateAzureOpenAIChatClientWithApiKey();
            }
            else if (preferredProvider == "openai" && !string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("OPENAI_API_KEY")))
            {
                return CreateOpenAIChatClientWithApiKey();
            }
            
            // If preferred provider credentials aren't available, warn the user
            ConsoleHelpers.WriteWarning($"Preferred provider '{preferredProvider}' credentials not found. Falling back to default selection.");
            ConsoleHelpers.WriteLine(overrideQuiet: true);
        }
        
        return null;
    }
    
    private static IChatClient? TryCreateChatClientFromEnv()
    {
        ConsoleHelpers.WriteDebugLine("Creating chat client from environment variables...");

        if (!string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("GITHUB_TOKEN")))
        {
            return CreateCopilotChatClientWithGitHubToken();
        }
        
        if (!string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("COPILOT_HMAC_KEY")))
        {
            return CreateCopilotChatClientWithHmacKey();
        }

        if (!string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_API_KEY")))
        {
            return CreateAzureOpenAIChatClientWithApiKey();
        }

        if (!string.IsNullOrEmpty(EnvironmentHelpers.FindEnvVar("OPENAI_API_KEY")))
        {
            return CreateOpenAIChatClientWithApiKey();
        }
        
        return null;
    }

    public static IChatClient CreateChatClient()
    {
        // First try to create client from preferred provider
        var client = TryCreateChatClientFromPreferredProvider();
        
        // If that fails, try to create from environment variables
        client ??= TryCreateChatClientFromEnv();
        
        // If no client could be created, throw an exception with helpful message
        if (client == null)
        {
            var message =
                string.Join('\n',
                    @"No valid environment variables found.

                    To use OpenAI, please set:
                    - OPENAI_API_KEY
                    - OPENAI_CHAT_MODEL_NAME (optional)

                    To use Azure OpenAI, please set:
                    - AZURE_OPENAI_API_KEY
                    - AZURE_OPENAI_ENDPOINT
                    - AZURE_OPENAI_CHAT_DEPLOYMENT

                    To use GitHub Copilot with token authentication, please set:
                    - GITHUB_TOKEN
                    - COPILOT_API_ENDPOINT (optional)
                    - COPILOT_INTEGRATION_ID (optional)
                    - COPILOT_EDITOR_VERSION (optional)
                    - COPILOT_MODEL_NAME (optional)

                    To use GitHub Copilot with HMAC authentication, please set:
                    - COPILOT_HMAC_KEY
                    - COPILOT_INTEGRATION_ID
                    - COPILOT_API_ENDPOINT (optional)
                    - COPILOT_MODEL_NAME (optional)"
                .Split(new[] { '\n' })
                .Select(line => line.Trim()));

            throw new EnvVarSettingException(message);
        }
        
        return client;
    }

    private static AzureOpenAIClientOptions InitAzureOpenAIClientOptions()
    {
        var options = new AzureOpenAIClientOptions();
        InitPipelineOptionsPolicies(options);
        return options;
    }

    private static OpenAIClientOptions InitOpenAIClientOptions(string? endpoint = null, string? authHeader = null)
    {
        var options = new OpenAIClientOptions();
        InitPipelineOptionsPolicies(options);

        var endpointOk = !string.IsNullOrEmpty(endpoint);
        if (endpointOk) options.Endpoint = new Uri(endpoint!);

        var authHeaderOk = authHeader != null; // string.Empty Authorization header is required for Copilot API w/HMAC authentication
        if (authHeaderOk) options.AddPolicy(new CustomHeaderPolicy("Authorization", authHeader!), PipelinePosition.BeforeTransport);

        return options;
    }

    private static ClientPipelineOptions InitPipelineOptionsPolicies(ClientPipelineOptions options)
    {
        options.AddPolicy(new CustomJsonPropertyRemovalPolicy("tool_choice"), PipelinePosition.PerCall);
        options.AddPolicy(new FixNullFunctionArgsPolicy(), PipelinePosition.PerCall);
        options.AddPolicy(new LogTrafficEventPolicy(), PipelinePosition.PerCall);
        options.RetryPolicy = new ClientRetryPolicy(maxRetries: 10);
        
        // Apply timeout if configured
        var timeoutSetting = ConfigStore.Instance.GetFromAnyScope(KnownSettings.AppChatCompletionTimeout);
        if (timeoutSetting.AsInt() > 0)
        {
            int timeoutSeconds = timeoutSetting.AsInt();
            ConsoleHelpers.WriteDebugLine($"Setting chat completion timeout to {timeoutSeconds} seconds");
            options.NetworkTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        }
        
        return options;
    }
}
