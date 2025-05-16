public class PowershellShellSession : ShellSession
{
    protected override PersistentShellType GetShellType()
    {
        return PersistentShellType.PowerShell;
    }

    public static PowershellShellSession Instance => _instance ??= new PowershellShellSession();
    private static PowershellShellSession? _instance;
}
