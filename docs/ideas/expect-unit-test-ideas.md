# Unit Test Ideas for `expect` Attribute

## Basic Functionality Tests

```yaml
- name: Simple LLM expectation
  command: echo "The capital of France is Paris."
  expect: The output must correctly state that Paris is the capital of France.

- name: Expectation with numeric output
  command: echo "2 + 2 = 4"
  expect: The output must contain the correct result of adding 2 and 2, which is 4.

- name: Expectation with scientific fact
  command: echo "Water boils at 100 degrees Celsius at standard pressure."
  expect: The output must state a correct scientific fact about the boiling point of water at standard pressure.
```

## Complex Validation Tests

```yaml
- name: Semantic understanding check
  command: echo "Apples are fruits that grow on trees and are often red or green."
  expect: The output should provide factually correct information about apples, including that they are fruits and their common colors.

- name: Logical reasoning check
  command: echo "All humans are mortal. Socrates is human. Therefore, Socrates is mortal."
  expect: The output should present a logically valid syllogism.

- name: Code correctness check
  command: echo "function add(a, b) { return a + b; }"
  expect: The output should contain a correct implementation of a function that adds two numbers.
```

## Edge Cases

```yaml
- name: Empty output check
  command: echo -n ""
  expect: The output should be empty.

- name: Ambiguous output check
  command: echo "It depends on several factors."
  expect: The output should acknowledge that the answer depends on multiple factors without making a definitive claim.

- name: Long output assessment
  command: cat long_document.txt
  expect: The output should be a coherent document with multiple paragraphs covering the topic of artificial intelligence, its history, applications, and ethical considerations.
```

## Error Detection Tests

```yaml
- name: Semantic error detection
  command: echo "The capital of France is London."
  expect: The output contains incorrect information. It incorrectly states that London is the capital of France, when the capital of France is actually Paris.
  # This should fail

- name: Spelling error detection
  command: echo "Recieved the package yesterday."
  expect: The output should contain correct spelling of all words. The word "received" is misspelled.
  # This should fail

- name: Grammatical error detection
  command: echo "The team are going to win."
  expect: The output should use correct subject-verb agreement in all sentences. (Depending on whether British or American English is expected)
```

## Combined with Output Processing

```yaml
- name: JSON structure validation
  command: echo '{"name":"John","age":30,"city":"New York"}'
  expect: The output should be valid JSON representing a person with a name, age, and city properties. All values should be of appropriate types.

- name: Table format validation
  command: |
    echo "Name  | Age | City"
    echo "------|-----|------"
    echo "John  | 30  | New York"
    echo "Alice | 25  | London"
  expect: The output should be a properly formatted Markdown table with headers and at least two rows of data showing people's names, ages, and cities.
```

## Integration with Other Test Features

```yaml
- name: Expect with environment variables
  command: echo "Running in $ENV_NAME environment"
  env:
    ENV_NAME: production
  expect: The output should indicate that the code is running in the production environment.

- name: Matrix-based expectation
  matrix:
    operation: [addition, subtraction, multiplication]
    result: [15, 5, 50]
  command: echo "The result of ${{ matrix.operation }} is ${{ matrix.result }}"
  expect: |
    The output should correctly state that the result of ${{ matrix.operation }} is ${{ matrix.result }}.
```

## Language Model Reasoning Tests

```yaml
- name: Multi-step reasoning
  command: |
    echo "Problem: A train travels 120 km at 60 km/h, then another 120 km at 40 km/h."
    echo "Question: What is the average speed for the entire journey?"
  expect: |
    The output should provide the correct answer of 48 km/h and explain the reasoning:
    - First leg: 120 km at 60 km/h takes 2 hours
    - Second leg: 120 km at 40 km/h takes 3 hours
    - Total distance: 240 km
    - Total time: 5 hours
    - Average speed: 240 km / 5 hours = 48 km/h

- name: Contextual understanding
  command: |
    echo "Alice: Can I borrow your pen?"
    echo "Bob: Sure, here you go."
    echo "Alice: Thanks, I've been looking everywhere for one!"
  expect: |
    The output should contain a dialogue between two people where one person borrows a pen from the other.
```

## Domain-Specific Validation

```yaml
- name: Code output validation
  command: |
    echo "function isPrime(n) {"
    echo "  if (n <= 1) return false;"
    echo "  if (n <= 3) return true;"
    echo "  if (n % 2 === 0 || n % 3 === 0) return false;"
    echo "  for (let i = 5; i * i <= n; i += 6) {"
    echo "    if (n % i === 0 || n % (i + 2) === 0) return false;"
    echo "  }"
    echo "  return true;"
    echo "}"
  expect: |
    The output should contain a correct JavaScript function that determines if a number is prime.
    The function should:
    1. Handle edge cases (numbers ≤ 1)
    2. Use optimization techniques like checking divisibility by 2 and 3
    3. Use an efficient algorithm for checking larger numbers
    4. Return a boolean result

- name: SQL query validation
  command: |
    echo "SELECT c.customer_name, SUM(o.total_amount) as total_spent"
    echo "FROM customers c"
    echo "JOIN orders o ON c.customer_id = o.customer_id"
    echo "WHERE o.order_date BETWEEN '2023-01-01' AND '2023-12-31'"
    echo "GROUP BY c.customer_name"
    echo "HAVING SUM(o.total_amount) > 1000"
    echo "ORDER BY total_spent DESC;"
  expect: |
    The output should contain a valid SQL query that:
    1. Joins the customers and orders tables
    2. Filters for orders in the year 2023
    3. Groups by customer name
    4. Shows only customers who spent over $1000
    5. Orders results by total spent in descending order
```

## Timeout and Performance Tests

```yaml
- name: Expect with timeout check
  command: |
    sleep 2
    echo "Operation completed successfully in 2 seconds."
  timeout: 3000
  expect: The output should indicate that the operation completed successfully.
```