using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

abstract public class ProgramRunner
{
    protected async Task<int> RunProgramAsync(string[] args)
    {
        try
        {
            SaveConsoleColor();
            return await DoProgram(ProcessDirectives(args));
        }
        catch (EnvVarSettingException ex)
        {
            ConsoleHelpers.WriteLine($"Error: {ex.Message}");
            return 2;
        }
        catch (GitHubTokenExpiredException)
        {
            return 1;
        }
        catch (CalcException ex)
        {
            ExceptionHelpers.SaveAndDisplayException(ex);
            return 1;
        }
        catch (Exception ex)
        {
            ExceptionHelpers.SaveAndDisplayException(ex);
            return 1;
        }
        finally
        {
            RestoreConsoleColor();
        }
    }

    abstract protected bool ParseCommandLine(string[] args, out CommandLineOptions? commandLineOptions, out CommandLineException? ex);

    private async Task<int> DoProgram(string[] args)
    {
        if (!ParseCommandLine(args, out var commandLineOptions, out var ex))
        {
            DisplayBanner();
            if (ex != null)
            {
                DisplayException(ex);
                HelpHelpers.DisplayUsage(ex.GetHelpTopic());
                return 2;
            }
            else
            {
                HelpHelpers.DisplayUsage(commandLineOptions!.HelpTopic);
                return 1;
            }
        }

        var debug = ConsoleHelpers.IsDebug() || commandLineOptions!.Debug;
        var verbose = ConsoleHelpers.IsVerbose() || commandLineOptions!.Verbose;
        var quiet = ConsoleHelpers.IsQuiet() || commandLineOptions!.Quiet;
        ConsoleHelpers.Configure(debug, verbose, quiet);

        var helpCommand = commandLineOptions!.Commands.OfType<HelpCommand>().FirstOrDefault();
        if (helpCommand != null)
        {
            DisplayBanner();
            HelpHelpers.DisplayHelpTopic(commandLineOptions.HelpTopic, commandLineOptions.ExpandHelpTopics);
            return 0;
        }

        var shouldSaveAlias = !string.IsNullOrEmpty(commandLineOptions.SaveAliasName);
        if (shouldSaveAlias)
        {
            var filesSaved = AliasFileHelpers.SaveAlias(
                commandLineOptions.SaveAliasName!,
                commandLineOptions.AllOptions,
                commandLineOptions.SaveAliasScope ?? ConfigFileScope.Local);

            DisplayBanner();
            AliasDisplayHelpers.DisplaySavedAliasFiles(filesSaved);

            return 0;
        }

        var shouldSetWorkingDir = !string.IsNullOrEmpty(commandLineOptions.WorkingDirectory);
        if (shouldSetWorkingDir)
        {
            DirectoryHelpers.EnsureDirectoryExists(commandLineOptions.WorkingDirectory!);
            Directory.SetCurrentDirectory(commandLineOptions.WorkingDirectory!);
        }

        var threadCountMax = commandLineOptions.ThreadCount;
        var parallelism = threadCountMax > 0 ? threadCountMax : Environment.ProcessorCount;

        var inOrOutRedirected = Console.IsInputRedirected || Console.IsOutputRedirected;
        var isTrulyInteractive = commandLineOptions.Interactive && !inOrOutRedirected;
        if (isTrulyInteractive && parallelism > 1)
        {
            ConsoleHelpers.WriteDebugLine($"Max 1 thread in truly interactive mode");
            parallelism = 1;
        }

        var allTasks = new List<Task<object>>();
        var throttler = new SemaphoreSlim(parallelism);

        var commands = ForEachVarHelpers.ExpandForEachVars(commandLineOptions.Commands).ToList();
        foreach (var command in commands)
        {
            throttler.Wait();

            var needDelayForTemplateFileNameUniqueness = !isTrulyInteractive;
            if (needDelayForTemplateFileNameUniqueness)
            {
                var delay = TimeSpan.FromMilliseconds(10);
                await Task.Delay(delay);
            }

            var startedTaskNotAwaited = command.ExecuteAsync(isTrulyInteractive);
            allTasks.Add(WrapRunAndRelease(throttler, startedTaskNotAwaited));
        }

        await Task.WhenAll(allTasks.ToArray());
        ConsoleHelpers.DisplayStatusErase();

        return ExitCodeFromResults(allTasks);
    }

    private static Task<object> WrapRunAndRelease(SemaphoreSlim throttler, Task<object> startedTask)
    {
        return Task.Run(async () =>
        {
            try
            {
                return await startedTask;
            }
            finally
            {
                throttler.Release();
            }
        });
    }

    private static int ExitCodeFromResults(List<Task<object>> allTasks)
    {
        var exitCode = allTasks
            .Where(t => t.Result is int)
            .Select(t => (int)t.Result)
            .Where(t => t != 0)
            .FirstOrDefault();

        var soFarSoGood = exitCode == 0;
        var checkNullStrings = soFarSoGood;
        if (checkNullStrings)
        {
            var nullStrings = allTasks
                .Where(t => t.Result is string)
                .Select(t => (string)t.Result)
                .Where(t => string.IsNullOrEmpty(t))
                .ToList();
            var stringsOk = nullStrings.Count == 0;
            exitCode = stringsOk ? 0 : 1;
        }

        return exitCode;
    }

    private static void DisplayBanner()
    {
        var programNameUppercase = ProgramInfo.Name.ToUpper();
        var programDescription = ProgramInfo.Description;
        ConsoleHelpers.WriteLine(
            $"{programNameUppercase} - {programDescription}, Version {VersionInfo.GetVersion()}\n" +
            "Copyright(c) 2025, Rob Chambers. All rights reserved.\n");
    }

    private static void DisplayException(CommandLineException ex)
    {
        var printMessage = !string.IsNullOrEmpty(ex.Message);
        if (printMessage)
        {
            ConsoleHelpers.WriteWarning($"{ex.Message}");
            ConsoleHelpers.Write("\n\n", overrideQuiet: true);
        }
    }

    private static string[] ProcessDirectives(string[] args)
    {
        ConsoleHelpers.WriteDebugLine($"ProcessDirectives: {string.Join(" ", args)}");
        args = CheckWaitForDebugger(args);
        args = CheckDebug(args);
        ConsoleHelpers.WriteDebugLine($"ProcessDirectives: exiting w/ {string.Join(" ", args)}");
        return args;
    }

    private static string[] CheckWaitForDebugger(string[] args)
    {
        var debug = args.Length >= 2 && args[0] == "debug" && args[1] == "wait";
        if (debug)
        {
            args = args.Skip(2).ToArray();
            ConsoleHelpers.Write("Waiting for debugger...", overrideQuiet: true);
            for (; !Debugger.IsAttached; Thread.Sleep(200))
            {
                ConsoleHelpers.Write(".");
            }
            ConsoleHelpers.WriteLine();
        }

        return args;
    }

    private static string[] CheckDebug(string[] args)
    {
        var debug = args.Length >= 1 && args[0] == "debug";
        if (debug)
        {
            args = args.Skip(1).ToArray();
            ConsoleHelpers.ConfigureDebug(true);
        }

        return args;
    }

    private static void SaveConsoleColor()
    {
        _originalForegroundColor = Console.ForegroundColor;
        _originalBackgroundColor = Console.BackgroundColor;
    }
    
    private static void RestoreConsoleColor()
    {
        Console.ForegroundColor = _originalForegroundColor;
        Console.BackgroundColor = _originalBackgroundColor;
    }

    private static ConsoleColor _originalForegroundColor;
    private static ConsoleColor _originalBackgroundColor;
}