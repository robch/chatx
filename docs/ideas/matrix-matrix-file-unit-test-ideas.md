# Unit Test Ideas for `matrix` and `matrix-file` Attributes

## Basic Matrix Functionality Tests

```yaml
- name: Simple single-parameter matrix
  matrix:
    value: [1, 2, 3]
  command: echo "Value is ${{ matrix.value }}"
  expect-regex: Value is ${{ matrix.value }}

- name: Multi-parameter matrix
  matrix:
    animal: [cat, dog, bird]
    color: [red, blue]
  command: echo "The ${{ matrix.color }} ${{ matrix.animal }}"
  expect-regex: The ${{ matrix.color }} ${{ matrix.animal }}
```

## Matrix Expansion Tests

```yaml
- name: Matrix expansion validation
  steps:
    - name: Create results file
      bash: |
        rm -f results.txt
        touch results.txt
        echo "Results file created"
      expect-regex: Results file created
      
    - name: Matrix test
      matrix:
        x: [1, 2]
        y: [a, b]
      bash: |
        echo "${{ matrix.x }}-${{ matrix.y }}" >> results.txt
        echo "Recorded ${{ matrix.x }}-${{ matrix.y }}"
      expect-regex: Recorded ${{ matrix.x }}-${{ matrix.y }}
      
    - name: Verify all combinations
      bash: |
        sort results.txt
        if [ $(wc -l < results.txt) -eq 4 ]; then
          echo "All 4 combinations found"
        else
          echo "ERROR: Missing combinations"
          exit 1
        fi
      expect-regex: |
        1-a
        1-b
        2-a
        2-b
        All 4 combinations found
```

## Matrix File Tests

```yaml
# Create a file named matrix-data.yaml with:
# animals:
#   - cat
#   - dog
#   - bird
# colors:
#   - red
#   - blue

- name: Basic matrix-file test
  matrix-file: matrix-data.yaml
  command: echo "The ${{ matrix.colors }} ${{ matrix.animals }}"
  expect-regex: The ${{ matrix.colors }} ${{ matrix.animals }}

- name: Matrix-file with foreach
  matrix-file: matrix-data-foreach.yaml
  # matrix-data-foreach.yaml contains:
  # foreach:
  #   - question: How are you?
  #   - question: What time is it?
  #   - question: What is the weather?
  command: echo "Question: ${{ matrix.question }}"
  expect-regex: Question: ${{ matrix.question }}
```

## Complex Matrix Structures

```yaml
- name: Matrix with foreach objects
  matrix:
    foreach:
      - name: Alice
        age: 30
      - name: Bob
        age: 25
      - name: Carol
        age: 35
  command: echo "Name: ${{ matrix.name }}, Age: ${{ matrix.age }}"
  expect-regex: Name: ${{ matrix.name }}, Age: ${{ matrix.age }}

- name: Nested matrix values
  matrix:
    config:
      - debug: true
        optimize: false
      - debug: false
        optimize: true
    platform: [linux, windows, macos]
  command: echo "Platform: ${{ matrix.platform }}, Debug: ${{ matrix.config.debug }}, Optimize: ${{ matrix.config.optimize }}"
  expect-regex: Platform: ${{ matrix.platform }}, Debug: (true|false), Optimize: (true|false)
```

## Special Matrix Values

```yaml
- name: Matrix with empty values
  matrix:
    param: ["", "value", null]
  command: echo "Param is '${{ matrix.param }}'"
  expect-regex: Param is '${{ matrix.param }}'

- name: Matrix with special characters
  matrix:
    special: ["a+b", "c&d", "e=f", "x<y>z"]
  command: echo "Special: ${{ matrix.special }}"
  expect-regex: Special: ${{ matrix.special }}

- name: Matrix with quotes
  matrix:
    quoted: ["single'quote", 'double"quote', "mixed'\"quotes"]
  command: echo "Quoted: ${{ matrix.quoted }}"
  expect-regex: Quoted: .*quotes?.*
```

## Matrix Variable Replacement

```yaml
- name: Matrix in command
  matrix:
    cmd: [echo, printf]
    text: ["Hello", "World"]
  command: ${{ matrix.cmd }} "${{ matrix.text }}"
  expect-regex: (Hello|World)

- name: Matrix in script
  matrix:
    value: [1, 2]
  bash: |
    echo "The value is ${{ matrix.value }}"
    result=$((${{ matrix.value }} * 10))
    echo "Result: $result"
  expect-regex: |
    The value is ${{ matrix.value }}
    Result: ${{ matrix.value * 10 }}

- name: Matrix in expect-regex
  matrix:
    expected: ["abc123", "xyz789"]
  command: echo "${{ matrix.expected }}"
  expect-regex: ${{ matrix.expected }}
```

## Matrix ID Tests

```yaml
- name: Matrix with ID
  matrix:
    value: [1, 2, 3]
  bash: |
    echo "Value: ${{ matrix.value }}"
    echo "Matrix ID: ${{ matrix.__matrix_id__ }}"
  expect-regex: |
    Value: ${{ matrix.value }}
    Matrix ID: .*
```

## Matrix with Environment Variables

```yaml
- name: Matrix with environment variables
  matrix:
    env_name: [dev, staging, prod]
  env:
    ENVIRONMENT: ${{ matrix.env_name }}
  command: echo "Running in $ENVIRONMENT environment"
  expect-regex: Running in ${{ matrix.env_name }} environment
```

## Matrix with Working Directory

```yaml
- name: Matrix with working directories
  steps:
    - name: Create directories
      bash: |
        mkdir -p dir1 dir2 dir3
        echo "Directories created"
      expect-regex: Directories created
      
    - name: Use matrix directories
      matrix:
        dir: [dir1, dir2, dir3]
      workingDirectory: ./${{ matrix.dir }}
      bash: |
        echo "Current dir: $(basename $(pwd))"
        echo "${{ matrix.dir }}" > marker.txt
      expect-regex: Current dir: ${{ matrix.dir }}
      
    - name: Verify matrix run in each directory
      bash: |
        for dir in dir1 dir2 dir3; do
          content=$(cat $dir/marker.txt)
          if [ "$content" != "$dir" ]; then
            echo "ERROR: $dir contains wrong content"
            exit 1
          fi
        done
        echo "All directory contents verified"
      expect-regex: All directory contents verified
```

## Matrix with Input

```yaml
- name: Matrix with different inputs
  matrix:
    user_input: [Alice, Bob, Charlie]
  command: python -c "name = input('Enter name: '); print(f'Hello, {name}!')"
  input: ${{ matrix.user_input }}
  expect-regex: |
    Enter name: Hello, ${{ matrix.user_input }}!
```

## Matrix Combination Control

```yaml
- name: Matrix include additional combinations
  matrix:
    os: [linux, windows]
    version: [v1, v2]
    include:
      - os: macos
        version: v3
  command: echo "Testing on ${{ matrix.os }} with ${{ matrix.version }}"
  expect-regex: Testing on ${{ matrix.os }} with ${{ matrix.version }}
  # Should run linux-v1, linux-v2, windows-v1, windows-v2, macos-v3

- name: Matrix exclude combinations
  matrix:
    os: [linux, windows, macos]
    arch: [x86, arm]
    exclude:
      - os: windows
        arch: arm
  command: echo "Testing on ${{ matrix.os }} with ${{ matrix.arch }}"
  expect-regex: Testing on ${{ matrix.os }} with ${{ matrix.arch }}
  # Should run all combinations except windows-arm
```

## Error Cases

```yaml
- name: Reference to non-existent matrix parameter
  matrix:
    a: [1, 2]
  command: echo "Value: ${{ matrix.b }}"
  expect-regex: Value: 
  # Should handle undefined matrix value gracefully

- name: Empty matrix
  matrix: {}
  command: echo "No matrix values"
  expect-regex: No matrix values
  # Should run once with no matrix values

- name: Invalid matrix file
  matrix-file: non-existent-file.yaml
  command: echo "Test"
  expect-regex: Test
  # Should handle missing file gracefully
```

## Matrix Display Name Templates

```yaml
- name: Matrix with template in name
  matrix:
    browser: [chrome, firefox, safari]
    device: [desktop, mobile]
  name: Test in {browser} on {device}
  command: echo "Testing ${{ matrix.browser }} on ${{ matrix.device }}"
  expect-regex: Testing ${{ matrix.browser }} on ${{ matrix.device }}
  # Should have display names like "Test in chrome on desktop"
```

## Large Matrix Tests

```yaml
- name: Large matrix test
  matrix:
    param1: [1, 2, 3, 4, 5]
    param2: [a, b, c, d, e]
    param3: [low, medium, high]
  command: echo "${{ matrix.param1 }}-${{ matrix.param2 }}-${{ matrix.param3 }}"
  expect-regex: ${{ matrix.param1 }}-${{ matrix.param2 }}-${{ matrix.param3 }}
  # Should generate 5 * 5 * 3 = 75 tests
```