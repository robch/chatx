using System;
using System.Text.RegularExpressions;

/// <summary>
/// Process execution within a persistent Bash shell session.
/// </summary>
public class BashPersistentShellProcess : PersistentShellProcess
{
    /// <summary>
    /// Gets the shell type for this process.
    /// </summary>
    public override PersistentShellType PersistentShellType => PersistentShellType.Bash;
    
    /// <summary>
    /// Creates a new Bash shell process with the specified underlying process.
    /// </summary>
    /// <param name="shellProcess">The shell process instance.</param>
    public BashPersistentShellProcess(RunnableProcess shellProcess) : base(shellProcess)
    {
    }
    
    /// <summary>
    /// Gets a simple command to verify the shell is responsive.
    /// </summary>
    /// <returns>A command that outputs a predictable result.</returns>
    protected override string GetVerifyCommand()
    {
        return "echo 'BASH_VERIFY_OK'";
    }
    
    /// <summary>
    /// Wraps a command with shell-specific code to output the marker and exit code.
    /// </summary>
    protected override string WrapCommandWithMarker(string command)
    {
        command = command.Replace("\r\n", "\n");
        var hasLFs = command.Contains("\n");
        var hasAndOr = command.Contains("&&") || command.Contains("||");
        var wrapInSubShell = hasLFs || hasAndOr;
        if (wrapInSubShell)
        {
            command = $"( {command} )";
        }

        // Try to set UTF-8 locale if available, but don't fail if it's not
        // Explicitly disable job control with 'set +m' to prevent backgrounding issues
        return "{ set +m; export LC_ALL=C.UTF-8 2>/dev/null || export LC_ALL=en_US.UTF-8 2>/dev/null || true; " + command + " ; EC=$?; echo " + _marker + "$EC; }";
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
            
        // Look for common Bash error patterns
        var syntaxMatch = Regex.Match(stderr, @"^(syntax error|parse error).*", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (syntaxMatch.Success)
            return syntaxMatch.Value;
            
        var commandNotFoundMatch = Regex.Match(stderr, @"^.*: command not found$", RegexOptions.Multiline);
        if (commandNotFoundMatch.Success)
            return commandNotFoundMatch.Value;
            
        var permissionDeniedMatch = Regex.Match(stderr, @"^.*: Permission denied$", RegexOptions.Multiline);
        if (permissionDeniedMatch.Success)
            return permissionDeniedMatch.Value;
            
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
        // In Bash, syntax errors typically have exit code 2
        if (exitCode == 2)
            return true;
            
        // Also check the stderr for syntax error messages
        return Regex.IsMatch(stderr, @"(syntax error|parse error)", RegexOptions.IgnoreCase);
    }
}