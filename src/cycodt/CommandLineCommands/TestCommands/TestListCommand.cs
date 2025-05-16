class TestListCommand : TestBaseCommand
{
    public override string GetCommandName()
    {
        return "list";
    }

    public override async Task<object> ExecuteAsync(bool interactive)
    {
        return await Task.Run(() => ExecuteList());
    }

    private int ExecuteList()
    {
        try
        {
            Logger.Log(new CycoDtTestFrameworkLogger());
            var tests = FindAndFilterTests();
            
            if (ConsoleHelpers.IsVerbose())
            {
                var grouped = tests
                    .GroupBy(t => t.CodeFilePath)
                    .OrderBy(g => g.Key)
                    .ToList();
                for (var i = 0; i < grouped.Count; i++)
                {
                    if (i > 0) ConsoleHelpers.WriteLine();

                    var group = grouped[i];
                    ConsoleHelpers.WriteLine($"{group.Key}\n", ConsoleColor.DarkGray);
                    foreach (var test in group)
                    {
                        ConsoleHelpers.WriteLine($"  {test.FullyQualifiedName}", ConsoleColor.DarkGray);
                    }
                }
            }
            else
            {
                foreach (var test in tests)
                {
                    ConsoleHelpers.WriteLine(test.FullyQualifiedName, ConsoleColor.DarkGray);
                }
            }

            Console.WriteLine(tests.Count() == 1
                ? $"\nFound {tests.Count()} test..."
                : $"\nFound {tests.Count()} tests...");

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
            return 1;
        }
    }
}