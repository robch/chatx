# Unit Test Ideas for `tests` and `steps` Attributes

## Basic Functionality Tests

```yaml
- name: Basic tests list
  tests:
    - name: Test 1
      command: echo "Test 1"
      expect-regex: Test 1
    - name: Test 2
      command: echo "Test 2"
      expect-regex: Test 2
    - name: Test 3
      command: echo "Test 3"
      expect-regex: Test 3

- name: Basic steps list
  steps:
    - name: Step 1
      command: echo "Step 1"
      expect-regex: Step 1
    - name: Step 2
      command: echo "Step 2"
      expect-regex: Step 2
    - name: Step 3
      command: echo "Step 3"
      expect-regex: Step 3
```

## Sequential Execution in Steps

```yaml
- name: Sequential steps with file verification
  steps:
    - name: Step 1 - Create file
      bash: |
        echo "Content" > test_file.txt
        echo "File created"
      expect-regex: File created
    - name: Step 2 - Read file
      bash: |
        cat test_file.txt
        echo "File read"
      expect-regex: |
        Content
        File read
    - name: Step 3 - Delete file
      bash: |
        rm test_file.txt
        echo "File deleted"
      expect-regex: File deleted
```

## State Preservation Between Steps

```yaml
- name: State preservation between steps
  steps:
    - name: Step 1 - Set variable
      bash: |
        export TEST_VAR="shared value"
        echo "Variable set"
      expect-regex: Variable set
    - name: Step 2 - Read variable
      bash: |
        echo "Variable value: $TEST_VAR"
      expect-regex: Variable value: shared value
```

## Nested Tests Structure

```yaml
- name: Parent test group
  env:
    PARENT_VAR: parent value
  tests:
    - name: Child test group 1
      tests:
        - name: Nested test 1
          command: echo "PARENT_VAR=$PARENT_VAR"
          expect-regex: PARENT_VAR=parent value
        - name: Nested test 2
          command: echo "Nested 2"
          expect-regex: Nested 2
    - name: Child test group 2
      env:
        CHILD_VAR: child value
      tests:
        - name: Nested test 3
          command: echo "PARENT_VAR=$PARENT_VAR, CHILD_VAR=$CHILD_VAR"
          expect-regex: PARENT_VAR=parent value, CHILD_VAR=child value
```

## Error Handling in Steps

```yaml
- name: Error handling in steps
  steps:
    - name: Step 1 - Successful
      command: echo "Success"
      expect-regex: Success
    - name: Step 2 - Fails
      command: exit 1
      skipOnFailure: true
    - name: Step 3 - Should still run
      command: echo "Still running"
      expect-regex: Still running
```

## Empty Lists

```yaml
- name: Empty tests list
  tests: []
  # Should pass with no tests executed

- name: Empty steps list
  steps: []
  # Should pass with no steps executed
```

## Mixed Step Types

```yaml
- name: Mixed step types
  steps:
    - name: Command step
      command: echo "Command output"
      expect-regex: Command output
    - name: Script step
      script: |
        echo "Script output"
      expect-regex: Script output
    - name: Bash step
      bash: |
        echo "Bash output"
      expect-regex: Bash output
```

## Test Metadata Inheritance

```yaml
- name: Metadata inheritance
  tag: parent-tag
  workingDirectory: ./parent-directory
  env:
    PARENT_ENV: parent-value
  steps:
    - name: Step inheriting metadata
      command: echo "TAG=$tag WORKDIR=$PWD ENV=$PARENT_ENV"
      expect-regex: TAG=parent-tag WORKDIR=.*parent-directory ENV=parent-value
    - name: Step overriding metadata
      tag: step-tag
      workingDirectory: ./step-directory
      env:
        PARENT_ENV: overridden-value
      command: echo "TAG=$tag WORKDIR=$PWD ENV=$PARENT_ENV"
      expect-regex: TAG=step-tag WORKDIR=.*step-directory ENV=overridden-value
```

## Parallelization Tests

```yaml
- name: Parallel execution tests
  tests:
    - name: Parallel test 1
      command: echo "Parallel 1" && sleep 1
      parallelize: true
      expect-regex: Parallel 1
    - name: Parallel test 2
      command: echo "Parallel 2" && sleep 1
      parallelize: true
      expect-regex: Parallel 2
    - name: Non-parallel test
      command: echo "Non-parallel"
      parallelize: false
      expect-regex: Non-parallel
```

## Steps with Complex Dependencies

```yaml
- name: Steps with shared file dependencies
  steps:
    - name: Create config file
      bash: |
        cat > config.json << EOF
        {
          "setting1": "value1",
          "setting2": "value2"
        }
        EOF
        echo "Config created"
      expect-regex: Config created
    - name: Read and modify config
      bash: |
        cat config.json
        # Modify the file
        sed -i 's/value1/modified-value1/' config.json
        echo "Config modified"
      expect-regex: Config modified
    - name: Verify modifications
      bash: |
        cat config.json
        echo "Verification complete"
      expect-regex: modified-value1
```