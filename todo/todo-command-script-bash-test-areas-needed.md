# Command/Script/Bash Test Coverage Matrix

This document identifies gaps in test coverage across different execution methods (command, script, bash) and various test features.

## Additional YAML Elements Not Yet Considered

Based on examination of the YamlTestCaseParser.cs file, the following YAML elements also need test coverage:

1. `cli` - Purpose appears to be related to command-line interface settings
2. `arguments` - A mapping of argument names to values passed to commands or scripts
3. `sanitize` - Likely related to output sanitization
4. `foreach` - Used in matrix tests to iterate over values (separate from matrix-file)
5. `optional` - Marks a test as optional, meaning it might be skipped based on certain conditions

## Feature Coverage Matrix

| Feature          | command | script | bash |
|------------------|---------|--------|------|
| args/arguments   | ✓       | ✗      | ✗    |
| input (stdin)    | ✓       | ✓      | ✓    |
| timeout          | ✗       | ✓      | ✓    |
| expect-regex     | ✓       | ✓      | ✓    |
| expect (LLM)     | ✓       | ✗      | ✗    |
| env variables    | ✗       | ✗      | ✓    |
| workingDirectory | ✓       | ✗      | ✗    |
| optional         | ✓       | ✓      | ✓    |
| tag/tags         | ✓       | ✗      | ✗    |

Legend:
- ✓: Tested
- ✗: Not tested

## Coverage Gaps:

1. **Command:**
   - No tests for timeout with command execution
   - No tests for environment variable handling with command execution

2. **Script:**
   - No tests for command-line arguments with script execution
   - No tests for LLM-based expect validation
   - No tests for environment variables
   - No tests for workingDirectory
   - No tests using tag/tags specifically with script execution

3. **Bash:**
   - No tests for command-line arguments with bash execution
   - No tests for LLM-based expect validation
   - No tests for workingDirectory
   - No tests using tag/tags specifically with bash execution

4. **Common Gaps Across All Methods:**
   - No tests for `not-expect-regex`
   - No tests for `parallelize`
   - No tests for `skipOnFailure` in active use
   - No tests for `matrix` or `matrix-file`
   - No tests for `cli`
   - No tests for `arguments` (as a top-level element)
   - No tests for `sanitize`
   - No comprehensive test for nested hierarchical organization (`area`, `class`, etc.)

## Priority Test Cases to Implement:

1. Environment variables with command and script execution
2. Working directory settings with script and bash execution
3. LLM-based expect validation with script and bash
4. Command-line arguments with script and bash execution
5. Timeout settings with command execution
6. Matrix parameterization tests across all execution methods
7. Parallelize option testing
8. not-expect-regex validation tests
9. CLI element testing with all execution methods
10. Arguments mapping element with all execution methods
11. Sanitize element testing
12. Foreach element outside of matrix context