# Unit Test Ideas for `name` Attribute

## Basic Functionality Tests

```yaml
- name: Simple test name
  command: echo "Hello"
  expect-regex: Hello

- name: Test name with spaces
  command: echo "Hello World"
  expect-regex: Hello World

- name: Test name with special characters
  command: echo "Special chars: !@#$%^&*()"
  expect-regex: Special chars: !@#\$%\^&\*\(\)
```

## Name Display Formatting

```yaml
- name: Very long test name that exceeds typical display width and may require truncation or wrapping in some contexts because it contains many words and characters
  command: echo "Test with long name"
  expect-regex: Test with long name

- name: Short name
  command: echo "Short"
  expect-regex: Short

- name: Name with unicode characters - こんにちは世界
  command: echo "Unicode test"
  expect-regex: Unicode test
```

## Nested Test Names

```yaml
- name: Parent test group
  tests:
    - name: Child test case 1
      command: echo "Child 1"
      expect-regex: Child 1
    
    - name: Child test case 2
      command: echo "Child 2"
      expect-regex: Child 2

- name: Multi-level nesting
  tests:
    - name: Level 1
      tests:
        - name: Level 2
          tests:
            - name: Level 3
              command: echo "Deep nesting"
              expect-regex: Deep nesting
```

## Name with Structured Information

```yaml
- name: "[CORE] Basic functionality test"
  command: echo "Core test"
  expect-regex: Core test

- name: "[UI] Button click test - #priority:high"
  command: echo "UI test"
  expect-regex: UI test

- name: "[API] GET /users endpoint - #regression #smoke"
  command: echo "API test"
  expect-regex: API test
```

## Name with Test Case ID

```yaml
- name: "TC001: Verify login with valid credentials"
  command: echo "Login test"
  expect-regex: Login test

- name: "TC002: Verify login with invalid credentials"
  command: echo "Invalid login test"
  expect-regex: Invalid login test

- name: "BUG-123: Fix for null reference exception"
  command: echo "Bug fix test"
  expect-regex: Bug fix test
```

## Name Templates with Matrix Values

```yaml
- name: Test with {value}
  matrix:
    value: [10, 20, 30]
  command: echo "Testing with ${{ matrix.value }}"
  expect-regex: Testing with ${{ matrix.value }}
  # Should display as "Test with 10", "Test with 20", "Test with 30"

- name: Test {browser} on {os}
  matrix:
    browser: [chrome, firefox, safari]
    os: [windows, macos, linux]
  command: echo "Testing ${{ matrix.browser }} on ${{ matrix.os }}"
  expect-regex: Testing ${{ matrix.browser }} on ${{ matrix.os }}
  # Should display as "Test chrome on windows", "Test chrome on macos", etc.
```

## Name with Environment Variables

```yaml
- name: Test in $ENV_NAME environment
  command: echo "Environment test"
  env:
    ENV_NAME: production
  expect-regex: Environment test
  # Should display as "Test in production environment"
```

## Name Uniqueness Tests

```yaml
- name: Duplicate test name
  command: echo "Test 1"
  expect-regex: Test 1

- name: Duplicate test name
  command: echo "Test 2"
  expect-regex: Test 2
  # Framework should handle duplicate names appropriately
```

## Name in Test Reports

```yaml
- name: "Test name with \"quoted\" text"
  command: echo "Quoted text test"
  expect-regex: Quoted text test
  # Should handle quoting in test reports

- name: Test name with XML special chars <>&
  command: echo "XML special chars test"
  expect-regex: XML special chars test
  # Should escape XML special characters in test reports
```

## Empty or Missing Name

```yaml
- command: echo "No name test"
  expect-regex: No name test
  # Should use default name or handle missing name

- name: ""
  command: echo "Empty name test"
  expect-regex: Empty name test
  # Should handle empty name
```

## Name with Shorthand Command Form

```yaml
- Login test: echo "Testing login"
  expect-regex: Testing login
  # Shorthand form where name is used as key for command

- "Get user data": echo "User data test"
  expect-regex: User data test
  # With quotes for name containing spaces
```

## Name with Steps

```yaml
- name: Test with sequential steps
  steps:
    - name: Step 1
      command: echo "Step 1 execution"
      expect-regex: Step 1 execution
    
    - name: Step 2
      command: echo "Step 2 execution"
      expect-regex: Step 2 execution
    
    - name: Step 3
      command: echo "Step 3 execution"
      expect-regex: Step 3 execution
```

## Test Class and Area with Name

```yaml
- area: Authentication
  class: LoginTests
  tests:
    - name: Valid login
      command: echo "Testing valid login"
      expect-regex: Testing valid login
    
    - name: Invalid login
      command: echo "Testing invalid login"
      expect-regex: Testing invalid login
```

## Dynamic Test Names

```yaml
- name: Test run at $(date)
  bash: |
    echo "Current date is $(date)"
    echo "Test complete"
  expect-regex: |
    Current date is .*
    Test complete
  # Should include actual date in test name

- name: "Test #$(date +%s)"
  command: echo "Timestamp test"
  expect-regex: Timestamp test
  # Should include timestamp in test name
```

## Escaping in Names

```yaml
- name: "Name with escaped \\{ characters \\}"
  command: echo "Escaped characters test"
  expect-regex: Escaped characters test
  # Should handle escaped curly braces correctly

- name: "Name with escaped quotes \\\" and more"
  command: echo "Escaped quotes test"
  expect-regex: Escaped quotes test
  # Should handle escaped quotes correctly
```

## Name Used in Command or Script

```yaml
- name: Echo test name
  bash: |
    echo "Test name: $TEST_NAME"
  env:
    TEST_NAME: Echo test name
  expect-regex: Test name: Echo test name

- name: Reference in script
  bash: |
    test_name="${TEST_NAME}"
    echo "Running test: $test_name"
  env:
    TEST_NAME: Reference in script
  expect-regex: Running test: Reference in script
```