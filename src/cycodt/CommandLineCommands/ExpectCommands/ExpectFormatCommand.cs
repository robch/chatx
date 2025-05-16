using System.Text;
using System.Text.RegularExpressions;

class ExpectFormatCommand : ExpectBaseCommand
{
    public ExpectFormatCommand() : base()
    {
        Strict = true; // Default to true
    }

    public bool Strict { get; set; }

    public override string GetCommandName()
    {
        return "expect format";
    }

    public override async Task<object> ExecuteAsync(bool interactive)
    {
        return await Task.Run(() => ExecuteFormat());
    }

    private int ExecuteFormat()
    {
        try
        {
            var input = GetInput();
            var formattedText = FormatInput(input);
            WriteOutput(formattedText);
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

    private string FormatInput(string input)
    {
        var lines = input.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var formattedLines = new List<string>();

        foreach (var line in lines)
        {
            var formattedLine = Strict ? FormatLineStrict(line) : line;
            formattedLines.Add(formattedLine);
        }

        return string.Join("\n", formattedLines);
    }

    private string FormatLineStrict(string line)
    {
        // Special regex characters that need to be escaped: ()[]{}.*+?|^$\
        string escapedLine = Regex.Replace(line, @"([\\()\[\]{}.*+?|^$])", @"\$1");
        
        // Add ^ prefix to start and \r?$ suffix to end
        return $"^{escapedLine}\\r?$";
    }
}