using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Utility methods for process preparation and argument handling.
/// </summary>
public static class PersistentShellHelpers
{
    /// <summary>
    /// Escapes an argument for the appropriate shell based on shell type.
    /// </summary>
    /// <param name="shellType">The type of shell.</param>
    /// <param name="arg">The argument to escape.</param>
    /// <param name="forStdin">Whether the argument is for stdin injection vs. command line.</param>
    /// <returns>The escaped argument.</returns>
    public static string EscapeShellArgument(PersistentShellType shellType, string arg, bool forStdin = false)
    {
        switch (shellType)
        {
            case PersistentShellType.Bash:
                return ProcessHelpers.EscapeBashArgument(arg, forStdin);
            case PersistentShellType.Cmd:
                return ProcessHelpers.EscapeCmdArgument(arg, forStdin);
            case PersistentShellType.PowerShell:
                return ProcessHelpers.EscapePowerShellArgument(arg, forStdin);
            default:
                throw new ArgumentException($"Unsupported shell type: {shellType}");
        }
    }
    
    /// <summary>
    /// Gets the default shell type based on the current operating system.
    /// </summary>
    /// <returns>The default shell type for the current OS.</returns>
    public static PersistentShellType GetDefaultShellType()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return PersistentShellType.Cmd;
        }
        else
        {
            return PersistentShellType.Bash;
        }
    }
    
    /// <summary>
    /// Gets the appropriate shell executable file name based on shell type and platform.
    /// </summary>
    /// <param name="shellType">The type of shell.</param>
    /// <returns>The shell executable file name.</returns>
    public static string GetShellFileNameFromPersistentShellType(PersistentShellType shellType)
    {
        switch (shellType)
        {
            case PersistentShellType.Cmd:
                return "cmd.exe";

            case PersistentShellType.Bash:
                return OS.IsWindows()
                    ? ProcessHelpers.FindBashExe()
                    : "bash";

            case PersistentShellType.PowerShell:
                return OS.IsWindows()
                    ? ProcessHelpers.FindPwshExe()
                    : "pwsh";

            default:
                throw new ArgumentException($"Unsupported shell type: {shellType}");
        }
    }
    
    /// <summary>
    /// Gets standard arguments to start a shell in interactive mode.
    /// </summary>
    /// <param name="shellType">The type of shell.</param>
    /// <returns>The arguments to start the shell.</returns>
    public static string GetPersistentShellStartupArgs(PersistentShellType shellType)
    {
        switch (shellType)
        {
            case PersistentShellType.Cmd:
                return "/K chcp 65001"; // Keep session open, set UTF-8 encoding
                
            case PersistentShellType.Bash:
                return "--norc --noprofile";

            case PersistentShellType.PowerShell:
                return "-NoExit -Command -"; // Keep session open, accept commands from stdin
                
            default:
                throw new ArgumentException($"Unsupported shell type: {shellType}");
        }
    }
}