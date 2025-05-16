using System.Text;
using System.Text.RegularExpressions;

public class ExpectHelper
{
    public static bool CheckOutput(string output, string? expected, string? unexpected, bool quiet = false)
    {
        var outputLines = output
            .Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .ToList() ?? new List<string>();
        var expectedLines = expected?.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .ToList() ?? new List<string>();
        var unexpectedLines = unexpected?.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .ToList() ?? new List<string>();

        return CheckLines(outputLines, expectedLines, unexpectedLines, quiet);
    }

    public static bool CheckLines(IEnumerable<string> lines, IEnumerable<string> expected, IEnumerable<string> unexpected, bool quiet = false)
    {
        var helper = new ExpectHelper(lines, expected, unexpected, quiet);
        return helper.Expect();
    }

    private ExpectHelper(IEnumerable<string> lines, IEnumerable<string> expected, IEnumerable<string> unexpected, bool quiet)
    {
        _allLines = lines;
        _expected = expected != null ? new Queue<string>(expected) : null;
        _unexpected = unexpected != null ? new List<string>(unexpected) : null;
        _quiet = quiet;
    }

    private bool Expect()
    {
        foreach (string line in _allLines)
        {
            if (_expected != null) CheckExpected(line);
            if (_unexpected != null) CheckUnexpected(line);
        }

        var allExpectedFound = _expected == null || _expected.Count == 0;
        if (!allExpectedFound && !_quiet)
        {
            ConsoleHelpers.WriteWarningLine($"UNEXPECTED: Couldn't find '{_expected!.Peek()}' in:\n```\n{_unmatchedInput}```");
        }

        return !_foundUnexpected && allExpectedFound;
    }

    private void CheckExpected(string line)
    {
        _unmatchedInput.AppendLine(line);
        while (_expected!.Count > 0)
        {
            var pattern = _expected.Peek();
            var check = _unmatchedInput.ToString();

            var match = Regex.Match(check, pattern);
            if (!match.Success) break; // continue reading input...

            _unmatchedInput.Remove(0, match.Index + match.Length);
            _expected.Dequeue();
        }
    }

    private void CheckUnexpected(string line)
    {
        foreach (var pattern in _unexpected!)
        {
            var match = Regex.Match(line, pattern);
            if (!match.Success) continue; // check more patterns

            _foundUnexpected = true;

            if (!_quiet)
            {
                ConsoleHelpers.WriteWarningLine($"UNEXPECTED: Found '{pattern}' in '{line}'");
            }
        }
    }

    private StringBuilder _unmatchedInput = new StringBuilder();
    private IEnumerable<string> _allLines;

    private bool _quiet = false;
    private Queue<string>? _expected;
    private List<string>? _unexpected;
    bool _foundUnexpected = false;
}
