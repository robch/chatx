public class BashShellSession : ShellSession
{
    protected override PersistentShellType GetShellType()
    {
        return PersistentShellType.Bash;
    }

    public static BashShellSession Instance => _instance ??= new BashShellSession();
    private static BashShellSession? _instance;
}
