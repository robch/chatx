using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

public class CheckExpectInstructionsHelper
{
    private static Dictionary<string, string> _cliCache = new Dictionary<string, string>();

    public static TestOutcome CheckExpectGptOutcome(string output, string expectGpt, string workingDirectory, out string gptStdOut, out string gptStdErr, out string gptMerged)
    {
        ConsoleHelpers.WriteDebugLine($"CheckExpectInstructionsHelper.CheckExpectGptOutcome: Checking for {expectGpt} in '{output}'");

        TestOutcome outcome;
        gptStdOut = string.Empty;
        gptStdErr = string.Empty;
        gptMerged = string.Empty;

        var question = new StringBuilder();
        question.AppendLine($"Here's the console output:\n\n{output}\n");
        question.AppendLine($"Here's the expectation:\n\n{expectGpt}\n");
        question.AppendLine("You **must always** answer \"PASS\" if the expectation is met.");
        question.AppendLine("You **must always** answer \"FAIL\" if the expectation is not met.");
        question.AppendLine("You **must only** answer \"PASS\" with no additional text if the expectation is met.");
        question.AppendLine("If you answer \"FAIL\", you **must** provide additional text to explain why the expectation was not met (without using the word \"PASS\" as we will interpret that as a \"PASS\").");
        var questionTempFile = FileHelpers.WriteTextToTempFile(question.ToString())!;

        try
        {
            var startProcess = FindCacheCli("cycod");
            var startArgs = $"--input @{questionTempFile} --interactive false --quiet";
            var commandLine = $"{startProcess} {startArgs}";

            ConsoleHelpers.WriteDebugLine($"CheckExpectInstructionsHelper.CheckExpectGptOutcome: RunnableProcessBuilder executing '{commandLine}'");
            var result = new RunnableProcessBuilder()
                .WithCommandLine(commandLine)
                .WithWorkingDirectory(workingDirectory)
                .WithTimeout(60000)
                .Run();

            gptStdOut = result.StandardOutput;
            gptStdErr = result.StandardError;
            gptMerged = result.MergedOutput;

            var exitedNotKilled = result.CompletionState == ProcessCompletionState.Completed;
            var exitedNormally = exitedNotKilled && result.ExitCode == 0;
            outcome = exitedNormally ? TestOutcome.Passed : TestOutcome.Failed;

            var timedoutOrKilled = !exitedNotKilled;
            if (timedoutOrKilled)
            {
                var message = "CheckExpectInstructionsHelper.CheckExpectGptOutcome: WARNING: Timedout or killed!";
                gptStdErr += $"\n{message}\n";
                gptMerged += $"\n{message}\n";
                ConsoleHelpers.WriteDebugLine(message);
            }
        }
        catch (Exception ex)
        {
            outcome = TestOutcome.Failed;

            var exception = $"CheckExpectInstructionsHelper.CheckExpectGptOutcome: EXCEPTION: {ex.Message}";
            gptStdErr += $"\n{exception}\n";
            gptMerged += $"\n{exception}\n";
            ConsoleHelpers.WriteDebugLine(exception);
        }

        File.Delete(questionTempFile);
        if (outcome == TestOutcome.Passed)
        {
            ConsoleHelpers.WriteDebugLine($"CheckExpectInstructionsHelper.CheckExpectGptOutcome: Checking for 'PASS' in '{gptMerged}'");

            var passed = gptMerged.Contains("PASS") || gptMerged.Contains("TRUE") || gptMerged.Contains("YES");
            outcome = passed ? TestOutcome.Passed : TestOutcome.Failed;

            ConsoleHelpers.WriteDebugLine($"CheckExpectInstructionsHelper.CheckExpectGptOutcome: {outcome}");
        }

        return outcome;
    }

    private static string FindCacheCli(string cli)
    {
        if (_cliCache.ContainsKey(cli))
        {
            return _cliCache[cli];
        }

        var found = FindCli(cli);
        _cliCache[cli] = found;

        return found;
    }

    private static string FindCli(string cli)
    {
        var specified = !string.IsNullOrEmpty(cli);
        if (specified)
        {
            var found = FindCliOrNull(cli);
            return found != null
                ? CliFound(cli, found)              // use what we found
                : CliNotFound(cli);                 // use what was specified
        }
        else
        {
            var clis = new[] { "cycod" };
            var found = PickCliOrNull(clis);
            return found != null
                ? PickCliFound(clis, found)         // use what we found
                : PickCliNotFound(clis, clis[0]);   // use cycod
        }
    }

    private static string? FindCliOrNull(string cli)
    {
        var dll = $"{cli}.dll";
        var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{cli}.exe" : cli;

        var path1 = Environment.GetEnvironmentVariable("PATH");
        var path2 = Directory.GetCurrentDirectory();
        var path3 = FileHelpers.GetProgramAssemblyFileInfo().DirectoryName;
        var path = $"{path3}{Path.PathSeparator}{path2}{Path.PathSeparator}{path1}";

        var paths = path.Split(Path.PathSeparator);
        foreach (var part2 in new string[]{ "", "net6.0"})
        {
            foreach (var part1 in paths)
            {
                var checkExe = Path.Combine(part1, part2, exe);
                if (File.Exists(checkExe))
                {
                    // Logger.TraceInfo($"FindCliOrNull: Found CLI: {checkExe}");
                    var checkDll = FindCliDllOrNull(checkExe, dll);
                    if (checkDll != null)
                    {
                        // Logger.TraceInfo($"FindCliOrNull: Found DLL: {checkDll}");
                        return checkExe;
                    }
                }
            }
        }

        return null;
    }

    private static string? FindCliDllOrNull(string cli, string dll)
    {
        var fi = new FileInfo(cli);
        if (!fi.Exists) return null;

        var check = Path.Combine(fi.DirectoryName!, dll);
        if (File.Exists(check)) return check;

        var matches = fi.Directory!.GetFiles(dll, SearchOption.AllDirectories);
        if (matches.Length == 1) return matches.First().FullName;

        return null;
    }

    private static string CliFound(string cli, string found)
    {
        ConsoleHelpers.WriteDebugLine($"CliFound: CLI specified ({cli}); found; using {found}");
        return found;
    }

    private static string CliNotFound(string cli)
    {
        var message = $"CliNotFound: CLI specified ({cli}); tried searching PATH and working directory; not found; using {cli}";
        ConsoleHelpers.WriteWarningLine(message);
        return cli;
    }

    private static string? PickCliOrNull(IEnumerable<string> clis)
    {
        var cliOrNulls = new List<string?>();
        foreach (var cli in clis)
        {
            cliOrNulls.Add(FindCliOrNull(cli));
        }

        var clisFound = cliOrNulls.Where(cli => !string.IsNullOrEmpty(cli));
        return clisFound.Count() == 1
            ? clisFound.First()
            : null;
    }

    private static string PickCliFound(IEnumerable<string> clis, string cli)
    {
        var message = $"PickCliFound: found CLI, using {cli}";
        ConsoleHelpers.WriteDebugLine(message);
        return cli;
    }

    private static string PickCliNotFound(IEnumerable<string> clis, string cli)
    {
        var message = $"PickCliNotFound: CLI not found; tried searching PATH and working directory; using {cli}";
        ConsoleHelpers.WriteDebugLine(message);
        return cli;
    }
}