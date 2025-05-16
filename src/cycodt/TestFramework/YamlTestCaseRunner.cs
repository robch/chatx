using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

public class YamlTestCaseRunner
{
    public static IEnumerable<TestResult> TestCaseGetResults(TestCase test, string cli, string? command, string? script, bool scriptIsBash, string? arguments, string? input, string? expectGpt, string? expectRegex, string? notExpectRegex, string? env, string? workingDirectory, int timeout, bool skipOnFailure, string? @foreach)
    {
        Logger.Log($"YamlTestCaseRunner.TestCaseGetResults: ENTER");

        foreach (var foreachItem in ExpandForEachGroups(@foreach))
        {
            var result = TestCaseGetResult(test, cli, command, script, scriptIsBash, foreachItem, arguments, input, expectGpt, expectRegex, notExpectRegex, env, workingDirectory!, timeout, skipOnFailure);
            if (!string.IsNullOrEmpty(foreachItem) && foreachItem != "{}")
            {
                result.DisplayName = GetTestResultDisplayName(test.DisplayName, foreachItem);
            }
            yield return result;
        }

        Logger.Log($"YamlTestCaseRunner.TestCaseGetResults: EXIT");
    }

    #region private methods

    private static TestResult TestCaseGetResult(TestCase test, string cli, string? command, string? script, bool scriptIsBash, string? foreachItem, string? arguments, string? input, string? expectGpt, string? expectRegex, string? notExpectRegex, string? env, string workingDirectory, int timeout, bool skipOnFailure)
    {
        var start = DateTime.Now;

        var outcome = RunTestCase(skipOnFailure, cli, command, script, scriptIsBash, foreachItem, arguments, input, expectGpt, expectRegex, notExpectRegex, env, workingDirectory, timeout, out string stdOut, out string stdErr, out string errorMessage, out string stackTrace, out string additional, out string debugTrace);

        var stop = DateTime.Now;
        return CreateTestResult(test, start, stop, stdOut, stdErr, errorMessage, stackTrace, additional, debugTrace, outcome);
    }

    private static string GetTestResultDisplayName(string testDisplayName, string json)
    {
        var testResultDisplayName = testDisplayName;

        var document = JsonDocument.Parse(json);
        if(document.RootElement.ValueKind == JsonValueKind.Object)
        {
            var root = document.RootElement;
            foreach(var property in root.EnumerateObject())
            {
                var keys = property.Name.Split(new char[] { '\t' });
                var values = property.Value.GetString()!.Split(new char[] { '\t' });

                for (int i = 0; i < keys.Length; i++)
                {
                    if (testResultDisplayName.Contains("{" + keys[i] + "}"))
                    {
                        testResultDisplayName = testResultDisplayName.Replace("{" +keys[i] + "}", values[i]);
                    }
                }
            }
        }

        // if the testDisplayName was not templatized, ie, it had no {}
        if (testResultDisplayName == testDisplayName)
        {
            return $"{testDisplayName}: {RedactSensitiveDataFromForeachItem(json)}";
        }

        return testResultDisplayName;
    }

    private static string RedactSensitiveDataFromForeachItem(string foreachItem)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });
        writer.WriteStartObject();

        var foreachObject = JsonDocument.Parse(foreachItem).RootElement;
        foreach (var item in foreachObject.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(item.Value.ToString()))
            {
                continue;
            }
            var keys = item.Name.ToLower().Split(new char[] {'\t'});
            
            //find index of "token" in foreach key and redact its value to avoid getting it displayed
            var tokenIndex = Array.IndexOf(keys, "token");
            var valueString = item.Value.GetRawText();
            
            if (tokenIndex >= 0)
            {
                var values = item.Value.ToString().Split(new char[] {'\t'});
                if (values.Count() == keys.Count())
                {
                    values[tokenIndex] = "***";
                    valueString = string.Join("\t", values);
                }
            }
            writer.WritePropertyName(item.Name);
            writer.WriteRawValue(valueString);
        }

        writer.WriteEndObject();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static IEnumerable<string> ExpandForEachGroups(string? @foreach)
    {
        var kvs = KeyValuePairsFromJson(@foreach, false)
            .Select(kv => new KeyValuePair<string, IEnumerable<string>>(
                kv.Key,
                kv.Value.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)));

        var dicts = new[] { new Dictionary<string, string>() }.ToList();
        foreach (var item in kvs)
        {
            var lines = item.Value;
            dicts = lines.SelectMany(
                line => dicts.Select(
                    d => DupAndAdd(d, item.Key, line)))
                .ToList();
        }

        Logger.Log($"YamlTestCaseRunner.ExpandForEachGroups: expanded count = {dicts.Count()}");

        return dicts.Select(d =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false
            };
            return JsonSerializer.Serialize(d, options);
        });
    }

    private static Dictionary<string, string> DupAndAdd(Dictionary<string, string> d, string key, string value)
    {
        var dup = new Dictionary<string, string>(d);
        dup[key] = value;
        return dup;
    }

    private static TestOutcome RunTestCase(bool skipOnFailure, string cli, string? command, string? script, bool scriptIsBash, string? @foreach, string? arguments, string? input, string? expectGpt, string? expectRegex, string? notExpectRegex, string? env, string workingDirectory, int timeout, out string stdOut, out string stdErr, out string errorMessage, out string stackTrace, out string additional, out string debugTrace)
    {
        var outcome = TestOutcome.None;

        additional = $"START TIME: {DateTime.Now}";
        debugTrace = "";
        stackTrace = script ?? string.Empty;

        var merged = string.Empty;
        stdOut = string.Empty;
        stdErr = string.Empty;

        try
        {
            var isCommand = !string.IsNullOrEmpty(command) || string.IsNullOrEmpty(script);
            var isScript = !string.IsNullOrEmpty(script) && string.IsNullOrEmpty(command);
            var ok = isCommand || isScript;
            if (!ok) throw new Exception("Neither command nor script specified!");

            var kvs = KeyValuePairsFromJson(arguments, true);
            kvs.AddRange(KeyValuePairsFromJson(@foreach, false));
            stackTrace = UpdateStackTrace(stackTrace, command, kvs);

            var startArgs = GetKeyValueArgs(kvs);
            var envVars = YamlEnvHelpers.GetEnvironmentFromMultiLineString(env);

            var result = isCommand
                ? ProcessHelpers.RunProcess($"{command} {startArgs}", workingDirectory, envVars, input, timeout)
                : ProcessHelpers.RunShellScript(scriptIsBash ? "bash" : "cmd", script!, startArgs, workingDirectory, envVars, input, timeout);

            stdOut = result!.StandardOutput;
            stdErr = result.StandardError;
            merged = result.MergedOutput;

            var exitCode = result.ExitCode;
            var completed = result.CompletionState == ProcessCompletionState.Completed;
            var completedSuccessfully = completed && exitCode == 0;

            outcome = completedSuccessfully
                ? TestOutcome.Passed
                : skipOnFailure
                    ? TestOutcome.Skipped
                    : TestOutcome.Failed;

            var exitCodeText = completed
                ? exitCode.ToString()
                : $"(did not exit; timedout; killed)";
            var exitTime = DateTime.Now.ToString();

            errorMessage = $"EXIT CODE: {exitCodeText}";
            additional = additional
                + $" STOP TIME: {exitTime}"
                + $" EXIT CODE: {exitCodeText}";

            var checkRegExExpectations = outcome == TestOutcome.Passed && (!string.IsNullOrEmpty(expectRegex) || !string.IsNullOrEmpty(notExpectRegex));
            if (checkRegExExpectations) outcome = CheckExpectRegExPatterns(merged, expectRegex, notExpectRegex);
        }
        catch (Exception ex)
        {
            outcome = TestOutcome.Failed;
            errorMessage = ex.Message;
            debugTrace = ex.ToString();
            stackTrace = $"{stackTrace}\n{ex.StackTrace}";
        }
            
        return outcome == TestOutcome.Passed && !string.IsNullOrEmpty(expectGpt)
            ? CheckExpectGptOutcome(merged, expectGpt, workingDirectory, ref stdOut, ref stdErr)
            : outcome;
    }

    private static List<KeyValuePair<string, string>> KeyValuePairsFromJson(string? json, bool allowSimpleString)
    {
        var kvs = new List<KeyValuePair<string, string>>();
        if (!string.IsNullOrEmpty(json))
        {
            Logger.Log($"KeyValuePairsFromJson: 'json'='{json}'");
            using JsonDocument document = JsonDocument.Parse(json);
            var parsed = document.RootElement;
            if (parsed.ValueKind == JsonValueKind.String && allowSimpleString)
            {
                // if it's a simple string, there is no "key" for the argument... pass it as value with an empty string as key
                // this will ensure that an additional '--' isn't emitted preceding the string-only arguments
                kvs.Add(new KeyValuePair<string, string>("", parsed.GetString() ?? string.Empty));
            }
            else if (parsed.ValueKind != JsonValueKind.Object)
            {
                // if it's not a simple string, it must be an object... if it's not, we'll just log and continue
                Logger.Log("KeyValuePairsFromJson: Invalid json (only supports `\"string\"`, or `{\"mapItem1\": \"value1\", \"...\": \"...\"}`!");
            }
            else
            {
                foreach (var item in parsed.EnumerateObject())
                {
                    kvs.Add(new KeyValuePair<string, string>(item.Name, item.Value.GetString() ?? string.Empty));
                }
            }
        }
        return kvs;
    }

    private static string UpdateStackTrace(string stackTrace, string? command, List<KeyValuePair<string, string>> kvs)
    {
        if (command?.EndsWith("dev shell") ?? false)
        {
            var devShellRunBashScriptArguments = string.Join("\n", kvs
                .Where(kv => kv.Key switch { "run" => true, "bash" => true, "script" => true, _ => false })
                .Select(kv => !string.IsNullOrEmpty(kv.Key)
                    ? $"{kv.Key}:\n{kv.Value.Replace("\n", "\n  ")}"
                    : kv.Value));
            if (!string.IsNullOrEmpty(devShellRunBashScriptArguments))
            {
                stackTrace = $"{stackTrace}\n{devShellRunBashScriptArguments}".Trim('\r', '\n', ' ');
            }
        }
        else if (command != null)
        {
            var commandArguments = string.Join("\n", kvs
                .Where(kv => !string.IsNullOrEmpty(kv.Key))
                .Select(kv => $"{kv.Key}:\n{kv.Value.Replace("\n", "\n  ")}"));
            stackTrace = !string.IsNullOrEmpty(commandArguments)
                ? $"{stackTrace}\nCOMMAND: {command}\n{commandArguments}".Trim('\r', '\n', ' ')
                : $"{stackTrace}\nCOMMAND: {command}".Trim('\r', '\n', ' ');
        }

        return stackTrace;
    }

    private static string WriteMultilineTsvToTempFile(string text, ref List<string>? files)
    {
        files ??= new List<string>();

        var lines = text.Split('\r', '\n');
        var newLines = new List<string>();
        foreach (var line in lines)
        {
            if (!line.Contains('\f'))
            {
                newLines.Add(line);
                continue;
            }

            var values = line.Split('\t');
            var newValues = new List<string>();
            foreach (var value in values)
            {
                if (!value.Contains('\f'))
                {
                    newValues.Add(value);
                    continue;
                }

                var newValue = FileHelpers.WriteTextToTempFile(value.Replace('\f', '\n'))!;
                files.Add(newValue);

                newValues.Add($"@{newValue}");
            }

            newLines.Add(string.Join("\t", newValues));
        }

        var newText = string.Join("\n", newLines);
        var file = FileHelpers.WriteTextToTempFile(newText);
        files.Add(file!);
        return file!;
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
        Logger.Log($"CliFound: CLI specified ({cli}); found; using {found}");
        return found;
    }

    private static string CliNotFound(string cli)
    {
        var message = $"CliNotFound: CLI specified ({cli}); tried searching PATH and working directory; not found; using {cli}";
        Logger.LogWarning(message);
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
        Logger.LogInfo(message);
        return cli;
    }

    private static string PickCliNotFound(IEnumerable<string> clis, string cli)
    {
        var message = $"PickCliNotFound: CLI not found; tried searching PATH and working directory; using {cli}";
        Logger.LogInfo(message);
        return cli;
    }


    private static string GetKeyValueArgs(List<KeyValuePair<string, string>> kvs)
    {
        var args = new StringBuilder();
        foreach (var item in kvs)
        {
            if (!string.IsNullOrEmpty(item.Key))
            {
                if (item.Key.Contains('\t'))
                {
                    var key = item.Key.Replace('\t', ';');
                    args.Append($"--foreach {key} in ");
                }
                else
                {
                    args.Append($"--{item.Key} ");
                }
                
                if (!string.IsNullOrEmpty(item.Value))
                {
                    args.Append($"\"{item.Value}\" ");
                }
            }
            else if (!string.IsNullOrEmpty(item.Value))
            {
                args.Append(item.Value);
            }
        }
        return args.ToString().TrimEnd();
    }

    private static TestResult CreateTestResult(TestCase test, DateTime start, DateTime stop, string stdOut, string stdErr, string errorMessage, string stackTrace, string additional, string debugTrace, TestOutcome outcome)
    {
        Logger.Log($"YamlTestCaseRunner.CreateTestResult({test.DisplayName})");

        var result = new TestResult(test) { Outcome = outcome };
        result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, stdOut));
        result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, stdErr));
        result.Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, additional));
        result.Messages.Add(new TestResultMessage(TestResultMessage.DebugTraceCategory, debugTrace));
        result.ErrorMessage = errorMessage;
        result.ErrorStackTrace = stackTrace;
        result.StartTime = start;
        result.EndTime = stop;
        result.Duration = stop - start;

        Logger.Log("----------------------------\n\n");
        Logger.Log($"    STDOUT: {stdOut}");
        Logger.Log($"    STDERR: {stdErr}");
        Logger.Log($"     STACK: {stackTrace}");
        Logger.Log($"     ERROR: {errorMessage}");
        Logger.Log($"   OUTCOME: {outcome}");
        Logger.Log($"ADDITIONAL: {additional}");
        Logger.Log($"DEBUGTRACE: {debugTrace}");
        Logger.Log("----------------------------\n\n");

        return result;
    }

    private static TestOutcome CheckExpectGptOutcome(string output, string expectGpt, string workingDirectory, ref string stdOut, ref string stdErr)
    {
        var outcome = ExpectGptOutcome(output, expectGpt, workingDirectory, out var gptStdOut, out var gptStdErr, out var gptMerged);
        if (outcome == TestOutcome.Failed)
        {
            if (!string.IsNullOrEmpty(gptStdOut)) stdOut = $"{stdOut}\n--expect--\n{gptStdOut}\n".Trim('\n');
            if (!string.IsNullOrEmpty(gptStdErr)) stdErr = $"{stdErr}\n--expect--\n{gptStdErr}\n".Trim('\n');
        }
        return outcome;
    }

    private static TestOutcome ExpectGptOutcome(string output, string expectGpt, string workingDirectory, out string gptStdOut, out string gptStdErr, out string gptMerged)
    {
        return CheckExpectInstructionsHelper.CheckExpectGptOutcome(output, expectGpt, workingDirectory, out gptStdOut, out gptStdErr, out gptMerged);
    }

    private static TestOutcome CheckExpectRegExPatterns(string output, string? expectRegex, string? notExpectRegex)
    {
        var expected = ExpectHelper.CheckOutput(output, expectRegex, notExpectRegex);
        return expected ? TestOutcome.Passed : TestOutcome.Failed;
    }

    #endregion

    private static Dictionary<string, string> _cliCache = new Dictionary<string, string>();
}
