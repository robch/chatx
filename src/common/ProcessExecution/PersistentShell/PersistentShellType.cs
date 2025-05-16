/// <summary>
/// Types of shells supported by the process executors.
/// </summary>
public enum PersistentShellType
{
    /// <summary>
    /// Bash shell (default on Linux/macOS).
    /// </summary>
    Bash,
    
    /// <summary>
    /// CMD shell (Windows command prompt).
    /// </summary>
    Cmd,
    
    /// <summary>
    /// PowerShell.
    /// </summary>
    PowerShell
}