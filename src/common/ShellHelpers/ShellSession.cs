using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract class ShellSession
{
    public ShellSession()
    {
        lock (_sessions)
        {
            _sessions.Add(this);
        }
    }

    // Each derived class supplies its own shell type.
    protected abstract PersistentShellType GetShellType();

    // Makes sure the shell process is running.
    protected void EnsureProcess()
    {
        if (_shellProcess != null && !_shellProcess.HasExited)
            return;

        ConsoleHelpers.WriteDebugLine($"Starting {GetShellType()} shell...");
        var shellBuilder = new PersistentShellProcessBuilder()
            .WithShellType(GetShellType())
            .WithVerboseLogging(ConsoleHelpers.IsVerbose());

        if (ConsoleHelpers.IsVerbose())
        {
            shellBuilder.OnOutput(line => ConsoleHelpers.WriteLine(line, ConsoleColor.DarkMagenta));
            shellBuilder.OnError(line => ConsoleHelpers.WriteErrorLine(line));
        }

        _shellProcess = shellBuilder.Build();
        
        // Make sure the shell is started
        _shellProcess.EnsureStarted();
    }

    // Executes a command and waits for completion.
    public async Task<(string stdout, string stderr, string merged, int exitCode)> ExecuteCommandAsync(string command, int timeoutMs = 10000)
    {
        var isExit = command.Trim().ToLower() == "exit";
        if (isExit) return ResetShell(allShells: true);

        EnsureProcess();

        try
        {
            var commandBuilder = new PersistentShellCommandBuilder(_shellProcess!)
                .WithCommand(command)
                .WithTimeout(timeoutMs);

            ConsoleHelpers.WriteDebugLine($"Executing command: {command}");
            var result = await commandBuilder.RunAsync();

            return (result.StandardOutput, result.StandardError, result.MergedOutput, result.ExitCode);
        }
        catch (TimeoutException)
        {
            string errorMsg = $"<Command timed out after {timeoutMs}ms>";
            return ("", errorMsg, errorMsg, -1);
        }
        catch (Exception ex)
        {
            string errorMsg = $"<Error executing command: {ex.Message}>";
            return ("", errorMsg, errorMsg, -1);
        }
    }

    public void ForceShutdown()
    {
        _shellProcess?.ForceShutdown();
        _shellProcess = null;
    }

    public static void ShutdownAll()
    {
        lock (_sessions)
        {
            foreach (var session in _sessions)
            {
                session.ForceShutdown();
            }
            _sessions.Clear();
        }
    }

    private static (string stdout, string stderr, string merged, int exitCode) ResetShell(bool allShells = false)
    {
        if (allShells)
        {
            ShutdownAll();
            var shutdownAllShellsMessage = $"<All persistent shells have been closed... current working directory is now: {Environment.CurrentDirectory}>";
            return (shutdownAllShellsMessage, "", shutdownAllShellsMessage, 0);
        }

        var shutdownThisShellMessage = $"<Persistent shell has been closed... current working directory is now: {Environment.CurrentDirectory}>";
        return (shutdownThisShellMessage, "", shutdownThisShellMessage, 0);
    }

    protected PersistentShellProcess? _shellProcess;
    private static readonly List<ShellSession> _sessions = new List<ShellSession>();
}
