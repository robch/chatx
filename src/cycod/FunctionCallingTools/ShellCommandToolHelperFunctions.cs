using System.ComponentModel;

public class ShellCommandToolHelperFunctions
{
    [Description("Run commands in a bash shell. This persistent session maintains state across commands. " +
        "You can run commands such as Python, Node.js and Go, and install packages with apt, pip, npm, or go. " +
        "If a command times out, the session is reset.")]
    public async Task<string> RunBashCommandAsync(
        [Description("The bash command to run.")] string command,
        [Description("Timeout in milliseconds for the command execution.")] int timeoutMs = 60000)
    {
        try
        {
            var result = await BashShellSession.Instance.ExecuteCommandAsync(command, timeoutMs);
            var output = result.merged;
            if (result.exitCode != 0)
            {
                output += Environment.NewLine + $"<exited with exit code {result.exitCode}>";
            }
            return output;
        }
        catch (TimeoutException te)
        {
            // If there is a timeout, reset the session.
            BashShellSession.Instance.ForceShutdown();
            return $"<timed out and killed process - environment state has been reset>\n{te.Message}";
        }
        catch (Exception ex)
        {
            return $"<exited with error: {ex.Message}>";
        }
    }

    [Description("Run commands in a cmd shell. This persistent session maintains state across commands. " +
        "You can run commands such as Python, Node.js and Go, and install packages with winget, pip, npm, or go. " +
        "If a command times out, the session is reset.")]
    public async Task<string> RunCmdCommandAsync(
        [Description("The cmd command to run.")] string command,
        [Description("Timeout in milliseconds for the command execution.")] int timeoutMs = 60000)
    {
        try
        {
            var result = await CmdShellSession.Instance.ExecuteCommandAsync(command, timeoutMs);
            var output = result.merged;
            if (result.exitCode != 0)
            {
                output += Environment.NewLine + $"<exited with exit code {result.exitCode}>";
            }
            return output;
        }
        catch (TimeoutException te)
        {
            // If there is a timeout, reset the session.
            CmdShellSession.Instance.ForceShutdown();
            return $"<timed out and killed process - environment state has been reset>\n{te.Message}";
        }
        catch (Exception ex)
        {
            return $"<exited with error: {ex.Message}>";
        }
    }

    [Description("Run commands in a PowerShell shell. This persistent session maintains state across commands. " +
        "You can run commands such as Python, Node.js and Go, and install packages with winget, pip, npm, or go. " +
        "If a command times out, the session is reset.")]
    public async Task<string> RunPowershellCommandAsync(
        [Description("The PowerShell command to run.")] string command,
        [Description("Timeout in milliseconds for the command execution.")] int timeoutMs = 60000)
    {
        try
        {
            var result = await PowershellShellSession.Instance.ExecuteCommandAsync(command, timeoutMs);
            var output = result.merged;
            if (result.exitCode != 0)
            {
                output += Environment.NewLine + $"<exited with exit code {result.exitCode}>";
            }
            return output;
        }
        catch (TimeoutException te)
        {
            // If there is a timeout, reset the session.
            PowershellShellSession.Instance.ForceShutdown();
            return $"<timed out and killed process - environment state has been reset>\n{te.Message}";
        }
        catch (Exception ex)
        {
            return $"<exited with error: {ex.Message}>";
        }
    }
}

