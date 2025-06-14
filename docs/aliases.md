# Creating Aliases

Aliases in CycoD allow you to save frequently used command configurations as shortcuts, making it easier to reuse complex or common configurations without having to type them out each time.

## How Aliases Work

Aliases are stored as plain text files in an `aliases` directory within the CycoD configuration folders. Aliases can be stored in three different scopes:

1. **Local scope** - stored in the `.cycod/aliases` directory of the current working directory
2. **User scope** - stored in the user's CycoD configuration directory (`~/.cycod/aliases` on Unix, `%USERPROFILE%\.cycod\aliases` on Windows)
3. **Global scope** - stored in the system-wide CycoD configuration directory

When looking for an alias, CycoD searches in the local scope first, then the user scope, and finally the global scope.

## Creating an Alias

There are two different ways to create aliases, each with different use cases:

### 1. Using the `--save-alias` Option (Validated Aliases)

To create a validated alias that must be syntactically correct at creation time, use one of the following options followed by a name for your alias:

- `--save-alias` or `--save-local-alias` - Save in the local scope (current directory)
- `--save-user-alias` - Save in the user scope (available to the current user in any directory)
- `--save-global-alias` - Save in the global scope (available to all users)

Example:

```bash
cycod --system-prompt "You are an expert Python programmer who gives concise code examples." --save-alias python-helper
```

This will save all the command line options (except the save-alias option itself) to an alias named `python-helper` in the local scope.

To save an alias in the user scope:

```bash
cycod --system-prompt "You are an expert Python programmer who gives concise code examples." --save-user-alias python-helper
```

### 2. Using the `alias add` Command (Raw Aliases)

To create a raw alias that doesn't require syntax validation at creation time, use the `alias add` command. This is particularly useful for creating template-like aliases that might be missing required parameters:

```bash
cycod alias add code-prompt "--add-system-prompt @/prompts/code-system-prompt.md --instruction"
```

This command adds a raw alias named `code-prompt` that contains arguments that might not be valid on their own (in this case, `--instruction` requires a value). When used, you'll need to provide the missing values.

Raw aliases can be created in different scopes using the scope flags:

```bash
# Create in user scope
cycod alias add python-expert --user "--system-prompt \"You are a Python expert\""

# Create in global scope
cycod alias add linux-admin --global "--system-prompt \"You are a Linux administrator\""
```

## Using an Alias

To use a previously saved alias, simply prefix the alias name with `--`:

```bash
cycod --python-helper --input "Write a function that sorts a list of dictionaries by a given key"
```

This will apply all the options stored in the `python-helper` alias (in this case, setting the system prompt) and then apply any additional options provided in the command.

### Using Raw Aliases (Created with `alias add`)

Raw aliases can be used in the same way as validated aliases, but they're more flexible and can be used at any point in the command line:

```bash
# Use a raw alias with the required value for --instruction
cycod --code-prompt "Write a sorting algorithm"

# Use the alias with other options
cycod --verbose --code-prompt "Write a sorting algorithm"

# Place the alias anywhere in the command
cycod --input "Let's talk about" --code-prompt "sorting algorithms"
```

## Managing Aliases

CycoD provides a set of commands for managing aliases:

### `alias list` - List Available Aliases

Lists all aliases in all scopes or a specific scope:

```bash
# List aliases in all scopes
cycod alias list

# List aliases in a specific scope
cycod alias list --global
cycod alias list --user
cycod alias list --local
```

### `alias get` - View Alias Content

Shows the content of a specific alias:

```bash
# Get an alias from any scope
cycod alias get my-alias

# Get an alias from a specific scope
cycod alias get my-alias --user
```

### `alias add` - Create a Raw Alias

Creates a new alias without syntax validation, useful for template-like aliases:

```bash
# Basic usage
cycod alias add my-alias --content "--system-prompt \"Custom prompt\" --instruction"

# Create in user scope
cycod alias add my-alias --user --content "--system-prompt \"Custom prompt\""

# Create in global scope
cycod alias add my-alias --global --content "--system-prompt \"Custom prompt\""

# Read content from stdin
echo "--system-prompt \"Content from stdin\"" | cycod alias add stdin-alias
```

The `alias add` command is particularly useful for creating aliases that:
- Contain incomplete commands (like missing required arguments)
- Need to be used as templates
- Should be filled in with additional parameters when used

### `alias delete` - Remove an Alias

Deletes an alias from the specified scope:

```bash
# Delete an alias from any scope
cycod alias delete my-alias

# Delete an alias from a specific scope
cycod alias delete my-alias --user
```

## Examples

### Creating a Role-Specific Validated Alias

```bash
cycod --system-prompt "You are an expert Linux system administrator. Provide clear and concise answers to technical questions about Linux systems." --save-alias linux-admin
```

### Creating a Raw Template Alias

```bash
cycod alias add code-review "--system-prompt \"You are a code reviewer. Review this code:\" --input"
```

Then use it with the missing input:

```bash
cycod --code-review "function sum(a, b) { return a + b; }"
```

### Creating a User-Scoped Workflow Alias

```bash
cycod --system-prompt "You are an assistant focused on helping with Git operations and best practices." --save-user-alias git-helper
```

### Creating a Global Alias for All Users

```bash
cycod --system-prompt "You are a technical documentation writer who creates clear and thorough explanations." --save-global-alias tech-writer
```

### Combining Aliases with Additional Options

```bash
# First create the alias
cycod --system-prompt "You are a technical documentation writer who creates clear and thorough explanations." --save-alias tech-writer

# Later, use the alias with additional options
cycod --tech-writer --input "Write documentation for a REST API endpoint that creates user accounts" --output-chat-history "api-docs.jsonl"
```

## Technical Details

- Alias files are stored in `aliases/` within the appropriate configuration directory for the selected scope
- Each alias is stored in a file named `<alias-name>.alias`
- Multi-line inputs in aliases are handled properly
- Aliases cannot be nested (an alias cannot reference another alias)
- Raw aliases (created with `alias add`) have a `#TYPE=raw` marker at the beginning of the file
- Validated aliases (created with `--save-alias`) undergo strict syntax checking at creation time