# Command Line Options

CycoD provides a variety of command line options to customize its behavior. This document provides a comprehensive overview of all available options.

## Basic Usage

```
cycod [options]
```

## Commands

CycoD supports the following commands:

| Command | Description |
|---------|-------------|
| `chat` | Start a chat session (default) |
| `config` | Manage configuration settings |
| `github login` | Authenticate with GitHub Copilot |
| `alias list` | List all available aliases |
| `alias get <name>` | View the content of a specific alias |
| `alias add <name> <content>` | Add a new raw alias without syntax validation |
| `alias delete <name>` | Delete an alias |
| `version` | Display version information |
| `help` | Display help information |

## Global Options

These options apply to the overall behavior of CycoD:

| Option | Description |
|--------|-------------|
| `--debug` | Enable debug output |
| `--verbose` | Enable verbose output |
| `--quiet` | Suppress non-essential output |
| `--interactive <true/false>` | Control whether to enter interactive mode (default: true) |
| `--no-templates` | Disable template processing in input |
| `--save-alias <n>` | Save the current command options as a named alias in the local scope |
| `--save-local-alias <n>` | Same as `--save-alias`, saves in the local scope |
| `--save-user-alias <n>` | Save the current command options as a named alias in the user scope |
| `--save-global-alias <n>` | Save the current command options as a named alias in the global scope |
| `--help` | Display help information |

## Chat Options

These options control the chat interaction:

| Option | Description |
|--------|-------------|
| `--system-prompt <prompt>` | Set a custom system prompt for the AI |
| `--add-system-prompt <text>` | Add text to the system prompt (can be used multiple times) |
| `--add-user-prompt <text>` | Add a user prompt that gets inserted when chat history is cleared (can be used multiple times) |
| `--input <text>` | Provide a single input/question to the AI |
| `--question <text>` | Alias for `--interactive false --quiet --input` |
| `--instruction <text>` | Alias for `--input` |
| `--inputs <text...>` | Provide multiple inputs/questions to the AI |
| `--questions <text...>` | Alias for `--interactive false --quiet --inputs` |
| `--instructions <text...>` | Alias for `--inputs` |
| `--input-chat-history <file>` | Load previous chat history from a JSONL file |
| `--continue` | Continue the most recent chat history (auto-finds latest chat history file) |
| `--output-chat-history <file>` | Save chat history to a JSONL file |
| `--output-trajectory <file>` | Save chat history in a more readable trajectory format |
| `--max-token-target <n>` | Set a target token count for trimming chat history when it gets too large |
| `--chat-completion-timeout <n>` | Set a timeout in seconds for chat completion API calls |

## Provider Selection Options

These options control which AI provider to use:

| Option | Description |
|--------|-------------|
| `--use-openai` | Use OpenAI API as the chat provider |
| `--use-azure-openai` | Use Azure OpenAI API as the chat provider |
| `--use-azure` | Alias for `--use-azure-openai` |
| `--use-copilot` | Use GitHub Copilot
| `--profile <n>` | Load a named profile from `.cycod/profiles/<n>.yaml` |

## Configuration Options

These options control the configuration system:

| Option | Description |
|--------|-------------|
| `--global`, `-g` | Use global scope (all users) |
| `--user`, `-u` | Use user scope (current user) |
| `--local`, `-l` | Use local scope (current directory) |
| `--any`, `-a` | Include settings from all scopes (for 'list' command) |

## Help Options

These options control the help system:

| Option | Description |
|--------|-------------|
| `--expand` | Show expanded help topics with complete content |

## Examples

### Basic Chat Session

```bash
cycod
```

### Using a Custom System Prompt

```bash
cycod --system-prompt "You are an expert Linux system administrator who gives concise answers."
```

### Adding User Prompts That Persist Through Chat Clearing

```bash
cycod --add-user-prompt "Remember that I prefer code examples with extensive comments."
```

This user prompt will be automatically inserted when starting a new chat or when using the /clear command.

### Asking a Specific Question

```bash
cycod --input "How do I find the largest files in a directory using bash?"
```

### Multiple Questions in Sequence

```bash
cycod --inputs "What is a shell script?" "How do I make a shell script executable?" "How do I add error handling to a shell script?"
```

### Loading and Saving Chat History

```bash
cycod --output-chat-history "programming-help.jsonl"
cycod --input-chat-history "programming-help.jsonl" --input "Can you explain that last example again?"
```

### Saving Conversation in Trajectory Format

```bash
cycod --output-trajectory "conversation.md" --input "What is the most efficient sorting algorithm?"
```

This saves the conversation in a more human-readable format.

### Creating an Alias

There are two ways to create aliases:

#### 1. Using --save-alias (for validated aliases)

```bash
cycod --system-prompt "You are a helpful coding assistant." --save-alias coding-helper
```

This creates an alias with syntax validation - all arguments must be valid at creation time.

#### 2. Using the alias add command (for raw aliases)

```bash
cycod alias add template-alias --content "--system-prompt \"You are a template\" --instruction"
```

This creates a raw alias without syntax validation - perfect for templates or partial commands.

Using the aliases:

```bash
# Using a validated alias
cycod --coding-helper --input "Help me write a Python function to sort a list of dictionaries by a specific key."

# Using a raw alias
cycod --template-alias "Write a function to do X"
```

### Managing Token Usage

If you're having long conversations that might exceed the AI model's context window:

```bash
cycod --max-token-target 120000 --input-chat-history "long-conversation.jsonl" --output-chat-history "long-conversation.jsonl"
```

This will automatically trim tool call content when the history approaches the specified token target.

### Setting Completion Timeout

If you're experiencing timeouts with API calls:

```bash
cycod --chat-completion-timeout 120 --input "Please write a comprehensive analysis of this long text..."
```

This sets a 120-second timeout for chat completion API calls, useful for complex queries that take longer to process.

### Using Different Providers

You can explicitly select which AI provider to use:

```bash
cycod --use-openai --input "Tell me about OpenAI's models"
cycod --use-azure-openai --input "Tell me about Azure OpenAI services"
cycod --use-copilot --input "Tell me about GitHub Copilot"
```

### Using Configuration Profiles

Load a named configuration profile:

```bash
cycod --profile work --input "What's on my schedule today?"
```

### Configuration Management

View and manage configuration settings:

```bash
cycod config list --any           # List all configuration settings from all scopes
cycod config get OPENAI_API_KEY   # Get the value of a specific setting
cycod config set OPENAI_API_KEY value123  # Set a configuration value
```

### GitHub Copilot Authentication

To use GitHub Copilot with CycoD, you need to authenticate:

```bash
cycod github login
```

This will initiate the GitHub device flow authentication process, displaying a code and URL to visit. After authenticating, CycoD will store your GitHub token in the `.cycod/config` file and you'll be able to use Copilot for chat completions.

### Non-Interactive Mode

For scripting or automation:

```bash
cycod --interactive false --input "What is the current date?" > result.txt
```

## Command Line Input from Files

You can read input content from files in several ways:

### Single Input from a File

Use the @ symbol to read a single input from a file:

```bash
cycod --system-prompt @system-prompt.txt --input @question.txt
```

### Multiple Inputs from a File

Use @@ to read multiple inputs from a file (one per line):

```bash
cycod --inputs @@questions-list.txt
```

### Stdin Input

You can also pipe content to CycoD:

```bash
echo "What is the capital of France?" | cycod
```

## Environment Configuration

In addition to command line options, CycoD can be configured through environment variables. See the [Getting Started guide](getting-started.md) for more details on environment variables.

## Advanced Usage

### Combining Aliases with Options

You can combine aliases with additional options:

```bash
cycod --python-expert --input "Explain decorators in Python" --output-chat-history "python-learning.jsonl"
```

This applies all options from the `python-expert` alias and then adds the additional options specified.

### Combining Multiple Aliases

You can use multiple aliases in a single command (especially useful with raw aliases):

```bash
# Combine a system prompt alias with a specific instruction template
cycod --python-expert --code-review-template "def add(a, b): return a + b"
```

### Using Chat Commands

You can use special commands during chat:

```
/clear      Clear the current conversation history
```

### Debugging Problems

If you're having issues with CycoD, you can enable debug output:

```bash
cycod --debug --input "Test question"
```

This will show detailed information about API calls, token usage, and other internals.
