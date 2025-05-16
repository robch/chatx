using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class AiInstructionProcessor
{
    public const string DefaultSaveChatHistoryTemplate = "chat-history-{time}.jsonl";

    public static string ApplyAllInstructions(List<string> instructionsList, string content, bool useBuiltInFunctions, string? saveChatHistory, int retries = 1)
    {
        try
        {
            ConsoleHelpers.DisplayStatus("Applying instructions ...");
            return instructionsList.Aggregate(content, (current, instruction) => ApplyInstructions(instruction, current, useBuiltInFunctions, saveChatHistory, retries));
        }
        finally
        {
            ConsoleHelpers.DisplayStatusErase();
        }
    }

    public static string ApplyInstructions(string instructions, string content, bool useBuiltInFunctions, string? saveChatHistory, int retries = 1)
    {
        while (true)
        {
            ApplyInstructions(instructions, content, useBuiltInFunctions, saveChatHistory, out var returnCode, out var stdOut, out var stdErr, out var exception);

            var retryable = retries-- > 0;
            var tryAgain = retryable && (returnCode != 0 || exception != null);
            if (tryAgain) continue;

            return exception != null
                ? $"{stdOut}\n\n## Error Applying Instructions\n\nEXIT CODE: {returnCode}\n\nERROR: {exception.Message}\n\nSTDERR: {stdErr}"
                : returnCode != 0
                    ? $"{stdOut}\n\n## Error Applying Instructions\n\nEXIT CODE: {returnCode}\n\nSTDERR: {stdErr}"
                    : stdOut;
        }
    }

    private static void ApplyInstructions(string instructions, string content, bool useBuiltInFunctions, string? saveChatHistory, out int returnCode, out string stdOut, out string stdErr, out Exception? exception)
    {
        returnCode = 0;
        stdOut = string.Empty;
        stdErr = string.Empty;
        exception = null;

        var userPromptFileName = Path.GetTempFileName();
        var systemPromptFileName = Path.GetTempFileName();
        var instructionsFileName = Path.GetTempFileName();
        var contentFileName = Path.GetTempFileName();
        try
        {
            var backticks = new string('`', MarkdownHelpers.GetCodeBlockBacktickCharCountRequired(content) + 3);
            File.WriteAllText(systemPromptFileName, GetSystemPrompt());
            File.WriteAllText(userPromptFileName, GetUserPrompt(backticks, contentFileName, instructionsFileName));
            File.WriteAllText(instructionsFileName, instructions);
            File.WriteAllText(contentFileName, content);

            ConsoleHelpers.WriteDebugLine($"user:\n{File.ReadAllText(userPromptFileName)}\n\n");
            ConsoleHelpers.WriteDebugLine($"system:\n{File.ReadAllText(systemPromptFileName)}\n\n");
            ConsoleHelpers.WriteDebugLine($"instructions:\n{File.ReadAllText(instructionsFileName)}\n\n");

            var useCycoD = UseCycoD();
            var arguments = useCycoD
                ? $"--input \"@{userPromptFileName}\" --system-prompt \"@{systemPromptFileName}\" --quiet --interactive false --no-templates"
                : $"chat --user \"@{userPromptFileName}\" --system \"@{systemPromptFileName}\" --quiet true";

            if (useBuiltInFunctions && !useCycoD) arguments += " --built-in-functions";

            if (!string.IsNullOrEmpty(saveChatHistory))
            {
                var fileName = FileHelpers.GetFileNameFromTemplate(DefaultSaveChatHistoryTemplate, saveChatHistory);
                arguments += $" --output-chat-history \"{fileName}\"";
            }

            if (useCycoD)
            {
                RewriteExpandedFile(userPromptFileName);
                RewriteExpandedFile(systemPromptFileName);
            }

            ConsoleHelpers.WriteDebugLine($"Command: {(useCycoD ? "cycod" : "ai")} {arguments}");
            ConsoleHelpers.DisplayStatus("Applying instructions ...");
            
            var processResult = new RunnableProcessBuilder()
                .WithFileName(useCycoD ? "cycod" : "ai")
                .WithArguments(arguments)
                .WithTimeout(300000) // 5 minute timeout
                .Run();
            
            stdOut = processResult.StandardOutput;
            stdErr = processResult.StandardError;
            returnCode = processResult.ExitCode;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            ConsoleHelpers.DisplayStatusErase();
            if (File.Exists(userPromptFileName)) File.Delete(userPromptFileName);
            if (File.Exists(systemPromptFileName)) File.Delete(systemPromptFileName);
            if (File.Exists(instructionsFileName)) File.Delete(instructionsFileName);
            if (File.Exists(contentFileName)) File.Delete(contentFileName);
        }
    }

    private static string GetSystemPrompt()
    {
        return EmbeddedFileHelpers.ReadEmbeddedStream("prompts.system.md")!;
    }

    private static string GetUserPrompt(string backticks, string contentFile, string instructionsFile)
    {
        return EmbeddedFileHelpers.ReadEmbeddedStream("prompts.user.md")!
            .Replace("{instructionsFile}", instructionsFile)
            .Replace("{contentFile}", contentFile)
            .Replace("{backticks}", backticks);
    }

    private static bool UseCycoD()
    {
        if (_useCycoD == null)
        {
            _useCycoD = false;

            try
            {
                ConsoleHelpers.WriteDebugLine("Checking cycod installation ...");
                
                var commandResult = new RunnableProcessBuilder()
                    .WithFileName("cycod")
                    .WithArguments("version")
                    .WithTimeout(5000) // 5 second timeout
                    .Run();
                
                _useCycoD = commandResult.Success;
            }
            catch (Exception ex)
            {
                ConsoleHelpers.WriteDebugLine($"Error checking cycod installation: {ex.Message}");
                _useCycoD = false;
            }
            finally
            {
                if (_useCycoD == null)
                {
                    _useCycoD = false;
                }
            }
            ConsoleHelpers.WriteDebugLine($"CycoD installed: {_useCycoD}");
        }

        return _useCycoD.Value;
    }

    private static void RewriteExpandedFile(string userPromptFileName)
    {
        var content = File.ReadAllText(userPromptFileName);
        
        // Find patterns that look like this {@FILENAME}, and replace them with the content of the file
        // For example, {@FILENAME} will be replaced with the content of the file FILENAME
        var pattern = @"\{@(.*?)\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(content, pattern);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var fileName = match.Groups[1].Value;
            var directoryName = Path.GetDirectoryName(userPromptFileName) ?? string.Empty;
            var filePath = Path.Combine(directoryName, fileName);
            if (File.Exists(filePath))
            {
                var fileContent = File.ReadAllText(filePath);
                content = content.Replace(match.Value, fileContent);
            }
        }

        File.WriteAllText(userPromptFileName, content);
    }

    private static bool? _useCycoD;
}
