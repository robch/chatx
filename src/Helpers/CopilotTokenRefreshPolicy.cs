using System;
using System.ClientModel.Primitives;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Policy that automatically refreshes a GitHub Copilot token before it expires.
/// </summary>
public class CopilotTokenRefreshPolicy : PipelinePolicy
{

    /// <summary>
    /// Creates a new instance of the CopilotTokenRefreshPolicy.
    /// </summary>
    /// <param name="initialToken">The initial Copilot token.</param>
    /// <param name="expiresAt">Unix timestamp when the token expires.</param>
    /// <param name="githubToken">The GitHub token used to refresh the Copilot token.</param>
    /// <param name="tokenRefreshCallback">Callback function to refresh the token.</param>
    public CopilotTokenRefreshPolicy(string initialToken, int expiresAt, string githubToken, Func<string> tokenRefreshCallback)
    {
        _copilotToken = initialToken;
        _tokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAt);
        _githubToken = githubToken;
        _tokenRefreshCallback = tokenRefreshCallback;
    }

    /// <summary>
    /// Process the HTTP request and refresh the token if needed.
    /// </summary>
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        // Check if token needs refreshing
        if (ShouldRefreshToken())
        {
            RefreshTokenIfNeeded();
        }

        // Update the Authorization header with the current token
        UpdateAuthorizationHeader(message);

        // Continue with the pipeline
        ProcessNext(message, pipeline, currentIndex);
    }

    /// <summary>
    /// Process the HTTP request asynchronously and refresh the token if needed.
    /// </summary>
    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        // Check if token needs refreshing
        if (ShouldRefreshToken())
        {
            RefreshTokenIfNeeded();
        }

        // Update the Authorization header with the current token
        UpdateAuthorizationHeader(message);

        // Continue with the pipeline
        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    /// <summary>
    /// Determines if the token needs refreshing based on its expiration time.
    /// </summary>
    private bool ShouldRefreshToken()
    {
        var now = DateTimeOffset.UtcNow;
        return now.Add(_refreshBuffer) >= _tokenExpiresAt;
    }

    /// <summary>
    /// Refreshes the token if needed.
    /// </summary>
    private void RefreshTokenIfNeeded()
    {
        var locked = _refreshLock.Wait(0);
        if (!locked) return; // Another thread is already refreshing the token

        try
        {
            // Double check after acquiring the lock
            if (ShouldRefreshToken())
            {
                ConsoleHelpers.WriteDebugLine("Refreshing GitHub Copilot token...");
                try
                {
                    // Get a new token
                    var newToken = _tokenRefreshCallback();
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        // Update the token
                        _copilotToken = newToken;
                        // Note: The tokenRefreshCallback is responsible for updating the expiration time via UpdateTokenExpiration
                        ConsoleHelpers.WriteDebugLine("GitHub Copilot token refreshed successfully");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleHelpers.WriteDebugLine($"Error refreshing GitHub Copilot token: {ex.Message}");
                }
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Updates the Authorization header in the request with the current token.
    /// </summary>
    private void UpdateAuthorizationHeader(PipelineMessage message)
    {
        // Find and update the Authorization header
        var hasAuthHeader = message.Request.Headers.TryGetValue("Authorization", out var existingAuth);
        
        // Replace only if it's a Bearer token
        if (hasAuthHeader && existingAuth != null && existingAuth.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            message.Request.Headers.Remove("Authorization");
        }
        
        // Set the updated token
        message.Request.Headers.Set("Authorization", $"Bearer {_copilotToken}");
    }

    /// <summary>
    /// Updates the token expiration time.
    /// </summary>
    public void UpdateTokenExpiration(int expiresAt)
    {
        _tokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAt);
    }

    /// <summary>
    /// Updates the token and its expiration time.
    /// </summary>
    public void UpdateToken(string token, int expiresAt)
    {
        _copilotToken = token;
        _tokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAt);
    }
    
    /// <summary>
    /// Creates a CopilotTokenRefreshPolicy with automatic token refresh capability.
    /// </summary>
    /// <param name="initialToken">The initial Copilot token.</param>
    /// <param name="expiresAt">Unix timestamp when the token expires.</param>
    /// <param name="githubToken">The GitHub token used to refresh the Copilot token.</param>
    /// <param name="helper">GitHubCopilotHelper instance used for token refreshing.</param>
    /// <returns>A configured CopilotTokenRefreshPolicy instance.</returns>
    public static CopilotTokenRefreshPolicy CreateWithAutoRefresh(
        string initialToken, 
        int expiresAt, 
        string githubToken, 
        GitHubCopilotHelper helper)
    {
        CopilotTokenRefreshPolicy? policy = null;
        policy = new CopilotTokenRefreshPolicy(
            initialToken,
            expiresAt,
            githubToken,
            () => {
                // Get a new token with expiration details when refreshing
                var refreshedDetails = helper.RefreshCopilotTokenWithDetails(githubToken);
                if (refreshedDetails.expires_at.HasValue && policy != null) {
                    policy.UpdateTokenExpiration(refreshedDetails.expires_at.Value);
                }
                return refreshedDetails.token!;
            });
        return policy;
    }
    
    private readonly string _githubToken;
    private readonly Func<string> _tokenRefreshCallback;
    private string _copilotToken;
    private DateTimeOffset _tokenExpiresAt;
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
    private readonly TimeSpan _refreshBuffer = TimeSpan.FromMinutes(5); // Refresh 5 minutes before expiration
}