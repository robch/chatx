using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a process that can be started and monitored.
/// Implements hybrid disposal pattern for resource management.
/// </summary>
public class RunnableProcess
{
    #region Properties

    /// <summary>
    /// Gets whether the process has been started.
    /// </summary>
    public bool HasStarted => _hasStarted;

    /// <summary>
    /// Gets whether the process has exited.
    /// </summary>
    public bool HasExited => _hasExited || (_process?.HasExited ?? false);

    #endregion

    #region Initialization

    /// <summary>
    /// Creates a new runnable process.
    /// </summary>
    public RunnableProcess()
    {
    }

    /// <summary>
    /// Creates a new runnable process with specified file name and arguments.
    /// </summary>
    /// <param name="fileName">The name of the file to run.</param>
    /// <param name="arguments">The arguments to pass to the process.</param>
    public RunnableProcess(string fileName, string arguments)
    {
        _fileName = fileName;
        _arguments = arguments;
    }

    /// <summary>
    /// Sets the working directory for the process.
    /// </summary>
    /// <param name="workingDirectory">The directory to use.</param>
    public void SetWorkingDirectory(string workingDirectory)
    {
        ThrowIfStarted();
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Sets an environment variable for the process.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    public void SetEnvironmentVariable(string name, string value)
    {
        ThrowIfStarted();
        _environmentVariables[name] = value;
    }

    /// <summary>
    /// Sets environment variables for the process.
    /// </summary>
    /// <param name="variables">Dictionary of environment variables.</param>
    public void SetEnvironmentVariables(IDictionary<string, string> variables)
    {
        ThrowIfStarted();
        foreach (var kvp in variables)
        {
            _environmentVariables[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Sets a timeout for the process.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public void SetTimeout(int timeoutMs)
    {
        ThrowIfStarted();
        _timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Sets a cancellation token for the process.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public void SetCancellationToken(CancellationToken cancellationToken)
    {
        ThrowIfStarted();
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Sets the timeout strategy for the process.
    /// </summary>
    /// <param name="strategy">The timeout strategy to use.</param>
    public void SetTimeoutStrategy(TimeoutStrategy strategy)
    {
        ThrowIfStarted();
        _timeoutStrategy = strategy;
    }

    /// <summary>
    /// Sets standard input text for the process.
    /// </summary>
    /// <param name="input">The input text to send.</param>
    public void SetStandardInput(string input)
    {
        ThrowIfStarted();
        _standardInput = input;
    }
    
    /// <summary>
    /// Enables or disables verbose logging.
    /// </summary>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    public void SetVerboseLogging(bool verbose)
    {
        ThrowIfStarted();
        _verboseLogging = verbose;
    }

    #endregion

    #region Callbacks

    /// <summary>
    /// Sets a callback for standard output data.
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of output.</param>
    public void SetStdoutCallback(Action<string> callback)
    {
        ThrowIfStarted();
        _stdoutCallback = callback;
    }

    /// <summary>
    /// Sets a callback for standard error data.
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of error output.</param>
    public void SetStderrCallback(Action<string> callback)
    {
        ThrowIfStarted();
        _stderrCallback = callback;
    }

    /// <summary>
    /// Sets a callback for all output data (both stdout and stderr).
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of output.</param>
    public void SetMergedOutputCallback(Action<string> callback)
    {
        ThrowIfStarted();
        _mergedCallback = callback;
    }

    /// <summary>
    /// Sets a callback for process events.
    /// </summary>
    /// <param name="callback">The callback to invoke for process events.</param>
    public void SetEventCallback(Action<ProcessEvent, string> callback)
    {
        ThrowIfStarted();
        _eventCallback = callback;
    }

    /// <summary>
    /// Sets a callback for when the process starts.
    /// </summary>
    /// <param name="callback">The callback to invoke when the process starts.</param>
    public void SetStartedCallback(Action<int> callback)
    {
        ThrowIfStarted();
        _startedCallback = callback;
    }

    /// <summary>
    /// Sets a callback for when the process times out.
    /// </summary>
    /// <param name="callback">The callback to invoke when the process times out.</param>
    public void SetTimeoutCallback(Action callback)
    {
        ThrowIfStarted();
        _timeoutCallback = callback;
    }

    /// <summary>
    /// Sets a callback for when the process exits.
    /// </summary>
    /// <param name="callback">The callback to invoke when the process exits.</param>
    public void SetExitCallback(Action<int> callback)
    {
        ThrowIfStarted();
        _exitCallback = callback;
    }

    #endregion

    #region Process Execution

    /// <summary>
    /// Starts the process asynchronously without waiting for completion.
    /// </summary>
    /// <returns>A task that completes when the process is started.</returns>
    public async Task StartAsync()
    {
        if (_hasStarted)
            return;
            
        if (string.IsNullOrEmpty(_fileName))
            throw new InvalidOperationException("File name must be set before starting the process.");

        try
        {
            // Clear outputs
            ClearOutputs();

            // Reset signals
            _stdoutDoneSignal.Reset();
            _stderrDoneSignal.Reset();

            // Configure the process
            var startInfo = ConfigureProcessStartInfo(_fileName, _arguments);

            // Set working directory if specified
            if (!string.IsNullOrEmpty(_workingDirectory))
            {
                startInfo.WorkingDirectory = _workingDirectory;
            }

            // Set environment variables
            foreach (var kvp in _environmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }

            // Create the process
            _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            // Configure event handlers
            ConfigureEventHandlers();

            // Start the process
            RaiseEvent(ProcessEvent.Started, $"Starting process: {startInfo.FileName} {startInfo.Arguments}");
            var started = _process.Start();

            if (!started)
            {
                throw new InvalidOperationException("Failed to start process.");
            }

            _hasStarted = true;
            
            // Notify started callback
            _startedCallback?.Invoke(_process.Id);

            // Begin reading output and error asynchronously
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            // Send input if configured
            if (_standardInput != null)
            {
                await _process.StandardInput.WriteLineAsync(_standardInput);
                _process.StandardInput.Close();
                _standardInputClosed = true;
            }
        }
        catch (Exception ex)
        {
            _hasStarted = false;
            throw new InvalidOperationException($"Failed to start process: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Runs the process asynchronously and waits for completion.
    /// </summary>
    /// <returns>A task that completes with the process result.</returns>
    public async Task<RunnableProcessResult> RunAsync()
    {
        var startTime = DateTime.Now;
        
        try
        {
            await StartAsync();
            return await WaitForExitAsync();
        }
        catch (OperationCanceledException)
        {
            // Process was canceled
            LogDebug("Process execution was canceled");
            
            return CreateProcessResult(
                -1,
                ProcessCompletionState.Canceled,
                startTime,
                ProcessErrorType.Other,
                "Operation was canceled");
        }
        catch (Exception ex)
        {
            // Process failed to start or other error
            LogDebug($"Error executing process: {ex.Message}");
            
            return CreateProcessResult(
                -1,
                ProcessCompletionState.Error,
                startTime,
                ProcessErrorType.StartFailure,
                $"Error starting process: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Runs the process synchronously and waits for completion.
    /// </summary>
    /// <returns>The process result.</returns>
    public RunnableProcessResult Run()
    {
        return RunAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sends input to the process's standard input stream.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="close">Whether to close the standard input stream after sending.</param>
    public async Task SendInputAsync(string input, bool close = false)
    {
        if (_process == null)
            throw new InvalidOperationException("Process is null.");

        await _process.StandardInput.WriteLineAsync(input);
        await _process.StandardInput.FlushAsync();

        if (close)
        {
            _process.StandardInput.Close();
        }
    }

    /// <summary>
    /// Waits for the process to exit after it has been started.
    /// </summary>
    /// <returns>A task that completes with the process result.</returns>
    public async Task<RunnableProcessResult> WaitForExitAsync()
    {
        if (!_hasStarted)
            throw new InvalidOperationException("Process has not been started.");

        if (_process == null)
            throw new InvalidOperationException("Process is null.");

        if (!_standardInputClosed)
        {
            _process.StandardInput.Close();
            _standardInputClosed = true;
        }

        var startTime = DateTime.Now;
        
        try
        {
            // Monitor the process with timeout and cancellation
            var completionState = ProcessCompletionState.Completed;
            ProcessErrorType? errorType = null;
            string? errorMessage = null;
            
            try
            {
                await MonitorProcessAsync();
            }
            catch (TimeoutException)
            {
                completionState = ProcessCompletionState.TimedOut;
                errorType = ProcessErrorType.Timeout;
                errorMessage = $"Process timed out after {_timeoutMs}ms";
                LogDebug(errorMessage);
                _timeoutCallback?.Invoke();
            }
            catch (OperationCanceledException)
            {
                completionState = ProcessCompletionState.Canceled;
                errorType = ProcessErrorType.Other;
                errorMessage = "Process execution was canceled";
                LogDebug(errorMessage);
            }
            
            // Wait for output completion
            if (completionState == ProcessCompletionState.Completed)
            {
                // Only wait for a limited time to avoid hanging
                _stdoutDoneSignal.WaitOne(100);
                _stderrDoneSignal.WaitOne(100);
            }
            
            // Calculate duration
            var duration = DateTime.Now - startTime;
            
            // Get exit code (default to -1 if process was killed or didn't exit)
            var exitCode = _process.HasExited ? _process.ExitCode : -1;
            _hasExited = _process.HasExited;
            
            // Notify exit callback if process exited
            if (_hasExited)
            {
                _exitCallback?.Invoke(exitCode);
            }
            
            // Create and return the result
            return CreateProcessResult(
                exitCode,
                completionState,
                startTime,
                errorType,
                errorMessage);
        }
        catch (Exception ex)
        {
            LogDebug($"Error waiting for process: {ex.Message}");
            
            return CreateProcessResult(
                -1,
                ProcessCompletionState.Error,
                startTime,
                ProcessErrorType.Other,
                $"Error waiting for process: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Waits for the process to exit after it has been started.
    /// </summary>
    /// <returns>The process result.</returns>
    public RunnableProcessResult WaitForExit()
    {
        return WaitForExitAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Monitors a process, waiting for it to exit or timeout.
    /// </summary>
    /// <returns>A task that completes when the process exits or times out.</returns>
    protected virtual async Task MonitorProcessAsync()
    {
        if (_process == null)
            throw new InvalidOperationException("Process is null.");

        // Create a task that completes when the process exits
        var processExitTask = Task.Run(() => _process.WaitForExit());
        
        // Create a task that completes after the timeout
        var timeoutTask = Task.Delay(_timeoutMs, _cancellationToken);
        
        // Wait for either the process to exit or the timeout to occur
        var completedTask = await Task.WhenAny(processExitTask, timeoutTask);
        
        // If the timeout task completed first
        if (completedTask == timeoutTask && !_process.HasExited)
        {
            await HandleTimeoutAsync();
            throw new TimeoutException($"Process timed out after {_timeoutMs}ms");
        }
        
        // If the cancellation was requested
        if (_cancellationToken.IsCancellationRequested && !_process.HasExited)
        {
            await HandleTimeoutAsync();
            _cancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <summary>
    /// Handles a process timeout according to the configured strategy.
    /// </summary>
    protected virtual async Task HandleTimeoutAsync()
    {
        if (_process == null || _process.HasExited)
            return;
            
        RaiseEvent(ProcessEvent.Timeout, $"Process timed out after {_timeoutMs}ms");
        
        switch (_timeoutStrategy)
        {
            case TimeoutStrategy.ImmediateKill:
                await KillProcessAsync();
                break;
                
            case TimeoutStrategy.CtrlCOnly:
                await SendCtrlCAsync();
                break;
                
            case TimeoutStrategy.KillOnly:
                await KillProcessAsync();
                break;
                
            case TimeoutStrategy.Progressive:
            default:
                // Try Ctrl+C first
                if (!await SendCtrlCAsync())
                {
                    // If Ctrl+C didn't work (or process didn't exit), use Kill
                    await KillProcessAsync();
                }
                break;
        }
    }

    /// <summary>
    /// Sends Ctrl+C to the process.
    /// </summary>
    /// <returns>True if the process exited after Ctrl+C, otherwise false.</returns>
    public async Task<bool> SendCtrlCAsync()
    {
        if (_process == null || _process.HasExited)
            return true;
            
        RaiseEvent(ProcessEvent.SendingCtrlC, "Sending Ctrl+C to process");
        
        try
        {
            // Send Ctrl+C character to standard input
            _process.StandardInput.WriteLine("\u0003");
            _process.StandardInput.Flush();
            
            // Wait briefly to see if the process exits
            return await Task.Run(() => _process.WaitForExit(200));
        }
        catch (Exception ex)
        {
            RaiseEvent(ProcessEvent.Error, $"Error sending Ctrl+C: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Kills the process.
    /// </summary>
    public async Task KillProcessAsync()
    {
        if (_process == null || _process.HasExited)
            return;
            
        RaiseEvent(ProcessEvent.AttemptingKill, "Attempting to kill process");
        
        try
        {
            _process.Kill();
            RaiseEvent(ProcessEvent.Killed, "Process killed");
            
            // Wait for exit after kill
            await Task.Run(() => _process.WaitForExit(100));
        }
        catch (Exception ex)
        {
            RaiseEvent(ProcessEvent.Error, $"Error killing process: {ex.Message}");
        }
    }

    /// <summary>
    /// Kills the process synchronously.
    /// </summary>
    public void Kill()
    {
        KillProcessAsync().GetAwaiter().GetResult();
    }

    public void ForceShutdown()
    {
        if (_process != null && !_process.HasExited)
        {
            try { _process.Kill(); } catch { }
            _process.Dispose();
            _process = null;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Configures event handlers for the process.
    /// </summary>
    protected virtual void ConfigureEventHandlers()
    {
        if (_process == null)
            return;

        _process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                // ConsoleHelpers.WriteLine($"[STDOUT] {e.Data.Length}: {e.Data}", ConsoleColor.DarkGreen);
                var line = e.Data.TrimEnd(new char[] { '\r', '\n' });
                lock (_lock)
                {
                    _stdoutBuffer.AppendLine(line);
                    _mergedBuffer.AppendLine(line);
                }
                
                _stdoutCallback?.Invoke(line);
                _mergedCallback?.Invoke(line);
            }
            else
            {
                _stdoutDoneSignal.Set();
            }
        };
        
        _process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                // ConsoleHelpers.WriteLine($"[STDERR] {e.Data.Length}: {e.Data}", ConsoleColor.DarkGreen);
                var line = e.Data.TrimEnd(new char[] { '\r', '\n' });
                lock (_lock)
                {
                    _stderrBuffer.AppendLine(line);
                    _mergedBuffer.AppendLine(line);
                }
                
                _stderrCallback?.Invoke(line);
                _mergedCallback?.Invoke(line);
            }
            else
            {
                _stderrDoneSignal.Set();
            }
        };

        _process.Exited += (sender, e) =>
        {
            _hasExited = true;
            RaiseEvent(ProcessEvent.Completed, $"Process exited with code {_process.ExitCode}");
        };
    }

    /// <summary>
    /// Raises a process event.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="message">The event message.</param>
    protected virtual void RaiseEvent(ProcessEvent eventType, string message)
    {
        _eventCallback?.Invoke(eventType, message);
        LogDebug($"Process event: {eventType} - {message}");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Logs a debug message if verbose logging is enabled.
    /// </summary>
    /// <param name="message">The message to log.</param>
    protected virtual void LogDebug(string message)
    {
        if (_verboseLogging)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }
    
    /// <summary>
    /// Gets the current standard output content.
    /// </summary>
    /// <returns>The current content of the standard output buffer.</returns>
    public string GetCurrentOutput()
    {
        lock (_lock)
        {
            return _stdoutBuffer.ToString();
        }
    }
    
    /// <summary>
    /// Gets the current standard error content.
    /// </summary>
    /// <returns>The current content of the standard error buffer.</returns>
    public string GetCurrentError()
    {
        lock (_lock)
        {
            return _stderrBuffer.ToString();
        }
    }
    
    /// <summary>
    /// Gets the current merged output content.
    /// </summary>
    /// <returns>The current content of the merged output buffer.</returns>
    public string GetCurrentMergedOutput()
    {
        lock (_lock)
        {
            return _mergedBuffer.ToString();
        }
    }

    /// <summary>
    /// Clears the output buffers.
    /// </summary>
    public void ClearOutputs()
    {
        lock (_lock)
        {
            _stdoutBuffer.Clear();
            _stderrBuffer.Clear();
            _mergedBuffer.Clear();
        }
    }

    protected static ProcessStartInfo ConfigureProcessStartInfo(
        string fileName,
        string arguments)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

    /// <summary>
    /// Creates a RunnableProcessResult from the current state.
    /// </summary>
    protected virtual RunnableProcessResult CreateProcessResult(
        int exitCode,
        ProcessCompletionState completionState,
        DateTime startTime,
        ProcessErrorType? errorType = null,
        string? errorMessage = null,
        Exception? exception = null)
    {
        string stdout, stderr, merged;
        lock (_lock)
        {
            stdout = _stdoutBuffer.ToString();
            stderr = _stderrBuffer.ToString();
            merged = _mergedBuffer.ToString();
        }
        
        return new RunnableProcessResult(
            stdout,
            stderr,
            merged,
            exitCode,
            completionState,
            DateTime.Now - startTime,
            errorType,
            errorMessage,
            exception
        );
    }

    /// <summary>
    /// Throws if the process has already been started.
    /// </summary>
    protected void ThrowIfStarted()
    {
        if (_hasStarted)
        {
            var callstack = Environment.StackTrace;
            ConsoleHelpers.WriteLine($"Process has already been started. {callstack}");
            throw new InvalidOperationException("Cannot modify configuration after the process has been started.");
        }
    }

    #endregion

    #region private variables

    // Process and state management
    private Process? _process;
    private readonly object _lock = new object();
    protected bool _hasStarted;
    protected bool _hasExited;

    // Output collection
    protected readonly StringBuilder _stdoutBuffer = new StringBuilder();
    protected readonly StringBuilder _stderrBuffer = new StringBuilder();
    protected readonly StringBuilder _mergedBuffer = new StringBuilder();
    protected readonly ManualResetEvent _stdoutDoneSignal = new ManualResetEvent(false);
    protected readonly ManualResetEvent _stderrDoneSignal = new ManualResetEvent(false);

    // Callbacks
    protected Action<string>? _stdoutCallback;
    protected Action<string>? _stderrCallback;
    protected Action<string>? _mergedCallback;
    protected Action<ProcessEvent, string>? _eventCallback;
    protected Action<int>? _startedCallback;
    protected Action? _timeoutCallback;
    protected Action<int>? _exitCallback;

    // Configuration
    protected string _fileName = "";
    protected string _arguments = "";
    protected string? _workingDirectory;
    protected readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
    protected string? _standardInput;
    protected bool _standardInputClosed = false;
    protected bool _verboseLogging = false;

    // Timeout and cancellation
    protected int _timeoutMs = 30000; // Default: 30 seconds
    protected TimeoutStrategy _timeoutStrategy = TimeoutStrategy.Progressive;
    protected CancellationToken _cancellationToken = CancellationToken.None;

    #endregion
}