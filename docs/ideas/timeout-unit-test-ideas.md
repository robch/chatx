# Unit Test Ideas for `timeout` Attribute

## Basic Functionality Tests

```yaml
- name: Basic timeout - command completes
  command: echo "Quick command"
  timeout: 1000
  expect-regex: Quick command

- name: Basic timeout - command exceeds timeout
  command: sleep 3
  timeout: 1000
  # Should fail due to timeout

- name: Exact timeout
  command: sleep 1
  timeout: 1000
  # Should pass because command completes right at timeout
```

## Different Time Ranges

```yaml
- name: Very short timeout
  command: echo "Immediate"
  timeout: 100
  expect-regex: Immediate

- name: Medium timeout
  command: sleep 2
  timeout: 3000
  expect-regex: ^$

- name: Long timeout
  command: sleep 5
  timeout: 10000
  expect-regex: ^$

- name: Very long timeout
  command: sleep 10
  timeout: 60000
  expect-regex: ^$
```

## Different Command Types

```yaml
- name: Bash script with timeout
  bash: |
    echo "Starting long process..."
    sleep 5
    echo "Done"
  timeout: 2000
  # Should fail due to timeout

- name: Script with timeout
  script: |
    @echo off
    echo Starting long process...
    timeout /t 5
    echo Done
  timeout: 2000
  # Should fail due to timeout

- name: Process with output before timeout
  command: bash -c "echo 'Output before hanging'; sleep 10"
  timeout: 2000
  expect-regex: Output before hanging
  # Should fail but capture initial output
```

## Process Handling Tests

```yaml
- name: Graceful termination
  command: bash -c "trap 'echo Caught SIGTERM; exit' TERM; echo Starting; sleep 10; echo Should not see this"
  timeout: 2000
  expect-regex: |
    Starting
    Caught SIGTERM

- name: Process with children
  bash: |
    echo "Parent starting"
    sleep 1 &
    sleep 2 &
    sleep 3 &
    echo "Parent waiting"
    wait
    echo "Parent done"
  timeout: 2000
  expect-regex: |
    Parent starting
    Parent waiting
  # Should terminate parent and all children
```

## Integration with Other Features

```yaml
- name: Timeout with environment variables
  command: bash -c 'echo "Sleeping for $SLEEP_TIME seconds"; sleep $SLEEP_TIME; echo "Done"'
  env:
    SLEEP_TIME: 2
  timeout: 1000
  expect-regex: Sleeping for 2 seconds
  # Should fail due to timeout

- name: Matrix with varying timeouts
  matrix:
    seconds: [1, 3, 5]
    limit: [2000, 2000, 2000]
  command: sleep ${{ matrix.seconds }}
  timeout: ${{ matrix.limit }}
  # Should pass for seconds=1, fail for seconds=3, fail for seconds=5

- name: Timeout with parallelize
  command: sleep 3
  timeout: 1000
  parallelize: true
  # Should fail due to timeout
```

## Edge Cases

```yaml
- name: Zero timeout
  command: echo "Immediate execution"
  timeout: 0
  # Should behave as if no timeout was specified

- name: Very large timeout value
  command: echo "Normal command"
  timeout: 2147483647  # ~24.8 days
  expect-regex: Normal command

- name: Negative timeout value
  command: echo "Test command"
  timeout: -1000
  expect-regex: Test command
  # Should be treated as invalid and use default timeout
```

## Resource-Intensive Operations

```yaml
- name: CPU-intensive operation
  bash: |
    echo "Starting CPU-intensive calculation"
    perl -e 'for($i=0;$i<1000000000;$i++){$a=$i*$i}'
    echo "Calculation complete"
  timeout: 2000
  # May time out depending on system speed

- name: Memory-intensive operation
  bash: |
    echo "Starting memory-intensive operation"
    # Create a large array in memory
    node -e "const arr = new Array(1000000).fill('x'.repeat(1000)); console.log('Memory allocated');"
    echo "Operation complete"
  timeout: 2000
  # May time out depending on system memory
```

## Interactive Processes

```yaml
- name: Interactive process with timeout
  command: python -c "name = input('Enter name: '); print(f'Hello, {name}!')"
  input: Alice
  timeout: 2000
  expect-regex: |
    Enter name: Hello, Alice!

- name: Slow interactive process with timeout
  command: python -c "import time; name = input('Enter name: '); time.sleep(3); print(f'Hello, {name}!')"
  input: Bob
  timeout: 2000
  expect-regex: Enter name:
  # Should fail due to timeout after input
```

## Multiple Command Test

```yaml
- name: Multiple commands with individual timeouts
  steps:
    - name: First command (should succeed)
      command: echo "Quick command"
      timeout: 1000
      expect-regex: Quick command
    
    - name: Second command (should timeout)
      command: sleep 3
      timeout: 1000
      skipOnFailure: true
      # Should timeout but be marked as skipped
    
    - name: Third command (should succeed)
      command: echo "Another quick command"
      timeout: 1000
      expect-regex: Another quick command
```

## Timeout with Error Handling

```yaml
- name: Timeout with skipOnFailure
  command: sleep 5
  timeout: 2000
  skipOnFailure: true
  # Should be marked as skipped instead of failed

- name: Timeout error message check
  command: sleep 5
  timeout: 2000
  skipOnFailure: true
  # Check that timeout error message is properly captured in results
```

## Race Conditions

```yaml
- name: Race condition at timeout boundary
  command: bash -c "sleep 1.99; echo 'Just in time'"
  timeout: 2000
  expect-regex: Just in time
  # Should pass but may be close to timeout

- name: Output handling at timeout
  command: bash -c "for i in {1..10}; do echo \"Line $i\"; sleep 0.5; done"
  timeout: 2500
  expect-regex: Line 1
  # Should capture partial output before timeout
```