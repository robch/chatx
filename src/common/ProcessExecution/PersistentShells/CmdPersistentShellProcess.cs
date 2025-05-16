using System;
using System.Text.RegularExpressions;

/// <summary>
/// Process execution within a persistent CMD shell session.
/// </summary>
public class CmdPersistentShellProcess : PersistentShellProcess
{
    /// <summary>
    /// Gets the shell type for this process.
    /// </summary>
    public override PersistentShellType PersistentShellType => PersistentShellType.Cmd;
    
    /// <summary>
    /// Creates a new CMD shell process with the specified underlying process.
    /// </summary>
    /// <param name="shellProcess">The shell process instance.</param>
    public CmdPersistentShellProcess(RunnableProcess shellProcess) : base(shellProcess)
    {
    }
    
    /// <summary>
    /// Gets a simple command to verify the shell is responsive.
    /// </summary>
    /// <returns>A command that outputs a predictable result.</returns>
    protected override string GetVerifyCommand()
    {
        return "echo CMD_VERIFY_OK";
    }
    
    /// <summary>
    /// Wraps a command with shell-specific code to output the marker and exit code.
    /// </summary>
    protected override string WrapCommandWithMarker(string command)
    {
        // Execute the command, then echo the marker and errorlevel
        return $"{command} & echo {_marker}%ERRORLEVEL%";
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
            
        // Look for common CMD error patterns
        var notRecognizedMatch = Regex.Match(stderr, @"'.*' is not recognized as an internal or external command", RegexOptions.IgnoreCase);
        if (notRecognizedMatch.Success)
            return notRecognizedMatch.Value;
            
        var syntaxMatch = Regex.Match(stderr, @"The syntax of the command is incorrect", RegexOptions.IgnoreCase);
        if (syntaxMatch.Success)
            return syntaxMatch.Value;
            
        var accessDeniedMatch = Regex.Match(stderr, @"Access is denied", RegexOptions.IgnoreCase);
        if (accessDeniedMatch.Success)
            return accessDeniedMatch.Value;
            
        // Return the first line if no specific pattern is matched
        var lines = stderr.Split('\n');
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
        // In CMD, syntax errors can have various exit codes
        // Check the stderr for syntax error messages
        return Regex.IsMatch(stderr, @"(The syntax of the command is incorrect|unexpected at this time)", RegexOptions.IgnoreCase);
    }
}