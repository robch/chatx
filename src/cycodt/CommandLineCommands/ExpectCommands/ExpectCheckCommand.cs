using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

class ExpectCheckCommand : ExpectBaseCommand
{
    public ExpectCheckCommand() : base()
    {
        RegexPatterns = new List<string>();
        NotRegexPatterns = new List<string>();
        Format = "TEXT";
    }

    public List<string> RegexPatterns { get; set; }
    public List<string> NotRegexPatterns { get; set; }
    public string? Instructions { get; set; }
    public string Format { get; set; }

    public override string GetCommandName()
    {
        return "expect check";
    }

    public override async Task<object> ExecuteAsync(bool interactive)
    {
        return await Task.Run(() => ExecuteCheck());
    }

    private int ExecuteCheck()
    {
        try
        {
            var input = GetInput();
            var result = new StringBuilder();
            bool passed = true;
            
            // Check regex patterns
            if (RegexPatterns.Any() || NotRegexPatterns.Any())
            {
                var regexPassed = CheckRegexPatterns(input, result);
                passed = passed && regexPassed;
            }

            // Check LLM instructions
            if (!string.IsNullOrEmpty(Instructions))
            {
                var instructionsPassed = CheckInstructions(input, result);
                passed = passed && instructionsPassed;
            }

            if (!RegexPatterns.Any() && !NotRegexPatterns.Any() && string.IsNullOrEmpty(Instructions))
            {
                result.AppendLine("ERROR: No expectations provided. Use --regex, --not-regex, or --instructions.");
                passed = false;
            }

            // Format the result
            var formattedResult = FormatResult(passed, result.ToString());
            WriteOutput(formattedResult);

            return passed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
            return 1;
        }
    }

    private bool CheckRegexPatterns(string input, StringBuilder result)
    {
        bool passed = true;

        if (RegexPatterns.Any())
        {
            result.AppendLine("Checking regex patterns that should match:");
            foreach (var pattern in RegexPatterns)
            {
                bool matched = CheckRegexPattern(input, pattern, true, result);
                passed = passed && matched;
            }
        }

        if (NotRegexPatterns.Any())
        {
            if (RegexPatterns.Any()) result.AppendLine();
            result.AppendLine("Checking regex patterns that should NOT match:");
            foreach (var pattern in NotRegexPatterns)
            {
                bool matched = CheckRegexPattern(input, pattern, false, result);
                passed = passed && matched;
            }
        }

        return passed;
    }

    private bool CheckRegexPattern(string input, string pattern, bool shouldMatch, StringBuilder result)
    {
        string patternSource = pattern;

        // Handle file patterns with @
        if (pattern.StartsWith("@"))
        {
            var filePath = pattern.Substring(1);
            if (File.Exists(filePath))
            {
                var linePatterns = File.ReadAllLines(filePath);
                bool allLinesMatch = true;

                result.AppendLine($"  Checking patterns from file: {filePath}");
                
                foreach (var linePattern in linePatterns)
                {
                    if (string.IsNullOrWhiteSpace(linePattern)) continue;
                    
                    bool lineMatch = CheckSingleRegexPattern(input, linePattern, shouldMatch, result, indent: 4);
                    allLinesMatch = allLinesMatch && lineMatch;
                }
                
                return allLinesMatch;
            }
            else
            {
                result.AppendLine($"  ERROR: File not found: {filePath}");
                return false;
            }
        }

        return CheckSingleRegexPattern(input, pattern, shouldMatch, result, indent: 2);
    }

    private bool CheckSingleRegexPattern(string input, string pattern, bool shouldMatch, StringBuilder result, int indent = 0)
    {
        string indentation = new string(' ', indent);
        
        try
        {
            bool isMatch = Regex.IsMatch(input, pattern, RegexOptions.Multiline);
            
            bool passed = isMatch == shouldMatch;
            string status = passed ? "PASS" : "FAIL";
            string matchStatus = isMatch ? "matches" : "does not match";
            
            result.AppendLine($"{indentation}{status}: Pattern '{pattern}' {matchStatus} input");
            
            return passed;
        }
        catch (ArgumentException ex)
        {
            result.AppendLine($"{indentation}ERROR: Invalid regex pattern '{pattern}': {ex.Message}");
            return false;
        }
    }

    private bool CheckInstructions(string input, StringBuilder result)
    {
        result.AppendLine("\nChecking LLM instructions:");
        result.AppendLine($"  Instructions: {Instructions}");
        
        try
        {
            var outcome = CheckExpectInstructionsHelper.CheckExpectGptOutcome(
                input, 
                Instructions!, 
                Directory.GetCurrentDirectory(), 
                out string gptStdOut, 
                out string gptStdErr, 
                out string gptMerged);
            
            bool passed = outcome == TestOutcome.Passed;
            string status = passed ? "PASS" : "FAIL";
            
            result.AppendLine($"  {status}: LLM evaluation result");
            
            if (!passed)
            {
                result.AppendLine("  Details:");
                result.AppendLine($"    {gptMerged.Replace("\n", "\n    ")}");
            }
            
            return passed;
        }
        catch (Exception ex)
        {
            result.AppendLine($"  ERROR: Failed to evaluate with LLM: {ex.Message}");
            return false;
        }
    }

    private string FormatResult(bool passed, string details)
    {
        if (Format.Equals("JSON", StringComparison.OrdinalIgnoreCase))
        {
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($"  \"passed\": {passed.ToString().ToLower()},");
            json.AppendLine($"  \"details\": \"{details.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r")}\"");
            json.AppendLine("}");
            return json.ToString();
        }
        else  // Default to TEXT format
        {
            var result = new StringBuilder();
            result.AppendLine(passed ? "PASS: All expectations met." : "FAIL: Some expectations not met.");
            result.AppendLine();
            result.AppendLine(details);
            return result.ToString();
        }
    }
}