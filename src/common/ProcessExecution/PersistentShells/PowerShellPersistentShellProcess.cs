using System;
using System.Text.RegularExpressions;

/// <summary>
/// Process execution within a persistent PowerShell session.
/// </summary>
public class PowerShellPersistentShellProcess : PersistentShellProcess
{
    /// <summary>
    /// Gets the shell type for this process.
    /// </summary>
    public override PersistentShellType PersistentShellType => PersistentShellType.PowerShell;
    
    /// <summary>
    /// Creates a new PowerShell process with the specified underlying process.
    /// </summary>
    /// <param name="shellProcess">The shell process instance.</param>
    public PowerShellPersistentShellProcess(RunnableProcess shellProcess) : base(shellProcess)
    {
    }
    
    /// <summary>
    /// Gets a simple command to verify the shell is responsive.
    /// </summary>
    /// <returns>A command that outputs a predictable result.</returns>
    protected override string GetVerifyCommand()
    {
        return "Write-Output 'POWERSHELL_VERIFY_OK'";
    }
    
    /// <summary>
    /// Wraps a command with shell-specific code to output the marker and exit code.
    /// </summary>
    protected override string WrapCommandWithMarker(string command)
    {
        // Execute the command in a try/catch block, then output the marker and exit code
        return 
            "try { " +
            "$global:LASTEXITCODE = 0; " +  // Initialize to 0 before command
            command + 
            " } catch { " +
            "if (-not $LASTEXITCODE) { $global:LASTEXITCODE = 1 } " + 
            "} finally { " +
            "if (-not $?) { if ($LASTEXITCODE -eq 0) { $global:LASTEXITCODE = 1 } }" +
            "Write-Output \"" + _marker + "$LASTEXITCODE\"" +
            " }";
    }
    
    /// <summary>
    /// Extracts shell-specific error messages from the stderr output.
    /// </summary>
    /// <param name="stderr">The standard error output.</param>
    /// <returns>Shell-specific error message if found, otherwise null.</returns>
    protected override string? ExtractShellSpecificError(string stderr)
    {
        if (string.IsNullOrEmpty(stderr))
            return null;
            
        // Look for common PowerShell error patterns
        var syntaxMatch = Regex.Match(stderr, @"ParseException|SyntaxError", RegexOptions.IgnoreCase);
        if (syntaxMatch.Success)
        {
            var errorLineMatch = Regex.Match(stderr, @"At line:(\d+) char:(\d+)[\r\n]+(.+?)[\r\n]+", RegexOptions.Singleline);
            if (errorLineMatch.Success && errorLineMatch.Groups.Count >= 4)
            {
                return $"Syntax error at line {errorLineMatch.Groups[1].Value}, char {errorLineMatch.Groups[2].Value}: {errorLineMatch.Groups[3].Value.Trim()}";
            }
            return "PowerShell syntax error";
        }
            
        var commandNotFoundMatch = Regex.Match(stderr, @"CommandNotFoundException", RegexOptions.IgnoreCase);
        if (commandNotFoundMatch.Success)
        {
            var cmdMatch = Regex.Match(stderr, @"The term '(.+?)' is not recognized", RegexOptions.IgnoreCase);
            if (cmdMatch.Success && cmdMatch.Groups.Count >= 2)
            {
                return $"Command not found: {cmdMatch.Groups[1].Value}";
            }
            return "Command not found";
        }
            
        // For other errors, try to extract the most relevant part
        var errorTextMatch = Regex.Match(stderr, @"(?:Exception|Error):\s*(.+?)(?:\r?\n|$)", RegexOptions.IgnoreCase);
        if (errorTextMatch.Success && errorTextMatch.Groups.Count >= 2)
        {
            return errorTextMatch.Groups[1].Value.Trim();
        }
            
        // Return the first line if no specific pattern is matched
        var lines = stderr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 ? lines[0] : null;
    }
    
    /// <summary>
    /// Determines if the command resulted in a syntax error.
    /// </summary>
    /// <param name="exitCode">The command exit code.</param>
    /// <param name="stderr">The standard error output.</param>
    /// <returns>True if the error was a syntax error, otherwise false.</returns>
    protected override bool IsSyntaxError(int exitCode, string stderr)
    {
        // In PowerShell, check for specific error messages
        return Regex.IsMatch(stderr, @"(ParseException|SyntaxError|ParserError)", RegexOptions.IgnoreCase);
    }
}