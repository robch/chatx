# Unit Test Ideas for `expect-regex` Attribute

## Basic Functionality Tests

```yaml
- name: Simple exact match
  command: echo "Hello, world!"
  expect-regex: Hello, world!

- name: Simple regex pattern
  command: echo "Testing 123"
  expect-regex: Testing \d+

- name: Case-insensitive match
  command: echo "HELLO world"
  expect-regex: (?i)hello world

- name: Multi-line output match
  command: printf "Line 1\nLine 2\nLine 3"
  expect-regex: |
    Line 1
    Line 2
    Line 3
```

## Pattern Matching Tests

```yaml
- name: Word boundary match
  command: echo "The word is test"
  expect-regex: \btest\b

- name: Start of line anchor
  command: echo "Start here, not there"
  expect-regex: ^Start

- name: End of line anchor
  command: echo "End here"
  expect-regex: here$

- name: Character class match
  command: echo "Testing a1b2c3"
  expect-regex: [abc][123]

- name: Negated character class
  command: echo "Testing x9y8z7"
  expect-regex: [^abc][0-9]

- name: Capture groups
  command: echo "Name: John, Age: 30"
  expect-regex: Name: (\w+), Age: (\d+)

- name: Non-capturing groups
  command: echo "prefix-value"
  expect-regex: prefix-(?:value|other)
```

## Quantifier Tests

```yaml
- name: Zero or more quantifier
  command: echo "aaa123"
  expect-regex: a*\d+

- name: One or more quantifier
  command: echo "aaa123"
  expect-regex: a+\d+

- name: Optional quantifier
  command: echo "abc123"
  expect-regex: abc?\d+

- name: Exact count quantifier
  command: echo "aaabbb"
  expect-regex: a{3}b{3}

- name: Range quantifier
  command: echo "aaabbb"
  expect-regex: a{2,4}b{2,4}

- name: Greedy vs lazy quantifier
  command: echo "<tag>content</tag>"
  expect-regex: <tag>.*?</tag>
```

## Special Character Tests

```yaml
- name: Escaping special characters
  command: echo "Price is $100"
  expect-regex: Price is \$\d+

- name: Dot matches any character
  command: echo "a1b2c3"
  expect-regex: a.b.c.

- name: Escape sequences
  command: echo -e "First line\tTabbed\nSecond line"
  expect-regex: First line\tTabbed
```

## Multiple Patterns

```yaml
- name: Multiple patterns (all must match)
  command: echo -e "First line\nSecond line\nThird line"
  expect-regex: |
    First line
    Second line
    Third line

- name: Multiple independent patterns
  command: echo -e "Error: X123\nWarning: Y456"
  expect-regex: |
    Error: X\d+
    Warning: Y\d+

- name: Semi-colon separated patterns
  command: echo -e "Error: X123\nWarning: Y456"
  expect-regex: Error: X\d+;Warning: Y\d+
```

## Complex Pattern Tests

```yaml
- name: Email validation pattern
  command: echo "Contact us at user@example.com"
  expect-regex: \b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b

- name: URL validation pattern
  command: echo "Visit https://example.com/path?query=value"
  expect-regex: https?://[\w-]+(\.[\w-]+)+(/[\w-./?%&=]*)?

- name: JSON structure validation
  command: echo '{"name":"John","age":30}'
  expect-regex: \{"name":"[^"]+","age":\d+\}
```

## Error Cases

```yaml
- name: Pattern that shouldn't match
  command: echo "Hello, world!"
  expect-regex: Goodbye
  # This test should fail

- name: Invalid regex pattern (unclosed group)
  command: echo "Test"
  expect-regex: Test(unclosed
  # This should fail due to invalid regex

- name: Empty regex pattern
  command: echo "Test"
  expect-regex: ""
  # Should match empty string, but not the output

- name: Incomplete match
  command: echo "Prefix-Content-Suffix"
  expect-regex: Content
  # Should pass as partial matches are valid
```

## Combinations with Other Attributes

```yaml
- name: Regex with timeout
  command: sleep 2 && echo "Delayed output"
  timeout: 3000
  expect-regex: Delayed output

- name: Regex with exitcode check
  command: bash -c 'echo "Normal exit"; exit 0'
  expect-regex: Normal exit
  # Should pass due to exit code 0 AND matching regex

- name: Regex with input
  command: cat
  input: Test input string
  expect-regex: Test input string
```

## Unicode and Special Cases

```yaml
- name: Unicode character matching
  command: echo "Unicode symbols: ñáéíóú"
  expect-regex: ñáéíóú

- name: Unicode categories
  command: echo "Unicode categories: A1ñ"
  expect-regex: \p{L}\p{N}\p{L}

- name: Very long output
  command: python -c "print('x' * 10000)"
  expect-regex: x{10000}
```

## Integration with Other Test Features

```yaml
- name: Regex with matrix values
  matrix:
    value: [10, 20, 30]
  command: echo "The value is ${{ matrix.value }}"
  expect-regex: The value is ${{ matrix.value }}

- name: Regex with environment variables
  command: echo "MYVAR=$MYVAR"
  env:
    MYVAR: special_value
  expect-regex: MYVAR=special_value
```