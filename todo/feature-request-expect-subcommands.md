# Feature Request: `cycodt expect` Command

## Overview

This feature introduces a new `expect` verb to the `cycodt` tool with two subcommands: `format` and `check`. The command addresses two common pain points in working with test expectations:

1. **Creating regex patterns for test files** - The `expect format` subcommand transforms standard input into properly formatted regex patterns, including escaping special characters and adding line ending patterns.

2. **Validating output against expectations** - The `expect check` subcommand allows users to check if input matches specified regex patterns or LLM instructions directly from the command line.

## Motivation

Writing proper regex patterns for the `expect-regex` field in test YAML files is challenging. Users need to:
- Escape special regex characters like `()[]{}.*+?|^$`
- Add proper line prefixes (`^`) and suffixes (`\r?$\n`) for exact line matching
- Format multi-line output properly

Additionally, users often need to check if output matches expectations directly without creating a test file first.

## Implementation Details

### `cycodt expect format`

This command transforms input text into properly formatted regex patterns for use in YAML test files.

**Input Sources:**
- Standard input (default)
- File input (`--input FILE`)

**Output Destinations:**
- Standard output (default)
- File output (`--output FILE`)

**Formatting Options:**
- `--strict true/false` - When true (default):
  - Adds `^` prefix to the start of each line
  - Adds `\r?$` suffix to the end of each line
  - Escapes special regex characters
  - When false, no special formatting is applied

### `cycodt expect check`

This command validates input against specified expectations, similar to running a test case but directly from the command line.

**Input Sources:**
- Standard input (default)
- File input (`--input FILE`)

**Expectation Types:**
- Regex patterns that should match (`--regex PATTERN`)
- Regex patterns that should not match (`--not-regex PATTERN`)
- LLM instructions (`--instructions TEXT`)

**Pattern Sources:**
- Inline patterns (`--regex "pattern"`)
- File patterns (`--regex-file FILE`)
- Individual lines from a file (`--regex @FILE`)

**Output Options:**
- File output (`--output FILE`)
- Format options (`--format TEXT|JSON`)
- Verbosity control (`--verbose`)

## Usage Examples

### Formatting Examples

**Transform command output to regex patterns:**
```
my-command | cycodt expect format > patterns.txt
```

**Format content with non-strict formatting:**
```
cat output.txt | cycodt expect format --strict false > simple-patterns.txt
```

**Format special character output:**
```
echo "Special chars: ( ) [ ] { }" | cycodt expect format
# Output: ^Special chars: \( \) \[ \] \{ \}\r?$\n
```

### Checking Examples

**Check if command output matches regex patterns:**
```
my-command | cycodt expect check --regex "^Success" --regex "^Items: \d+"
```

**Check output against patterns from a file:**
```
my-command | cycodt expect check --regex @patterns.txt --not-regex "Error"
```

**Use LLM instructions to check output:**
```
my-command | cycodt expect check --instructions "Output should show a successful login"
```

**Process file content against expectations:**
```
cycodt expect check --input results.log --regex-file valid-patterns.txt
```

## Help Documentation

The following help screens should be added to the `cycodt` documentation:

```
CYCODT EXPECT

  The cycodt expect commands provide tools for working with test expectations.
  Use 'expect format' to prepare regex patterns, and 'expect check' to 
  validate output against expectations directly from the command line.

USAGE: cycodt expect format [options]
   OR: cycodt expect check [options]

EXAMPLES

  EXAMPLE 1: Format command output as regex patterns

    my-command | cycodt expect format > patterns.txt

  EXAMPLE 2: Check if command output meets expectations

    my-command | cycodt expect check --regex "^Expected output\r?$"

  EXAMPLE 3: Check output using LLM instructions

    my-command | cycodt expect check --instructions "Output should include three bullet points"

SEE ALSO

  cycodt help expect format
  cycodt help expect check
  cycodt help run
  cycodt help list
```

```
CYCODT EXPECT FORMAT

  The cycodt expect format command transforms input text into properly formatted 
  regex patterns for use in the expect-regex field of YAML test files.

USAGE: cycodt expect format [options]

  INPUT
    Input is read from standard input (stdin) by default
    --input FILE        Read input from FILE
    --input -           Read input from stdin (default)

  OUTPUT
    --output FILE       Write output to FILE instead of stdout
    --append            Append to output file instead of overwriting

  FORMAT OPTIONS
    --strict true/false Whether to apply strict regex formatting (default: true)
                        When true: adds ^ prefix, \r?$ suffix, and escapes special chars
                        When false: performs no regex-specific formatting

EXAMPLES

  EXAMPLE 1: Transform command output into expect-regex format

    my-command | cycodt expect format

  EXAMPLE 2: Format content of a file with non-strict formatting

    cycodt expect format --input output.txt --output patterns.txt --strict false

  EXAMPLE 3: Create patterns from special character output

    echo "Special chars: ( ) [ ] { }" | cycodt expect format

SEE ALSO

  cycodt help expect check
  cycodt help run
```

```
CYCODT EXPECT CHECK

  The cycodt expect check command validates input against specified expectations,
  similar to running a test case but directly from the command line.

USAGE: cycodt expect check [options]

  INPUT
    Input is read from standard input (stdin) by default
    --input FILE        Read input from FILE
    --input -           Read input from stdin (default)

  EXPECTATIONS
    --regex PATTERN     Regex pattern(s) that should match (can specify multiple)
    --regex-file FILE   File containing regex patterns to match
    --regex @FILE       Read regex patterns from FILE (one per line)
    
    --not-regex PATTERN Regex pattern(s) that should NOT match (can specify multiple)
    --not-regex-file FILE File containing regex patterns that should not match
    --not-regex @FILE   Read not-regex patterns from FILE (one per line)
    
    --instructions TEXT LLM instructions for checking output
    --instructions-file FILE File containing LLM instructions

  OUTPUT OPTIONS
    --output FILE       Write check results to FILE
    --format TEXT|JSON  Format for output results (default: TEXT)
    --verbose           Include detailed match information

EXAMPLES

  EXAMPLE 1: Check if command output matches regex patterns

    my-command | cycodt expect check --regex "^Success" --regex "^Items: \d+"

  EXAMPLE 2: Check output against patterns from a file

    my-command | cycodt expect check --regex @patterns.txt --not-regex "Error"

  EXAMPLE 3: Use LLM instructions to check output

    my-command | cycodt expect check --instructions "Output should show a successful login"

  EXAMPLE 4: Process file content against expectations

    cycodt expect check --input results.log --regex-file valid-patterns.txt

SEE ALSO

  cycodt help expect format
  cycodt help run
```

## Integration with Existing Commands

The `expect` commands complement the existing `list` and `run` commands by:

- Providing a way to generate properly formatted regex patterns for test YAML files
- Allowing quick validation of command output without creating full test cases
- Supporting the same input/output conventions using `@FILE` and `-` for stdin

## Benefits

1. **Simplified Test Creation**: Users can easily generate properly formatted regex patterns from actual command output.
2. **Rapid Validation**: Output can be validated directly without creating test files.
3. **Consistent Pattern Format**: Ensures regex patterns are formatted consistently across all test files.
4. **LLM-Based Validation**: Supports natural language expectations for complex validation cases.
5. **Fits Existing Workflow**: Integrates seamlessly with the existing command structure and file handling.

## Next Steps

1. Implement the `expect format` subcommand
2. Implement the `expect check` subcommand
3. Add appropriate help documentation
4. Include examples in the general help topics
5. Document the new functionality in the README