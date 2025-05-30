using System;

class RunCommand : CycoDmdCommand
{
    public enum ScriptType
    {
        Default, // Uses cmd on Windows, bash on Linux/Mac
        Cmd,
        Bash,
        PowerShell
    }

    public RunCommand() : base()
    {
        ScriptToRun = string.Empty;
        Type = ScriptType.Default;
    }

    override public string GetCommandName()
    {
        return "run";
    }

    override public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(ScriptToRun);
    }

    override public CycoDmdCommand Validate()
    {
        return this;
    }

    public string ScriptToRun { get; set; }
    public ScriptType Type { get; set; }
}
