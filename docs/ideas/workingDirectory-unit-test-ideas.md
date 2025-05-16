# Unit Test Ideas for `workingDirectory` Attribute

## Basic Functionality Tests

```yaml
- name: Absolute path working directory
  command: pwd
  workingDirectory: /tmp
  expect-regex: /tmp

- name: Relative path working directory
  command: pwd
  workingDirectory: ./subdir
  expect-regex: .*subdir$

- name: Parent directory
  command: pwd
  workingDirectory: ..
  expect-regex: .*
```

## Directory Creation and Manipulation

```yaml
- name: Create and use new directory
  steps:
    - name: Create a new directory
      bash: |
        mkdir -p ./test_working_dir
        echo "Directory created"
      expect-regex: Directory created
      
    - name: Use the new directory
      command: pwd
      workingDirectory: ./test_working_dir
      expect-regex: .*test_working_dir$
      
    - name: Create file in working directory
      bash: |
        echo "Hello from working directory" > test_file.txt
        cat test_file.txt
      workingDirectory: ./test_working_dir
      expect-regex: Hello from working directory
```

## Path Inheritance Tests

```yaml
- name: Parent with working directory
  workingDirectory: ./parent_dir
  steps:
    - name: Setup parent dir
      bash: |
        mkdir -p ./parent_dir
        echo "Parent dir created"
      expect-regex: Parent dir created
      
    - name: Child inherits parent working directory
      command: pwd
      expect-regex: .*parent_dir$
      
    - name: Child with relative path
      command: pwd
      workingDirectory: ./child_dir
      expect-regex: .*parent_dir/child_dir$
      
    - name: Child with absolute path
      command: pwd
      workingDirectory: /tmp
      expect-regex: /tmp
```

## Multi-Level Path Tests

```yaml
- name: Multi-level path inheritance
  steps:
    - name: Setup directories
      bash: |
        mkdir -p ./level1/level2/level3
        echo "Directories created"
      expect-regex: Directories created
      
    - name: Set top level
      workingDirectory: ./level1
      steps:
        - name: Verify level 1
          command: pwd
          expect-regex: .*level1$
          
        - name: Set mid level
          workingDirectory: ./level2
          steps:
            - name: Verify level 2
              command: pwd
              expect-regex: .*level1/level2$
              
            - name: Set final level
              workingDirectory: ./level3
              command: pwd
              expect-regex: .*level1/level2/level3$
```

## File Interaction Tests

```yaml
- name: File operations across working directories
  steps:
    - name: Setup directories
      bash: |
        mkdir -p ./dir1 ./dir2
        echo "content1" > ./dir1/file1.txt
        echo "Directories and file created"
      expect-regex: Directories and file created
      
    - name: Verify file in dir1
      command: cat file1.txt
      workingDirectory: ./dir1
      expect-regex: content1
      
    - name: Copy file to dir2
      bash: |
        cp ../dir1/file1.txt file2.txt
        echo "File copied"
      workingDirectory: ./dir2
      expect-regex: File copied
      
    - name: Verify file in dir2
      command: cat file2.txt
      workingDirectory: ./dir2
      expect-regex: content1
```

## Special Path Tests

```yaml
- name: Empty working directory
  command: pwd
  workingDirectory: ""
  expect-regex: .*
  # Should use default working directory

- name: Home directory
  command: pwd
  workingDirectory: ~/
  expect-regex: /home/.*|/Users/.*|/root
  # Should expand to user's home directory

- name: Current directory
  command: pwd
  workingDirectory: .
  expect-regex: .*
  # Should use current directory
```

## Environment Variable Paths

```yaml
- name: Working directory from environment variable
  steps:
    - name: Setup environment
      bash: |
        mkdir -p ./env_dir
        echo "Directory created"
      expect-regex: Directory created
      
    - name: Set directory in environment
      env:
        TEST_DIR: ./env_dir
      command: pwd
      workingDirectory: $TEST_DIR
      expect-regex: .*env_dir$
```

## Edge Cases

```yaml
- name: Non-existent directory
  command: pwd
  workingDirectory: ./does_not_exist
  # Should create the directory or fail gracefully

- name: Directory with spaces
  steps:
    - name: Create directory with spaces
      bash: |
        mkdir -p "./directory with spaces"
        echo "Directory created"
      expect-regex: Directory created
      
    - name: Use directory with spaces
      command: pwd
      workingDirectory: "./directory with spaces"
      expect-regex: .*directory with spaces$

- name: Directory with special characters
  steps:
    - name: Create directory with special chars
      bash: |
        mkdir -p "./dir-with_special@chars"
        echo "Directory created"
      expect-regex: Directory created
      
    - name: Use directory with special chars
      command: pwd
      workingDirectory: "./dir-with_special@chars"
      expect-regex: .*dir-with_special@chars$
```

## Platform-Specific Tests

```yaml
- name: Windows-style paths (on Windows)
  command: cd
  workingDirectory: C:\Windows\Temp
  expect-regex: C:\\Windows\\Temp

- name: Backslash paths (on Windows)
  command: cd
  workingDirectory: .\test_dir
  expect-regex: .*test_dir$

- name: UNC paths (on Windows)
  command: cd
  workingDirectory: \\localhost\c$\temp
  expect-regex: \\\\localhost\\c\$\\temp
```

## Permission Tests

```yaml
- name: Read-only directory test
  steps:
    - name: Create and set read-only directory
      bash: |
        mkdir -p ./readonly_dir
        chmod 555 ./readonly_dir
        echo "Read-only directory created"
      expect-regex: Read-only directory created
      
    - name: Try to write in read-only directory
      bash: |
        touch test_file.txt 2>&1 || echo "Cannot write to directory"
      workingDirectory: ./readonly_dir
      expect-regex: Cannot write to directory
```

## Path Resolution Tests

```yaml
- name: Symbolic link resolution
  steps:
    - name: Create directory with symlink
      bash: |
        mkdir -p ./target_dir
        ln -sf ./target_dir ./link_dir
        echo "Link created"
      expect-regex: Link created
      
    - name: Use symlink as working directory
      command: pwd -P  # Physical directory
      workingDirectory: ./link_dir
      expect-regex: .*target_dir$
```

## Integration with Other Test Features

```yaml
- name: Matrix with different working directories
  matrix:
    dirname: [dir1, dir2, dir3]
  steps:
    - name: Create directories
      bash: |
        mkdir -p ./${{ matrix.dirname }}
        echo "Created ${{ matrix.dirname }}"
      expect-regex: Created ${{ matrix.dirname }}
      
    - name: Use matrix directory
      command: pwd
      workingDirectory: ./${{ matrix.dirname }}
      expect-regex: .*${{ matrix.dirname }}$
```