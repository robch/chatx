# ChatX - AI-powered CLI

ChatX is a command-line interface (CLI) application that provides a chat-based interaction with AI assistants. Built in C#, it leverages AI chat models from multiple providers with function calling capabilities to create a powerful tool for AI-assisted command-line operations.

## Features

- **Interactive AI Chat**: Have conversations with an AI assistant directly in your terminal
- **Multiple AI Providers**: Support for OpenAI, Azure OpenAI, and GitHub Copilot APIs
- **Function Calling**: Allow the AI assistant to execute various operations:
  - Run shell commands (Bash, CMD, PowerShell) with persistent sessions
  - Manipulate files (view, create, edit, replace text)
  - Access date and time information
- **Customizable Experience**: Configure the AI's behavior with system prompts and other options
- **Chat History**: Load and save chat histories for later reference
- **Command Aliases**: Create shortcuts for frequently used command configurations
- **Token Management**: Automatically manages token usage for long conversations

## Installation

### Prerequisites

- .NET 8.0 SDK or later

### Building from Source

1. Clone this repository:
   ```
   git clone https://github.com/robch/chatx.git
   cd chatx
   ```

2. Build the project:
   ```
   dotnet build
   ```

3. Run the application:
   ```
   dotnet run
   ```

## Usage

Basic usage:

```
chatx [options]
```

### Common Options

- `--system-prompt <prompt>`: Set a custom system prompt for the AI
- `--input <text>` or `--question <text>`: Provide input or questions to the AI
- `--inputs <text...>` or `--questions <text...>`: Provide multiple inputs to process sequentially
- `--input-chat-history <file>`: Load previous chat history from a file
- `--output-chat-history <file>`: Save chat history to a file
- `--trim-token-target <n>`: Set a target for trimming chat history when it gets too large
- `--interactive`: Control whether to enter interactive mode (default: true)
- `--save-alias <name>`: Save the current command options as a named alias
- `--help`: Display help information

### Examples

Start a chat session:
```
chatx
```

Ask a specific question:
```
chatx --input "How do I find the largest files in a directory using bash?"
```

Use a custom system prompt:
```
chatx --system-prompt "You are an expert Linux system administrator who gives concise answers."
```

Save and load chat history:
```
chatx --output-chat-history "linux-help-session.jsonl"
chatx --input-chat-history "linux-help-session.jsonl"
```

## Environment Variables

The application uses the following environment variables:

### OpenAI API
- `OPENAI_API_KEY`: Your OpenAI API key
- `OPENAI_CHAT_MODEL_NAME`: Model name to use (default: gpt-4o)
- `OPENAI_SYSTEM_PROMPT`: Default system prompt if not specified

### Azure OpenAI API
- `AZURE_OPENAI_API_KEY`: Your Azure OpenAI API key
- `AZURE_OPENAI_ENDPOINT`: Your Azure OpenAI endpoint
- `AZURE_OPENAI_CHAT_DEPLOYMENT`: Your Azure OpenAI deployment name

### GitHub Copilot API
- `GITHUB_TOKEN`: Your GitHub personal access token for Copilot API (preferred method)
- `COPILOT_API_ENDPOINT`: Copilot API endpoint (default: https://api.githubcopilot.com)
- `COPILOT_MODEL_NAME`: Model name to use (default: claude-3.7-sonnet)
- `COPILOT_INTEGRATION_ID`: Your Copilot integration ID (required for both auth methods)

*Alternative HMAC authentication:*
- `COPILOT_HMAC_KEY`: Your Copilot HMAC key

### Configuration Files

Environment variables can also be set in a `.chatx/config` file in your home or project directory, with each variable on a separate line in `NAME=VALUE` format.

## Documentation

For more detailed documentation, see:
- [Getting Started](docs/getting-started.md)
- [Command Line Options](docs/cli-options.md)
- [Function Calling](docs/function-calling.md)
- [Creating Aliases](docs/aliases.md)
- [Chat History](docs/chat-history.md)

## License

Copyright(c) 2025, Rob Chambers. All rights reserved.