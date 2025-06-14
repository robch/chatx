/// <summary>
/// Command to add a new alias with raw arguments, without syntax validation.
/// </summary>

using System.Text;

class AliasAddCommand : AliasBaseCommand
{
    /// <summary>
    /// The name of the alias to add.
    /// </summary>
    public string? AliasName { get; set; }

    /// <summary>
    /// The content/arguments for the alias.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Constructor initializes the base command.
    /// </summary>
    public AliasAddCommand() : base()
    {
        Scope = ConfigFileScope.Local;
    }

    /// <summary>
    /// Checks if the command is empty (i.e., no alias name or content provided).
    /// </summary>
    /// <returns>True if empty, false otherwise.</returns>
    public override bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(AliasName);
    }

    /// <summary>
    /// Gets the command name.
    /// </summary>
    /// <returns>The command name.</returns>
    public override string GetCommandName()
    {
        return "alias add";
    }

    /// <summary>
    /// Execute the add command.
    /// </summary>
    /// <param name="interactive">Whether the command is running in interactive mode.</param>
    /// <returns>Exit code, 0 for success, non-zero for failure.</returns>
    public override async Task<object> ExecuteAsync(bool interactive)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(AliasName))
            {
                ConsoleHelpers.WriteErrorLine("Error: Alias name is required.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(Content))
            {
                ConsoleHelpers.WriteErrorLine("Error: Alias content is required. Provide it with as the next parameter after the alias name.");
                ConsoleHelpers.WriteLine("Example: cycod alias add myalias \"--system-prompt \\\"You are helpful\\\" --instruction\"");
                return 1;
            }

            return ExecuteAdd(AliasName, Content, Scope ?? ConfigFileScope.Local);
        });
    }

    /// <summary>
    /// Execute the add command for the specified alias name, content, and scope.
    /// </summary>
    /// <param name="aliasName">The name of the alias to add.</param>
    /// <param name="content">The content for the alias.</param>
    /// <param name="scope">The scope to save the alias in.</param>
    /// <returns>Exit code, 0 for success, non-zero for failure.</returns>
    private int ExecuteAdd(string aliasName, string content, ConfigFileScope scope)
    {
        ConsoleHelpers.WriteDebugLine($"ExecuteAdd; aliasName: {aliasName}; scope: {scope}");

        // Check if the alias already exists
        var existingAliasPath = AliasFileHelpers.FindAliasInScope(aliasName, scope);
        if (existingAliasPath != null)
        {
            ConsoleHelpers.WriteWarningLine($"Warning: Alias '{aliasName}' already exists in the {scope.ToString().ToLower()} scope and will be overwritten.");
        }

        // Remove any "cycod" executable name if the user accidentally included it
        if (content.TrimStart().StartsWith("cycod "))
        {
            content = content.TrimStart().Substring(6).TrimStart();
            ConsoleHelpers.WriteWarningLine("Note: 'cycod' prefix removed from alias content (not needed).");
        }

        var contentLines = TokenizeAliasValue(content);

        // Get the alias directory and ensure it exists
        var aliasDirectory = AliasFileHelpers.FindAliasDirectoryInScope(scope, create: true)!;
        var fileName = Path.Combine(aliasDirectory, $"{aliasName}.alias");

        try
        {
            // Write the content to the alias file
            File.WriteAllLines(fileName, contentLines);
            ConsoleHelpers.WriteLine($"Created alias '{aliasName}' in {scope.ToString().ToLower()} scope.");
            ConsoleHelpers.WriteLine($"Usage examples:");
            ConsoleHelpers.WriteLine($"  cycod --{aliasName} [additional arguments]");
            ConsoleHelpers.WriteLine($"  cycod [arguments] --{aliasName} [more arguments]");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteErrorLine($"Error creating alias: {ex.Message}");
            return 1;
        }
    }

    public static string[] TokenizeAliasValue(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return Array.Empty<string>();

        var args = new List<string>();
        var currentArg = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
            {
                // Handle escaped quotes
                currentArg.Append('"');
                i++; // Skip the next character (the quote)
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                // End of current argument
                if (currentArg.Length > 0)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                }

                // Skip consecutive whitespace
                while (i + 1 < commandLine.Length && char.IsWhiteSpace(commandLine[i + 1]))
                    i++;
            }
            else
            {
                currentArg.Append(c);
            }
        }

        // Add final argument if any
        if (currentArg.Length > 0)
            args.Add(currentArg.ToString());

        return args.ToArray();
    }
}