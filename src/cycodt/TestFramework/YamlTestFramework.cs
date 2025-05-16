using Microsoft.VisualStudio.TestPlatform.ObjectModel;

public class YamlTestFramework
{
    public static IEnumerable<TestCase> GetTestsFromYaml(string source, FileInfo file)
    {
        Logger.Log($"YamlTestFramework.GetTestsFromYaml('{source}', '{file.FullName}'): ENTER");
        var tests = YamlTestCaseParser.TestCasesFromYaml(source, file);

        Logger.Log($"YamlTestFramework.GetTestsFromYaml('{source}', '{file.FullName}'): EXIT");
        return tests;
    }

    public static IDictionary<string, IList<TestResult>> RunTests(IEnumerable<TestCase> tests, IYamlTestFrameworkHost host)
    {
        Logger.Log($"YamlTestFramework.RunTests(): ENTER (test count: {tests.Count()})");

        tests = tests.ToList(); // force enumeration
        var runnableTests = tests.Select(test => new RunnableTestCase(test)).ToList();

        var runnableTestItems = runnableTests.SelectMany(x => x.Items).ToList();
        var groups = GetPriorityGroups(runnableTestItems);

        var resultsByTestCaseIdMap = InitResultsByTestCaseIdMap(tests);
        foreach (var group in groups)
        {
            if (group.Count == 0) continue;

            var resultsByTestCaseIdForGroup = RunAndRecordTests(host, group);
            foreach (var resultsForTestCase in resultsByTestCaseIdForGroup)
            {
                var testCaseId = resultsForTestCase.Key;
                var testResults = resultsForTestCase.Value;
                foreach (var result in testResults)
                {
                    resultsByTestCaseIdMap[testCaseId].Add(result);
                }
            }
        }

        Logger.Log($"YamlTestFramework.RunTests(): EXIT");
        return resultsByTestCaseIdMap;
    }

    #region private methods

    private static Dictionary<string, IList<TestResult>> InitResultsByTestCaseIdMap(IEnumerable<TestCase> tests)
    {
        var testIds = tests
            .Select(test => test.Id.ToString())
            .Distinct()
            .ToList();

        var resultsMap = new Dictionary<string, IList<TestResult>>();
        foreach (var id in testIds)
        {
            resultsMap[id] = new List<TestResult>();
        }

        return resultsMap;
    }

    private static IDictionary<string, IList<TestResult>> RunAndRecordTests(IYamlTestFrameworkHost host, IEnumerable<RunnableTestCaseItem> items)
    {
        InitFromItemIdMaps(items, out var itemFromItemIdMap, out var itemCompletionFromItemIdMap);

        RunAndRecordRunnableTestCaseItems(host, itemFromItemIdMap, itemCompletionFromItemIdMap, onlyWithNextSteps: true, onlyParallel: true);
        RunAndRecordRunnableTestCaseItems(host, itemFromItemIdMap, itemCompletionFromItemIdMap, onlyWithNextSteps: true, onlyParallel: false);
        RunAndRecordRunnableTestCaseItems(host, itemFromItemIdMap, itemCompletionFromItemIdMap, onlyWithNextSteps: false, onlyParallel: true);
        RunAndRecordRunnableTestCaseItems(host, itemFromItemIdMap, itemCompletionFromItemIdMap, onlyWithNextSteps: false, onlyParallel: false);

        return GetTestResultsByTestId(itemCompletionFromItemIdMap);
    }

    private static IDictionary<string, IList<TestResult>> GetTestResultsByTestId(Dictionary<string, TaskCompletionSource<IList<TestResult>>> itemCompletionFromItemIdMap)
    {
        var results = itemCompletionFromItemIdMap
            .Select(x => x.Value.Task.Result)
            .SelectMany(x => x)
            .ToList();
        var resultsByTestId = InitResultsByTestCaseIdMap(results
            .Select(x => x.TestCase)
            .Distinct()
            .ToList());
        foreach (var result in results)
        {
            var testCaseId = result.TestCase.Id.ToString();
            resultsByTestId[testCaseId].Add(result);
        }

        return resultsByTestId;
    }

    private static void InitFromItemIdMaps(IEnumerable<RunnableTestCaseItem> items, out Dictionary<string, RunnableTestCaseItem> itemFromItemIdMap, out Dictionary<string, TaskCompletionSource<IList<TestResult>>> itemCompletionFromItemIdMap)
    {
        itemFromItemIdMap = new Dictionary<string, RunnableTestCaseItem>();
        itemCompletionFromItemIdMap = new Dictionary<string, TaskCompletionSource<IList<TestResult>>>();
        foreach (var item in items)
        {
            var itemId = item.Id;
            itemFromItemIdMap[itemId] = item;
            itemCompletionFromItemIdMap[itemId] = new TaskCompletionSource<IList<TestResult>>();
        }
    }

    private static void RunAndRecordRunnableTestCaseItems(IYamlTestFrameworkHost host, Dictionary<string, RunnableTestCaseItem> itemFromItemIdMap, Dictionary<string, TaskCompletionSource<IList<TestResult>>> itemCompletionFromItemIdMap, bool onlyWithNextSteps = false, bool onlyParallel = false)
    {
        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItems(): ENTER (onlyWithNextSteps: {onlyWithNextSteps}, onlyParallel: {onlyParallel})");

        var items = itemCompletionFromItemIdMap
            .Where(kvp => kvp.Value.Task.Status < TaskStatus.RanToCompletion)
            .Select(kvp => itemFromItemIdMap[kvp.Key])
            .ToList();

        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItems(): PRE-FILTER: item count: {items.Count}");

        if (onlyWithNextSteps)
        {
            items = items
                .Where(item => !string.IsNullOrEmpty(YamlTestProperties.Get(item.RunnableTest.Test, "nextTestCaseId")))
                .Where(item => string.IsNullOrEmpty(YamlTestProperties.Get(item.RunnableTest.Test, "afterTestCaseId")))
                .ToList();
        }

        if (onlyParallel)
        {
            items = items
                .Where(item => YamlTestProperties.Get(item.RunnableTest.Test, "parallelize") == "true")
                .ToList();
        }

        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItems(): POST-FILTER: item count: {items.Count}");

        if (onlyParallel)
        {
            RunAndRecordRunnableTestCaseItemsInParallel(host, itemFromItemIdMap, itemCompletionFromItemIdMap, items);
        }
        else
        {
            RunAndRecordRunnableTestCaseItemsSequentially(host, itemFromItemIdMap, itemCompletionFromItemIdMap, items);
        }

        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItems(): EXIT");
    }

    private static void RunAndRecordRunnableTestCaseItemsSequentially(IYamlTestFrameworkHost host, Dictionary<string, RunnableTestCaseItem> itemFromItemIdMap, Dictionary<string, TaskCompletionSource<IList<TestResult>>> itemCompletionFromItemIdMap, List<RunnableTestCaseItem> items)
    {
        foreach (var item in items)
        {
            var id = item.Id;
            RunAndRecordRunnableTestCaseItemsStepByStep(host, itemFromItemIdMap, itemCompletionFromItemIdMap, id);
        }
    }

    private static void RunAndRecordRunnableTestCaseItemsInParallel(IYamlTestFrameworkHost host, Dictionary<string, RunnableTestCaseItem> itemFromItemIdMap, Dictionary<string, TaskCompletionSource<IList<TestResult>>> itemCompletionFromItemIdMap, List<RunnableTestCaseItem> items)
    {
        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItemsInParallel() ==> Running {items.Count} tests in parallel");

        foreach (var item in items)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                RunAndRecordRunnableTestCaseItemsStepByStep(host, itemFromItemIdMap, itemCompletionFromItemIdMap, item.Id);
            });
        }

        var parallelCompletionTasks = itemCompletionFromItemIdMap
            .Where(kvp => items.Any(item => item.Id == kvp.Key))
            .Select(kvp => kvp.Value.Task);
        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItemsInParallel() ==> Waiting for {parallelCompletionTasks.Count()} parallel tests to complete");
        
        Task.WaitAll(parallelCompletionTasks.ToArray());
        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItemsInParallel() ==> All parallel tests complete");
    }

    private static void RunAndRecordRunnableTestCaseItemsStepByStep(IYamlTestFrameworkHost host, Dictionary<string, RunnableTestCaseItem> itemFromItemIdMap, Dictionary<string, TaskCompletionSource<IList<TestResult>>> itemCompletionFromItemIdMap, string firstStepItemId)
    {
        var firstItem = itemFromItemIdMap[firstStepItemId];
        var firstTestResults = RunAndRecordRunnableTestCaseItem(firstItem, host);
        var firstTestOutcome = TestResultHelpers.TestOutcomeFromResults(firstTestResults);
        // defer setting completion until all steps are complete

        var checkItem = firstItem;
        while (true)
        {
            var nextItem = GetNextRunnableTestCaseItem(itemFromItemIdMap, checkItem);
            if (nextItem == null)
            {
                Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItemsStepByStep() ==> No next runnable item for {checkItem.RunnableTest.Test.DisplayName}");
                break;
            }
            var nextItemId = nextItem!.Id;
            var itemCompletion = itemCompletionFromItemIdMap.ContainsKey(nextItemId) ? itemCompletionFromItemIdMap[nextItemId] : null;
            if (itemCompletion == null)
            {
                Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItemsStepByStep() ==> nextItemId '{nextItemId}' completion not found for test '{checkItem.RunnableTest.Test.DisplayName}'");
                break;
            }

            var itemResults = RunAndRecordRunnableTestCaseItem(nextItem, host);
            var itemOutcome = TestResultHelpers.TestOutcomeFromResults(itemResults);
            Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItemsStepByStep() ==> Setting completion outcome for {nextItem.RunnableTest.Test.DisplayName} to {itemOutcome}");
            itemCompletion.SetResult(itemResults);

            checkItem = nextItem;
        }

        // now that all steps are complete, set the completion outcome
        itemCompletionFromItemIdMap[firstStepItemId].SetResult(firstTestResults);
        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItemsStepByStep() ==> Setting completion; outcome for {firstItem.RunnableTest.Test.DisplayName}: {firstTestOutcome}");
    }

    private static RunnableTestCaseItem? GetNextRunnableTestCaseItem(Dictionary<string, RunnableTestCaseItem> itemFromItemIdMap, RunnableTestCaseItem current)
    {
        var test = current.RunnableTest.Test;
        var nextTestCaseId = YamlTestProperties.Get(test, "nextTestCaseId");
        if (string.IsNullOrEmpty(nextTestCaseId))
        {
            Logger.Log($"YamlTestFramework.GetNextRunnableTestCaseItem() ==> No nextTestCaseId for {test.Id}");
            return null;
        }

        var matrixId = current.MatrixId;
        if (string.IsNullOrEmpty(matrixId))
        {
            Logger.Log($"YamlTestFramework.GetNextRunnableTestCaseItem() ==> matrixId not found for {test.Id}");
            return null;
        }

        var nextItemId = RunnableTestCaseItem.ItemIdFromIds(nextTestCaseId, matrixId);
        if (!itemFromItemIdMap.ContainsKey(nextItemId))
        {
            Logger.Log($"YamlTestFramework.GetNextRunnableTestCaseItem() ==> nextItemId '{nextItemId}' not found for {test.Id}");
            return null;
        }

        return itemFromItemIdMap[nextItemId];
    }

    private static IEnumerable<FileInfo> FindFiles(DirectoryInfo directory)
    {
        return directory.GetFiles($"*{YamlFileExtension}", SearchOption.AllDirectories)
            .Where(file => file.Name != YamlDefaultTagsFileName)
            .Where(file => file.Name != YamlTestsConfigFileName);
    }

    private static bool IsTrait(Trait trait, string check)
    {
        return trait.Name == check || trait.Value == check;
    }

    private static List<List<RunnableTestCaseItem>> GetPriorityGroups(IEnumerable<RunnableTestCaseItem> items)
    {
        Logger.Log($"YamlTestFramework.GetPriorityGroups(): items count: {items.Count()}");

        var before = items.Where(item => item.RunnableTest.Test.Traits.Count(x => IsTrait(x, "before")) > 0);
        var after = items.Where(item => item.RunnableTest.Test.Traits.Count(x => IsTrait(x, "after")) > 0);
        var middle = items.Where(item => !before.Contains(item) && !after.Contains(item));

        var itemsList = new List<List<RunnableTestCaseItem>>();
        itemsList.Add(before.ToList());
        itemsList.Add(middle.ToList());
        itemsList.Add(after.ToList());

        return itemsList;
    }

    private static IList<TestResult> RunAndRecordRunnableTestCaseItem(RunnableTestCaseItem item, IYamlTestFrameworkHost host)
    {
        Logger.Log($"YamlTestFramework.RunAndRecordRunnableTestCaseItem({item.RunnableTest.Test.DisplayName})");
        return item.RunAndRecord(host);
    }

    #endregion

    #region constants
    public const string YamlFileExtension = ".yaml";
    public const string FakeExecutor = "executor://cycodt/cli/TestFramework/v1";
    public readonly static string YamlDefaultTagsFileName = $"cycodt-tags.yaml";
    public const string YamlTestsConfigDirectoryName = "tests";
    public const string YamlTestsConfigFileName = "cycodt-config.yaml";
    public const string DefaultTimeout = "600000";
    #endregion
}
