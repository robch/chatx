using Microsoft.Extensions.AI;
using System.Text.Json;

public class FunctionCallingChat : IAsyncDisposable
{
    public FunctionCallingChat(IChatClient chatClient, string systemPrompt, FunctionFactory factory, ChatOptions? options, int? maxOutputTokens = null)
    {
        _systemPrompt = systemPrompt;
        _functionFactory = factory;
        _functionCallDetector = new FunctionCallDetector();

        var useMicrosoftExtensionsAIFunctionCalling = false; // Can't use this for now; (1) doesn't work with copilot w/ all models, (2) functionCallCallback not available
        _chatClient = useMicrosoftExtensionsAIFunctionCalling
            ? chatClient.AsBuilder().UseFunctionInvocation().Build()
            : chatClient;

        _messages = new List<ChatMessage>();
        _options = new ChatOptions()
        {
            ModelId = options?.ModelId,
            ToolMode = options?.ToolMode,
            Tools = _functionFactory.GetAITools().ToList(),
            MaxOutputTokens = maxOutputTokens.HasValue
                ? maxOutputTokens.Value
                : options?.MaxOutputTokens,
        };

        if (maxOutputTokens.HasValue) _options.MaxOutputTokens = maxOutputTokens.Value;

        ClearChatHistory();
    }

    public void ClearChatHistory()
    {
        _messages.Clear();
        _messages.Add(new ChatMessage(ChatRole.System, _systemPrompt));

        foreach (var userMessage in _userMessageAdds)
        {
            _messages.Add(new ChatMessage(ChatRole.User, userMessage));
        }
    }
    
    public void AddUserMessage(string userMessage, int tokenTrimTarget = 0)
    {
        _userMessageAdds.Add(userMessage);
        _messages.Add(new ChatMessage(ChatRole.User, userMessage));

        if (tokenTrimTarget > 0)
        {
            _messages.TryTrimToTarget(tokenTrimTarget);
        }
    }
    
    public void AddUserMessages(IEnumerable<string> userMessages, int tokenTrimTarget = 0)
    {
        foreach (var userMessage in userMessages)
        {
            AddUserMessage(userMessage);
        }

        if (tokenTrimTarget > 0)
        {
            _messages.TryTrimToTarget(tokenTrimTarget);
        }
    }

    public void LoadChatHistory(string fileName, int tokenTrimTarget = 0, bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat)
    {
        _messages.ReadChatHistoryFromFile(fileName, useOpenAIFormat);
        _messages.FixDanglingToolCalls();

        if (tokenTrimTarget > 0)
        {
            _messages.TryTrimToTarget(tokenTrimTarget);
        }
    }

    public void SaveChatHistoryToFile(string fileName, bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat, string? saveToFolderOnAccessDenied = null)
    {
        _messages.SaveChatHistoryToFile(fileName, useOpenAIFormat, saveToFolderOnAccessDenied);
    }

    public void SaveTrajectoryToFile(string fileName, bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat, string? saveToFolderOnAccessDenied = null)
    {
        _messages.SaveTrajectoryToFile(fileName, useOpenAIFormat, saveToFolderOnAccessDenied);
    }

    public async Task<string> CompleteChatStreamingAsync(
        string userPrompt,
        Action<IList<ChatMessage>>? messageCallback = null,
        Action<ChatResponseUpdate>? streamingCallback = null,
        Action<string, string, string?>? functionCallCallback = null)
    {
        _messages.Add(new ChatMessage(ChatRole.User, userPrompt));
        messageCallback?.Invoke(_messages);

        var contentToReturn = string.Empty;
        while (true)
        {
            var responseContent = string.Empty;
            await foreach (var update in _chatClient.GetStreamingResponseAsync(_messages, _options))
            {
                _functionCallDetector.CheckForFunctionCall(update);

                var content = string.Join("", update.Contents
                    .Where(c => c is TextContent)
                    .Cast<TextContent>()
                    .Select(c => c.Text)
                    .ToList());

                if (update.FinishReason == ChatFinishReason.ContentFilter)
                {
                    content = $"{content}\nWARNING: Content filtered!";
                }

                responseContent += content;
                contentToReturn += content;

                streamingCallback?.Invoke(update);
            }

            if (TryCallFunctions(responseContent, functionCallCallback, messageCallback))
            {
                _functionCallDetector.Clear();
                continue;
            }

            _messages.Add(new ChatMessage(ChatRole.Assistant, responseContent));
            messageCallback?.Invoke(_messages);

            return contentToReturn;
        }
    }

    private bool TryCallFunctions(string responseContent, Action<string, string, string?>? functionCallCallback, Action<IList<ChatMessage>>? messageCallback)
    {
        var noFunctionsToCall = !_functionCallDetector.HasFunctionCalls();
        if (noFunctionsToCall) return false;
        
        var readyToCallFunctionCalls = _functionCallDetector.GetReadyToCallFunctionCalls();

        var assistentContent = readyToCallFunctionCalls
            .AsAIContentList()
            .Prepend(new TextContent(responseContent))
            .ToList();
        _messages.Add(new ChatMessage(ChatRole.Assistant, assistentContent));
        messageCallback?.Invoke(_messages);

        var functionResultContents = CallFunctions(readyToCallFunctionCalls, functionCallCallback);
        var toolContent = functionResultContents.Cast<AIContent>().ToList();

        _messages.Add(new ChatMessage(ChatRole.Tool, toolContent));
        messageCallback?.Invoke(_messages);

        return true;
    }

    private List<FunctionResultContent> CallFunctions(List<FunctionCallDetector.ReadyToCallFunctionCall> readyToCallFunctionCalls, Action<string, string, string?>? functionCallCallback)
    {
        ConsoleHelpers.WriteDebugLine($"Calling functions: {string.Join(", ", readyToCallFunctionCalls.Select(call => call.Name))}");

        var functionResultContents = new List<FunctionResultContent>();
        foreach (var functionCall in readyToCallFunctionCalls)
        {
            functionCallCallback?.Invoke(functionCall.Name, functionCall.Arguments, null);

            ConsoleHelpers.WriteDebugLine($"Calling function: {functionCall.Name} with arguments: {functionCall.Arguments}");
            var functionResult = _functionFactory.TryCallFunction(functionCall.Name, functionCall.Arguments, out var functionResponse)
                ? functionResponse ?? "Function call succeeded"
                : $"Function not found or failed to execute: {functionResponse}";
            ConsoleHelpers.WriteDebugLine($"Function call result: {functionResult}");

            functionResultContents.Add(new FunctionResultContent(functionCall.CallId, functionResult));
            functionCallCallback?.Invoke(functionCall.Name, functionCall.Arguments, functionResult);
        }

        return functionResultContents;
    }

    /// <summary>
    /// Disposes the function factory and any associated resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_functionFactory is McpFunctionFactory mcpFactory)
        {
            await mcpFactory.DisposeAsync();
        }
    }

    private readonly string _systemPrompt;
    private readonly List<string> _userMessageAdds = new();

    private readonly FunctionFactory _functionFactory;
    private readonly FunctionCallDetector _functionCallDetector;
    private readonly ChatOptions _options;
    private readonly IChatClient _chatClient;
    private List<ChatMessage> _messages;
}
