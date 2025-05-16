public class CycoDtTestFrameworkLogger : IYamlTestFrameworkLogger
{
    public void LogVerbose(string text)
    {
        ConsoleHelpers.WriteDebugLine($"VERBOSE: {text}");
    }

    public void LogInfo(string text)
    {
        ConsoleHelpers.WriteDebugLine($"INFO: {text}");
    }

    public void LogWarning(string text)
    {
        ConsoleHelpers.WriteWarningLine($"WARNING: {text}");
    }

    public void LogError(string text)
    {
        ConsoleHelpers.WriteErrorLine($"ERROR: {text}");
    }
}
