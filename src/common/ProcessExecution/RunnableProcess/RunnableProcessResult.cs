using System;
using System.Collections.Generic;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public class RunnableProcessResult
{
    /// <summary>
    /// Gets the standard output from the process.
    /// </summary>
    public string StandardOutput { get; }
    
    /// <summary>
    /// Gets the standard error output from the process.
    /// </summary>
    public string StandardError { get; }
    
    /// <summary>
    /// Gets the combined output from both stdout and stderr.
    /// </summary>
    public string MergedOutput { get; }
    
    /// <summary>
    /// Gets the process exit code.
    /// </summary>
    public int ExitCode { get; }
    
    /// <summary>
    /// Gets the state in which the process execution completed.
    /// </summary>
    public ProcessCompletionState CompletionState { get; }
    
    /// <summary>
    /// Gets the duration of the process execution.
    /// </summary>
    public TimeSpan Duration { get; }
    
    /// <summary>
    /// Gets the type of error that occurred, if any.
    /// </summary>
    public ProcessErrorType? ErrorType { get; }
    
    /// <summary>
    /// Gets a user-friendly error message if an error occurred.
    /// </summary>
    public string? FriendlyErrorMessage { get; }
    
    /// <summary>
    /// Gets the exception that occurred during execution, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets whether the process completed successfully (exit code 0 and completed normally).
    /// </summary>
    public bool Success => ExitCode == 0 && CompletionState == ProcessCompletionState.Completed;
    
    /// <summary>
    /// Gets whether the process timed out.
    /// </summary>
    public bool IsTimeout => CompletionState == ProcessCompletionState.TimedOut;
    
    /// <summary>
    /// Gets whether the process was killed.
    /// </summary>
    public bool WasKilled => ErrorType == ProcessErrorType.Killed;
    
    /// <summary>
    /// Gets whether any error occurred during the process execution.
    /// </summary>
    public bool HasError => ErrorType != null || CompletionState != ProcessCompletionState.Completed;
    
    /// <summary>
    /// Creates a new RunnableProcessResult.
    /// </summary>
    /// <param name="stdout">The standard output.</param>
    /// <param name="stderr">The standard error output.</param>
    /// <param name="merged">The merged stdout and stderr.</param>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="completionState">The state in which the process completed.</param>
    /// <param name="duration">The duration of the process execution.</param>
    /// <param name="errorType">The type of error that occurred, if any.</param>
    /// <param name="friendlyErrorMessage">A user-friendly error message.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    public RunnableProcessResult(
        string stdout, 
        string stderr, 
        string merged, 
        int exitCode,
        ProcessCompletionState completionState = ProcessCompletionState.Completed,
        TimeSpan duration = default,
        ProcessErrorType? errorType = null,
        string? friendlyErrorMessage = null,
        Exception? exception = null)
    {
        StandardOutput = stdout ?? string.Empty;
        StandardError = stderr ?? string.Empty;
        MergedOutput = merged ?? string.Empty;
        ExitCode = exitCode;
        CompletionState = completionState;
        Duration = duration;
        ErrorType = errorType;
        FriendlyErrorMessage = friendlyErrorMessage;
        Exception = exception;
    }
    
    /// <summary>
    /// Returns a string representation of the process result.
    /// </summary>
    public override string ToString()
    {
        return $"ExitCode: {ExitCode}, State: {CompletionState}, " + 
                $"Duration: {Duration.TotalMilliseconds}ms" +
                (HasError ? $", Error: {FriendlyErrorMessage ?? ErrorType?.ToString() ?? "Unknown error"}" : "");
    }
}

/// <summary>
/// Represents the state in which a process execution completed.
/// </summary>
public enum ProcessCompletionState
{
    /// <summary>
    /// The process completed normally.
    /// </summary>
    Completed,
    
    /// <summary>
    /// The process timed out and was terminated.
    /// </summary>
    TimedOut,
    
    /// <summary>
    /// The process was canceled by the user.
    /// </summary>
    Canceled,
    
    /// <summary>
    /// An error occurred during execution.
    /// </summary>
    Error
}

/// <summary>
/// Represents different types of errors that can occur during process execution.
/// </summary>
public enum ProcessErrorType
{
    /// <summary>
    /// The process failed to start.
    /// </summary>
    StartFailure,
    
    /// <summary>
    /// The process timed out.
    /// </summary>
    Timeout,
    
    /// <summary>
    /// The process was forcefully killed.
    /// </summary>
    Killed,
    
    /// <summary>
    /// The process crashed.
    /// </summary>
    Crashed,
    
    /// <summary>
    /// Access was denied.
    /// </summary>
    AccessDenied,
    
    /// <summary>
    /// Another type of error occurred.
    /// </summary>
    Other
}

/// <summary>
/// Represents different strategies for handling process timeouts.
/// </summary>
public enum TimeoutStrategy
{
    /// <summary>
    /// Immediately kill the process when it times out.
    /// </summary>
    ImmediateKill,
    
    /// <summary>
    /// Send Ctrl+C to the process when it times out, but don't kill it.
    /// </summary>
    CtrlCOnly,
    
    /// <summary>
    /// Kill the process without trying Ctrl+C first.
    /// </summary>
    KillOnly,
    
    /// <summary>
    /// Try Ctrl+C first, then kill if the process doesn't exit.
    /// </summary>
    Progressive
}

/// <summary>
/// Represents process lifecycle events.
/// </summary>
public enum ProcessEvent
{
    /// <summary>
    /// Process has started.
    /// </summary>
    Started,
    
    /// <summary>
    /// Process has timed out.
    /// </summary>
    Timeout,
    
    /// <summary>
    /// Sending Ctrl+C to the process.
    /// </summary>
    SendingCtrlC,
    
    /// <summary>
    /// Attempting to kill the process.
    /// </summary>
    AttemptingKill,
    
    /// <summary>
    /// Process has been killed.
    /// </summary>
    Killed,
    
    /// <summary>
    /// Process has completed.
    /// </summary>
    Completed,
    
    /// <summary>
    /// An error occurred during process execution.
    /// </summary>
    Error
}