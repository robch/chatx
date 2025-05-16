using System;

/// <summary>
/// Builder for configuring and creating PersistentShellProcess instances.
/// </summary>
public class PersistentShellProcessBuilder
{
    /// <summary>
    /// Creates a new PersistentShellProcessBuilder.
    /// </summary>
    public PersistentShellProcessBuilder()
    {
    }
    
    /// <summary>
    /// Sets the shell type to use.
    /// </summary>
    /// <param name="shellType">The type of shell.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithShellType(PersistentShellType shellType)
    {
        _shellType = shellType;
        return this;
    }
    
    /// <summary>
    /// Sets the working directory for the shell.
    /// </summary>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithWorkingDirectory(string? workingDirectory)
    {
        var ok = !string.IsNullOrEmpty(workingDirectory);
        if (ok) _processBuilder.WithWorkingDirectory(workingDirectory);
        return this;
    }
    
    /// <summary>
    /// Sets an environment variable for the shell.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithEnvironmentVariable(string name, string value)
    {
        _processBuilder.WithEnvironmentVariable(name, value);
        return this;
    }
    
    /// <summary>
    /// Sets environment variables for the shell.
    /// </summary>
    /// <param name="variables">Dictionary of environment variables.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithEnvironmentVariables(Dictionary<string, string>? variables)
    {
        _processBuilder.WithEnvironmentVariables(variables);
        return this;
    }
    
    /// <summary>
    /// Sets a timeout for shell operations.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithTimeout(int? timeoutMs)
    {
        var ok = timeoutMs.HasValue;
        if (ok) _processBuilder.WithTimeout(timeoutMs);
        return this;
    }
    
    /// <summary>
    /// Sets a cancellation token for shell operations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithCancellationToken(System.Threading.CancellationToken cancellationToken)
    {
        _processBuilder.WithCancellationToken(cancellationToken);
        return this;
    }
    
    /// <summary>
    /// Sets the timeout strategy for shell operations.
    /// </summary>
    /// <param name="strategy">The timeout strategy.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithTimeoutStrategy(TimeoutStrategy strategy)
    {
        _processBuilder.WithTimeoutStrategy(strategy);
        return this;
    }
    
    /// <summary>
    /// Enables or disables verbose logging.
    /// </summary>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithVerboseLogging(bool verbose = true)
    {
        _processBuilder.WithVerboseLogging(verbose);
        return this;
    }
    
    /// <summary>
    /// Sets a timeout for shell commands.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder WithCommandTimeout(int timeoutMs)
    {
        _commandTimeoutMs = timeoutMs > 0 ? timeoutMs : throw new ArgumentException("Timeout must be greater than 0", nameof(timeoutMs));
        return this;
    }
    
    /// <summary>
    /// Sets a callback for standard output data.
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of output.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder OnOutput(Action<string> callback)
    {
        _processBuilder.OnOutput(callback);
        return this;
    }
    
    /// <summary>
    /// Sets a callback for standard error data.
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of error output.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder OnError(Action<string> callback)
    {
        _processBuilder.OnError(callback);
        return this;
    }
    
    /// <summary>
    /// Sets a callback for all output data (both stdout and stderr).
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of output.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder OnMergedOutput(Action<string> callback)
    {
        _processBuilder.OnMergedOutput(callback);
        return this;
    }
    
    /// <summary>
    /// Sets a callback for process events.
    /// </summary>
    /// <param name="callback">The callback to invoke for process events.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder OnEvent(Action<ProcessEvent, string> callback)
    {
        _processBuilder.OnEvent(callback);
        return this;
    }
    
    /// <summary>
    /// Sets a callback for when the shell starts.
    /// </summary>
    /// <param name="callback">The callback to invoke when the shell starts.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder OnStarted(Action<int> callback)
    {
        _processBuilder.OnStarted(callback);
        return this;
    }
    
    /// <summary>
    /// Sets a callback for when the shell times out.
    /// </summary>
    /// <param name="callback">The callback to invoke when the shell times out.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder OnTimeout(Action callback)
    {
        _processBuilder.OnTimeout(callback);
        return this;
    }
    
    /// <summary>
    /// Sets a callback for when the shell exits.
    /// </summary>
    /// <param name="callback">The callback to invoke when the shell exits.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellProcessBuilder OnExit(Action<int> callback)
    {
        _processBuilder.OnExit(callback);
        return this;
    }
    
    /// <summary>
    /// Builds a configured PersistentShellProcess instance.
    /// </summary>
    /// <returns>A configured PersistentShellProcess.</returns>
    public PersistentShellProcess Build()
    {
        // Get the shell executable and arguments
        string shellExecutable = PersistentShellHelpers.GetShellFileNameFromPersistentShellType(_shellType);
        string shellArguments = PersistentShellHelpers.GetPersistentShellStartupArgs(_shellType);
        
        // Configure the process builder with the shell executable and arguments
        _processBuilder.WithFileName(shellExecutable);
        _processBuilder.WithArguments(shellArguments);
        
        // Build the base process
        RunnableProcess baseProcess = _processBuilder.Build();
        
        // Create the appropriate shell process based on shell type
        PersistentShellProcess shellProcess;
        switch (_shellType)
        {
            case PersistentShellType.Bash:
                shellProcess = new BashPersistentShellProcess(baseProcess);
                break;
                
            case PersistentShellType.Cmd:
                shellProcess = new CmdPersistentShellProcess(baseProcess);
                break;
                
            case PersistentShellType.PowerShell:
                shellProcess = new PowerShellPersistentShellProcess(baseProcess);
                break;
                
            default:
                throw new ArgumentException($"Unsupported shell type: {_shellType}");
        }
        
        // Configure the command timeout
        shellProcess.SetCommandTimeout(_commandTimeoutMs);
        
        return shellProcess;
    }

    private readonly RunnableProcessBuilder _processBuilder = new RunnableProcessBuilder();
    private PersistentShellType _shellType = PersistentShellHelpers.GetDefaultShellType();
    private int _commandTimeoutMs = 30000; // Default 30 seconds for commands
}