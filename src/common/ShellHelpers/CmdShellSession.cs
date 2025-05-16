public class CmdShellSession : ShellSession
{
    protected override PersistentShellType GetShellType()
    {
        return PersistentShellType.Cmd;
    }

    public static CmdShellSession Instance => _instance ??= new CmdShellSession();
    private static CmdShellSession? _instance;
}
