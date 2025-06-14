using System.Text.RegularExpressions;

public class CommandLineOptions
{
    protected CommandLineOptions()
    {
        Interactive = true;

        Debug = false;
        Verbose = false;
        Quiet = false;

        HelpTopic = string.Empty;
        ExpandHelpTopics = false;

        Commands = new List<Command>();

        AllOptions = new string[0];
        SaveAliasName = null;

        ThreadCount = 0;
        WorkingDirectory = null;
    }

    public bool Interactive;

    public bool Debug;
    public bool Verbose;
    public bool Quiet;

    public string HelpTopic;
    public bool ExpandHelpTopics;

    public int ThreadCount { get; private set; }
    public string? WorkingDirectory { get; set; }
    public List<Command> Commands;

    public string[] AllOptions;
    public string? SaveAliasName;
    public ConfigFileScope? SaveAliasScope;

    virtual public bool Parse(string[] args, out CommandLineException? ex)
    {
        ex = null;

        try
        {
            var allInputs = this.ExpandedInputsFromCommandLine(args);
            this.ParseInputOptions(allInputs);
            return this.Commands.Any();
        }
        catch (CommandLineException e)
        {
            ex = e;
            return false;
        }
    }

    virtual protected string PeekCommandName(string[] args, int i)
    {
        var name1 = GetInputOptionArgs(i, args, max: 1).FirstOrDefault();
        var name2 = GetInputOptionArgs(i + 1, args, max: 1).FirstOrDefault();
        var commandName = name1 switch
        {
            "version" => "version",
            "help" => "help",
            _ => $"{name1} {name2}".Trim()
        };
        return commandName;
    }

    virtual protected bool CheckPartialCommandNeedsHelp(string commandName)
    {
        return false;
    }

    virtual protected Command? NewCommandFromName(string commandName)
    {
        return commandName switch
        {
            "help" => new HelpCommand(),
            "version" => new VersionCommand(),
            _ => null
        };
    }

    virtual protected Command? NewDefaultCommand()
    {
        return null;
    }

    virtual protected bool TryParseOtherCommandOptions(Command? command, string[] args, ref int i, string arg)
    {
        return false;
    }

    virtual protected bool TryParseOtherCommandArg(Command? command, string arg)
    {
        return false;
    }

    protected IEnumerable<string> ExpandedInputsFromCommandLine(string[] args)
    {
        return args.SelectMany(arg => ExpandedInput(arg));
    }

    protected IEnumerable<string> ExpandedInput(string input)
    {
        return input.StartsWith("@@")
            ? ExpandedAtAtFileInput(input)
            : input.StartsWith("@")
                ? [ExpandedAtFileInput(input)]
                : [input];
    }

    protected IEnumerable<string> ExpandedAtAtFileInput(string input)
    {
        if (!input.StartsWith("@@")) throw new ArgumentException("Not an @@ file input");

        var fileName = input.Substring(2);
        var fileNameOk = FileHelpers.FileExists(fileName);
        if (fileNameOk)
        {
            var lines = ConsoleHelpers.IsStandardInputReference(fileName)
                ? ConsoleHelpers.GetAllLinesFromStdin()
                : File.ReadLines(fileName);

            return lines.SelectMany(line => ExpandedInput(line));
        }

        return [input];
    }

    protected string ExpandedAtFileInput(string input)
    {
        if (!input.StartsWith("@"))
        {
            throw new ArgumentException("Not an @ file input");
        }

        return AtFileHelpers.ExpandAtFileValue(input);
    }

    protected void ParseInputOptions(IEnumerable<string> allInputs)
    {
        Command? command = null;

        var args = this.AllOptions = allInputs.ToArray();

        // Make a pass to deference all aliases.
        for (int i = 0; i < args.Count(); i++)
        {
            if (args[i].StartsWith("--"))
            {
                ExpandAliasOptions(ref command, ref args, ref i, args[i].Substring(2));
            }
        }

        for (int i = 0; i < args.Count(); i++)
        {
            var parsed = TryParseInputOptions(ref command, args, ref i, args[i]);
            if (!parsed)
            {
                for (var j = 0; j < i; j++) ConsoleHelpers.WriteDebugLine($"arg[{j}] = {args[j]}");
                ConsoleHelpers.WriteDebugLine($"(INVALID) arg[{i}] = {args[i]}");
                throw InvalidArgException(command, args[i]);
            }
        }

        if (args.Count() == 0)
        {
            command = NewDefaultCommand();
        }

        if (string.IsNullOrEmpty(this.HelpTopic) && command != null && command.IsEmpty())
        {
            this.HelpTopic = command.GetHelpTopic();
        }

        if (command != null && !command.IsEmpty())
        {
            this.Commands.Add(command.Validate());
        }
    }

    protected bool TryParseKnownSettingOption(string[] args, ref int i, string arg)
    {
        // Check if this is a known setting CLI option
        if (KnownSettingsCLIParser.TryParseCLIOption(arg, out string? settingName, out string? value))
        {
            // If value is in the same arg (e.g. --setting=value)
            if (value != null)
            {
                // Add to configuration store
                ConfigStore.Instance.SetFromCommandLine(settingName!, value);
                ConsoleHelpers.WriteDebugLine($"Set known setting from CLI: {settingName} = {value}");
                return true;
            }

            // Otherwise, get one or more args
            var allowMultipleArgs = KnownSettings.IsMultiValue(settingName!);
            var arguments = allowMultipleArgs
                ? GetInputOptionArgs(i + 1, args).ToList()
                : GetInputOptionArgs(i + 1, args, max: 1).ToList();

            if (arguments.Count == 0)
            {
                throw new CommandLineException($"Missing value for {arg}");
            }
            else if (arguments.Count == 1)
            {
                // If there's only one argument, use it directly as a string
                var settingValue = arguments[0];

                // Add to configuration store
                ConfigStore.Instance.SetFromCommandLine(settingName!, settingValue);
                ConsoleHelpers.WriteDebugLine($"Set known setting from CLI: {settingName} = {settingValue}");
            }
            else
            {
                // If there are multiple arguments, use them as a list
                ConfigStore.Instance.SetFromCommandLine(settingName!, arguments);
                ConsoleHelpers.WriteDebugLine($"Set known setting from CLI: {settingName} = [{string.Join(", ", arguments)}]");
            }

            i += arguments.Count;
            return true;
        }

        return false;
    }

    protected bool TryParseInputOptions(ref Command? command, string[] args, ref int i, string arg)
    {
        var isEndOfCommand = arg == "--" && command != null && !command.IsEmpty();
        if (isEndOfCommand)
        {
            this.Commands.Add(command!.Validate());
            command = null;
            return true;
        }

        var needNewCommand = command == null;
        if (needNewCommand)
        {
            var commandName = PeekCommandName(args, i);
            var partialCommandNeedsHelp = CheckPartialCommandNeedsHelp(commandName);
            if (partialCommandNeedsHelp)
            {
                command = new HelpCommand();
                this.HelpTopic = commandName;
                return true;
            }

            command = NewCommandFromName(commandName);

            var parsedCommand = command != null;
            if (parsedCommand)
            {
                var skipHowManyExtraArgs = commandName.Count(x => x == ' ');
                i += skipHowManyExtraArgs;
                return true;
            }

            command = NewDefaultCommand();
        }

        var parsedOption = TryParseGlobalCommandLineOptions(args, ref i, arg) ||
            TryParseHelpCommandOptions(command as HelpCommand, args, ref i, arg) ||
            TryParseVersionCommandOptions(command as VersionCommand, args, ref i, arg) ||
            TryParseOtherCommandOptions(command, args, ref i, arg) ||
            TryParseSharedCommandOptions(command, args, ref i, arg) ||
            TryParseKnownSettingOption(args, ref i, arg);
        if (parsedOption) return true;

        if (arg == "--help")
        {
            this.HelpTopic = command?.GetHelpTopic() ?? "usage";
            command = new HelpCommand();
            i = args.Count();
            parsedOption = true;
        }
        else if (arg == "--version")
        {
            command = new VersionCommand();
            i = args.Count();
            parsedOption = true;
        }
        else if (command is HelpCommand helpCommand)
        {
            this.HelpTopic = $"{this.HelpTopic} {arg}".Trim();
            parsedOption = true;
        }

        if (!parsedOption && TryParseOtherCommandArg(command, arg))
        {
            parsedOption = true;
        }

        if (!parsedOption)
        {
            ConsoleHelpers.WriteDebugLine($"Unknown command line option: {arg}");
        }

        return parsedOption;
    }

    protected void ExpandAliasOptions(ref Command? command, ref string[] args, ref int currentIndex, string alias)
    {
        var aliasFilePath = AliasFileHelpers.FindAliasFile(alias);
        if (aliasFilePath != null && File.Exists(aliasFilePath))
        {
            var aliasLines = File.ReadAllLines(aliasFilePath);

            var aliasArgs = aliasLines
                .Select(x => x.StartsWith('@')
                    ? AtFileHelpers.ExpandAtFileValue(x)
                    : x)
                .ToArray();

            args = args.Take(currentIndex).Concat(aliasArgs).Concat(args.Skip(currentIndex + 1)).ToArray();
        }
    }

    protected bool TryParseGlobalCommandLineOptions(string[] args, ref int i, string arg)
    {
        var parsed = true;

        if (arg == "--and")
        {
            // ignore --and ... used when combining @@ files
        }
        else if (arg == "--interactive")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var interactive = max1Arg.FirstOrDefault() ?? "true";
            this.Interactive = interactive.ToLower() == "true" || interactive == "1";
            i += max1Arg.Count();
        }
        else if (arg == "--debug")
        {
            this.Debug = true;
            ConsoleHelpers.ConfigureDebug(true);
        }
        else if (arg == "--verbose")
        {
            this.Verbose = true;
        }
        else if (arg == "--quiet")
        {
            this.Quiet = true;
        }
        else if (arg == "--save-local-alias")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var aliasName = max1Arg.FirstOrDefault() ?? throw new CommandLineException("Missing alias name for --save-alias");
            this.SaveAliasName = aliasName;
            this.SaveAliasScope = ConfigFileScope.Local;
            i += max1Arg.Count();
        }
        else if (arg == "--save-alias" || arg == "--save-user-alias")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var aliasName = max1Arg.FirstOrDefault() ?? throw new CommandLineException("Missing alias name for --save-user-alias");
            this.SaveAliasName = aliasName;
            this.SaveAliasScope = ConfigFileScope.User;
            i += max1Arg.Count();
        }
        else if (arg == "--save-global-alias")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var aliasName = max1Arg.FirstOrDefault() ?? throw new CommandLineException("Missing alias name for --save-global-alias");
            this.SaveAliasName = aliasName;
            this.SaveAliasScope = ConfigFileScope.Global;
            i += max1Arg.Count();
        }
        else if (arg == "--profile")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var profileName = ValidateString(arg, max1Arg.FirstOrDefault(), "profile name");
            ProfileFileHelpers.LoadProfile(profileName!);
            i += max1Arg.Count();
        }
        else if (arg == "--config")
        {
            var configFiles = GetInputOptionArgs(i + 1, args);
            ConfigStore.Instance.LoadConfigFiles(ValidateFilesExist(configFiles));
            i += configFiles.Count();
        }
        else if (arg == "--threads")
        {
            var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
            this.ThreadCount = ValidateInt(arg, countStr, "thread count");
        }
        else if (arg == "--working-dir" || arg == "--folder" || arg == "--dir" || arg == "--cwd")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var dirPath = ValidateString(arg, max1Arg.FirstOrDefault(), "directory path");
            this.WorkingDirectory = dirPath;
            i += max1Arg.Count();
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    protected bool TryParseHelpCommandOptions(HelpCommand? helpCommand, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (helpCommand == null)
        {
            parsed = false;
        }
        else if (arg == "--expand")
        {
            this.ExpandHelpTopics = true;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    protected bool TryParseVersionCommandOptions(VersionCommand? versionCommand, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (versionCommand == null)
        {
            parsed = false;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }


    protected bool TryParseSharedCommandOptions(Command? command, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (command == null)
        {
            parsed = false;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    protected IEnumerable<string> GetInputOptionArgs(int startAt, string[] args, int max = int.MaxValue, int required = 0)
    {
        var found = 0;
        for (int i = startAt; i < args.Length && i - startAt < max; i++, found++)
        {
            if (args[i].StartsWith("--") && found >= required)
            {
                yield break;
            }

            yield return args[i];
        }
    }

    protected string? ValidateString(string arg, string? argStr, string argDescription)
    {
        if (string.IsNullOrEmpty(argStr))
        {
            throw new CommandLineException($"Missing {argDescription} for {arg}");
        }

        return argStr;
    }

    protected IEnumerable<string> ValidateStrings(string arg, IEnumerable<string> argStrs, string argDescription, bool allowEmptyStrings = false)
    {
        var strings = argStrs.ToList();
        if (!strings.Any())
        {
            throw new CommandLineException($"Missing {argDescription} for {arg}");
        }

        return strings.Select(x => allowEmptyStrings ? x : ValidateString(arg, x, argDescription)!);
    }

    protected static string ValidateJoinedString(string arg, string seed, IEnumerable<string> values, string separator, string argDescription)
    {
        seed = string.Join(separator, values.Prepend(seed)).Trim();
        if (string.IsNullOrEmpty(seed))
        {
            throw new CommandLineException($"Missing {argDescription} for {arg}");
        }

        return seed;
    }

    protected (string, string) ValidateAssignment(string arg, string? assignment)
    {
        assignment = ValidateString(arg, assignment, "assignment")!;

        var parts = assignment.Split('=', 2);
        if (parts.Length != 2)
        {
            throw new CommandLineException($"Invalid variable definition for {arg}: {assignment}. Use NAME=VALUE format.");
        }

        return (parts[0], parts[1]);
    }

    protected IEnumerable<(string, string)> ValidateAssignments(string arg, IEnumerable<string> assignments)
    {
        if (!assignments.Any())
        {
            throw new CommandLineException($"Missing variable assignments for {arg}");
        }

        return assignments.Select(x => ValidateAssignment(arg, x));
    }

    protected string ValidateOkFileName(string? arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            throw new CommandLineException("Missing file name");
        }

        return arg;
    }

    protected IEnumerable<string> ValidateFilesExist(IEnumerable<string> args)
    {
        args = args.ToList();
        foreach (var arg in args)
        {
            if (string.IsNullOrEmpty(arg))
            {
                throw new CommandLineException("Missing file name");
            }

            if (!File.Exists(arg))
            {
                throw new CommandLineException($"File does not exist: {arg}");
            }
        }

        return args;
    }

    protected string? ValidateFileExists(string? arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            throw new CommandLineException("Missing file name");
        }

        if (!File.Exists(arg))
        {
            throw new CommandLineException($"File does not exist: {arg}");
        }

        return arg;
    }

    protected IEnumerable<Regex> ValidateRegExPatterns(string arg, IEnumerable<string> patterns)
    {
        patterns = patterns.ToList();
        if (!patterns.Any())
        {
            throw new CommandLineException($"Missing regular expression patterns for {arg}");
        }

        return patterns.Select(x => ValidateRegExPattern(arg, x));
    }

    protected Regex ValidateRegExPattern(string arg, string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (Exception)
        {
            throw new CommandLineException($"Invalid regular expression pattern for {arg}: {pattern}");
        }
    }

    protected void ValidateExcludeRegExAndGlobPatterns(string arg, IEnumerable<string> patterns, out List<Regex> asRegExs, out List<string> asGlobs)
    {
        if (patterns.Count() == 0)
        {
            throw new CommandLineException($"Missing patterns for {arg}");
        }

        var containsSlash = (string x) => x.Contains('/') || x.Contains('\\');
        asRegExs = patterns
            .Where(x => !containsSlash(x))
            .Select(x => ValidateFilePatternToRegExPattern(arg, x))
            .ToList();
        asGlobs = patterns
            .Where(x => containsSlash(x))
            .ToList();
    }

    protected Regex ValidateFilePatternToRegExPattern(string arg, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new CommandLineException($"Missing file pattern for {arg}");
        }

        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        var patternPrefix = isWindows ? "(?i)^" : "^";
        var regexPattern = patternPrefix + pattern
            .Replace(".", "\\.")
            .Replace("*", ".*")
            .Replace("?", ".") + "$";

        try
        {
            return new Regex(regexPattern, RegexOptions.CultureInvariant);
        }
        catch (Exception)
        {
            throw new CommandLineException($"Invalid file pattern for {arg}: {pattern}");
        }
    }

    protected int ValidateLineCount(string arg, string? countStr)
    {
        return ValidateInt(arg, countStr, "line count");
    }

    protected int ValidateInt(string arg, string? countStr, string argDescription)
    {
        if (string.IsNullOrEmpty(countStr))
        {
            throw new CommandLineException($"Missing {argDescription} for {arg}");
        }

        if (!int.TryParse(countStr, out var count))
        {
            throw new CommandLineException($"Invalid {argDescription} for {arg}: {countStr}");
        }

        return count;
    }

    protected CommandLineException InvalidArgException(Command? command, string arg)
    {
        var message = $"Invalid argument: {arg}";
        return new CommandLineException(message, command?.GetHelpTopic() ?? "");
    }
}
