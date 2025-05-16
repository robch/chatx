using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class CycoDmdCommandLineOptions : CommandLineOptions
{
    public static bool Parse(string[] args, out CommandLineOptions? options, out CommandLineException? ex)
    {
        options = new CycoDmdCommandLineOptions();
        return options.Parse(args, out ex);
    }

    override protected string PeekCommandName(string[] args, int i)
    {
        var name1 = GetInputOptionArgs(i, args, max: 1).FirstOrDefault();
        return name1 switch
        {
            "run" => "run",
            _ => base.PeekCommandName(args, i)
        };
    }

    override protected bool CheckPartialCommandNeedsHelp(string commandName)
    {
	    return commandName == "web";
    }

    override protected Command? NewDefaultCommand()
    {
        return new FindFilesCommand();
    }

    override protected Command? NewCommandFromName(string commandName)
    {
        return commandName switch
        {
            "web search" => new WebSearchCommand(),
            "web get" => new WebGetCommand(),
            "run" => new RunCommand(),
            _ => base.NewCommandFromName(commandName)
        };
    }

    override protected bool TryParseOtherCommandOptions(Command? command, string[] args, ref int i, string arg)
    {
        return TryParseFindFilesCommandOptions(command as FindFilesCommand, args, ref i, arg) ||
               TryParseWebCommandOptions(command as WebCommand, args, ref i, arg) ||
               TryParseRunCommandOptions(command as RunCommand, args, ref i, arg) ||
               TryParseSharedCycoDmdCommandOptions(command as CycoDmdCommand, args, ref i, arg);
    }

    private bool TryParseRunCommandOptions(RunCommand? command, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (command == null)
        {
            parsed = false;
        }
        else if (arg == "--script")
        {
            var scriptArgs = GetInputOptionArgs(i + 1, args);
            command.ScriptToRun = ValidateJoinedString(arg, command.ScriptToRun, scriptArgs, "\n", "command");
            command.Type = RunCommand.ScriptType.Default;
            i += scriptArgs.Count();
        }
        else if (arg == "--cmd")
        {
            var scriptArgs = GetInputOptionArgs(i + 1, args, 1);
            command.ScriptToRun = ValidateJoinedString(arg, command.ScriptToRun, scriptArgs, "\n", "command");
            command.Type = RunCommand.ScriptType.Cmd;
            i += scriptArgs.Count();
        }
        else if (arg == "--bash")
        {
            var scriptArgs = GetInputOptionArgs(i + 1, args, 1);
            command.ScriptToRun = ValidateJoinedString(arg, command.ScriptToRun, scriptArgs, "\n", "command");
            command.Type = RunCommand.ScriptType.Bash;
            i += scriptArgs.Count();
        }
        else if (arg == "--powershell")
        {
            var scriptArgs = GetInputOptionArgs(i + 1, args, 1);
            command.ScriptToRun = ValidateJoinedString(arg, command.ScriptToRun, scriptArgs, "\n", "command");
            command.Type = RunCommand.ScriptType.PowerShell;
            i += scriptArgs.Count();
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseFindFilesCommandOptions(FindFilesCommand? command, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (command == null)
        {
            parsed = false;
        }
        else if (arg == "--contains")
        {
            var patterns = GetInputOptionArgs(i + 1, args, required: 1);
            var asRegExs = ValidateRegExPatterns(arg, patterns);
            command.IncludeFileContainsPatternList.AddRange(asRegExs);
            command.IncludeLineContainsPatternList.AddRange(asRegExs);
            i += patterns.Count();
        }
        else if (arg == "--file-contains")
        {
            var patterns = GetInputOptionArgs(i + 1, args, required: 1);
            var asRegExs = ValidateRegExPatterns(arg, patterns);
            command.IncludeFileContainsPatternList.AddRange(asRegExs);
            i += patterns.Count();
        }
        else if (arg == "--file-not-contains")
        {
            var patterns = GetInputOptionArgs(i + 1, args, required: 1);
            var asRegExs = ValidateRegExPatterns(arg, patterns);
            command.ExcludeFileContainsPatternList.AddRange(asRegExs);
            i += patterns.Count();
        }
        else if (arg == "--line-contains")
        {
            var patterns = GetInputOptionArgs(i + 1, args, required: 1);
            var asRegExs = ValidateRegExPatterns(arg, patterns);
            command.IncludeLineContainsPatternList.AddRange(asRegExs);
            i += patterns.Count();
        }
        else if (arg == "--lines")
        {
            var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
            var count = ValidateLineCount(arg, countStr);
            command.IncludeLineCountBefore = count;
            command.IncludeLineCountAfter = count;
        }
        else if (arg == "--lines-before")
        {
            var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
            command.IncludeLineCountBefore = ValidateLineCount(arg, countStr);
        }
        else if (arg == "--lines-after")
        {
            var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
            command.IncludeLineCountAfter = ValidateLineCount(arg, countStr);
        }
        else if (arg == "--remove-all-lines")
        {
            var patterns = GetInputOptionArgs(i + 1, args);
            var asRegExs = ValidateRegExPatterns(arg, patterns);
            command.RemoveAllLineContainsPatternList.AddRange(asRegExs);
            i += patterns.Count();
        }
        else if (arg == "--line-numbers")
        {
            command.IncludeLineNumbers = true;
        }
        else if (arg.StartsWith("--") && arg.EndsWith("file-instructions"))
        {
            var instructions = GetInputOptionArgs(i + 1, args);
            if (instructions.Count() == 0)
            {
                throw new CommandLineException($"Missing instructions for {arg}");
            }
            var fileNameCriteria = arg != "--file-instructions"
                ? arg.Substring(2, arg.Length - 20)
                : string.Empty;
            var withCriteria = instructions.Select(x => Tuple.Create(x, fileNameCriteria));
            command.FileInstructionsList.AddRange(withCriteria);
            i += instructions.Count();
        }
        else if (arg == "--exclude")
        {
            var patterns = GetInputOptionArgs(i + 1, args);
            ValidateExcludeRegExAndGlobPatterns(arg, patterns, out var asRegExs, out var asGlobs);
            command.ExcludeFileNamePatternList.AddRange(asRegExs);
            command.ExcludeGlobs.AddRange(asGlobs);
            i += patterns.Count();
        }
        else if (arg == "--save-file-output")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var saveFileOutput = max1Arg.FirstOrDefault() ?? DefaultSaveFileOutputTemplate;
            command.SaveFileOutput = saveFileOutput;
            i += max1Arg.Count();
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseWebCommandOptions(WebCommand? command, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (command == null)
        {
            parsed = false;
        }
        else if (arg == "--interactive")
        {
            command.Interactive = true;
        }
        else if (arg == "--chromium")
        {
            command.Browser = BrowserType.Chromium;
        }
        else if (arg == "--firefox")
        {
            command.Browser = BrowserType.Firefox;
        }
        else if (arg == "--webkit")
        {
            command.Browser = BrowserType.Webkit;
        }
        else if (arg == "--strip")
        {
            command.StripHtml = true;
        }
        else if (arg == "--save-page-folder")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, 1);
            command.SaveFolder = max1Arg.FirstOrDefault() ?? "web-pages";
            i += max1Arg.Count();
        }
        else if (arg == "--bing")
        {
            command.SearchProvider = WebSearchProvider.Bing;
        }
        else if (arg == "--duck-duck-go" || arg == "--duckduckgo")
        {
            command.SearchProvider = WebSearchProvider.DuckDuckGo;
        }
        else if (arg == "--google")
        {
            command.SearchProvider = WebSearchProvider.Google;
        }
        else if (arg == "--yahoo")
        {
            command.SearchProvider = WebSearchProvider.Yahoo;
        }
        else if (arg == "--bing-api")
        {
            command.SearchProvider = WebSearchProvider.BingAPI;
        }
        else if (arg == "--google-api")
        {
            command.SearchProvider = WebSearchProvider.GoogleAPI;
        }
        else if (arg == "--get")
        {
            command.GetContent = true;
        }
        else if (arg == "--max")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, 1);
            command.MaxResults = ValidateInt(arg, max1Arg.FirstOrDefault(), "max results");
            i += max1Arg.Count();
        }
        else if (arg == "--exclude")
        {
            var patterns = GetInputOptionArgs(i + 1, args);
            var asRegExs = ValidateRegExPatterns(arg, patterns);
            command.ExcludeURLContainsPatternList.AddRange(asRegExs);
            i += patterns.Count();
        }
        else if (arg.StartsWith("--") && arg.EndsWith("page-instructions"))
        {
            var instructions = GetInputOptionArgs(i + 1, args);
            if (instructions.Count() == 0)
            {
                throw new CommandLineException($"Missing instructions for {arg}");
            }
            var webPageCriteria = arg != "--page-instructions"
                ? arg.Substring(2, arg.Length - 20)
                : string.Empty;
            var withCriteria = instructions.Select(x => Tuple.Create(x, webPageCriteria));
            command.PageInstructionsList.AddRange(withCriteria);
            i += instructions.Count();
        }
        else if (arg == "--save-page-output" ||arg == "--save-web-output" || arg == "--save-web-page-output")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var savePageOutput = max1Arg.FirstOrDefault() ?? DefaultSavePageOutputTemplate;
            command.SavePageOutput = savePageOutput;
            i += max1Arg.Count();
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseSharedCycoDmdCommandOptions(CycoDmdCommand? command, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (command == null)
        {
            parsed = false;
        }
        else if (arg == "--instructions")
        {
            var instructions = GetInputOptionArgs(i + 1, args);
            if (instructions.Count() == 0)
            {
                throw new CommandLineException($"Missing instructions for {arg}");
            }
            command.InstructionsList.AddRange(instructions);
            i += instructions.Count();
        }
        else if (arg == "--save-output")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var saveOutput = max1Arg.FirstOrDefault() ?? DefaultSaveOutputTemplate;
            command.SaveOutput = saveOutput;
            i += max1Arg.Count();
        }
        else if (arg == "--save-chat-history")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var saveChatHistory = max1Arg.FirstOrDefault() ?? AiInstructionProcessor.DefaultSaveChatHistoryTemplate;
            command.SaveChatHistory = saveChatHistory;
            i += max1Arg.Count();
        }
        else if (arg == "--built-in-functions")
        {
            command.UseBuiltInFunctions = true;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

	override protected bool TryParseOtherCommandArg(Command? command, string arg)
    {
        var parsedOption = false;

        if (command is FindFilesCommand findFilesCommand)
        {
            findFilesCommand.Globs.Add(arg);
            parsedOption = true;
        }
        else if (command is RunCommand runCommand)
        {
            runCommand.ScriptToRun = $"{runCommand.ScriptToRun}\n{arg}".Trim();
            parsedOption = true;
        }
        else if (command is WebSearchCommand webSearchCommand)
        {
            webSearchCommand.Terms.Add(arg);
            parsedOption = true;
        }
        else if (command is WebGetCommand webGetCommand)
        {
            webGetCommand.Urls.Add(arg);
            parsedOption = true;
        }

        return parsedOption;
    }

    public const string DefaultSaveFileOutputTemplate = "{filePath}/{fileBase}-output.md";
    public const string DefaultSavePageOutputTemplate = "{filePath}/{fileBase}-output.md";
    public const string DefaultSaveOutputTemplate = "output.md";
}