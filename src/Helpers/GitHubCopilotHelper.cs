using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

public class GitHubCopilotHelper
{
    // Constants for GitHub API endpoints
    private const string DEVICE_CODE_URL = "https://github.com/login/device/code";
    private const string ACCESS_TOKEN_URL = "https://github.com/login/oauth/access_token";
    private const string COPILOT_TOKEN_URL = "https://api.github.com/copilot_internal/v2/token";
    private const string CLIENT_ID = "Iv1.b507a08c87ecfe98";
    
    // Default headers for GitHub API calls
    private static readonly Dictionary<string, string> DEFAULT_HEADERS = new Dictionary<string, string>
    {
        { "accept", "application/json" },
        { "editor-version", "Neovim/0.6.1" },
        { "editor-plugin-version", "copilot.vim/1.16.0" },
        { "content-type", "application/json" },
        { "user-agent", "GithubCopilot/1.155.0" }
    };

    private readonly HttpClient _httpClient;

    public GitHubCopilotHelper()
    {
        _httpClient = new HttpClient();
        foreach (var header in DEFAULT_HEADERS)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Initiates the GitHub device flow authentication and returns a GitHub token
    /// </summary>
    public async Task<string> GetGitHubTokenAsync()
    {
        // Step 1: Get device code
        var deviceCodeContent = new StringContent(
            JsonSerializer.Serialize(new { client_id = CLIENT_ID, scope = "read:user" }),
            Encoding.UTF8, 
            "application/json");
        
        var deviceCodeResponse = await _httpClient.PostAsync(DEVICE_CODE_URL, deviceCodeContent);

        ConsoleHelpers.WriteDebugLine($"Response status code: {deviceCodeResponse.StatusCode}");
        foreach (var header in deviceCodeResponse.Headers)
        {
            ConsoleHelpers.WriteDebugLine($"Header: {header.Key} = {string.Join(", ", header.Value)}");
        }

        deviceCodeResponse.EnsureSuccessStatusCode();
        var deviceCodeJson = await deviceCodeResponse.Content.ReadAsStringAsync();
        ConsoleHelpers.WriteDebugLine($"Device code response: {deviceCodeJson}");

        var deviceCodeData = JsonSerializer.Deserialize<DeviceCodeResponse>(deviceCodeJson);
        
        if (deviceCodeData == null || string.IsNullOrEmpty(deviceCodeData.device_code) || 
            string.IsNullOrEmpty(deviceCodeData.user_code) || string.IsNullOrEmpty(deviceCodeData.verification_uri))
        {
            throw new InvalidOperationException("Failed to get device code information from GitHub");
        }
        
        // Step 2: Display instructions to user
        ConsoleHelpers.WriteLine($"Please visit {deviceCodeData.verification_uri} and enter code {deviceCodeData.user_code} to authenticate.", 
            ConsoleColor.Yellow, 
            overrideQuiet: true);
        
        // Step 3: Poll for token
        int intervalSeconds = deviceCodeData.interval ?? 5;
        string? accessToken = null;
        
        while (accessToken == null)
        {
            await Task.Delay(intervalSeconds * 1000);
            
            var tokenContent = new StringContent(
                JsonSerializer.Serialize(new 
                { 
                    client_id = CLIENT_ID, 
                    device_code = deviceCodeData.device_code, 
                    grant_type = "urn:ietf:params:oauth:grant-type:device_code" 
                }),
                Encoding.UTF8, 
                "application/json");
            
            var tokenResponse = await _httpClient.PostAsync(ACCESS_TOKEN_URL, tokenContent);
            
            if (tokenResponse.IsSuccessStatusCode)
            {
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<AccessTokenResponse>(tokenJson);
                
                if (tokenData != null && !string.IsNullOrEmpty(tokenData.access_token))
                {
                    accessToken = tokenData.access_token;
                    break;
                }
            }
        }
        
        ConsoleHelpers.WriteLine("GitHub authentication successful!", ConsoleColor.Green, overrideQuiet: true);
        return accessToken!;
    }

    /// <summary>
    /// Gets a Copilot token using the GitHub token (async version)
    /// </summary>
    public async Task<CopilotTokenResponse> GetCopilotTokenDetailsAsync(string githubToken)
    {
        // Create new request message to control headers exactly
        var request = new HttpRequestMessage(HttpMethod.Get, COPILOT_TOKEN_URL);
        
        // Add the authorization header with the GitHub token
        request.Headers.Authorization = new AuthenticationHeaderValue("token", githubToken);
        
        // Add the other required headers
        request.Headers.Add("editor-version", "Neovim/0.6.1");
        request.Headers.Add("editor-plugin-version", "copilot.vim/1.16.0");
        request.Headers.Add("user-agent", "GithubCopilot/1.155.0");
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        ConsoleHelpers.WriteDebugLine($"Copilot token response: {responseJson}");
        foreach (var header in response.Headers)
        {
            ConsoleHelpers.WriteDebugLine($"Header: {header.Key} = {string.Join(", ", header.Value)}");
        }

        var tokenData = JsonSerializer.Deserialize<CopilotTokenResponse>(responseJson);
        
        if (tokenData == null || string.IsNullOrEmpty(tokenData.token))
        {
            throw new InvalidOperationException("Failed to get Copilot token");
        }
        
        return tokenData;
    }
    
    /// <summary>
    /// Gets a Copilot token using the GitHub token (async version) - returns just the token
    /// </summary>
    public async Task<string> GetCopilotTokenAsync(string githubToken)
    {
        var tokenData = await GetCopilotTokenDetailsAsync(githubToken);
        return tokenData.token!;
    }
    
    /// <summary>
    /// Gets a Copilot token using the GitHub token (sync version for use in factory methods)
    /// </summary>
    public string GetCopilotTokenSync(string githubToken)
    {
        try
        {
            return GetCopilotTokenAsync(githubToken).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Copilot token: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Gets a Copilot token details using the GitHub token (sync version)
    /// </summary>
    public CopilotTokenResponse GetCopilotTokenDetailsSync(string githubToken)
    {
        try
        {
            return GetCopilotTokenDetailsAsync(githubToken).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Copilot token details: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Refreshes the Copilot token using the GitHub token
    /// </summary>
    public CopilotTokenResponse RefreshCopilotTokenWithDetails(string githubToken)
    {
        ConsoleHelpers.WriteDebugLine("Refreshing Copilot token with expiration details...");
        return GetCopilotTokenDetailsSync(githubToken);
    }
    
    /// <summary>
    /// Refreshes the Copilot token using the GitHub token (returns just the token)
    /// </summary>
    public string RefreshCopilotToken(string githubToken)
    {
        var details = RefreshCopilotTokenWithDetails(githubToken);
        return details.token!;
    }

    /// <summary>
    /// Saves the GitHub token to the configuration
    /// </summary>
    public void SaveGitHubTokenToConfig(string token, ConfigFileScope scope = ConfigFileScope.Local, string? configFileName = null)
    {
        var configStore = ConfigStore.Instance;

        var saveInFile = scope == ConfigFileScope.FileName;
        if (saveInFile) configStore.Set("GitHub.Token", token, configFileName!);

        var saveInKnownScope = scope == ConfigFileScope.Global || scope == ConfigFileScope.User || scope == ConfigFileScope.Local;
        if (saveInKnownScope) configStore.Set("GitHub.Token", token, scope, true);

        var invalid = !saveInFile && !saveInKnownScope;
        if (invalid)
        {
            throw new ArgumentException($"Invalid scope: {scope}. Use FileName, Global, User, or Local.");
        }
        
        var scopeName = scope.ToString().ToLowerInvariant();
        ConsoleHelpers.WriteLine(
            scope == ConfigFileScope.FileName
                ? $"GitHub token saved to {scopeName} configuration file: {configFileName}"
                : $"GitHub token saved to {scopeName} configuration",
            ConsoleColor.Green,
            overrideQuiet: true);
    }

    // Response classes for JSON deserialization
    private class DeviceCodeResponse
    {
        public string? device_code { get; set; }
        public string? user_code { get; set; }
        public string? verification_uri { get; set; }
        public int? expires_in { get; set; }
        public int? interval { get; set; }
    }

    private class AccessTokenResponse
    {
        public string? access_token { get; set; }
        public string? token_type { get; set; }
        public string? scope { get; set; }
    }

    public class CopilotTokenResponse
    {
        public string? token { get; set; }
        public int? expires_at { get; set; }
    }
}