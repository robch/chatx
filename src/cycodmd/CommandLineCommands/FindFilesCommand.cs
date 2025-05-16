using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class FindFilesCommand : CycoDmdCommand
{
    public FindFilesCommand()
    {
        Globs = new();
        ExcludeGlobs = new();
        ExcludeFileNamePatternList = new();

        IncludeFileContainsPatternList = new();
        ExcludeFileContainsPatternList = new();

        IncludeLineContainsPatternList = new();
        IncludeLineCountBefore = 0;
        IncludeLineCountAfter = 0;
        IncludeLineNumbers = false;

        RemoveAllLineContainsPatternList = new();
        FileInstructionsList = new();
    }

    override public string GetCommandName()
    {
        return "";
    }

    override public bool IsEmpty()
    {
        return !Globs.Any() &&
            !ExcludeGlobs.Any() &&
            !ExcludeFileNamePatternList.Any() &&
            !IncludeFileContainsPatternList.Any() &&
            !ExcludeFileContainsPatternList.Any() &&
            !IncludeLineContainsPatternList.Any() &&
            IncludeLineCountBefore == 0 &&
            IncludeLineCountAfter == 0 &&
            IncludeLineNumbers == false &&
            !RemoveAllLineContainsPatternList.Any() &&
            !FileInstructionsList.Any();
    }

    override public CycoDmdCommand Validate()
    {
        if (!Globs.Any()) 
        {
            Globs.Add("**");
        }

        var ignoreFile = FileHelpers.FindFileSearchParents(".cycodmdignore");
        if (ignoreFile != null)
        {
            FileHelpers.ReadIgnoreFile(ignoreFile, out var excludeGlobs, out var excludeFileNamePatternList);
            ExcludeGlobs.AddRange(excludeGlobs);
            ExcludeFileNamePatternList.AddRange(excludeFileNamePatternList);
        }

        return this;
    }

    public List<string> Globs;
    public List<string> ExcludeGlobs;
    public List<Regex> ExcludeFileNamePatternList;

    public List<Regex> IncludeFileContainsPatternList;
    public List<Regex> ExcludeFileContainsPatternList;

    public List<Regex> IncludeLineContainsPatternList;
    public int IncludeLineCountBefore;
    public int IncludeLineCountAfter;
    public bool IncludeLineNumbers;
    public List<Regex> RemoveAllLineContainsPatternList;

    public List<Tuple<string, string>> FileInstructionsList;

    public string? SaveFileOutput;
}
