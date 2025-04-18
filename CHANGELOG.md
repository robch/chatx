# Changelog

All notable changes to the ChatX project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- GitHub Copilot integration: Fixed token expiration issue by implementing automatic token refresh
  - Tokens are now automatically refreshed before they expire
  - Added CopilotTokenRefreshPolicy that intercepts requests and updates authorization headers
  - Enhanced GitHubCopilotHelper to better track token expiration information

## [1.0.0-alpha-20250402] - 2025-04-02

### Added
- Slash Commands:
  - Added new slash commands (/find, /file, /files, /get, /search) that integrate with MDX functionality
  - These provide convenient shortcuts for common operations like file searching and web content retrieval
- Range Support for --foreach:
  - Added support for numeric ranges in --foreach using the #..# syntax
  - Example: --foreach i in 1..5 will iterate over values 1, 2, 3, 4, 5
- Token Trimming:
  - Added functionality to trim tokens to a target limit
  - Ensures chat history doesn't exceed token limits, particularly important when using --input-chat-history

### Changed
- Behavior Changes:
  - Force non-interactive mode when using --foreach to ensure proper batch processing
- Code Refactoring:
  - Refactored profile methods into ProfileFileHelpers.cs for better organization
  - Refactored alias methods into AliasFileHelpers.cs for better organization

### Documentation
- Updated documentation for slash commands, foreach ranges, and token trimming features
- Added --prerelease flag for dotnet install instructions in README

## [1.0.0-alpha-20250401.2] - 2025-04-01

### Added
- Initial release of ChatX
- Interactive chat functionality with AI assistant
- Function calling capabilities:
  - Shell command execution (Bash, CMD, PowerShell)
  - File operations (list, view, create, edit)
  - Time and date utilities
- Command line options for customizing AI behavior
- Chat history loading and saving
- Command aliases for easy reuse
- Help system for documentation

### Changed
- N/A (initial release)

### Fixed
- N/A (initial release)