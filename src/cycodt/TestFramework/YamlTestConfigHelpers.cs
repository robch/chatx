public class YamlTestConfigHelpers
{
    public static FileInfo? FindTestConfigFile(DirectoryInfo checkHereAndParents)
    {
        Logger.Log($"YamlTestConfigHelpers.GetTestConfigFile: Looking for test config file in {checkHereAndParents.FullName}");

        var existing = ScopeFileHelpers.FindFileInAnyScope(YamlTestFramework.YamlTestsConfigFileName, YamlTestFramework.YamlTestsConfigDirectoryName, true);
        if (existing == null)
        {
            var testDir = ScopeFileHelpers.EnsureDirectoryInScope(YamlTestFramework.YamlTestsConfigDirectoryName, ConfigFileScope.Local);
            var newConfigFileName = PathHelpers.Combine(testDir, YamlTestFramework.YamlTestsConfigFileName);
            FileHelpers.WriteAllText(newConfigFileName!, string.Empty);
            existing = newConfigFileName;
        }

        var configFile = new FileInfo(existing!);
        if (configFile?.Exists ?? false)
        {
            Logger.Log($"YamlTestConfigHelpers.GetTestConfigFile: Found test config file at {configFile.FullName}");
            return configFile;
        }
        
        return null;
    }

    public static DirectoryInfo GetTestDirectory(DirectoryInfo? checkHereAndParents = null)
    {
        checkHereAndParents ??= new DirectoryInfo(Directory.GetCurrentDirectory());

        var file = FindTestConfigFile(checkHereAndParents);
        if (file != null)
        {
            var tags = YamlTagHelpers.GetTagsFromFile(file.FullName);
            if (tags != null && tags.ContainsKey("testDirectory"))
            {
                var testDirectory = tags["testDirectory"].FirstOrDefault();
                if (testDirectory != null)
                {
                    testDirectory = PathHelpers.Combine(file.Directory!.FullName, testDirectory);
                    Logger.Log($"YamlTestConfigHelpers.GetTestDirectory: Found test directory in config file at {testDirectory}");
                    return new DirectoryInfo(testDirectory!);
                }
            }
        }

        file = YamlTagHelpers.FindDefaultTagsFile(checkHereAndParents);
        if (file != null)
        {
            var tags = YamlTagHelpers.GetTagsFromFile(file.FullName);
            if (tags != null && tags.ContainsKey("testDirectory"))
            {
                var testDirectory = tags["testDirectory"].FirstOrDefault();
                if (testDirectory != null)
                {
                    testDirectory = PathHelpers.Combine(file.Directory!.FullName, testDirectory);
                    Logger.Log($"YamlTestConfigHelpers.GetTestDirectory: Found test directory in default tags file at {testDirectory}");
                    return new DirectoryInfo(testDirectory!);
                }
            }
        }

        Logger.Log($"YamlTestConfigHelpers.GetTestDirectory: No test directory found; using {checkHereAndParents.FullName}");
        return checkHereAndParents;
    }
}
