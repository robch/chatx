using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Command to delete a prompt.
/// </summary>
class PromptDeleteCommand : PromptBaseCommand
{
    /// <summary>
    /// The name of the prompt to delete.
    /// </summary>
    public string? PromptName { get; set; }

    /// <summary>
    /// Constructor initializes the base command.
    /// </summary>
    public PromptDeleteCommand() : base()
    {
        Scope = ConfigFileScope.Any;
    }

    /// <summary>
    /// Checks if the command is empty (i.e., no prompt name provided).
    /// </summary>
    /// <returns>True if empty, false otherwise.</returns>
    public override bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(PromptName);
    }

    /// <summary>
    /// Gets the command name.
    /// </summary>
    /// <returns>The command name.</returns>
    public override string GetCommandName()
    {
        return "prompt delete";
    }

    /// <summary>
    /// Execute the delete command.
    /// </summary>
    /// <param name="interactive">Whether the command is running in interactive mode.</param>
    /// <returns>Exit code, 0 for success, non-zero for failure.</returns>
    public Task<int> Execute(bool interactive)
    {
        if (string.IsNullOrWhiteSpace(PromptName))
        {
            ConsoleHelpers.WriteErrorLine("Error: Prompt name is required.");
            return Task.FromResult(1);
        }

        var result = ExecuteDelete(PromptName, Scope ?? ConfigFileScope.Any);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Execute the delete command for the specified prompt name and scope.
    /// </summary>
    /// <param name="promptName">The name of the prompt to delete.</param>
    /// <param name="scope">The scope to look in.</param>
    /// <returns>Exit code, 0 for success, non-zero for failure.</returns>
    private int ExecuteDelete(string promptName, ConfigFileScope scope)
    {
        // Remove leading slash if someone added it (for consistency)
        if (promptName.StartsWith('/'))
        {
            promptName = promptName.Substring(1);
        }
        
        string? promptFilePath = null;

        if (scope == ConfigFileScope.Any)
        {
            promptFilePath = PromptFileHelpers.FindPromptFile(promptName);
        }
        else
        {
            var promptDir = PromptFileHelpers.FindPromptDirectoryInScope(scope);
            if (promptDir != null)
            {
                var potentialFilePath = Path.Combine(promptDir, $"{promptName}.prompt");
                if (File.Exists(potentialFilePath))
                {
                    promptFilePath = potentialFilePath;
                }
            }
        }

        if (promptFilePath == null || !File.Exists(promptFilePath))
        {
            ConsoleHelpers.WriteErrorLine($"Error: Prompt '{promptName}' not found in specified scope.");
            return 1;
        }

        // Check for additional files that might be referenced (multiline content)
        var directory = Path.GetDirectoryName(promptFilePath);
        var fileBase = Path.GetFileNameWithoutExtension(promptFilePath);
        var fileExt = Path.GetExtension(promptFilePath);
        var additionalFilePattern = $"{fileBase}-*{fileExt}";
        
        // Check if prompt file references another file
        var content = File.ReadAllText(promptFilePath);
        string? referencedFilePath = null;
        if (content.StartsWith('@'))
        {
            referencedFilePath = content.Substring(1);
        }

        try
        {
            // Delete the main prompt file
            File.Delete(promptFilePath);
            ConsoleHelpers.WriteLine($"Deleted: {promptFilePath}");

            // Delete the referenced file if it exists
            if (referencedFilePath != null && File.Exists(referencedFilePath))
            {
                File.Delete(referencedFilePath);
                ConsoleHelpers.WriteLine($"Deleted: {referencedFilePath}");
            }
            
            // Delete any additional files if they exist
            if (directory != null)
            {
                var additionalFiles = Directory.GetFiles(directory, additionalFilePattern);
                foreach (var additionalFile in additionalFiles)
                {
                    File.Delete(additionalFile);
                    ConsoleHelpers.WriteLine($"Deleted: {additionalFile}");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteErrorLine($"Error deleting prompt: {ex.Message}");
            return 1;
        }
    }
}