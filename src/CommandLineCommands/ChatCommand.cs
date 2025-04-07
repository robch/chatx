using Microsoft.Extensions.AI;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using chatx.FunctionCalling;
using chatx.McpHelpers;
using ModelContextProtocol.Client;

public class ChatCommand : Command
{
    public ChatCommand()
    {
    }

    override public bool IsEmpty()
    {
        return false;
    }

    override public string GetCommandName()
    {
        return "";
    }

    public ChatCommand Clone()
    {
        var clone = new ChatCommand();
        
        // Copy all properties
        clone.SystemPrompt = this.SystemPrompt;
        clone.SystemPromptAdds = new List<string>(this.SystemPromptAdds);
        clone.UserPromptAdds = new List<string>(this.UserPromptAdds);
        clone.TrimTokenTarget = this.TrimTokenTarget;
        clone.MaxOutputTokens = this.MaxOutputTokens;
        clone.LoadMostRecentChatHistory = this.LoadMostRecentChatHistory;
        clone.InputChatHistory = this.InputChatHistory;
        clone.OutputChatHistory = this.OutputChatHistory;
        clone.OutputTrajectory = this.OutputTrajectory;
        clone.InputInstructions = new List<string>(this.InputInstructions);
        clone.UseTemplates = this.UseTemplates;
        
        // Deep copy variables dictionary
        clone.Variables = new Dictionary<string, string>(this.Variables);
        
        return clone;
    }

    public async Task<int> ExecuteAsync(bool interactive)
    {
        // Transfer known settings to the command if not already set
        var maxOutputTokensSetting = ConfigStore.Instance.GetFromAnyScope(KnownSettings.AppMaxTokens);
        var useMaxOutputTokenSetting = !MaxOutputTokens.HasValue && maxOutputTokensSetting.AsInt() > 0;
        if (useMaxOutputTokenSetting) MaxOutputTokens = maxOutputTokensSetting.AsInt();

        // Ground the filenames (in case they're templatized, or auto-save is enabled).
        InputChatHistory = ChatHistoryFileHelpers.GroundInputChatHistoryFileName(InputChatHistory, LoadMostRecentChatHistory);
        OutputChatHistory = ChatHistoryFileHelpers.GroundOutputChatHistoryFileName(OutputChatHistory);
        OutputTrajectory = ChatHistoryFileHelpers.GroundOutputTrajectoryFileName(OutputTrajectory);

        // Ground the system prompt, added user messages, and InputInstructions.
        SystemPrompt = GroundSystemPrompt();
        UserPromptAdds = GroundUserPromptAdds();
        InputInstructions = GroundInputInstructions();

        // Create the function factory and add functions.
        var factory = new McpFunctionFactory();
        factory.AddFunctions(new DateAndTimeHelperFunctions());
        factory.AddFunctions(new ShellCommandToolHelperFunctions());
        factory.AddFunctions(new StrReplaceEditorHelperFunctions());
        factory.AddFunctions(new ThinkingToolHelperFunction());
        
        // Add MCP functions if any are configured
        await AddMcpFunctions(factory);
        factory.AddFunctions(new CodeExplorationHelperFunctions());

        // Create the chat completions object with the external ChatClient and system prompt.
        var chatClient = ChatClientFactory.CreateChatClient();
        var chat = new FunctionCallingChat(chatClient, SystemPrompt, factory, MaxOutputTokens);

        try
        {
            // Add the user prompt messages to the chat.
            chat.AddUserMessages(UserPromptAdds);

            // Load the chat history from the file.
            var loadChatHistory = !string.IsNullOrEmpty(InputChatHistory);
            if (loadChatHistory) chat.LoadChatHistory(InputChatHistory!, TrimTokenTarget ?? DefaultTrimTokenTarget, useOpenAIFormat: ChatHistoryDefaults.UseOpenAIFormat);

            // Check to make sure we're either in interactive mode, or have input instructions.
            if (!interactive && InputInstructions.Count == 0)
            {
                ConsoleHelpers.WriteWarning("\nNo input instructions provided. Exiting.");
                return 1;
            }

            while (true)
            {
                DisplayUserPrompt();
                var userPrompt = interactive && !Console.IsInputRedirected
                    ? InteractivelyReadLineOrSimulateInput(InputInstructions, "exit")
                    : ReadLineOrSimulateInput(InputInstructions, "exit");
                if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt == "exit") break;

                var handled = await TryHandleChatCommandAsync(chat, userPrompt);
                if (handled) continue;

                DisplayAssistantLabel();
                var response = await CompleteChatStreamingAsync(chat, userPrompt,
                    (messages) => HandleUpdateMessages(messages),
                    (update) => HandleStreamingChatCompletionUpdate(update),
                    (name, args, result) => HandleFunctionCallCompleted(name, args, result));
                ConsoleHelpers.WriteLine("\n", overrideQuiet: true);
            }

            return 0;
        }
        finally
        {
            // Clean up resources
            await chat.DisposeAsync();
            
            // If we have an MCP function factory, dispose its resources too
            if (factory is McpFunctionFactory mcpFactory)
            {
                await mcpFactory.DisposeAsync();
            }
        }
    }

    private string GroundSystemPrompt()
    {
        SystemPrompt ??= GetBuiltInSystemPrompt();
        return ProcessTemplate(SystemPrompt + "\n\n" + GetSystemPromptAdds());
    }

    private List<string> GroundUserPromptAdds()
    {
        return UserPromptAdds
            .Select(x => UseTemplates ? ProcessTemplate(x) : x)
            .ToList();
    }

    private List<string> GroundInputInstructions()
    {
        return InputInstructions
            .Select(x => UseTemplates ? ProcessTemplate(x) : x)
            .ToList();
    }



    private string GetBuiltInSystemPrompt()
    {
        if (EmbeddedFileHelpers.EmbeddedStreamExists("prompts.system.md"))
        {
            var text = EmbeddedFileHelpers.ReadEmbeddedStream("prompts.system.md")!;
            return ProcessTemplate(text);
        }

        return "You are a helpful AI assistant.";
    }

    private string GetSystemPromptAdds()
    {
        var processedAdds = SystemPromptAdds
            .Select(x => UseTemplates ? ProcessTemplate(x) : x)
            .ToList();
        var joined = string.Join("\n\n", processedAdds);
        return joined.Trim(new char[] { '\n', '\r', ' ' });
    }

    private async Task<bool> TryHandleChatCommandAsync(FunctionCallingChat chat, string userPrompt)
    {
        if (userPrompt == "/save")
        {
            return HandleSaveChatHistoryCommand(chat);
        }
        else if (userPrompt == "/clear")
        {
            return HandleClearChatHistoryCommand(chat);
        }
        else if (userPrompt == "/cost")
        {
            return HandleShowCostCommand();
        }
        else if (userPrompt == "/help")
        {
            return HandleHelpCommand();
        }
        
        if (_chatCommandHandler.IsCommand(userPrompt))
        {
            return await HandleSlashCommand(chat, userPrompt);
        }

        return false;
    }

    private async Task<bool> HandleSlashCommand(FunctionCallingChat chat, string userPrompt)
    {
        var userFunctionName = _chatCommandHandler.GetCommandName(userPrompt);
        DisplayUserFunctionCall(userFunctionName, null);

        var result = await _chatCommandHandler.HandleCommand(userPrompt);
        if (result != null) chat.AddUserMessage(result);

        DisplayUserFunctionCall(userFunctionName, result ?? string.Empty);
        return true;
    }

    private bool HandleClearChatHistoryCommand(FunctionCallingChat chat)
    {
        chat.ClearChatHistory();
        _totalTokensIn = 0;
        _totalTokensOut = 0;
        ConsoleHelpers.WriteLine("Cleared chat history.\n", ConsoleColor.Yellow, overrideQuiet: true);
        return true;
    }

    private bool HandleSaveChatHistoryCommand(FunctionCallingChat chat)
    {
        ConsoleHelpers.Write("Saving chat-history.jsonl ...", ConsoleColor.Yellow, overrideQuiet: true);
        chat.SaveChatHistoryToFile("chat-history.jsonl", useOpenAIFormat: ChatHistoryDefaults.UseOpenAIFormat);
        ConsoleHelpers.WriteLine("Saved!\n", ConsoleColor.Yellow, overrideQuiet: true);
        return true;
    }

    private bool HandleShowCostCommand()
    {
        ConsoleHelpers.WriteLine($"Tokens: {_totalTokensIn} in, {_totalTokensOut} out\n", ConsoleColor.Yellow, overrideQuiet: true);
        return true;
    }

    private bool HandleHelpCommand()
    {
        var helpBuilder = new StringBuilder();
        
        // Built-in chat commands
        helpBuilder.AppendLine("BUILT-IN");
        helpBuilder.AppendLine();
        helpBuilder.AppendLine("  /save     Save chat history to file");
        helpBuilder.AppendLine("  /clear    Clear chat history");
        helpBuilder.AppendLine("  /cost     Show token usage statistics");
        helpBuilder.AppendLine("  /help     Show this help message");
        helpBuilder.AppendLine();
        
        // Tool integration commands
        helpBuilder.AppendLine("TOOLS");
        helpBuilder.AppendLine();
        helpBuilder.AppendLine("  /mcp      Manage MCP servers (see 'chatx help mcp')");
        helpBuilder.AppendLine();
        
        // MDX integration commands
        helpBuilder.AppendLine("EXTERNAL");
        helpBuilder.AppendLine();
        helpBuilder.AppendLine("  /files    List files matching pattern");
        helpBuilder.AppendLine("  /file     Get contents of a file");
        helpBuilder.AppendLine("  /find     Find content in files");
        helpBuilder.AppendLine();
        helpBuilder.AppendLine("  /search   Search the web");
        helpBuilder.AppendLine("  /get      Get content from URL");
        helpBuilder.AppendLine();
        helpBuilder.AppendLine("  /run      Run a command");
        helpBuilder.AppendLine();
        
        // User-defined prompts
        helpBuilder.AppendLine("PROMPTS");
        helpBuilder.AppendLine();
        
        // Get all scopes for prompt files
        bool foundPrompts = false;
        foreach (var scope in new[] { ConfigFileScope.Local, ConfigFileScope.User, ConfigFileScope.Global })
        {
            var promptDir = PromptFileHelpers.FindPromptDirectoryInScope(scope);
            if (promptDir == null || !Directory.Exists(promptDir)) continue;
            
            var promptFiles = Directory.GetFiles(promptDir, "*.prompt")
                .OrderBy(file => Path.GetFileNameWithoutExtension(file))
                .ToList();
                
            if (promptFiles.Count == 0) continue;
            
            foundPrompts = true;
            
            // Format location header consistently
            var locationDisplay = CommonDisplayHelpers.FormatLocationHeader(promptDir, scope);
            helpBuilder.AppendLine($"  LOCATION: {locationDisplay}");
            helpBuilder.AppendLine();

            var indent = new string(' ', 4);
            
            foreach (var promptFile in promptFiles)
            {
                var promptName = Path.GetFileNameWithoutExtension(promptFile);
                helpBuilder.AppendLine($"{indent}/{promptName}");
                helpBuilder.AppendLine();

                // Get prompt content and truncate it for preview
                var promptText = PromptFileHelpers.GetPromptText(promptName, scope)!;
                var truncatedContent = CommonDisplayHelpers.TruncateContent(
                    promptText, 
                    CommonDisplayHelpers.DefaultMaxContentLines, 
                    CommonDisplayHelpers.DefaultMaxContentWidth, 
                    8);  // deeper indentation for content
                
                helpBuilder.AppendLine(truncatedContent);
                helpBuilder.AppendLine();
            }
            
            helpBuilder.AppendLine();
        }
        
        if (!foundPrompts)
        {
            helpBuilder.AppendLine("  No custom prompts found.");
            helpBuilder.AppendLine();
        }
        
        var indented = "\n  " + helpBuilder.ToString().Replace("\n", "\n  ").TrimEnd() + "\n";
        ConsoleHelpers.WriteLine(indented, overrideQuiet: true);
        return true;
    }

    private async Task<string> CompleteChatStreamingAsync(
        FunctionCallingChat chat,
        string userPrompt,
        Action<IList<ChatMessage>>? messageCallback = null,
        Action<ChatResponseUpdate>? streamingCallback = null,
        Action<string, string, string?>? functionCallCallback = null)
    {
        messageCallback = TryCatchHelpers.NoThrowWrap(messageCallback);
        streamingCallback = TryCatchHelpers.NoThrowWrap(streamingCallback);
        functionCallCallback = TryCatchHelpers.NoThrowWrap(functionCallCallback);

        try
        {
            var response = await chat.CompleteChatStreamingAsync(userPrompt,
                (messages) => messageCallback?.Invoke(messages),
                (update) => streamingCallback?.Invoke(update),
                (name, args, result) => functionCallCallback?.Invoke(name, args, result));

            return response;
        }
        catch (Exception)
        {
            var fileName = FileHelpers.GetFileNameFromTemplate("exception-chat-history.jsonl", "{filebase}-{time}.{fileext}")!;
            chat.SaveChatHistoryToFile(fileName, useOpenAIFormat: ChatHistoryDefaults.UseOpenAIFormat);

            ConsoleHelpers.Write("\n\n", overrideQuiet: true);
            ConsoleHelpers.WriteWarning($"SAVED: {fileName}");
            throw;
        }
    }

    private string? ReadLineOrSimulateInput(List<string> inputInstructions, string? defaultOnEndOfInput = null)
    {
        while (inputInstructions?.Count > 0)
        {
            var input = inputInstructions[0];
            inputInstructions.RemoveAt(0);

            var empty = string.IsNullOrEmpty(input);
            if (!empty)
            {
                ConsoleHelpers.WriteLine(input);
                return input;
            }
        }

        if (Console.IsInputRedirected)
        {
            var line = Console.ReadLine();
            line ??= defaultOnEndOfInput;
            if (line != null) ConsoleHelpers.WriteLine(line);
            return line;
        }

        return defaultOnEndOfInput;
    }

    private string? InteractivelyReadLineOrSimulateInput(List<string> inputInstructions, string? defaultOnEndOfInput = null)
    {
        var input = ReadLineOrSimulateInput(inputInstructions, null);
        if (input != null) return input;

        return Console.ReadLine() ?? defaultOnEndOfInput;
    }

    private void HandleUpdateMessages(IList<ChatMessage> messages)
    {
        messages.TryTrimToTarget(TrimTokenTarget ?? DefaultTrimTokenTarget);

        if (OutputChatHistory != null)
        {
            messages.SaveChatHistoryToFile(OutputChatHistory, useOpenAIFormat: ChatHistoryDefaults.UseOpenAIFormat);
        }
        
        if (OutputTrajectory != null && messages.Count > 0)
        {
            messages.Last().AppendMessageToTrajectoryFile(OutputTrajectory);
        }
    }

    private void HandleStreamingChatCompletionUpdate(ChatResponseUpdate update)
    {
        var usageUpdate = update.Contents
            .Where(x => x is UsageContent)
            .Cast<UsageContent>()
            .FirstOrDefault();
        var inTokens = usageUpdate?.Details.InputTokenCount ?? 0;
        var outTokens = usageUpdate?.Details.OutputTokenCount ?? 0;
        if (inTokens > 0 || outTokens > 0)
        {
            _totalTokensIn += inTokens;
            _totalTokensOut += outTokens;
            if (ConsoleHelpers.IsVerbose())
            {
                ConsoleHelpers.WriteLine($"\nTokens: {inTokens} in ({_totalTokensIn} total), {outTokens} out ({_totalTokensOut} total)", ConsoleColor.DarkYellow);
            }
        }

        var text = string.Join("", update.Contents
            .Where(x => x is TextContent)
            .Cast<TextContent>()
            .Select(x => x.Text)
            .ToList());
        DisplayAssistantResponse(text);
    }

    private void HandleFunctionCallCompleted(string name, string args, string? result)
    {
        DisplayAssistantFunctionCall(name, args, result);
    }

    private void DisplayUserPrompt()
    {
        ConsoleHelpers.Write("\rUser: ", ConsoleColor.Green);
        Console.ForegroundColor = ConsoleColor.White;
    }

    private void DisplayUserFunctionCall(string userFunctionName, string? result)
    {
        ConsoleHelpers.Write($"\ruser-function: {userFunctionName} => ", ConsoleColor.DarkGray);

        if (result == null) ConsoleHelpers.Write("...", ConsoleColor.DarkGray);
        if (result != null)
        {
            ConsoleHelpers.WriteLine(result, ConsoleColor.DarkGray);
            DisplayUserPrompt();
        }
    }

    private void DisplayAssistantLabel()
    {
        ConsoleHelpers.Write("\nAssistant: ", ConsoleColor.Green);
        _assistantResponseCharsSinceLabel = 0;
    }

    private void DisplayAssistantResponse(string text)
    {
        if (_assistantResponseCharsSinceLabel == 0 && text.StartsWith("\n"))
        {
            text = text.TrimStart(new char[] { '\n', '\r', ' ' });
        }

        ConsoleHelpers.Write(text, ConsoleColor.White, overrideQuiet: true);

        _assistantResponseCharsSinceLabel += text.Length;
        _asssistantResponseNeedsLF = !text.EndsWith("\n");
    }

    private void EnsureLineFeeds()
    {
        if (_asssistantResponseNeedsLF)
        {
            var oneLineFeedOrTwo = ConsoleHelpers.IsQuiet() ? "\n\n" : "\n";
            ConsoleHelpers.Write(oneLineFeedOrTwo, overrideQuiet: true);
            _asssistantResponseNeedsLF = false;
        }
    }

    private void DisplayAssistantFunctionCall(string name, string args, string? result)
    {
        EnsureLineFeeds();
        switch (name)
        {
            case "Think":
                DisplayAssistantThinkFunctionCall(args, result);
                break;

            default:
                DisplayGenericAssistantFunctionCall(name, args, result);
                break;
        }
    }
    
    private void DisplayAssistantThinkFunctionCall(string args, string? result)
    {
        var thought = JsonHelpers.GetJsonPropertyValue(args, "thought", args);
        var hasThought = !string.IsNullOrEmpty(thought);
        var hasResult = !string.IsNullOrEmpty(result);

        if (hasThought && !hasResult) ConsoleHelpers.WriteLine($"\n[THINKING]\n{thought}", ConsoleColor.DarkCyan);
        if (hasResult)
        {
            ConsoleHelpers.WriteLine($"\n{result}", ConsoleColor.DarkGray);
            DisplayAssistantLabel();
        }
    }
    
    private void DisplayGenericAssistantFunctionCall(string name, string args, string? result)
    {
        ConsoleHelpers.Write($"\rassistant-function: {name} {args} => ", ConsoleColor.DarkGray);
        
        if (result == null) ConsoleHelpers.Write("...", ConsoleColor.DarkGray);
        if (result != null)
        {
            ConsoleHelpers.WriteLine(result, ConsoleColor.DarkGray);
            DisplayAssistantLabel();
        }
    }

    private string ProcessTemplate(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var variables = new TemplateVariables(Variables);
        return TemplateHelpers.ProcessTemplate(template, variables);
    }

    public string? SystemPrompt { get; set; }
    public List<string> SystemPromptAdds { get; set; } = new List<string>();
    public List<string> UserPromptAdds { get; set; } = new List<string>();

    public int? TrimTokenTarget { get; set; }
    public int? MaxOutputTokens { get; set; }

    public bool LoadMostRecentChatHistory = false;
    public string? InputChatHistory;
    public string? OutputChatHistory;
    public string? OutputTrajectory;

    public List<string> InputInstructions = new();
    public bool UseTemplates = true;

    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    public List<ForEachVariable> ForEachVariables { get; set; } = new List<ForEachVariable>();

    private int _assistantResponseCharsSinceLabel = 0;
    private bool _asssistantResponseNeedsLF = false;

    private SlashCommandHandler _chatCommandHandler = new();
    private long _totalTokensIn = 0;
    private long _totalTokensOut = 0;
    private const int DefaultTrimTokenTarget = 160000;

    /// <summary>
    /// Adds MCP functions from configured MCP servers to the function factory.
    /// </summary>
    /// <param name="factory">The McpFunctionFactory to add functions to.</param>
    private async Task AddMcpFunctions(McpFunctionFactory factory)
    {
        try
        {
            // Create clients for all configured MCP servers
            var clients = await McpClientManager.CreateAllClientsAsync();
            if (clients.Count == 0)
            {
                return; // No configured MCP servers
            }

            Console.WriteLine($"Found {clients.Count} MCP server(s)");
            
            // Add tools from each client
            foreach (var clientEntry in clients)
            {
                var serverName = clientEntry.Key;
                var client = clientEntry.Value;

                try
                {
                    await factory.AddMcpClientToolsAsync(client, serverName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding tools from MCP server '{serverName}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading MCP functions: {ex.Message}");
        }
    }
}
