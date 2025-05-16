# Unit Test Ideas for `input` Attribute

## Basic Functionality Tests

```yaml
- name: Simple input
  command: cat
  input: Hello, world!
  expect-regex: Hello, world!

- name: Multiline input
  command: cat
  input: |
    Line 1
    Line 2
    Line 3
  expect-regex: |
    Line 1
    Line 2
    Line 3
```

## Input Processing Tests

```yaml
- name: Input to grep
  command: grep "needle"
  input: |
    haystack
    needle
    haystack
  expect-regex: needle

- name: Input to wc (word count)
  command: wc -w
  input: one two three four five
  expect-regex: 5

- name: Input to sort
  command: sort
  input: |
    banana
    apple
    cherry
  expect-regex: |
    apple
    banana
    cherry
```

## Special Character Tests

```yaml
- name: Input with quote characters
  command: cat
  input: 'Single quotes'' and "double quotes"'
  expect-regex: 'Single quotes'' and "double quotes"'

- name: Input with escape characters
  command: cat
  input: "Escaped characters: \t tab, \n newline, \\ backslash"
  expect-regex: "Escaped characters: \t tab, \n newline, \\ backslash"

- name: Input with special shell characters
  command: cat
  input: |
    Special characters: & | < > $ ; ( ) { } [ ] # * ? ~ `
  expect-regex: |
    Special characters: & | < > $ ; ( ) { } [ ] # * ? ~ `
```

## Edge Cases

```yaml
- name: Empty input
  command: wc -c
  input: ""
  expect-regex: 0

- name: Very long input
  command: wc -c
  input: ${{ "X" * 10000 }}
  expect-regex: 10000

- name: Input with null byte
  command: hexdump -C
  input: "Before\x00After"
  expect-regex: 00000000.*42 65 66 6f 72 65 00 41 66 74 65 72.*Before.After

- name: Unicode input
  command: cat
  input: "Unicode characters: こんにちは, 你好, Привет"
  expect-regex: "Unicode characters: こんにちは, 你好, Привет"
```

## Interactive Command Tests

```yaml
- name: Interactive command with single prompt
  command: python -c "name = input('Enter name: '); print(f'Hello, {name}!')"
  input: Alice
  expect-regex: |
    Enter name: Hello, Alice!

- name: Interactive command with multiple prompts
  command: python -c "name = input('Name: '); age = input('Age: '); print(f'{name} is {age} years old')"
  input: |
    Bob
    30
  expect-regex: |
    Name: Age: Bob is 30 years old

- name: Interactive command with y/n confirmation
  command: python -c "answer = input('Proceed? (y/n): '); print('Proceeding' if answer.lower() == 'y' else 'Cancelled')"
  input: y
  expect-regex: |
    Proceed\? \(y/n\): Proceeding
```

## Input with Other Attributes

```yaml
- name: Input with environment variables
  command: python -c "import os, sys; print(f\"ENV_VAR={os.environ.get('ENV_VAR')}, INPUT={sys.stdin.read().strip()}\")"
  env:
    ENV_VAR: env_value
  input: input_value
  expect-regex: ENV_VAR=env_value, INPUT=input_value

- name: Input within matrix
  matrix:
    value: [10, 20, 30]
  command: python -c "import sys; x = int(sys.stdin.read().strip()); print(f'Result: {x * 2}')"
  input: ${{ matrix.value }}
  expect-regex: Result: ${{ matrix.value * 2 }}

- name: Input with working directory
  workingDirectory: ./temp
  command: python -c "import os, sys; print(f\"PWD={os.getcwd()}, INPUT={sys.stdin.read().strip()}\")"
  input: test_input
  expect-regex: PWD=.*temp, INPUT=test_input
```

## File-Based Input Tests

```yaml
# Assuming @input.txt exists with content "File content"
- name: Input redirected from file
  command: cat
  input: "@input.txt"
  expect-regex: File content

- name: Generated input file
  command: cat
  input: |
    This is dynamically
    generated content
    for the test
  expect-regex: |
    This is dynamically
    generated content
    for the test
```

## Programming Language Input Tests

```yaml
- name: Python code input
  command: python
  input: |
    print("Hello from Python")
    print(2 + 2)
    exit()
  expect-regex: |
    Hello from Python
    4

- name: Node.js code input
  command: node
  input: |
    console.log("Hello from Node.js");
    console.log(2 + 2);
    process.exit();
  expect-regex: |
    Hello from Node.js
    4

- name: SQL query input
  command: sqlite3 :memory:
  input: |
    CREATE TABLE test (id INTEGER, name TEXT);
    INSERT INTO test VALUES (1, 'Alice');
    INSERT INTO test VALUES (2, 'Bob');
    SELECT * FROM test;
    .exit
  expect-regex: |
    1\|Alice
    2\|Bob
```

## Timing and Performance Tests

```yaml
- name: Delayed input processing
  command: bash -c "sleep 1; cat; echo 'Done'"
  input: Processed after delay
  expect-regex: |
    Processed after delay
    Done

- name: Large input throughput
  command: wc -l
  input: ${{ "\n".join(f"Line {i}" for i in range(1000)) }}
  expect-regex: 1000
```

## Error Handling Tests

```yaml
- name: Input to failing command
  command: bash -c "read line; echo \"Got: $line\"; exit 1"
  input: Test input
  expect-regex: Got: Test input
  # Should fail due to exit code 1

- name: Input timeout scenario
  command: bash -c "read line; sleep 5; echo $line"
  input: Test input
  timeout: 2000
  # Should fail due to timeout
```