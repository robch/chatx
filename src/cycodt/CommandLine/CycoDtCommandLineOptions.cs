public class CycoDtCommandLineOptions : CommandLineOptions
{
    public static bool Parse(string[] args, out CommandLineOptions? options, out CommandLineException? ex)
    {
        options = new CycoDtCommandLineOptions();
        return options.Parse(args, out ex);
    }

    override protected bool CheckPartialCommandNeedsHelp(string commandName)
    {
	    return commandName == "expect";
    }

    override protected Command? NewCommandFromName(string commandName)
    {
        return commandName switch
        {
            "list" => new TestListCommand(),
            "run" => new TestRunCommand(),
            "expect check" => new ExpectCheckCommand(),
            "expect format" => new ExpectFormatCommand(),
            _ => base.NewCommandFromName(commandName)
        };
    }

    override protected bool TryParseOtherCommandOptions(Command? command, string[] args, ref int i, string arg)
    {
        return TryParseTestCommandOptions(command as TestBaseCommand, args, ref i, arg) ||
               TryParseExpectCommandOptions(command as ExpectBaseCommand, args, ref i, arg);
    }

    private bool TryParseExpectCommandOptions(ExpectBaseCommand? command, string[] args, ref int i, string arg)
    {
        if (command == null)
        {
            return false;
        }

        bool parsed = true;

        if (arg == "--input")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var input = ValidateString(arg, max1Arg.FirstOrDefault(), "input");
            command.Input = input!;
            i += max1Arg.Count();
        }
        else if (arg == "--save-output" || arg == "--output")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var output = ValidateString(arg, max1Arg.FirstOrDefault(), "output");
            command.Output = output;
            i += max1Arg.Count();
        }
        else if (arg == "--append")
        {
            command.Append = true;
        }
        else if (command is ExpectFormatCommand formatCommand && arg == "--strict")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var strictStr = ValidateString(arg, max1Arg.FirstOrDefault(), "strict");
            if (bool.TryParse(strictStr, out bool strict))
            {
                formatCommand.Strict = strict;
            }
            i += max1Arg.Count();
        }
        else if (command is ExpectCheckCommand checkCommand && arg == "--regex")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var pattern = ValidateString(arg, max1Arg.FirstOrDefault(), "regex pattern");
            checkCommand.RegexPatterns.Add(pattern!);
            i += max1Arg.Count();
        }
        else if (command is ExpectCheckCommand checkCommand3 && arg == "--not-regex")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var pattern = ValidateString(arg, max1Arg.FirstOrDefault(), "not-regex pattern");
            checkCommand3.NotRegexPatterns.Add(pattern!);
            i += max1Arg.Count();
        }
        else if (command is ExpectCheckCommand checkCommand5 && arg == "--instructions")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var instructions = ValidateString(arg, max1Arg.FirstOrDefault(), "instructions");
            checkCommand5.Instructions = instructions;
            i += max1Arg.Count();
        }
        else if (command is ExpectCheckCommand checkCommand7 && arg == "--format")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var format = ValidateString(arg, max1Arg.FirstOrDefault(), "format");
            var allowedFormats = new[] { "TEXT", "JSON" };
            if (!allowedFormats.Contains(format!.ToUpperInvariant()))
            {
                throw new CommandLineException($"Invalid format for --format: {format}. Allowed values: TEXT, JSON");
            }
            checkCommand7.Format = format!;
            i += max1Arg.Count();
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseTestCommandOptions(TestBaseCommand? command, string[] args, ref int i, string arg)
    {
        if (command == null)
        {
            return false;
        }

        bool parsed = true;

        if (arg == "--file")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var filePattern = ValidateString(arg, max1Arg.FirstOrDefault(), "file pattern");
            command.Globs.Add(filePattern!);
            i += max1Arg.Count();
        }
        else if (arg == "--files")
        {
            var filePatterns = GetInputOptionArgs(i + 1, args);
            var validPatterns = ValidateStrings(arg, filePatterns, "file patterns");
            command.Globs.AddRange(validPatterns);
            i += filePatterns.Count();
        }
        else if (arg == "--exclude-files" || arg == "--exclude")
        {
            var patterns = GetInputOptionArgs(i + 1, args);
            ValidateExcludeRegExAndGlobPatterns(arg, patterns, out var asRegExs, out var asGlobs);
            command.ExcludeFileNamePatternList.AddRange(asRegExs);
            command.ExcludeGlobs.AddRange(asGlobs);
            i += patterns.Count();
        }
        else if (arg == "--test")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var testName = ValidateString(arg, max1Arg.FirstOrDefault(), "test name");
            command.Tests.Add(testName!);
            i += max1Arg.Count();
        }
        else if (arg == "--tests")
        {
            var testNames = GetInputOptionArgs(i + 1, args);
            var validTests = ValidateStrings(arg, testNames, "test names");
            command.Tests.AddRange(validTests);
            i += testNames.Count();
        }
        else if (arg == "--contains")
        {
            var containPatterns = GetInputOptionArgs(i + 1, args);
            var validContains = ValidateStrings(arg, containPatterns, "contains patterns");
            command.Contains.AddRange(validContains);
            i += containPatterns.Count();
        }
        else if (arg == "--remove")
        {
            var removePatterns = GetInputOptionArgs(i + 1, args);
            var validRemove = ValidateStrings(arg, removePatterns, "remove patterns");
            command.Remove.AddRange(validRemove);
            i += removePatterns.Count();
        }
        else if (arg == "--include-optional")
        {
            var optionalCategories = GetInputOptionArgs(i + 1, args);
            var validCategories = optionalCategories.Any()
                ? ValidateStrings(arg, optionalCategories, "optional categories")
                : new List<string> { string.Empty };
            command.IncludeOptionalCategories.AddRange(validCategories);
            i += optionalCategories.Count();
        }
        else if (command is TestRunCommand runCommand && arg == "--output-file")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var outputFile = ValidateString(arg, max1Arg.FirstOrDefault(), "output file");
            runCommand.OutputFile = outputFile;
            i += max1Arg.Count();
        }
        else if (command is TestRunCommand runCommand2 && arg == "--output-format")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var format = ValidateString(arg, max1Arg.FirstOrDefault(), "output format");
            var allowedFormats = new[] { "trx", "junit" };
            if (!allowedFormats.Contains(format))
            {
                throw new CommandLineException($"Invalid format for --output-format: {format}. Allowed values: trx, junit");
            }
            runCommand2.OutputFormat = format!;
            i += max1Arg.Count();
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }
}
