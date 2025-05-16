using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Builder for configuring and creating RunnableProcess instances.
/// </summary>
public class RunnableProcessBuilder
{
    /// <summary>
    /// Creates a new RunnableProcessBuilder.
    /// </summary>
    public RunnableProcessBuilder()
    {
    }

    /// <summary>
    /// Sets the file name (executable) to run.
    /// </summary>
    /// <param name="fileName">The file name of the executable.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    /// <summary>
    /// Sets the arguments for the process.
    /// </summary>
    /// <param name="arguments">The arguments string.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithArguments(string arguments)
    {
        _arguments = arguments;
        return this;
    }

    /// <summary>
    /// Sets the arguments for the process from an array of strings.
    /// </summary>
    /// <param name="arguments">The arguments as an array of strings.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithArguments(string[] arguments)
    {
        if (arguments == null || arguments.Length == 0)
        {
            _arguments = string.Empty;
            return this;
        }

        var escapedArgs = new string[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
        {
            escapedArgs[i] = ProcessHelpers.EscapeProcessArgument(arguments[i]);
        }

        _arguments = string.Join(" ", escapedArgs);
        return this;
    }

    /// <summary>
    /// Sets the arguments from a key-value dictionary, where each entry becomes a --key value pair.
    /// </summary>
    /// <param name="arguments">Dictionary of arguments.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithArguments(Dictionary<string, string> arguments)
    {
        if (arguments == null || arguments.Count == 0)
        {
            _arguments = string.Empty;
            return this;
        }

        var argsList = new List<string>();
        foreach (var kvp in arguments)
        {
            var key = kvp.Key.StartsWith("--") ? kvp.Key : $"--{kvp.Key}";
            argsList.Add(ProcessHelpers.EscapeProcessArgument(key));
            argsList.Add(ProcessHelpers.EscapeProcessArgument(kvp.Value));
        }

        _arguments = string.Join(" ", argsList);
        return this;
    }

    /// <summary>
    /// Sets the command line which will be parsed into file name and arguments.
    /// </summary>
    /// <param name="commandLine">The full command line.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithCommandLine(string commandLine)
    {
        ConsoleHelpers.WriteDebugLine($"RunnableProcessBuilder: WithCommandLine: '{commandLine}'");
        if (string.IsNullOrEmpty(commandLine))
        {
            _fileName = string.Empty;
            _arguments = string.Empty;
            return this;
        }

        ProcessHelpers.SplitCommand(commandLine, out string fileName, out string arguments);
        _fileName = fileName;
        _arguments = arguments;
        return this;
    }

    /// <summary>
    /// Sets the working directory for the process.
    /// </summary>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithWorkingDirectory(string? workingDirectory)
    {
        var ok = !string.IsNullOrEmpty(workingDirectory);
        if (ok) _workingDirectory = workingDirectory;
        return this;
    }

    /// <summary>
    /// Sets an environment variable for the process.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithEnvironmentVariable(string name, string value)
    {
        _environmentVariables[name] = value;
        return this;
    }

    /// <summary>
    /// Sets environment variables for the process.
    /// </summary>
    /// <param name="variables">Dictionary of environment variables.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithEnvironmentVariables(Dictionary<string, string>? variables)
    {
        variables ??= new Dictionary<string, string>();
        foreach (var kvp in variables)
        {
            _environmentVariables[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Sets a timeout for the process.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithTimeout(int? timeoutMs)
    {
        var ok = timeoutMs.HasValue;
        if (ok) _timeoutMs = timeoutMs!.Value;
        return this;
    }

    /// <summary>
    /// Sets a cancellation token for the process.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return this;
    }

    /// <summary>
    /// Sets the timeout strategy for the process.
    /// </summary>
    /// <param name="strategy">The timeout strategy.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithTimeoutStrategy(TimeoutStrategy strategy)
    {
        _timeoutStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets standard input text for the process.
    /// </summary>
    /// <param name="input">The input text.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithStandardInput(string? input)
    {
        _standardInput = input;
        return this;
    }

    /// <summary>
    /// Enables or disables verbose logging.
    /// </summary>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder WithVerboseLogging(bool verbose = true)
    {
        _verboseLogging = verbose;
        return this;
    }

    /// <summary>
    /// Sets a callback for standard output data.
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of output.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder OnOutput(Action<string> callback)
    {
        _stdoutCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback for standard error data.
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of error output.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder OnError(Action<string> callback)
    {
        _stderrCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback for all output data (both stdout and stderr).
    /// </summary>
    /// <param name="callback">The callback to invoke for each line of output.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder OnMergedOutput(Action<string> callback)
    {
        _mergedCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback for process events.
    /// </summary>
    /// <param name="callback">The callback to invoke for process events.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder OnEvent(Action<ProcessEvent, string> callback)
    {
        _eventCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback for when the process starts.
    /// </summary>
    /// <param name="callback">The callback to invoke when the process starts.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder OnStarted(Action<int> callback)
    {
        _startedCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback for when the process times out.
    /// </summary>
    /// <param name="callback">The callback to invoke when the process times out.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder OnTimeout(Action callback)
    {
        _timeoutCallback = callback;
        return this;
    }

    /// <summary>
    /// Sets a callback for when the process exits.
    /// </summary>
    /// <param name="callback">The callback to invoke when the process exits.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public RunnableProcessBuilder OnExit(Action<int> callback)
    {
        _exitCallback = callback;
        return this;
    }

    /// <summary>
    /// Builds a configured RunnableProcess instance.
    /// </summary>
    /// <returns>A configured RunnableProcess.</returns>
    public RunnableProcess Build()
    {
        if (string.IsNullOrEmpty(_fileName))
        {
            throw new InvalidOperationException("File name must be specified");
        }

        // Create the runnable process
        var process = new RunnableProcess(_fileName, _arguments ?? string.Empty);

        // Set all properties
        if (_workingDirectory != null)
        {
            process.SetWorkingDirectory(_workingDirectory);
        }

        if (_environmentVariables.Count > 0)
        {
            process.SetEnvironmentVariables(_environmentVariables);
        }

        if (_standardInput != null)
        {
            process.SetStandardInput(_standardInput);
        }

        process.SetTimeout(_timeoutMs);
        process.SetTimeoutStrategy(_timeoutStrategy);
        process.SetCancellationToken(_cancellationToken);
        process.SetVerboseLogging(_verboseLogging);

        // Set callbacks
        if (_stdoutCallback != null)
        {
            process.SetStdoutCallback(_stdoutCallback);
        }

        if (_stderrCallback != null)
        {
            process.SetStderrCallback(_stderrCallback);
        }

        if (_mergedCallback != null)
        {
            process.SetMergedOutputCallback(_mergedCallback);
        }

        if (_eventCallback != null)
        {
            process.SetEventCallback(_eventCallback);
        }

        if (_startedCallback != null)
        {
            process.SetStartedCallback(_startedCallback);
        }

        if (_timeoutCallback != null)
        {
            process.SetTimeoutCallback(_timeoutCallback);
        }

        if (_exitCallback != null)
        {
            process.SetExitCallback(_exitCallback);
        }

        return process;
    }

    /// <summary>
    /// Builds a configured RunnableProcess instance and runs it.
    /// </summary>
    /// <returns>The result of the process execution.</returns>
    public RunnableProcessResult Run()
    {
        return Build().Run();
    }

    /// <summary>
    /// Builds a configured RunnableProcess instance and runs it asynchronously.
    /// </summary>
    /// <returns>A task that completes with the process result.</returns>
    public Task<RunnableProcessResult> RunAsync()
    {
        return Build().RunAsync();
    }

    private string? _fileName;
    private string? _arguments;
    private string? _workingDirectory;
    private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
    private string? _standardInput;
    private int _timeoutMs = 30000; // Default 30 seconds
    private TimeoutStrategy _timeoutStrategy = TimeoutStrategy.Progressive;
    private CancellationToken _cancellationToken = CancellationToken.None;
    private bool _verboseLogging = false;

    private Action<string>? _stdoutCallback;
    private Action<string>? _stderrCallback;
    private Action<string>? _mergedCallback;
    private Action<ProcessEvent, string>? _eventCallback;
    private Action<int>? _startedCallback;
    private Action? _timeoutCallback;
    private Action<int>? _exitCallback;
}