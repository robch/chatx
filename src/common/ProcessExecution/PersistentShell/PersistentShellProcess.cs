using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for process execution within a persistent shell session.
/// </summary>
public abstract class PersistentShellProcess
{
    /// <summary>
    /// Gets whether the shell process has exited.
    /// </summary>
    public bool HasExited => _shellProcess?.HasExited ?? true;
    
    /// <summary>
    /// Gets the shell type for this process.
    /// </summary>
    public abstract PersistentShellType PersistentShellType { get; }
    
    /// <summary>
    /// Sets the default timeout for shell commands.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public void SetCommandTimeout(int timeoutMs)
    {
        if (timeoutMs <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(timeoutMs));
        _commandTimeoutMs = timeoutMs;
    }
    
    /// <summary>
    /// Creates a new runnable shell process with the specified underlying process.
    /// </summary>
    /// <param name="shellProcess">The shell process instance.</param>
    public PersistentShellProcess(RunnableProcess shellProcess)
    {
        _shellProcess = shellProcess ?? throw new ArgumentNullException(nameof(shellProcess));
        _marker = GenerateMarker();
    }
    
    /// <summary>
    /// Ensures the shell is started and ready to accept commands.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public virtual async Task EnsureStartedAsync()
    {
        if (!_shellProcess.HasStarted)
        {
            await _shellProcess.StartAsync();
        }
        
        if (!_shellVerified)
        {
            _shellVerified = await VerifyShellAsync();
        }
    }

    /// <summary>
    /// Ensures the shell is started and ready to accept commands.
    /// </summary>
    public void EnsureStarted()
    {
        EnsureStartedAsync().GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Verifies the shell is responsive by executing a simple command.
    /// </summary>
    /// <returns>A task representing the verification operation.</returns>
    protected virtual async Task<bool> VerifyShellAsync()
    {
        if (HasExited)
            throw new InvalidOperationException("Shell process has exited");
            
        var verifyCommand = GetVerifyCommand();
        var wrappedCommand = WrapCommandWithMarker(verifyCommand);
        await _shellProcess.SendInputAsync(wrappedCommand);

        var commandOutput = await WaitForMarkerAsync(CancellationToken.None, 10000);
        var exitCode = ParseExitCodeFromMarker(commandOutput.StandardOutput);
        return exitCode == 0;
    }
    
    /// <summary>
    /// Gets a simple command to verify the shell is responsive.
    /// </summary>
    /// <returns>A command that outputs a predictable result.</returns>
    protected abstract string GetVerifyCommand();
    
    /// <summary>
    /// Runs a command in the shell.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <returns>The result of the command execution.</returns>
    public PersistentShellCommandResult RunCommand(string command)
    {
        return RunCommandAsync(command).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Runs a command in the shell with a timeout.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>The result of the command execution.</returns>
    public PersistentShellCommandResult RunCommand(string command, int timeoutMs)
    {
        return RunCommandAsync(command, timeoutMs).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Runs a command in the shell with cancellation support.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the async operation with the command result.</returns>
    public async Task<PersistentShellCommandResult> RunCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        // Use the default command timeout
        return await RunCommandInternalAsync(command, cancellationToken, null);
    }
    
    /// <summary>
    /// Runs a command in the shell with a specific timeout.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>A task representing the async operation with the command result.</returns>
    public async Task<PersistentShellCommandResult> RunCommandAsync(string command, int timeoutMs)
    {
        if (timeoutMs <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(timeoutMs));
            
        // Create a cancellation token with the specified timeout
        using var tokenSource = new CancellationTokenSource(timeoutMs);
        
        try
        {
            return await RunCommandInternalAsync(command, tokenSource.Token, timeoutMs);
        }
        catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
        {
            // Convert cancellation due to timeout to a timeout exception result
            var startTime = DateTime.Now.AddMilliseconds(-timeoutMs); // Approximate start time
            return PersistentShellCommandResult.FromProcessResult(
                new RunnableProcessResult(
                    "",
                    "",
                    "",
                    -1,
                    ProcessCompletionState.TimedOut,
                    TimeSpan.FromMilliseconds(timeoutMs), // Approximate duration
                    ProcessErrorType.Timeout,
                    $"Command timed out after {timeoutMs}ms"
                ),
                command
            );
        }
    }

    /// <summary>
    /// Gets the current output from the shell process.
    /// </summary>
    /// <returns>The current output from the shell process.</returns>
    public string GetCurrentOutput()
    {
        return _shellProcess.GetCurrentOutput();
    }

    /// <summary>
    /// Gets the current error output from the shell process.
    /// </summary>
    /// <returns>The current error output from the shell process.</returns>
    public string GetCurrentError()
    {
        return _shellProcess.GetCurrentError();
    }

    /// <summary>
    /// Gets the current merged output from the shell process.
    /// </summary>
    /// <returns>The current merged output from the shell process.</returns>
    public string GetCurrentMergedOutput()
    {
        return _shellProcess.GetCurrentMergedOutput();
    }
    
    /// <summary>
    /// Generates a unique marker string for command completion detection.
    /// </summary>
    /// <returns>A unique marker string.</returns>
    protected static string GenerateMarker()
    {
        // Create a marker that's unlikely to appear in normal output
        return $"__COMMAND_COMPLETION_MARKER_{Guid.NewGuid():N}__";
    }

    /// <summary>
    /// Internal implementation to run a command with optional timeout.
    /// </summary>
    private async Task<PersistentShellCommandResult> RunCommandInternalAsync(string command, CancellationToken cancellationToken, int? timeoutMs)
    {
        // Make sure shell is started, and outputs are cleared
        await EnsureStartedAsync();
        _shellProcess.ClearOutputs();
        
        var startTime = DateTime.Now;
        var wrappedCommand = WrapCommandWithMarker(command);
        
        try
        {
            // Send command to the shell
            await _shellProcess.SendInputAsync(wrappedCommand);
            
            // Wait for marker to appear with the specified timeout
            var commandOutput = await WaitForMarkerAsync(cancellationToken, timeoutMs);
            var commandDuration = DateTime.Now - startTime;
            
            // Parse output and extract exit code from marker
            int exitCode = ParseExitCodeFromMarker(commandOutput.StandardOutput);
            
            // Check for shell-specific errors
            string? shellSpecificError = ExtractShellSpecificError(commandOutput.StandardError);
            bool isSyntaxError = IsSyntaxError(exitCode, commandOutput.StandardError);
            
            // Strip marker from output
            string stdout = StripMarkerFromOutput(commandOutput.StandardOutput);
            string merged = StripMarkerFromOutput(commandOutput.MergedOutput);
            
            // Create the result
            return PersistentShellCommandResult.FromProcessResult(
                new RunnableProcessResult(
                    stdout,
                    commandOutput.StandardError,
                    merged,
                    exitCode,
                    commandOutput.CompletionState,
                    commandOutput.Duration,
                    exitCode != 0 ? ProcessErrorType.Other : null,
                    exitCode != 0 ? $"Command exited with code {exitCode}" : null
                ),
                command,
                commandDuration,
                shellSpecificError,
                isSyntaxError
            );
        }
        catch (TimeoutException)
        {
            // Report the actual timeout used
            int actualTimeout = timeoutMs ?? _commandTimeoutMs;
            var duration = DateTime.Now - startTime;
            return PersistentShellCommandResult.FromProcessResult(
                new RunnableProcessResult(
                    "",
                    "",
                    "",
                    -1,
                    ProcessCompletionState.TimedOut,
                    duration,
                    ProcessErrorType.Timeout,
                    $"Command timed out after {duration.TotalMilliseconds:0}ms (timeout: {actualTimeout}ms)"
                ),
                command
            );
        }
        catch (OperationCanceledException)
        {
            return PersistentShellCommandResult.FromProcessResult(
                new RunnableProcessResult(
                    "",
                    "",
                    "",
                    -1,
                    ProcessCompletionState.Canceled,
                    DateTime.Now - startTime,
                    ProcessErrorType.Other,
                    "Command execution was canceled"
                ),
                command
            );
        }
        catch (Exception ex)
        {
            // Check if the shell is still running
            if (_shellProcess.HasExited)
            {
                return PersistentShellCommandResult.FromProcessResult(
                    new RunnableProcessResult(
                        "",
                        "",
                        "",
                        -1,
                        ProcessCompletionState.Error,
                        DateTime.Now - startTime,
                        ProcessErrorType.Other,
                        "Shell has exited unexpectedly",
                        ex
                    ),
                    command
                );
            }
            
            return PersistentShellCommandResult.FromProcessResult(
                new RunnableProcessResult(
                    "",
                    "",
                    "",
                    -1,
                    ProcessCompletionState.Error,
                    DateTime.Now - startTime,
                    ProcessErrorType.Other,
                    $"Error executing command: {ex.Message}",
                    ex
                ),
                command
            );
        }
    }
    
    /// <summary>
    /// Runs a command in the shell with stdin input.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="input">The standard input to provide.</param>
    /// <returns>The result of the command execution.</returns>
    public PersistentShellCommandResult RunCommand(string command, string input)
    {
        return RunCommandAsync(command, input).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Runs a command in the shell with stdin input and a specific timeout.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="input">The standard input to provide.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>The result of the command execution.</returns>
    public PersistentShellCommandResult RunCommand(string command, string input, int timeoutMs)
    {
        return RunCommandAsync(command, input, timeoutMs).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Runs a command in the shell with stdin input asynchronously.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="input">The standard input to provide.</param>
    /// <returns>A task representing the async operation with the command result.</returns>
    public async Task<PersistentShellCommandResult> RunCommandAsync(string command, string input)
    {
        // This implementation will need to be refined for each specific shell type
        // For now, a simple echo-based input might work for basic cases
        var inputCommand = command + " << EOF\n" + input + "\nEOF";
        return await RunCommandInternalAsync(inputCommand, CancellationToken.None, null);
    }
    
    /// <summary>
    /// Runs a command in the shell with stdin input and a specific timeout.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="input">The standard input to provide.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>A task representing the async operation with the command result.</returns>
    public async Task<PersistentShellCommandResult> RunCommandAsync(string command, string input, int timeoutMs)
    {
        if (timeoutMs <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(timeoutMs));
            
        // Use a cancellation token with the specified timeout
        using var tokenSource = new CancellationTokenSource(timeoutMs);
        
        // This implementation will need to be refined for each specific shell type
        var inputCommand = command + " << EOF\n" + input + "\nEOF";
        
        try
        {
            return await RunCommandInternalAsync(inputCommand, tokenSource.Token, timeoutMs);
        }
        catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
        {
            // Convert cancellation due to timeout to a timeout exception result
            var startTime = DateTime.Now.AddMilliseconds(-timeoutMs); // Approximate start time
            return PersistentShellCommandResult.FromProcessResult(
                new RunnableProcessResult(
                    "",
                    "",
                    "",
                    -1,
                    ProcessCompletionState.TimedOut,
                    TimeSpan.FromMilliseconds(timeoutMs), // Approximate duration
                    ProcessErrorType.Timeout,
                    $"Command with input timed out after {timeoutMs}ms"
                ),
                command
            );
        }
    }
    
    /// <summary>
    /// Wraps a command with shell-specific code to output the marker and exit code.
    /// </summary>
    protected abstract string WrapCommandWithMarker(string command);
    
    /// <summary>
    /// Waits for the command to complete by looking for the marker.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds. If not provided, uses the default command timeout.</param>
    /// <returns>A task that completes when the marker is found with the process output.</returns>
    protected virtual async Task<RunnableProcessResult> WaitForMarkerAsync(CancellationToken cancellationToken, int? timeoutMs = null)
    {
        // Pattern to match marker with exit code
        var pattern = $@"{Regex.Escape(_marker)}(-?\d+)";
        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);
        var startTime = DateTime.Now;
        var actualTimeoutMs = timeoutMs ?? _commandTimeoutMs; // Use provided timeout or default command timeout
        
        while (!cancellationToken.IsCancellationRequested)
        {
            // Check for marker in output
            string currentOutput;
            lock (_lock)
            {
                currentOutput = _shellProcess.GetCurrentOutput();
            }
            
            if (regex.IsMatch(currentOutput))
            {
                // Marker found, return the current output
                return new RunnableProcessResult(
                    currentOutput,
                    _shellProcess.GetCurrentError(),
                    _shellProcess.GetCurrentMergedOutput(),
                    0, // Actual exit code will be parsed later
                    ProcessCompletionState.Completed,
                    DateTime.Now - startTime
                );
            }
            
            // Check for timeout
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            if (elapsed > actualTimeoutMs)
            {
                throw new TimeoutException($"Timed out waiting for marker after {actualTimeoutMs}ms");
            }
            
            // Wait briefly before checking again
            await Task.Delay(50, cancellationToken);
        }
        
        cancellationToken.ThrowIfCancellationRequested();
        
        // Should never reach here if cancellation token behaves correctly
        throw new InvalidOperationException("Unexpected state in WaitForMarkerAsync");
    }
    
    /// <summary>
    /// Parses the exit code from the marker in the output.
    /// </summary>
    /// <param name="output">The command output containing the marker.</param>
    /// <returns>The exit code extracted from the marker.</returns>
    protected virtual int ParseExitCodeFromMarker(string output)
    {
        var pattern = $@"{Regex.Escape(_marker)}(-?\d+)";
        var match = Regex.Match(output, pattern);
        
        if (match.Success && match.Groups.Count >= 2)
        {
            if (int.TryParse(match.Groups[1].Value, out var exitCode))
            {
                return exitCode;
            }
        }
        
        // If no marker found or couldn't parse exit code, assume error
        return -1;
    }
    
    /// <summary>
    /// Strips the marker and exit code from the output.
    /// </summary>
    /// <param name="output">The output containing the marker.</param>
    /// <returns>The output with the marker removed.</returns>
    protected virtual string StripMarkerFromOutput(string output)
    {
        var lastMarkerPattern = $@"{Regex.Escape(_marker)}-?\d+.*$";
        var splitAndTrimmed = output.Split('\n', StringSplitOptions.None)
            .Select(line => line.TrimEnd(new char[] { '\r', '\n' }));
        var beforeLastMarker = splitAndTrimmed
            .TakeWhile(line => !Regex.IsMatch(line, lastMarkerPattern));
        var stripAllMarkers = beforeLastMarker
            .Where(line => !line.Contains(_marker));
        return string.Join("\n", stripAllMarkers);
    }
    
    /// <summary>
    /// Extracts shell-specific error messages from the stderr output.
    /// </summary>
    /// <param name="stderr">The standard error output.</param>
    /// <returns>Shell-specific error message if found, otherwise null.</returns>
    protected abstract string? ExtractShellSpecificError(string stderr);
    
    /// <summary>
    /// Determines if the command resulted in a syntax error.
    /// </summary>
    /// <param name="exitCode">The command exit code.</param>
    /// <param name="stderr">The standard error output.</param>
    /// <returns>True if the error was a syntax error, otherwise false.</returns>
    protected abstract bool IsSyntaxError(int exitCode, string stderr);
    
    /// <summary>
    /// Shuts down the shell session synchronously.
    /// </summary>
    public void ForceShutdown()
    {
        _shellProcess?.ForceShutdown();
    }

    protected readonly RunnableProcess _shellProcess;
    protected readonly string _marker;
    protected bool _shellVerified = false;
    protected readonly object _lock = new object();
    protected int _commandTimeoutMs = 30000; // Default 30 seconds for commands
}