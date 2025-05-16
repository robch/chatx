using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Builder for configuring and executing commands in an existing shell process.
/// </summary>
public class PersistentShellCommandBuilder
{
    /// <summary>
    /// Creates a new PersistentShellCommandBuilder using the specified shell process.
    /// </summary>
    /// <param name="shellProcess">The shell process to use.</param>
    public PersistentShellCommandBuilder(PersistentShellProcess shellProcess)
    {
        _shellProcess = shellProcess ?? throw new ArgumentNullException(nameof(shellProcess));
    }
    
    /// <summary>
    /// Sets the command to execute.
    /// </summary>
    /// <param name="command">The command string.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellCommandBuilder WithCommand(string command)
    {
        _command = command;
        return this;
    }
    
    /// <summary>
    /// Sets the working directory for the command.
    /// </summary>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellCommandBuilder WithWorkingDirectory(string? workingDirectory)
    {
        var ok = !string.IsNullOrEmpty(workingDirectory);
        if (ok) _workingDirectory = workingDirectory;
        return this;
    }
    
    /// <summary>
    /// Sets a timeout for the command.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellCommandBuilder WithTimeout(int? timeoutMs)
    {
        var ok = timeoutMs.HasValue;
        if (ok) _timeoutMs = timeoutMs!.Value;
        return this;
    }
    
    /// <summary>
    /// Sets a cancellation token for the command.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public PersistentShellCommandBuilder WithCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return this;
    }
    
    /// <summary>
    /// Runs the command and returns the result.
    /// </summary>
    /// <returns>The result of the command execution.</returns>
    public PersistentShellCommandResult Run()
    {
        return RunAsync().GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Runs the command asynchronously and returns the result.
    /// </summary>
    /// <returns>A task that completes with the command result.</returns>
    public async Task<PersistentShellCommandResult> RunAsync()
    {
        if (string.IsNullOrEmpty(_command))
        {
            throw new InvalidOperationException("Command must be specified");
        }
        
        if (_shellProcess.HasExited)
        {
            throw new InvalidOperationException("Shell process has exited");
        }
        
        // Change working directory if specified
        PersistentShellCommandResult? result = null;
        
        if (!string.IsNullOrEmpty(_workingDirectory))
        {
            // Prepend a cd command to change directory
            string cdCommand = GetChangeDirectoryCommand(_shellProcess.PersistentShellType, _workingDirectory);
            await _shellProcess.RunCommandAsync(cdCommand, _cancellationToken);
        }
        
        try
        {
            if (_timeoutMs.HasValue)
            {
                ConsoleHelpers.WriteDebugLine($"Running command with timeout: {_timeoutMs.Value}ms");
                result = await _shellProcess.RunCommandAsync(_command, _timeoutMs.Value);
            }
            else
            {
                ConsoleHelpers.WriteDebugLine("Running command with cancellation token");
                result = await _shellProcess.RunCommandAsync(_command, _cancellationToken);
            }
            
            return result;
        }
        finally
        {
            // If we changed directory, we could restore it here if needed
        }
    }
    
    /// <summary>
    /// Gets the appropriate change directory command for the specified shell.
    /// </summary>
    /// <param name="shellType">The shell type.</param>
    /// <param name="directory">The directory to change to.</param>
    /// <returns>A shell-specific command to change directory.</returns>
    private string GetChangeDirectoryCommand(PersistentShellType shellType, string directory)
    {
        switch (shellType)
        {
            case PersistentShellType.Bash:
                return $"cd {ProcessHelpers.EscapeBashArgument(directory)}";
                
            case PersistentShellType.Cmd:
                return $"cd /d {ProcessHelpers.EscapeCmdArgument(directory)}";
                
            case PersistentShellType.PowerShell:
                return $"Set-Location {ProcessHelpers.EscapePowerShellArgument(directory)}";
                
            default:
                return $"cd {directory}";
        }
    }

    private readonly PersistentShellProcess _shellProcess;
    private string? _command;
    private string? _workingDirectory;
    private int? _timeoutMs;
    private CancellationToken _cancellationToken = CancellationToken.None;
}