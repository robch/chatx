using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Wrapper class for executing CYCODMD CLI commands and processing their output.
/// </summary>
public class CycoDmdCliWrapper
{
    /// <summary>
    /// Executes an CYCODMD command and returns the output.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the CYCODMD command</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <returns>The command output as a string</returns>
    public async Task<string> ExecuteCycoDmdCommandAsync(string arguments, int timeoutMs = 120000)
    {
        try
        {
            if (ConsoleHelpers.IsVerbose())
            {
                ConsoleHelpers.WriteLine($"Executing CYCODMD command: cycodmd {arguments}");
            }

            // Create a process builder to execute the cycodmd command
            var processBuilder = new RunnableProcessBuilder()
                .WithFileName("cycodmd")
                .WithArguments(arguments)
                .WithTimeout(timeoutMs)
                .WithVerboseLogging(ConsoleHelpers.IsVerbose());

            // Add callbacks if verbose logging is enabled
            if (ConsoleHelpers.IsVerbose())
            {
                processBuilder.OnOutput(line => {
                    var trimmedLine = line.TrimEnd(new char[] { '\r', '\n' });
                    ConsoleHelpers.WriteLine(trimmedLine, ConsoleColor.DarkMagenta);
                });
                
                processBuilder.OnError(line => {
                    var trimmedLine = line.TrimEnd(new char[] { '\r', '\n' });
                    ConsoleHelpers.WriteErrorLine(trimmedLine);
                });
            }

            // Run the process and get the result
            var result = await processBuilder.RunAsync();

            string output = result.MergedOutput.TrimEnd();
            
            // Add exit code message if not successful
            if (result.ExitCode != 0)
            {
                output += Environment.NewLine + $"<cycodmd command exited with code {result.ExitCode}>";
            }
            
            // Handle the case where the process timed out
            if (result.CompletionState == ProcessCompletionState.TimedOut)
            {
                output = $"<waiting {timeoutMs}ms for cycodmd to finish, timed out>\n{output}";
            }
            
            // Write debug files if verbose is enabled
            if (ConsoleHelpers.IsVerbose())
            {
                var baseFileName = FileHelpers.GetFileNameFromTemplate("cycodmd_command.txt", "cycodmd-debug-{time}");
                Thread.Sleep(10); // Wait a bit, so template filename w/ time doesn't conflict with anything else

                ConsoleHelpers.WriteLine($"ExecuteCycoDmdCommandAsync inputs/outputs: {baseFileName}-*", ConsoleColor.DarkMagenta, overrideQuiet: true);
                FileHelpers.WriteAllText($"{baseFileName}-command.txt", $"cycodmd {arguments}");
                FileHelpers.WriteAllText($"{baseFileName}-stdout.txt", result.StandardOutput);
                FileHelpers.WriteAllText($"{baseFileName}-stderr.txt", result.StandardError);
                FileHelpers.WriteAllText($"{baseFileName}-merged.txt", result.MergedOutput);
            }
            
            return output;
        }
        catch (Exception ex)
        {
            return $"<cycodmd command exited with error: {ex.Message}>";
        }
    }

    /// <summary>
    /// Escapes an argument for use in a command line.
    /// </summary>
    /// <param name="arg">The argument to escape</param>
    /// <returns>The escaped argument</returns>
    public string EscapeArgument(string arg)
    {
        return ProcessHelpers.EscapeProcessArgument(arg);
    }

    #region Search Codebase Methods
    
    /// <summary>
    /// Executes CYCODMD command to search codebase for specific patterns.
    /// </summary>
    public async Task<string> ExecuteSearchCodebaseCommand(
        string[] filePatterns,
        string contentPattern,
        string[]? excludePatterns = null,
        bool showLineNumbers = true,
        int? contextLines = null,
        string? processingInstructions = null)
    {
        var arguments = BuildSearchCodebaseArguments(
            filePatterns, 
            contentPattern, 
            excludePatterns, 
            showLineNumbers, 
            contextLines, 
            processingInstructions);
            
        var output = await ExecuteCycoDmdCommandAsync(arguments);
        var noFilesFound = output.Contains("No files matched criteria") || output.Contains("No files found");
        var wasntRecursive = !contentPattern.Contains("**");
        if (noFilesFound && wasntRecursive)
        {
            output = $"{output}\n\n<You may want to try using '**' in your file pattern to search recursively.>";
            output = $"{output}\n<You may want to try using '(?i)' in your content pattern to search case-insensitively.>";
        }

        return output;
    }
    
    /// <summary>
    /// Builds command arguments for searching codebase.
    /// </summary>
    private string BuildSearchCodebaseArguments(
        string[] filePatterns,
        string contentPattern,
        string[]? excludePatterns = null,
        bool showLineNumbers = true,
        int? contextLines = null,
        string? processingInstructions = null)
    {
        var sb = new StringBuilder();
        
        // Add file patterns
        foreach (var pattern in filePatterns)
        {
            sb.Append($"{EscapeArgument(pattern)} ");
        }
        
        // Add exclude patterns
        if (excludePatterns != null && excludePatterns.Length > 0)
        {
            sb.Append("--exclude ");
            foreach (var pattern in excludePatterns)
            {
                sb.Append($"{EscapeArgument(pattern)} ");
            }
        }
        
        // Add content pattern for searching within files
        if (!string.IsNullOrEmpty(contentPattern))
        {
            sb.Append($"--contains {EscapeArgument(contentPattern)} ");
        }
        
        // Add line numbers option
        if (showLineNumbers)
        {
            sb.Append("--line-numbers ");
        }
        
        // Add context lines
        if (contextLines.HasValue)
        {
            sb.Append($"--lines {contextLines.Value} ");
        }
        
        // Add processing instructions
        if (!string.IsNullOrEmpty(processingInstructions))
        {
            sb.Append($"--instructions {EscapeArgument(processingInstructions)} ");
        }
        
        return sb.ToString().Trim();
    }
    
    /// <summary>
    /// Executes CYCODMD command to find files containing specific patterns.
    /// </summary>
    public async Task<string> ExecuteFindFilesContainingPatternCommand(
        string[] filePatterns,
        string contentPattern,
        string[]? excludePatterns = null,
        string? processingInstructions = null)
    {
        var arguments = BuildFindFilesContainingPatternArguments(
            filePatterns, 
            contentPattern, 
            excludePatterns,
            processingInstructions);
            
        var output = await ExecuteCycoDmdCommandAsync(arguments);
        var noFilesFound = output.Contains("No files matched criteria") || output.Contains("No files found");
        var wasntRecursive = !contentPattern.Contains("**");
        if (noFilesFound && wasntRecursive)
        {
            output = $"{output}\n\n<You may want to try using '**' in your content pattern to search recursively.>";
            output = $"{output}\n<You may want to try using '(?i)' in your content pattern to search case-insensitively.>";
        }

        return output;
    }
    
    /// <summary>
    /// Builds command arguments for finding files containing patterns.
    /// </summary>
    private string BuildFindFilesContainingPatternArguments(
        string[] filePatterns,
        string contentPattern,
        string[]? excludePatterns = null,
        string? processingInstructions = null)
    {
        var sb = new StringBuilder();
        
        // Add file patterns
        foreach (var pattern in filePatterns)
        {
            sb.Append($"{EscapeArgument(pattern)} ");
        }
        
        // Add exclude patterns
        if (excludePatterns != null && excludePatterns.Length > 0)
        {
            sb.Append("--exclude ");
            foreach (var pattern in excludePatterns)
            {
                sb.Append($"{EscapeArgument(pattern)} ");
            }
        }
        
        // Add content pattern for searching within files
        if (!string.IsNullOrEmpty(contentPattern))
        {
            sb.Append($"--contains {EscapeArgument(contentPattern)} ");
        }
        
        // Add processing instructions
        if (!string.IsNullOrEmpty(processingInstructions))
        {
            sb.Append($"--instructions {EscapeArgument(processingInstructions)} ");
        }
        
        return sb.ToString().Trim();
    }
    
    #endregion
    
    #region Documentation Generation Methods
    
    /// <summary>
    /// Executes CYCODMD command to convert files to markdown.
    /// </summary>
    public async Task<string> ExecuteConvertFilesToMarkdownCommand(
        string[] filePaths,
        string? formattingInstructions = null,
        bool showLineNumbers = false)
    {
        var arguments = BuildConvertFilesToMarkdownArguments(
            filePaths, 
            formattingInstructions, 
            showLineNumbers);
            
        return await ExecuteCycoDmdCommandAsync(arguments);
    }
    
    /// <summary>
    /// Builds command arguments for converting files to markdown.
    /// </summary>
    private string BuildConvertFilesToMarkdownArguments(
        string[] filePaths,
        string? formattingInstructions = null,
        bool showLineNumbers = false)
    {
        var sb = new StringBuilder();
        
        // Add file paths
        foreach (var path in filePaths)
        {
            sb.Append($"{EscapeArgument(path)} ");
        }
        
        // Add line numbers option
        if (showLineNumbers)
        {
            sb.Append("--line-numbers ");
        }
        
        // Add formatting instructions
        if (!string.IsNullOrEmpty(formattingInstructions))
        {
            sb.Append($"--instructions {EscapeArgument(formattingInstructions)} ");
        }
        
        return sb.ToString().Trim();
    }
    
    #endregion
    
    #region Web Research Methods
    
    /// <summary>
    /// Executes CYCODMD command to research web topics.
    /// </summary>
    public async Task<string> ExecuteResearchWebTopicCommand(
        string searchQuery,
        bool includePageContent = true,
        int maxResults = 5,
        string searchEngine = "duckduckgo",
        bool stripHtml = true,
        string? processingInstructions = null)
    {
        var arguments = BuildResearchWebTopicArguments(
            searchQuery, 
            includePageContent, 
            maxResults, 
            searchEngine, 
            stripHtml, 
            processingInstructions);
            
        return await ExecuteCycoDmdCommandAsync($"{arguments} --firefox");
    }
    
    /// <summary>
    /// Builds command arguments for researching web topics.
    /// </summary>
    private string BuildResearchWebTopicArguments(
        string searchQuery,
        bool includePageContent = true,
        int maxResults = 5,
        string searchEngine = "duckduckgo",
        bool stripHtml = true,
        string? processingInstructions = null)
    {
        var sb = new StringBuilder();
        
        // Start with web search command
        sb.Append("web search ");
        
        // Add search query
        sb.Append($"{EscapeArgument(searchQuery)} ");
        
        // Add search engine option
        sb.Append($"--{searchEngine.ToLower()} ");
        
        // Limit results
        sb.Append($"--max {maxResults} ");
        
        // Add option to get page content
        if (includePageContent)
        {
            sb.Append("--get ");
            
            // Strip HTML if requested
            if (stripHtml)
            {
                sb.Append("--strip ");
            }
        }
        
        // Add processing instructions
        if (!string.IsNullOrEmpty(processingInstructions))
        {
            sb.Append($"--instructions {EscapeArgument(processingInstructions)} ");
        }
        
        return sb.ToString().Trim();
    }
    
    /// <summary>
    /// Executes CYCODMD command to extract content from web pages.
    /// </summary>
    public async Task<string> ExecuteExtractContentFromWebPagesCommand(
        string[] urls,
        bool stripHtml = true,
        string? pageProcessingInstructions = null,
        string? finalProcessingInstructions = null)
    {
        var arguments = BuildExtractContentFromWebPagesArguments(
            urls, 
            stripHtml, 
            pageProcessingInstructions, 
            finalProcessingInstructions);
            
        return await ExecuteCycoDmdCommandAsync($"{arguments} --firefox");
    }
    
    /// <summary>
    /// Builds command arguments for extracting content from web pages.
    /// </summary>
    private string BuildExtractContentFromWebPagesArguments(
        string[] urls,
        bool stripHtml = true,
        string? pageProcessingInstructions = null,
        string? finalProcessingInstructions = null)
    {
        var sb = new StringBuilder();
        
        // Start with web get command
        sb.Append("web get ");
        
        // Add URLs
        foreach (var url in urls)
        {
            sb.Append($"{EscapeArgument(url)} ");
        }
        
        // Strip HTML if requested
        if (stripHtml)
        {
            sb.Append("--strip ");
        }
        
        // Add page processing instructions
        if (!string.IsNullOrEmpty(pageProcessingInstructions))
        {
            sb.Append($"--page-instructions {EscapeArgument(pageProcessingInstructions)} ");
        }
        
        // Add final processing instructions
        if (!string.IsNullOrEmpty(finalProcessingInstructions))
        {
            sb.Append($"--instructions {EscapeArgument(finalProcessingInstructions)} ");
        }
        
        return sb.ToString().Trim();
    }
    
    #endregion
    
    #region Command Analysis Methods
    
    /// <summary>
    /// Executes CYCODMD command to run commands and format their output.
    /// </summary>
    public async Task<string> ExecuteRunCommandsAndFormatOutputCommand(
        string[] commands,
        string shell = "cmd",
        string? processingInstructions = null)
    {
        var arguments = BuildRunCommandsAndFormatOutputArguments(
            commands, 
            shell, 
            processingInstructions);
            
        return await ExecuteCycoDmdCommandAsync(arguments);
    }
    
    /// <summary>
    /// Builds command arguments for running commands and formatting their output.
    /// </summary>
    private string BuildRunCommandsAndFormatOutputArguments(
        string[] commands,
        string shell = "cmd",
        string? processingInstructions = null)
    {
        var sb = new StringBuilder();
        
        // Start with run command
        sb.Append("run ");
        
        // Add shell option if not default
        if (!string.IsNullOrEmpty(shell) && shell.ToLower() != "cmd")
        {
            sb.Append($"--{shell.ToLower()} ");
        }
        
        // Add commands
        foreach (var cmd in commands)
        {
            sb.Append($"{EscapeArgument(cmd)} ");
        }
        
        // Add processing instructions
        if (!string.IsNullOrEmpty(processingInstructions))
        {
            sb.Append($"--instructions {EscapeArgument(processingInstructions)} ");
        }
        
        return sb.ToString().Trim();
    }
    
    #endregion
    
}