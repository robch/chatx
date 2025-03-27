using System;

/// <summary>
/// Base class for all configuration-related commands.
/// </summary>
abstract class ConfigBaseCommand : Command
{
    protected readonly ConfigStore _configStore;
    
    public bool Global { get; set; }
    public bool User { get; set; }

    /// <summary>
    /// Initializes a new instance of the ConfigBaseCommand class.
    /// </summary>
    public ConfigBaseCommand()
    {
        _configStore = ConfigStore.Instance;
    }

    /// <summary>
    /// Gets the description of the command.
    /// </summary>
    public virtual string Description => "Manage ChatX configuration settings";

    /// <summary>
    /// Determines the configuration scope based on command options.
    /// </summary>
    /// <returns>The selected configuration scope.</returns>
    protected ConfigFileScope GetConfigScope()
    {
        if (Global)
        {
            return ConfigFileScope.Global;
        }
        else if (User)
        {
            return ConfigFileScope.User;
        }
        return ConfigFileScope.Local;
    }

    /// <summary>
    /// Indicates whether the command is empty.
    /// </summary>
    public override bool IsEmpty()
    {
        return false;
    }
}