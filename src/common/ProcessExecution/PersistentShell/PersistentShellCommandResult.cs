using System;

/// <summary>
/// Represents the result of a shell command execution.
/// </summary>
public class PersistentShellCommandResult : RunnableProcessResult
{
    /// <summary>
    /// Gets the original command as provided before wrapping.
    /// </summary>
    public string OriginalCommand { get; }
    
    /// <summary>
    /// Gets the duration of just the command execution.
    /// </summary>
    public TimeSpan CommandDuration { get; }
    
    /// <summary>
    /// Gets any shell-specific error information, if available.
    /// </summary>
    public string? ShellSpecificError { get; }
    
    /// <summary>
    /// Gets whether the command produced a syntax error in the shell.
    /// </summary>
    public bool IsSyntaxError { get; }
    
    /// <summary>
    /// Creates a new PersistentShellCommandResult extending the base RunnableProcessResult.
    /// </summary>
    /// <param name="stdout">The standard output.</param>
    /// <param name="stderr">The standard error output.</param>
    /// <param name="merged">The merged stdout and stderr.</param>
    /// <param name="exitCode">The command/shell exit code.</param>
    /// <param name="completionState">The state in which the command completed.</param>
    /// <param name="duration">The overall duration including shell startup if applicable.</param>
    /// <param name="errorType">The type of error that occurred, if any.</param>
    /// <param name="friendlyErrorMessage">A user-friendly error message.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    /// <param name="originalCommand">The original command as provided.</param>
    /// <param name="commandDuration">The duration of just the command execution.</param>
    /// <param name="shellSpecificError">Any shell-specific error information.</param>
    /// <param name="isSyntaxError">Whether the error was a syntax error.</param>
    public PersistentShellCommandResult(
        string stdout,
        string stderr,
        string merged,
        int exitCode,
        ProcessCompletionState completionState = ProcessCompletionState.Completed,
        TimeSpan duration = default,
        ProcessErrorType? errorType = null,
        string? friendlyErrorMessage = null,
        Exception? exception = null,
        string originalCommand = "",
        TimeSpan commandDuration = default,
        string? shellSpecificError = null,
        bool isSyntaxError = false)
        : base(stdout, stderr, merged, exitCode, completionState, duration, errorType, friendlyErrorMessage, exception)
    {
        OriginalCommand = originalCommand;
        CommandDuration = commandDuration;
        ShellSpecificError = shellSpecificError;
        IsSyntaxError = isSyntaxError;
    }
    
    /// <summary>
    /// Returns a string representation of the shell command result.
    /// </summary>
    public override string ToString()
    {
        var baseString = base.ToString();
        var command = OriginalCommand.Length > 50 ? OriginalCommand.Substring(0, 47) + "..." : OriginalCommand;
        return $"{baseString}, Command: {command}" + (ShellSpecificError != null ? $", Shell Error: {ShellSpecificError}" : "");
    }
    
    /// <summary>
    /// Creates a PersistentShellCommandResult from an existing RunnableProcessResult.
    /// </summary>
    /// <param name="processResult">The base RunnableProcessResult.</param>
    /// <param name="originalCommand">The original command as provided.</param>
    /// <param name="commandDuration">The duration of just the command execution.</param>
    /// <param name="shellSpecificError">Any shell-specific error information.</param>
    /// <param name="isSyntaxError">Whether the error was a syntax error.</param>
    /// <returns>A new PersistentShellCommandResult.</returns>
    public static PersistentShellCommandResult FromProcessResult(
        RunnableProcessResult processResult,
        string originalCommand = "",
        TimeSpan commandDuration = default,
        string? shellSpecificError = null,
        bool isSyntaxError = false)
    {
        if (processResult == null)
            throw new ArgumentNullException(nameof(processResult));
            
        var result = new PersistentShellCommandResult(
            processResult.StandardOutput,
            processResult.StandardError,
            processResult.MergedOutput,
            processResult.ExitCode,
            processResult.CompletionState,
            processResult.Duration,
            processResult.ErrorType,
            processResult.FriendlyErrorMessage,
            processResult.Exception,
            originalCommand,
            commandDuration,
            shellSpecificError,
            isSyntaxError
        );

        return result;
    }
}