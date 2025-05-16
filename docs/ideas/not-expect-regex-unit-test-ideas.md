# Unit Test Ideas for `not-expect-regex` Attribute

## Basic Functionality Tests

```yaml
- name: Simple not-expect match
  command: echo "Success message"
  not-expect-regex: Error|Failure|Warning

- name: Not-expect with exact text
  command: echo "Good result"
  not-expect-regex: Bad result

- name: Multi-line not-expect
  command: echo -e "Line 1\nLine 2\nLine 3"
  not-expect-regex: |
    Line 4
    Line 5
    Error
```

## Negative Pattern Matching Tests

```yaml
- name: Not-expect word boundary
  command: echo "Testing process"
  not-expect-regex: \berror\b

- name: Not-expect error patterns
  command: echo "Operation completed"
  not-expect-regex: |
    Exception:
    Error:
    Failed:

- name: Not finding specific numbers
  command: echo "Code 200 OK"
  not-expect-regex: Code [45]\d\d
```

## Special Cases

```yaml
- name: Sensitive data not present
  command: echo "Public data only"
  not-expect-regex: password=|secret=|private_key=

- name: No empty output lines
  command: echo -e "Line 1\nLine 2"
  not-expect-regex: ^\s*$

- name: No specific commands found in output
  command: echo "Safe commands only"
  not-expect-regex: rm -rf|format|delete
```

## Error Cases

```yaml
- name: Pattern that should be found (negative test)
  command: echo "Error: Something failed"
  not-expect-regex: Error
  # This test should fail because Error is present

- name: Invalid regex pattern (unclosed group)
  command: echo "Test"
  not-expect-regex: Test(unclosed
  # This should fail due to invalid regex

- name: Empty not-expect-regex pattern
  command: echo "Test"
  not-expect-regex: ""
  # This would fail as empty string is matched everywhere
```

## Combinations with expect-regex

```yaml
- name: Expect and not-expect combined
  command: echo "Status: Success, No Errors"
  expect-regex: Status: Success
  not-expect-regex: Status: Failure

- name: Complex validation with both types
  command: echo -e "Operation started\nProcessing\nOperation completed"
  expect-regex: |
    Operation started
    Operation completed
  not-expect-regex: |
    Error
    Failed
    Aborted
```

## Edge Cases

```yaml
- name: Case sensitivity in not-expect
  command: echo "INFO: Process completed"
  not-expect-regex: info:
  # This should pass (case-sensitive by default)

- name: Case insensitivity in not-expect
  command: echo "INFO: Process completed"
  not-expect-regex: (?i)info:
  # This should fail (case-insensitive specified)

- name: Not-expect with zero-width assertions
  command: echo "abc123"
  not-expect-regex: \d+$xyz
  # This should pass as the regex won't match

- name: Not-expect with empty output
  command: echo -n ""
  not-expect-regex: .+
  # Should pass as there's no output to match against
```

## Complex Pattern Tests

```yaml
- name: No stack traces in output
  command: echo "Clean output"
  not-expect-regex: |
    at [\w\.$_]+\([^)]*\)
    at [\w\.$_]+\.[^(]+\([^)]*\)
    Exception in thread

- name: No invalid JSON structures
  command: echo '{"valid":"json"}'
  not-expect-regex: |
    \{\s*[^"']+\s*:
    [^"']\s*:\s*[^"'{[]
    [,{]\s*\}

- name: No HTML tags in output
  command: echo "Plain text output"
  not-expect-regex: <[^>]+>
```

## Integration with Other Test Features

```yaml
- name: Not-expect with matrix values
  matrix:
    errorCode: [500, 501, 503]
  command: echo "Status code: 200"
  not-expect-regex: Status code: ${{ matrix.errorCode }}

- name: Not-expect with environment variables
  command: echo "Mode: production"
  env:
    FORBIDDEN_MODE: development
  not-expect-regex: Mode: $FORBIDDEN_MODE
```

## Multiple Not-Expect Patterns

```yaml
- name: Multiple forbidden patterns
  command: echo "Clean output with useful information"
  not-expect-regex: |
    error:
    warning:
    deprecated:
    vulnerability detected

- name: Semi-colon separated not-expect patterns
  command: echo "Clean output"
  not-expect-regex: error;warning;failure
```

## Boundary Testing

```yaml
- name: Output boundary tests
  command: echo -e "==START==\nGood content\n==END=="
  expect-regex: Good content
  not-expect-regex: |
    ==ERROR==
    ==FAILURE==
    ==WARNING==
```

## Timing and Resource Tests

```yaml
- name: No timeout messages
  command: echo "Completed in 2s"
  not-expect-regex: |
    timed? ?out
    too (slow|long)
    (cpu|memory|disk) (usage|utilization) exceeded
```