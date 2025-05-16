# Unit Test Ideas for `skipOnFailure` Attribute

## Basic Functionality Tests

```yaml
- name: Basic skipOnFailure true
  command: exit 1
  skipOnFailure: true
  # Should be marked as skipped instead of failed

- name: Basic skipOnFailure false
  command: exit 1
  skipOnFailure: false
  # Should be marked as failed

- name: Successful command with skipOnFailure true
  command: echo "Success"
  skipOnFailure: true
  expect-regex: Success
  # Should pass normally
```

## Different Failure Types

```yaml
- name: Skip on non-zero exit code
  command: exit 2
  skipOnFailure: true
  # Should be marked as skipped

- name: Skip on command not found
  command: nonexistentcommand
  skipOnFailure: true
  # Should be marked as skipped

- name: Skip on failed expect-regex match
  command: echo "Actual output"
  expect-regex: Expected different output
  skipOnFailure: true
  # Should be marked as skipped

- name: Skip on failed not-expect-regex match
  command: echo "Unwanted pattern"
  not-expect-regex: Unwanted pattern
  skipOnFailure: true
  # Should be marked as skipped
```

## Sequential Test Dependencies

```yaml
- name: Sequential tests with skipOnFailure
  steps:
    - name: Step 1 - This will fail but be skipped
      command: exit 1
      skipOnFailure: true
      # Should be marked as skipped
    
    - name: Step 2 - Should still run
      command: echo "I still ran"
      expect-regex: I still ran
      # Should run and pass

    - name: Step 3 - This will fail and not be skipped
      command: exit 1
      skipOnFailure: false
      # Should be marked as failed
    
    - name: Step 4 - Should still run if test framework continues
      command: echo "Did I run?"
      expect-regex: Did I run?
      # May or may not run depending on how framework handles failures in sequence
```

## Parallelization with skipOnFailure

```yaml
- name: Parallel tests with skipOnFailure
  tests:
    - name: Parallel test 1 - Will fail but be skipped
      command: exit 1
      skipOnFailure: true
      parallelize: true
      # Should be marked as skipped
    
    - name: Parallel test 2 - Will succeed
      command: echo "Success"
      parallelize: true
      expect-regex: Success
      # Should pass
    
    - name: Parallel test 3 - Will fail and not be skipped
      command: exit 1
      skipOnFailure: false
      parallelize: true
      # Should be marked as failed
```

## Combination with Matrix

```yaml
- name: Matrix tests with skipOnFailure
  matrix:
    value: [0, 1, 2]
  command: exit ${{ matrix.value }}
  skipOnFailure: true
  # Should pass for value=0, skip for values 1 and 2
```

## Inheritance Tests

```yaml
- name: Parent with skipOnFailure
  skipOnFailure: true
  tests:
    - name: Child inherits skipOnFailure
      command: exit 1
      # Should be marked as skipped
    
    - name: Child overrides skipOnFailure
      command: exit 1
      skipOnFailure: false
      # Should be marked as failed

- name: Parent without skipOnFailure
  tests:
    - name: Child with skipOnFailure
      command: exit 1
      skipOnFailure: true
      # Should be marked as skipped
    
    - name: Child without skipOnFailure
      command: exit 1
      # Should be marked as failed
```

## Timeout Tests

```yaml
- name: Timeout with skipOnFailure
  command: sleep 10
  timeout: 1000
  skipOnFailure: true
  # Should be marked as skipped due to timeout

- name: Timeout without skipOnFailure
  command: sleep 10
  timeout: 1000
  skipOnFailure: false
  # Should be marked as failed due to timeout
```

## Complex Error Scenarios

```yaml
- name: Complex multi-condition failure with skipOnFailure
  command: echo "Some output with error"
  expect-regex: Expected output
  not-expect-regex: error
  skipOnFailure: true
  # Should be skipped due to both regex conditions failing
```

## Working Directory Tests

```yaml
- name: Working directory with skipOnFailure
  workingDirectory: /nonexistent/directory
  command: echo "Will not get here"
  skipOnFailure: true
  # Should be skipped due to invalid working directory
```

## Error with Valid Output

```yaml
- name: Command returns output but fails with skipOnFailure
  command: bash -c 'echo "Output before error"; exit 1'
  expect-regex: Output before error
  skipOnFailure: true
  # Should be marked as skipped despite matching output
```

## Reporting Skipped Tests

```yaml
- name: Verify skipped test in summary
  steps:
    - name: Create result holder
      bash: echo "0" > result_count.txt
      expect-regex: ^$

    - name: Test that will be skipped
      command: exit 1
      skipOnFailure: true
      # Should be marked as skipped
    
    - name: Count skipped test in report
      bash: |
        # This simulates accessing test results
        # In a real test, you'd examine the test output/report
        echo "1" > result_count.txt
        echo "Skipped test counted"
      expect-regex: Skipped test counted
    
    - name: Verify skipped test was counted
      bash: |
        count=$(cat result_count.txt)
        if [ "$count" -eq "1" ]; then
          echo "Skipped test was properly counted"
        else
          echo "Failed to count skipped test"
          exit 1
        fi
      expect-regex: Skipped test was properly counted
```

## Skip on Specific Exit Codes

```yaml
# Hypothetical feature extension - could be implemented in the framework
- name: Skip only on specific exit code
  command: exit 3
  skipOnFailure:
    exitCodes: [1, 2]
  # Should be marked as failed since exit code 3 is not in the skip list

- name: Skip on matching exit code
  command: exit 2
  skipOnFailure:
    exitCodes: [1, 2]
  # Should be marked as skipped since exit code 2 is in the skip list
```

## Expected Failures

```yaml
- name: Document expected failures
  command: exit 1
  skipOnFailure: true
  expect: This test is expected to fail, but we're marking it as skipped because we're working on fixing it.
  # Should be marked as skipped with documentation
```