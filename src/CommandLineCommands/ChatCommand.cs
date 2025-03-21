using OpenAI.Chat;

class ChatCommand : Command
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

    public async Task<List<Task<int>>> ExecuteAsync(bool interactive)
    {
        // Ground the filenames (in case they're templatized), and set the system prompt.
        InputChatHistory = FileHelpers.GetFileNameFromTemplate(InputChatHistory ?? "chat-history.jsonl", InputChatHistory);
        OutputChatHistory = FileHelpers.GetFileNameFromTemplate(OutputChatHistory ?? "chat-history.jsonl", OutputChatHistory);
        SystemPrompt ??= EnvironmentHelpers.FindEnvVar("OPENAI_SYSTEM_PROMPT") ?? "You are a helpful AI assistant.";

        // Create the function factory and add functions.
        var factory = new FunctionFactory();
        factory.AddFunctions(new DateAndTimeHelperFunctions());
        factory.AddFunctions(new ShellCommandToolHelperFunctions());
        factory.AddFunctions(new StrReplaceEditorHelperFunctions());

        // Create the chat completions object with the external ChatClient and system prompt.
        var chatClient = ChatClientFactory.CreateChatClientFromEnv();
        var chat = new FunctionCallingChat(chatClient, SystemPrompt, factory);

        // Load the chat history from the file.
        var loadChatHistory = !string.IsNullOrEmpty(InputChatHistory);
        if (loadChatHistory) chat.LoadChatHistory(InputChatHistory!);

        // Check to make sure we're either in interactive mode, or have input instructions.
        if (!interactive && InputInstructions.Count == 0)
        {
            ConsoleHelpers.WriteLine("No input instructions provided. Exiting.", ConsoleColor.Red, overrideQuiet: true);
            return new List<Task<int>>() { Task.FromResult(1) };
        }

        while (true)
        {
            DisplayUserPrompt();
            var userPrompt = interactive && !Console.IsInputRedirected
                ? ReadLineOrSimulateInput(InputInstructions, "exit")
                : ReadOrSimulateInput(InputInstructions, "exit");
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            DisplayAssistantLabel();
            var response = await chat.GetChatCompletionsStreamingAsync(userPrompt,
                (messages) => HandleUpdateMessages(messages),
                (update) => HandleStreamingChatCompletionUpdate(update),
                (name, args, result) => HandleFunctionCallCompleted(name, args, result));
            ConsoleHelpers.WriteLine("\n", overrideQuiet: true);
        }

        return new List<Task<int>>() { Task.FromResult(0) };
    }

    private string? ReadOrSimulateInput(List<string> inputInstructions, string? defaultOnEndOfInput = null)
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

    private string? ReadLineOrSimulateInput(List<string> inputInstructions, string? defaultOnEndOfInput = null)
    {
        var input = ReadOrSimulateInput(inputInstructions, null);
        if (input != null) return input;

        return Console.ReadLine() ?? defaultOnEndOfInput;
    }

    private void HandleUpdateMessages(IList<ChatMessage> messages)
    {
        var tokenTarget = TrimTokenTarget.HasValue
            ? TrimTokenTarget.Value
            : 160000;

        const int whenTrimmingToolContentTarget = 10;
        const string snippedIndicator = "...snip...";

        if (messages.IsTooBig(tokenTarget))
        {
            messages.ReduceToolCallContent(tokenTarget, whenTrimmingToolContentTarget, snippedIndicator);
        }

        if (OutputChatHistory != null)
        {
            messages.SaveChatHistoryToFile(OutputChatHistory);
        }
    }

    private void HandleStreamingChatCompletionUpdate(StreamingChatCompletionUpdate update)
    {
        var text = string.Join("", update.ContentUpdate
            .Where(x => x.Kind == ChatMessageContentPartKind.Text)
            .Select(x => x.Text)
            .ToList());
        DisplayAssistantResponse(text);
    }

    private void HandleFunctionCallCompleted(string name, string args, string? result)
    {
        DisplayFunctionResult(name, args, result);
    }

    private static void DisplayUserPrompt()
    {
        ConsoleHelpers.Write("User: ", ConsoleColor.Green);
        Console.ForegroundColor = ConsoleColor.White;
    }

    private static void DisplayAssistantLabel()
    {
        ConsoleHelpers.Write("\nAssistant: ", ConsoleColor.Green);
    }

    private void DisplayAssistantResponse(string text)
    {
        ConsoleHelpers.Write(text, ConsoleColor.White, overrideQuiet: true);
        _asssistantResponseNeedsLF = !text.EndsWith("\n");
    }

    private void DisplayFunctionResult(string name, string args, string? result)
    {
        if (_asssistantResponseNeedsLF)
        {
            ConsoleHelpers.WriteLine();
            _asssistantResponseNeedsLF = false;
        }
        
        ConsoleHelpers.Write($"\rassistant-function: {name} {args} => ", ConsoleColor.DarkGray);
        
        if (result == null) ConsoleHelpers.Write("...", ConsoleColor.DarkGray);
        if (result != null)
        {
            ConsoleHelpers.WriteLine(result, ConsoleColor.DarkGray);
            DisplayAssistantLabel();
        }
    }

    public string? SystemPrompt { get; set; }

    public int? TrimTokenTarget { get; set; }

    public string? InputChatHistory;
    public string? OutputChatHistory;

    public List<string> InputInstructions = new();

    private bool _asssistantResponseNeedsLF = false;
}
