# Unit Test Ideas for `command`, `script`, and `bash` Attributes

## Basic Functionality Tests

```yaml
- name: Simple command test
  command: echo "Hello, world!"
  expect-regex: Hello, world!

- name: Simple script test (Windows)
  script: |
    @echo off
    echo "Script output"
  expect-regex: Script output

- name: Simple bash test
  bash: |
    echo "Bash output"
  expect-regex: Bash output
```

## Edge Cases

```yaml
- name: Empty command
  command: ""
  expect-regex: ^$

- name: Command with quotes
  command: echo "Text with \"quotes\" inside"
  expect-regex: Text with "quotes" inside

- name: Command with special characters
  command: echo "Text with &<>|^ special chars"
  expect-regex: Text with &<>|^ special chars

- name: Multi-line bash script
  bash: |
    echo "Line 1"
    echo "Line 2"
    echo "Line 3"
  expect-regex: |
    Line 1
    Line 2
    Line 3

- name: Script with exit code 0
  bash: |
    echo "Success"
    exit 0
  expect-regex: Success

- name: UTF-8 characters in command
  command: echo "こんにちは"
  expect-regex: こんにちは
```

## Error Cases

```yaml
- name: Command with non-zero exit code
  command: exit 1
  expect-regex: ^$
  # This should fail due to exit code

- name: Bash script with syntax error
  bash: |
    echo "Before error"
    if then # Syntax error
    echo "After error"
  # This should fail due to syntax error

- name: Command that doesn't exist
  command: nonexistentcommand
  # This should fail because command doesn't exist

- name: Both command and script specified
  command: echo "From command"
  script: echo "From script"
  # This should fail because only one of command/script/bash should be specified
```

## Combinations with Other Attributes

```yaml
- name: Command with timeout
  command: sleep 10
  timeout: 500
  # Should fail due to timeout

- name: Command with environment variables
  command: echo $TEST_VAR
  env:
    TEST_VAR: Environment variable value
  expect-regex: Environment variable value

- name: Command with working directory
  command: pwd
  workingDirectory: /tmp
  expect-regex: /tmp

- name: Command with input
  command: cat
  input: |
    This is input
    On multiple lines
  expect-regex: |
    This is input
    On multiple lines
```

## Platform-Specific Tests

```yaml
- name: OS-specific command (Windows)
  command: ver
  expect-regex: Microsoft Windows

- name: OS-specific command (Linux/macOS)
  bash: uname -a
  expect-regex: (Linux|Darwin)
```