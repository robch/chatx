using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class CycoDevCommandLineOptions : CommandLineOptions
{
    public static bool Parse(string[] args, out CommandLineOptions? options, out CommandLineException? ex)
    {
        options = new CycoDevCommandLineOptions();

        var parsed = options.Parse(args, out ex);
        if (parsed && options.Commands.Count == 1)
        {
            var command = options.Commands.FirstOrDefault();
            var oneChatCommandWithNoInput =  command is ChatCommand chatCommand && chatCommand.InputInstructions.Count == 0;
            var inOrOutRedirected = Console.IsInputRedirected || Console.IsOutputRedirected;
            var implictlyUseStdIn = oneChatCommandWithNoInput && inOrOutRedirected;
            if (implictlyUseStdIn)
            {
                var stdinLines = ConsoleHelpers.GetAllLinesFromStdin();
                var joined = string.Join("\n", stdinLines);
                (command as ChatCommand)!.InputInstructions.Add(joined);
            }
        }

        return parsed;
    }


    override protected string PeekCommandName(string[] args, int i)
    {
        var name1 = GetInputOptionArgs(i, args, max: 1).FirstOrDefault();
        return name1 switch
        {
            "chat" => "chat",
            _ => base.PeekCommandName(args, i)
        };
    }

    override protected bool CheckPartialCommandNeedsHelp(string commandName)
    {
	    return commandName == "alias" ||
               commandName == "config" ||
               commandName == "github" ||
               commandName == "mcp" ||
               commandName == "prompt";
    }

    override protected Command? NewDefaultCommand()
    {
        return new ChatCommand();
    }

    override protected Command? NewCommandFromName(string commandName)
    {
        return commandName switch
        {
            "chat" => new ChatCommand(),
            "github login" => new GitHubLoginCommand(),
            "github models" => new GitHubModelsCommand(),
            "config list" => new ConfigListCommand(),
            "config get" => new ConfigGetCommand(),
            "config set" => new ConfigSetCommand(),
            "config clear" => new ConfigClearCommand(),
            "config add" => new ConfigAddCommand(),
            "config remove" => new ConfigRemoveCommand(),
            "alias list" => new AliasListCommand(),
            "alias get" => new AliasGetCommand(),
            "alias add" => new AliasAddCommand(),
            "alias delete" => new AliasDeleteCommand(),
            "prompt list" => new PromptListCommand(),
            "prompt get" => new PromptGetCommand(),
            "prompt create" => new PromptCreateCommand(),
            "prompt delete" => new PromptDeleteCommand(),
            "mcp list" => new McpListCommand(),
            "mcp get" => new McpGetCommand(),
            "mcp add" => new McpAddCommand(),
            "mcp remove" => new McpRemoveCommand(),
            _ => base.NewCommandFromName(commandName)
        };
    }

    override protected bool TryParseOtherCommandOptions(Command? command, string[] args, ref int i, string arg)
	{
		return TryParseChatCommandOptions(command as ChatCommand, args, ref i, arg) ||
               TryParseGitHubLoginCommandOptions(command as GitHubLoginCommand, args, ref i, arg) ||
               TryParseConfigCommandOptions(command as ConfigBaseCommand, args, ref i, arg) ||
               TryParseAliasCommandOptions(command as AliasBaseCommand, args, ref i, arg) ||
               TryParsePromptCommandOptions(command as PromptBaseCommand, args, ref i, arg) ||
               TryParseMcpCommandOptions(command as McpBaseCommand, args, ref i, arg);
    }
	
	override protected bool TryParseOtherCommandArg(Command? command, string arg)
    {
        var parsedOption = false;

        if (command is ConfigGetCommand configGetCommand && string.IsNullOrEmpty(configGetCommand.Key))
        {
            configGetCommand.Key = arg;
            parsedOption = true;
        }
        else if (command is ConfigClearCommand configClearCommand && string.IsNullOrEmpty(configClearCommand.Key))
        {
            configClearCommand.Key = arg;
            parsedOption = true;
        }
        else if (command is ConfigSetCommand configSetCommand)
        {
            if (string.IsNullOrEmpty(configSetCommand.Key))
            {
                configSetCommand.Key = arg;
                parsedOption = true;
            }
            else if (string.IsNullOrEmpty(configSetCommand.Value))
            {
                configSetCommand.Value = arg;
                parsedOption = true;
            }
        }
        else if (command is ConfigAddCommand configAddCommand)
        {
            if (string.IsNullOrEmpty(configAddCommand.Key))
            {
                configAddCommand.Key = arg;
                parsedOption = true;
            }
            else if (string.IsNullOrEmpty(configAddCommand.Value))
            {
                configAddCommand.Value = arg;
                parsedOption = true;
            }
        }
        else if (command is ConfigRemoveCommand configRemoveCommand)
        {
            if (string.IsNullOrEmpty(configRemoveCommand.Key))
            {
                configRemoveCommand.Key = arg;
                parsedOption = true;
            }
            else if (string.IsNullOrEmpty(configRemoveCommand.Value))
            {
                configRemoveCommand.Value = arg;
                parsedOption = true;
            }
        }
        else if (command is AliasGetCommand aliasGetCommand && string.IsNullOrEmpty(aliasGetCommand.AliasName))
        {
            aliasGetCommand.AliasName = arg;
            parsedOption = true;
        }
        else if (command is AliasAddCommand aliasAddCommand)
        {
            if (string.IsNullOrEmpty(aliasAddCommand.AliasName))
            {
                aliasAddCommand.AliasName = arg;
                parsedOption = true;
            }
            else if (string.IsNullOrEmpty(aliasAddCommand.Content))
            {
                // Just store the current argument as content
                // The proper handling of all content will be done in ExecuteAsync
                aliasAddCommand.Content = arg;
                parsedOption = true;
            }
        }
        else if (command is AliasDeleteCommand aliasDeleteCommand && string.IsNullOrEmpty(aliasDeleteCommand.AliasName))
        {
            aliasDeleteCommand.AliasName = arg;
            parsedOption = true;
        }
        else if (command is PromptCreateCommand createCommand && string.IsNullOrEmpty(createCommand.PromptName))
        {
            createCommand.PromptName = arg;
            parsedOption = true;
        }
        else if (command is PromptCreateCommand createCmd && string.IsNullOrEmpty(createCmd.PromptText))
        {
            createCmd.PromptText = arg;
            parsedOption = true;
        }
        else if (command is PromptDeleteCommand deleteCommand && string.IsNullOrEmpty(deleteCommand.PromptName))
        {
            deleteCommand.PromptName = arg;
            parsedOption = true;
        }
        else if (command is PromptGetCommand getCommand && string.IsNullOrEmpty(getCommand.PromptName))
        {
            getCommand.PromptName = arg;
            parsedOption = true;
        }
        else if (command is McpGetCommand mcpGetCommand && string.IsNullOrEmpty(mcpGetCommand.Name))
        {
            mcpGetCommand.Name = arg;
            parsedOption = true;
        }
        else if (command is McpAddCommand mcpAddCommand && string.IsNullOrEmpty(mcpAddCommand.Name))
        {
            mcpAddCommand.Name = arg;
            parsedOption = true;
        }
        else if (command is McpRemoveCommand mcpRemoveCommand && string.IsNullOrEmpty(mcpRemoveCommand.Name))
        {
            mcpRemoveCommand.Name = arg;
            parsedOption = true;
        }

        return parsedOption;
    }

    private bool TryParseConfigCommandOptions(ConfigBaseCommand? command, string[] args, ref int i, string arg)
    {
        if (command == null)
        {
            return false;
        }

        bool parsed = true;

        if (arg == "--file")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var configPath = ValidateOkFileName(max1Arg.FirstOrDefault());
            ConfigStore.Instance.LoadConfigFile(configPath);
            command.Scope = ConfigFileScope.FileName;
            command.ConfigFileName = configPath;
            i += max1Arg.Count();
        }
        else if (arg == "--global" || arg == "-g")
        {
            command.Scope = ConfigFileScope.Global;
            command.ConfigFileName = null;
        }
        else if (arg == "--user" || arg == "-u")
        {
            command.Scope = ConfigFileScope.User;
            command.ConfigFileName = null;
        }
        else if (arg == "--local" || arg == "-l")
        {
            command.Scope = ConfigFileScope.Local;
            command.ConfigFileName = null;
        }
        else if (arg == "--any" || arg == "-a")
        {
            command.Scope = ConfigFileScope.Any;
            command.ConfigFileName = null;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseAliasCommandOptions(AliasBaseCommand? command, string[] args, ref int i, string arg)
    {
        if (command == null)
        {
            return false;
        }

        bool parsed = true;

        if (arg == "--global" || arg == "-g")
        {
            command.Scope = ConfigFileScope.Global;
        }
        else if (arg == "--user" || arg == "-u")
        {
            command.Scope = ConfigFileScope.User;
        }
        else if (arg == "--local" || arg == "-l")
        {
            command.Scope = ConfigFileScope.Local;
        }
        else if (arg == "--any" || arg == "-a")
        {
            command.Scope = ConfigFileScope.Any;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParsePromptCommandOptions(PromptBaseCommand? command, string[] args, ref int i, string arg)
    {
        if (command == null)
        {
            return false;
        }

        bool parsed = true;

        if (arg == "--global" || arg == "-g")
        {
            command.Scope = ConfigFileScope.Global;
        }
        else if (arg == "--user" || arg == "-u")
        {
            command.Scope = ConfigFileScope.User;
        }
        else if (arg == "--local" || arg == "-l")
        {
            command.Scope = ConfigFileScope.Local;
        }
        else if (arg == "--any" || arg == "-a")
        {
            command.Scope = ConfigFileScope.Any;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseMcpCommandOptions(McpBaseCommand? command, string[] args, ref int i, string arg)
    {
        if (command == null)
        {
            return false;
        }

        bool parsed = true;

        if (arg == "--global" || arg == "-g")
        {
            command.Scope = ConfigFileScope.Global;
        }
        else if (arg == "--user" || arg == "-u")
        {
            command.Scope = ConfigFileScope.User;
        }
        else if (arg == "--local" || arg == "-l")
        {
            command.Scope = ConfigFileScope.Local;
        }
        else if (arg == "--any" || arg == "-a")
        {
            command.Scope = ConfigFileScope.Any;
        }
        else if (command is McpAddCommand cmdAddCommand && arg == "--command")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var cmdValue = ValidateString(arg, max1Arg.FirstOrDefault(), "command");
            cmdAddCommand.Command = cmdValue;
            cmdAddCommand.Transport = "stdio";
            i += max1Arg.Count();
        }
        else if (command is McpAddCommand urlAddCommand && arg == "--url")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var urlValue = ValidateString(arg, max1Arg.FirstOrDefault(), "url");
            urlAddCommand.Url = urlValue;
            urlAddCommand.Transport = "sse";
            i += max1Arg.Count();
        }
        else if (command is McpAddCommand argAddCommand && arg == "--arg")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var argValue = ValidateString(arg, max1Arg.FirstOrDefault(), "argument");
            argAddCommand.Args.Add(argValue!);
            i += max1Arg.Count();
        }
        else if (command is McpAddCommand argsAddCommand && arg == "--args")
        {
            var argArgs = GetInputOptionArgs(i + 1, args);
            var argValues = ValidateStrings(arg, argArgs, "argument");
            var needSplit = argValues.Count() == 1 && argValues.First().Contains(' ');
            argsAddCommand.Args.AddRange(needSplit
                ? ProcessHelpers.SplitArguments(argValues.First())
                : argValues);
            i += argArgs.Count();
        }
        else if (command is McpAddCommand envAddCommand && (arg == "--env" || arg == "-e"))
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var env = ValidateString(arg, max1Arg.FirstOrDefault(), "environment variable");
            envAddCommand.EnvironmentVars.Add(env!);
            i += max1Arg.Count();
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseChatCommandOptions(ChatCommand? command, string[] args, ref int i, string arg)
    {
        bool parsed = true;

        if (command == null)
        {
            parsed = false;
        }
        else if (arg == "--var")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var assignment = ValidateAssignment(arg, max1Arg.FirstOrDefault());
            command.Variables[assignment.Item1] = assignment.Item2;
            ConfigStore.Instance.SetFromCommandLine($"Var.{assignment.Item1}", assignment.Item2);
            i += max1Arg.Count();
        }
        else if (arg == "--vars")
        {
            var varArgs = GetInputOptionArgs(i + 1, args);
            var assignments = ValidateAssignments(arg, varArgs);
            foreach (var assignment in assignments)
            {
                command.Variables[assignment.Item1] = assignment.Item2;
                ConfigStore.Instance.SetFromCommandLine($"Var.{assignment.Item1}", assignment.Item2);
            }
            i += varArgs.Count();
        }
        else if (arg == "--foreach")
        {
            var foreachArgs = GetInputOptionArgs(i + 1, args).ToArray();
            var foreachVariable = ForEachVarHelpers.ParseForeachVarOption(foreachArgs, out var skipCount);
            command.ForEachVariables.Add(foreachVariable);
            this.Interactive = false;
            i += skipCount;
        }
        else if (arg == "--use-templates")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var useTemplates = max1Arg.FirstOrDefault() ?? "true";
            command.UseTemplates = useTemplates.ToLower() == "true" || useTemplates == "1";
            i += max1Arg.Count();
        }
        else if (arg == "--no-templates")
        {
            command.UseTemplates = false;
        }
        else if (arg == "--use-mcps" || arg == "--mcp")
        {
            var mcpArgs = GetInputOptionArgs(i + 1, args).ToList();
            i += mcpArgs.Count();

            var useAllMcps = mcpArgs.Count == 0;
            if (useAllMcps) mcpArgs.Add("*");

            command.UseMcps.AddRange(mcpArgs);
        }
        else if (arg == "--no-mcps")
        {
            command.UseMcps.Clear();
        }
        else if (arg == "--with-mcp")
        {
            var mcpCommandAndArgs = GetInputOptionArgs(i + 1, args);
            var mcpCommand = ValidateString(arg, mcpCommandAndArgs.FirstOrDefault(), "command to execute with MCP");
            var mcpName = $"mcp-{command.WithStdioMcps.Count + 1}";
            command.WithStdioMcps[mcpName] = new StdioMcpServerConfig
            {
                Command = mcpCommand!,
                Args = mcpCommandAndArgs.Skip(1).ToList(),
                Env = new Dictionary<string, string?>()
            };
            i += mcpCommandAndArgs.Count();
        }
        else if (arg == "--system-prompt")
        {
            var promptArgs = GetInputOptionArgs(i + 1, args);
            var prompt = ValidateString(arg, string.Join("\n\n", promptArgs), "system prompt");
            command.SystemPrompt = prompt;
            i += promptArgs.Count();
        }
        else if (arg == "--add-system-prompt")
        {
            var promptArgs = GetInputOptionArgs(i + 1, args);
            var prompt = ValidateString(arg, string.Join("\n\n", promptArgs), "additional system prompt");
            if (!string.IsNullOrEmpty(prompt))
            {
                command.SystemPromptAdds.Add(prompt);
            }
            i += promptArgs.Count();
        }
        else if (arg == "--add-user-prompt" || arg == "--prompt")
        {
            var promptArgs = GetInputOptionArgs(i + 1, args);
            var prompt = ValidateString(arg, string.Join("\n\n", promptArgs), "additional user prompt");
            if (!string.IsNullOrEmpty(prompt))
            {
                var needsSlashPrefix = arg == "--prompt" && !prompt.StartsWith("/");
                var prefix = needsSlashPrefix ? "/" : string.Empty;
                command.UserPromptAdds.Add($"{prefix}{prompt}");
            }
            i += promptArgs.Count();
        }
        else if (arg == "--input" || arg == "--instruction" || arg == "--question" || arg == "-q")
        {
            var inputArgs = GetInputOptionArgs(i + 1, args)
                .Select(x => FileHelpers.FileExists(x)
                    ? FileHelpers.ReadAllText(x)
                    : x);

            var isQuietNonInteractiveAlias = arg == "--question" || arg == "-q";
            if (isQuietNonInteractiveAlias)
            {
                this.Quiet = true;
                this.Interactive = false;
            }

            var implictlyUseStdIn = isQuietNonInteractiveAlias && inputArgs.Count() == 0;
            if (implictlyUseStdIn)
            {
                inputArgs = ConsoleHelpers.GetAllLinesFromStdin();
            }

            var joined = ValidateString(arg, string.Join("\n", inputArgs), "input");
            command.InputInstructions.Add(joined!);

            i += inputArgs.Count();
        }
        else if (arg == "--inputs" || arg == "--instructions" || arg == "--questions")
        {
            var inputArgs = GetInputOptionArgs(i + 1, args)
                .Select(x => FileHelpers.FileExists(x)
                    ? FileHelpers.ReadAllText(x)
                    : x);

            var isQuietNonInteractiveAlias = arg == "--questions";
            if (isQuietNonInteractiveAlias)
            {
                this.Quiet = true;
                this.Interactive = false;
            }

            var implictlyUseStdIn = isQuietNonInteractiveAlias && inputArgs.Count() == 0;
            if (implictlyUseStdIn)
            {
                inputArgs = ConsoleHelpers.GetAllLinesFromStdin();
            }

            var inputs = ValidateStrings(arg, inputArgs, "input", allowEmptyStrings: true);
            command.InputInstructions.AddRange(inputs);

            i += inputArgs.Count();
        }
        else if (arg == "--chat-history")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var chatHistory = max1Arg.FirstOrDefault() ?? DefaultSimpleChatHistoryFileName;
            command.InputChatHistory = !FileHelpers.FileExists(chatHistory) ? command.InputChatHistory : chatHistory;
            command.OutputChatHistory = chatHistory;
            command.LoadMostRecentChatHistory = false;
            i += max1Arg.Count();
        }
        else if (arg == "--input-chat-history")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var inputChatHistory = ValidateFileExists(max1Arg.FirstOrDefault());
            command.InputChatHistory = inputChatHistory;
            command.LoadMostRecentChatHistory = false;
            i += max1Arg.Count();
        }
        else if (arg == "--continue")
        {
            command.LoadMostRecentChatHistory = true;
            command.InputChatHistory = null;
        }
        else if (arg == "--output-chat-history")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var outputChatHistory = max1Arg.FirstOrDefault() ?? DefaultOutputChatHistoryFileNameTemplate;
            command.OutputChatHistory = outputChatHistory;
            i += max1Arg.Count();
        }
        else if (arg == "--output-trajectory")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var outputTrajectory = max1Arg.FirstOrDefault() ?? DefaultOutputTrajectoryFileNameTemplate;
            command.OutputTrajectory = outputTrajectory;
            i += max1Arg.Count();
        }
        else if (arg == "--use-anthropic")
        {
            ConfigStore.Instance.SetFromCommandLine(KnownSettings.AppPreferredProvider, "anthropic");
        }
        else if (arg == "--use-aws" || arg == "--use-bedrock" || arg == "--use-aws-bedrock")
        {
            ConfigStore.Instance.SetFromCommandLine(KnownSettings.AppPreferredProvider, "aws-bedrock");
        }
        else if (arg == "--use-azure-openai" || arg == "--use-azure")
        {
            ConfigStore.Instance.SetFromCommandLine(KnownSettings.AppPreferredProvider, "azure-openai");
        }
        else if (arg == "--use-google" || arg == "--use-gemini" || arg == "--use-google-gemini")
        {
            ConfigStore.Instance.SetFromCommandLine(KnownSettings.AppPreferredProvider, "google-gemini");
        }
        else if (arg == "--use-openai")
        {
            ConfigStore.Instance.SetFromCommandLine(KnownSettings.AppPreferredProvider, "openai");
        }
        else if (arg == "--use-copilot")
        {
            ConfigStore.Instance.SetFromCommandLine(KnownSettings.AppPreferredProvider, "copilot");
        }
        else if (arg == "--use-copilot-token")
        {
            ConfigStore.Instance.SetFromCommandLine(KnownSettings.AppPreferredProvider, "copilot-token");
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }

    private bool TryParseGitHubLoginCommandOptions(GitHubLoginCommand? command, string[] args, ref int i, string arg)
    {
        if (command == null)
        {
            return false;
        }

        bool parsed = true;

        if (arg == "--config")
        {
            var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
            var configPath = ValidateOkFileName(max1Arg.FirstOrDefault());
            ConfigStore.Instance.LoadConfigFile(configPath);
            command.Scope = ConfigFileScope.FileName;
            command.ConfigFileName = configPath;
            i += max1Arg.Count();
        }
        else if (arg == "--global" || arg == "-g")
        {
            command.Scope = ConfigFileScope.Global;
            command.ConfigFileName = null;
        }
        else if (arg == "--user" || arg == "-u")
        {
            command.Scope = ConfigFileScope.User;
            command.ConfigFileName = null;
        }
        else if (arg == "--local" || arg == "-l")
        {
            command.Scope = ConfigFileScope.Local;
            command.ConfigFileName = null;
        }
        else if (arg == "--any" || arg == "-a")
        {
            command.Scope = ConfigFileScope.Any;
            command.ConfigFileName = null;
        }
        else
        {
            parsed = false;
        }

        return parsed;
    }


    private const string DefaultSimpleChatHistoryFileName = "chat-history.jsonl";
    private const string DefaultOutputChatHistoryFileNameTemplate = "chat-history-{time}.jsonl";
    private const string DefaultOutputTrajectoryFileNameTemplate = "trajectory-{time}.md";
}
