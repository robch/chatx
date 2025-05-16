# Unit Test Ideas for `tag` and `tags` Attributes

## Basic Functionality Tests

```yaml
- name: Single tag
  tag: basic
  command: echo "Test with single tag"
  expect-regex: Test with single tag

- name: Multiple tags as array
  tags:
    - feature1
    - priority-high
    - smoke
  command: echo "Test with multiple tags"
  expect-regex: Test with multiple tags

- name: Multiple tags as string
  tags: "feature1, priority-high, smoke"
  command: echo "Test with multiple tags as string"
  expect-regex: Test with multiple tags as string

- name: Single tag with tags attribute
  tags: single-tag
  command: echo "Test with single tag in tags attribute"
  expect-regex: Test with single tag in tags attribute
```

## Tag Inheritance

```yaml
- name: Parent with tags
  tags:
    - parent-tag
    - shared-tag
  tests:
    - name: Child inherits parent tags
      command: echo "Should have parent tags"
      expect-regex: Should have parent tags
    
    - name: Child with additional tags
      tags:
        - child-tag
      command: echo "Should have parent and child tags"
      expect-regex: Should have parent and child tags
    
    - name: Child overriding tag format
      tag: override-tag
      command: echo "Should have parent tags plus override"
      expect-regex: Should have parent tags plus override

- name: Multi-level tag inheritance
  tag: top-level
  tests:
    - name: Middle level
      tags:
        - middle-level
      tests:
        - name: Bottom level with all inherited
          command: echo "Should have all ancestor tags"
          expect-regex: Should have all ancestor tags
        
        - name: Bottom level with additional
          tags:
            - bottom-level
          command: echo "Should have all ancestor plus own tags"
          expect-regex: Should have all ancestor plus own tags
```

## Special Tag Cases

```yaml
- name: Empty tags
  tags: []
  command: echo "Test with empty tags array"
  expect-regex: Test with empty tags array

- name: Tag with special characters
  tag: "tag-with-special_chars.123"
  command: echo "Test with special character tag"
  expect-regex: Test with special character tag

- name: Very long tag
  tag: abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789
  command: echo "Test with very long tag"
  expect-regex: Test with very long tag

- name: Unicode tags
  tags:
    - "標籤"
    - "тег" 
    - "태그"
  command: echo "Test with Unicode tags"
  expect-regex: Test with Unicode tags
```

## Default Tags File

```yaml
# This requires a default tags file (.ai-tests-default-tags.yaml) with the following content:
# tag: default-tag
# tags:
#   - global-tag

- name: Test using default tags
  command: echo "Should inherit from default tags file"
  expect-regex: Should inherit from default tags file
  # Should have tags: default-tag, global-tag

- name: Test overriding default tags
  tag: override-default
  command: echo "Should have overridden default tags"
  expect-regex: Should have overridden default tags
  # Should have tags: override-default, global-tag
```

## Tag Filtering

```yaml
# These tests demonstrate how filtering would work
# In an actual test run, you'd filter with command-line arguments

- name: Tagged for smoke testing
  tag: smoke
  command: echo "Smoke test"
  expect-regex: Smoke test
  # Would run when filtered with +smoke

- name: Tagged for regression testing
  tag: regression
  command: echo "Regression test"
  expect-regex: Regression test
  # Would run when filtered with +regression

- name: Multiple filterable tags
  tags:
    - api
    - slow
    - security
  command: echo "Multiple tags test"
  expect-regex: Multiple tags test
  # Would run when filtered with +api, +slow, or +security

- name: Skip tag
  tag: skip
  command: echo "Should be skipped"
  expect-regex: Should be skipped
  # Would NOT run when filtered with -skip
```

## Complex Tag Combinations

```yaml
- name: Feature area tags
  tags:
    - feature:authentication
    - feature:login
  command: echo "Feature area test"
  expect-regex: Feature area test

- name: Priority tags
  tags:
    - priority:high
    - blocker
  command: echo "High priority test"
  expect-regex: High priority test

- name: Environment tags
  tags:
    - env:production
    - env:staging
  command: echo "Environment-specific test"
  expect-regex: Environment-specific test

- name: Development phase tags
  tags:
    - phase:alpha
    - early-access
  command: echo "Development phase test"
  expect-regex: Development phase test
```

## Tag-Based Conditional Tests

```yaml
- name: Conditional tests based on tags
  steps:
    - name: Check if running smoke tests
      bash: |
        # This is a placeholder for actual tag-based logic
        # In reality, you'd use the test framework's filtering mechanism
        if [[ "$TAGS" == *"smoke"* ]]; then
          echo "Running smoke tests"
        else
          echo "Not running smoke tests"
        fi
      expect-regex: (Running|Not running) smoke tests
      
    - name: Check platform-specific tag
      bash: |
        if [[ "$(uname)" == "Darwin" ]]; then
          echo "tag: macos"
        elif [[ "$(uname)" == "Linux" ]]; then
          echo "tag: linux"
        else
          echo "tag: windows"
        fi
      expect-regex: tag: (macos|linux|windows)
```

## Tag Integration with Other Features

```yaml
- name: Matrix with tags
  matrix:
    platform: [windows, linux, macos]
  tags:
    - ${{ matrix.platform }}
    - cross-platform
  command: echo "Testing on ${{ matrix.platform }}"
  expect-regex: Testing on ${{ matrix.platform }}

- name: Environment-specific tags
  bash: |
    CURRENT_ENV=$(echo $ENV_TYPE | tr '[:upper:]' '[:lower:]')
    echo "Running in $CURRENT_ENV environment"
  env:
    ENV_TYPE: Production
  tags:
    - env:${{ env.ENV_TYPE | lowercase }}
  expect-regex: Running in production environment
```

## Tag-Driven Test Setup

```yaml
- name: Database setup based on tags
  tags:
    - requires:database
    - backend
  steps:
    - name: Set up database if required
      bash: |
        # In a real test, you would check if the current run includes the requires:database tag
        if [[ "$TAGS" == *"requires:database"* ]]; then
          echo "Setting up test database"
          # setup_test_db.sh
        else
          echo "Skipping database setup"
        fi
      expect-regex: Setting up test database
      
    - name: Run the actual test
      command: echo "Database test running"
      expect-regex: Database test running
```

## Tag Reporting

```yaml
- name: Test tag reporting
  tag: reported-tag
  steps:
    - name: Run test
      command: echo "Test with tag"
      expect-regex: Test with tag
      
    - name: Verify tag in results
      bash: |
        # This is a placeholder; in reality you would check the test results format
        # to confirm the tag was properly included in the test metadata
        echo "Tag 'reported-tag' included in test results"
      expect-regex: Tag 'reported-tag' included in test results
```