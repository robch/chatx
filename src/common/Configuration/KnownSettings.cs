using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Defines known configuration settings with standardized formats and access rules.
/// </summary>
public static class KnownSettings
{
    #region Setting Keys (Dot Notation)

    // Anthropic settings
    public const string AnthropicApiKey = "Anthropic.ApiKey";
    public const string AnthropicModelName = "Anthropic.ModelName";

    // Azure OpenAI settings
    public const string AzureOpenAIApiKey = "Azure.OpenAI.ApiKey";
    public const string AzureOpenAIEndpoint = "Azure.OpenAI.Endpoint";
    public const string AzureOpenAIChatDeployment = "Azure.OpenAI.ChatDeployment";
    
    // OpenAI settings
    public const string OpenAIApiKey = "OpenAI.ApiKey";
    public const string OpenAIChatModelName = "OpenAI.ChatModelName";
    
    // GitHub settings
    public const string GitHubToken = "GitHub.Token";
    
    // Copilot settings
    public const string CopilotModelName = "Copilot.ModelName";
    public const string CopilotApiEndpoint = "Copilot.ApiEndpoint";
    public const string CopilotIntegrationId = "Copilot.IntegrationId";
    public const string CopilotEditorVersion = "Copilot.EditorVersion";

    // Application settings
    public const string AppMaxTokens = "App.MaxTokens";
    public const string AppPreferredProvider = "App.PreferredProvider";
    public const string AppAutoSaveChatHistory = "App.AutoSaveChatHistory";
    public const string AppAutoSaveTrajectory = "App.AutoSaveTrajectory";
    public const string AppChatCompletionTimeout = "App.ChatCompletionTimeout";
    
    #endregion
    
    #region Secret Settings
    
    /// <summary>
    /// Collection of known setting names that should be treated as secrets.
    /// </summary>
    private static readonly HashSet<string> _secretSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Anthropic secrets
        AnthropicApiKey,
        
        // Azure OpenAI secrets
        AzureOpenAIApiKey,
        
        // OpenAI secrets
        OpenAIApiKey,
        
        // GitHub secrets
        GitHubToken,
    };
    
    #endregion
    
    #region Setting Format Mappings
    
    /// <summary>
    /// Mapping from dot notation to environment variable format.
    /// </summary>
    private static readonly Dictionary<string, string> _dotToEnvVarMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Anthropic mappings
        { AnthropicApiKey, "ANTHROPIC_API_KEY" },
        { AnthropicModelName, "ANTHROPIC_MODEL_NAME" },

        // Azure OpenAI mappings
        { AzureOpenAIApiKey, "AZURE_OPENAI_API_KEY" },
        { AzureOpenAIEndpoint, "AZURE_OPENAI_ENDPOINT" },
        { AzureOpenAIChatDeployment, "AZURE_OPENAI_CHAT_DEPLOYMENT" },
        
        // OpenAI mappings
        { OpenAIApiKey, "OPENAI_API_KEY" },
        { OpenAIChatModelName, "OPENAI_CHAT_MODEL_NAME" },
        
        // GitHub mappings
        { GitHubToken, "GITHUB_TOKEN" },
        
        // Copilot mappings
        { CopilotModelName, "COPILOT_MODEL_NAME" },
        { CopilotApiEndpoint, "COPILOT_API_ENDPOINT" },
        { CopilotIntegrationId, "COPILOT_INTEGRATION_ID" },
        { CopilotEditorVersion, "COPILOT_EDITOR_VERSION" },
        
        // Application settings
        { AppMaxTokens, "APP_MAX_TOKENS" },
        { AppPreferredProvider, "CYCOD_PREFERRED_PROVIDER" },
        { AppAutoSaveChatHistory, "CYCOD_AUTO_SAVE_CHAT_HISTORY" },
        { AppAutoSaveTrajectory, "CYCOD_AUTO_SAVE_TRAJECTORY" },
        { AppChatCompletionTimeout, "CYCOD_CHAT_COMPLETION_TIMEOUT" }
    };
    
    /// <summary>
    /// Mapping from dot notation to CLI option format.
    /// </summary>
    private static readonly Dictionary<string, string> _dotToCLIOptionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Anthropic mappings
        { AnthropicApiKey, "--anthropic-api-key" },
        { AnthropicModelName, "--anthropic-model-name" },

        // Azure OpenAI mappings
        { AzureOpenAIApiKey, "--azure-openai-api-key" },
        { AzureOpenAIEndpoint, "--azure-openai-endpoint" },
        { AzureOpenAIChatDeployment, "--azure-openai-chat-deployment" },
        
        // OpenAI mappings
        { OpenAIApiKey, "--openai-api-key" },
        { OpenAIChatModelName, "--openai-chat-model-name" },
        
        // GitHub mappings
        { GitHubToken, "--github-token" },
        
        // Copilot mappings
        { CopilotModelName, "--copilot-model-name" },
        { CopilotApiEndpoint, "--copilot-api-endpoint" },
        { CopilotIntegrationId, "--copilot-integration-id" },
        { CopilotEditorVersion, "--copilot-editor-version" },
        
        // Application settings
        { AppMaxTokens, "--max-tokens" },
        { AppAutoSaveChatHistory, "--auto-save-chat-history" },
        { AppAutoSaveTrajectory, "--auto-save-trajectory" },
        { AppChatCompletionTimeout, "--chat-completion-timeout" }
    };
    
    /// <summary>
    /// Mapping from environment variable format to dot notation.
    /// </summary>
    private static readonly Dictionary<string, string> _envVarToDotMap;
    
    /// <summary>
    /// Mapping from CLI option format to dot notation.
    /// </summary>
    private static readonly Dictionary<string, string> _cliOptionToDotMap;
    
    #endregion
    
    #region Category Groupings

    /// <summary>
    /// Collection of settings for Anthropic integration.
    /// </summary>
    public static readonly HashSet<string> AnthropicSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        AnthropicApiKey,
        AnthropicModelName
    };
    
    /// <summary>
    /// Collection of settings for Azure OpenAI integration.
    /// </summary>
    public static readonly HashSet<string> AzureOpenAISettings = new(StringComparer.OrdinalIgnoreCase)
    {
        AzureOpenAIApiKey,
        AzureOpenAIEndpoint,
        AzureOpenAIChatDeployment
    };
    
    /// <summary>
    /// Collection of settings for OpenAI integration.
    /// </summary>
    public static readonly HashSet<string> OpenAISettings = new(StringComparer.OrdinalIgnoreCase)
    {
        OpenAIApiKey,
        OpenAIChatModelName
    };
    
    /// <summary>
    /// Collection of settings for GitHub integration.
    /// </summary>
    public static readonly HashSet<string> GitHubSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        GitHubToken
    };
    
    /// <summary>
    /// Collection of settings for Copilot integration.
    /// </summary>
    public static readonly HashSet<string> CopilotSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        CopilotModelName,
        CopilotApiEndpoint,
        CopilotIntegrationId,
        CopilotEditorVersion
    };
    
    /// <summary>
    /// Collection of settings for application configuration.
    /// </summary>
    public static readonly HashSet<string> AppSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        AppMaxTokens,
        AppPreferredProvider,
        AppAutoSaveChatHistory,
        AppAutoSaveTrajectory,
        AppChatCompletionTimeout
    };
    
    #endregion
    
    static KnownSettings()
    {
        // Initialize reverse mappings
        _envVarToDotMap = _dotToEnvVarMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);
        _cliOptionToDotMap = _dotToCLIOptionMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Checks if the given key is a known setting.
    /// </summary>
    /// <param name="key">The key to check (in any format).</param>
    /// <returns>True if the key is a known setting, false otherwise.</returns>
    public static bool IsKnown(string key)
    {
        if (_dotToEnvVarMap.ContainsKey(key)) return true;
        if (_envVarToDotMap.ContainsKey(key)) return true;
        if (_cliOptionToDotMap.ContainsKey(key)) return true;
        return false;
    }
    
    /// <summary>
    /// Checks if the given key is a secret value that should be obfuscated.
    /// </summary>
    /// <param name="key">The key to check (in any format).</param>
    /// <returns>True if the key is a secret, false otherwise.</returns>
    public static bool IsSecret(string key)
    {
        var dotNotationKey = ToDotNotation(key);
        return _secretSettings.Contains(dotNotationKey);
    }
    
    /// <summary>
    /// Gets all known setting names in dot notation format.
    /// </summary>
    /// <returns>Collection of all known setting names.</returns>
    public static IEnumerable<string> GetAllKnownSettings()
    {
        return _dotToEnvVarMap.Keys;
    }
    
    /// <summary>
    /// Converts a setting name to environment variable format.
    /// </summary>
    /// <param name="key">The setting name in any format.</param>
    /// <returns>The equivalent environment variable name.</returns>
    public static string ToEnvironmentVariable(string key)
    {
        if (IsEnvironmentVariableFormat(key)) return key;
        
        // Try to map to an environment variable
        var dotNotationKey = ToDotNotation(key);
        if (_dotToEnvVarMap.TryGetValue(dotNotationKey, out string? envVarKey))
        {
            return envVarKey;
        }

        // Otherwise, just return it
        return key;
    }
    
    /// <summary>
    /// Converts a setting name to CLI option format.
    /// </summary>
    /// <param name="key">The setting name in any format.</param>
    /// <returns>The equivalent CLI option.</returns>
    public static string ToCLIOption(string key)
    {
        if (IsCLIOptionFormat(key)) return key;
        
        // Try to map to a CLI option
        var dotNotationKey = ToDotNotation(key);
        if (_dotToCLIOptionMap.TryGetValue(dotNotationKey, out string? cliOption))
        {
            return cliOption;
        }
        
        // Otherwise, use a general algorithm
        var parts = dotNotationKey.Split('.');
        var kebabParts = parts.Select(p => ToKebabCase(p));
        return "--" + string.Join("-", kebabParts).ToLowerInvariant();
    }
    
    /// <summary>
    /// Converts a setting name to dot notation format.
    /// </summary>
    /// <param name="key">The setting name in any format.</param>
    /// <returns>The equivalent dot notation.</returns>
    public static string ToDotNotation(string key)
    {
        // If it's an environment variable format
        if (IsEnvironmentVariableFormat(key))
        {
            if (_envVarToDotMap.TryGetValue(key, out string? dotNotation))
            {
                return dotNotation;
            }
        }
        
        // If it's a CLI option format
        if (IsCLIOptionFormat(key))
        {
            if (_cliOptionToDotMap.TryGetValue(key, out string? dotNotation))
            {
                return dotNotation;
            }
            
            // Remove leading -- and convert kebab-case to PascalCase with dots
            var trimmed = key.TrimStart('-');
            var parts = trimmed.Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }
            
            return string.Join(".", parts);
        }

        return key;
    }
    
    /// <summary>
    /// Determines if the given key is in environment variable format.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is in environment variable format, false otherwise.</returns>
    public static bool IsEnvironmentVariableFormat(string key)
    {
        return Regex.IsMatch(key, "^[A-Z0-9_]+$");
    }
    
    /// <summary>
    /// Determines if the given key is in CLI option format.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is in CLI option format, false otherwise.</returns>
    public static bool IsCLIOptionFormat(string key)
    {
        return key.StartsWith("--");
    }
    
    /// <summary>
    /// Converts a string from PascalCase to kebab-case.
    /// </summary>
    /// <param name="input">The PascalCase string to convert.</param>
    /// <returns>The kebab-case version of the input string.</returns>
    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        // Insert a hyphen before each uppercase letter that follows a lowercase letter
        var result = Regex.Replace(input, "(?<!^)([A-Z])", "-$1");
        return result.ToLowerInvariant();
    }
}